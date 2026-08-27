using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>THE one place that knows how a quantity turns into ROWS.
///
/// <para>Every container in the game — bag, private warehouse, account warehouse, trade, drops, craft
/// output, quest rewards, buy-back and the death-restore list — asks these questions and no site does
/// the arithmetic itself. That is the point of the file: a cap one container computes slightly
/// differently is not a cap, it is a laundering route around one. The caps themselves are not here —
/// they are <see cref="StackLimits"/> in Game.Shared, so the client can quote the same number.</para>
///
/// <para>🔑 FILL-THEN-SPILL, AND NOTHING IS EVER DESTROYED. New quantity tops up the partial rows that
/// already exist before it opens a new one, and the (cap+1)-th item starts a fresh row (owner, 0.93.0:
/// *"the 10,100,1000 etc item to make new stack"*). A container refuses only when it has run out of
/// ROWS — the same refusal it has always had, with the same message.</para>
///
/// <para>⚠ PLACEMENT IS ATOMIC AT THE CALL SITE, BY CONVENTION: callers ask <see cref="RowsNeeded"/>
/// first and bail with their existing "full" message if the rows are not there, so a purchase or a
/// deposit never half-completes and leaves the player short of gold holding part of an order. The one
/// deliberate exception is the shop, which clamps a purchase to a single stack instead
/// (<c>HandleBuy</c>) — his rule, and it deletes the question rather than answering it.</para></summary>
internal static class Stacking
{
    /// <summary>May these two rows share one stack? 🔑 IDENTITY, NOT JUST DefId — this is the rule that
    /// makes "boxes stack" safe.
    ///
    /// <para>A row can carry state the def does not: a half-picked selection box has
    /// <c>PicksRemaining</c>, a granted item can be bound or renamed or timed, gear has an enchant.
    /// Merging on DefId alone would let a fresh Blessing Box absorb a box with 4 picks left — losing
    /// six scrolls — or hand two acquisitions one expiry. Two rows merge only when swapping them would
    /// be undetectable.</para>
    ///
    /// <para>⚠ This is STRICTER than the old merge test, which compared DefId and nothing else. That
    /// was already wrong for bound and renamed instances; it only never bit because boxes could not
    /// stack at all before 0.93.0.</para></summary>
    public static bool SameStack(InventoryItem a, InventoryItem b) =>
        a.DefId == b.DefId
        && a.Enchant == b.Enchant
        && a.ExpiresAtUtc is null && b.ExpiresAtUtc is null
        && a.PicksRemaining == b.PicksRemaining
        && a.SellPriceOverride == b.SellPriceOverride
        && a.TradableOverride == b.TradableOverride
        && a.CustomName == b.CustomName
        && a.CanStorePrivate == b.CanStorePrivate
        && a.CanStoreAccount == b.CanStoreAccount
        // Rolled attributes are GEAR-only and gear never stacks, so for anything that reaches here a
        // non-empty list is the def's own FixedAttributes and therefore identical on both sides.
        && a.Attributes.Count == b.Attributes.Count;

    /// <summary>A bare row of this def — what a plain quantity (a drop, a purchase, a quest reward)
    /// merges into. Anything carrying instance state is deliberately NOT matched, so a purchase can
    /// never top up the bound copy someone was handed.</summary>
    private static bool IsPlainRow(InventoryItem it, ItemDef def) =>
        it.DefId == def.Id
        && it.Enchant == 0 && it.ExpiresAtUtc is null && it.PicksRemaining is null
        && it.SellPriceOverride is null && it.TradableOverride is null && it.CustomName is null
        && it.CanStorePrivate is null && it.CanStoreAccount is null;

    /// <summary>Room left in the PLAIN rows a container already has for this def, before any new row
    /// is opened. A non-stackable def always answers 0 — its rows are full at one item each.</summary>
    public static long RoomInExistingRows(IEnumerable<InventoryItem> container, ItemDef def)
    {
        if (!def.IsStackable) return 0;
        int cap = def.MaxStack;
        long room = 0;
        foreach (var it in container)
            if (IsPlainRow(it, def) && it.Quantity < cap)
                room += cap - it.Quantity;
        return room;
    }

    /// <summary>How many NEW rows placing <paramref name="quantity"/> would open, after topping up
    /// every partial row already there. This is the number a caller checks against its free slots
    /// before it commits to anything.</summary>
    public static int RowsNeeded(IEnumerable<InventoryItem> container, ItemDef def, int quantity)
    {
        if (quantity <= 0) return 0;
        if (!def.IsStackable) return quantity;          // one row each, by definition
        long remaining = quantity - RoomInExistingRows(container, def);
        if (remaining <= 0) return 0;
        int cap = def.MaxStack;
        return (int)((remaining + cap - 1) / cap);      // ceiling division
    }

    /// <summary>The most of this def the container could still accept, given how many rows it has
    /// free. What the shop sizes a purchase against.</summary>
    public static long Capacity(IEnumerable<InventoryItem> container, ItemDef def, int freeRows)
    {
        if (freeRows < 0) freeRows = 0;
        if (!def.IsStackable) return freeRows;
        return RoomInExistingRows(container, def) + (long)freeRows * def.MaxStack;
    }

    /// <summary>Fill-then-spill <paramref name="quantity"/> into <paramref name="container"/>.
    ///
    /// <para><paramref name="makeRow"/> builds a fresh row, because a row is not always a bare def: a
    /// buy-back row carries its enchant and attributes, a granted row its expiry. The DefId and the
    /// quantity are set here, so the factory need not care how the split lands. ⚠ A factory that
    /// stamps instance state must not be paired with a container that already holds plain rows of the
    /// same def — <see cref="IsPlainRow"/> keeps the top-up half honest, but the caller owns the
    /// intent.</para>
    ///
    /// <para>Returns how many were actually placed. A caller that pre-checked with
    /// <see cref="RowsNeeded"/> always gets all of them; the return value exists so the few paths that
    /// cannot pre-check can report honestly instead of claiming a full delivery.</para></summary>
    public static int Place(List<InventoryItem> container, ItemDef def, int quantity,
                            int freeRows, Func<InventoryItem> makeRow)
    {
        if (quantity <= 0) return 0;
        int placed = 0;
        int cap = def.MaxStack;

        if (def.IsStackable)
        {
            // Top up what is already there, oldest row first, so a bag settles into full stacks
            // instead of a spread of half-empty ones.
            foreach (var it in container)
            {
                if (placed >= quantity) break;
                if (!IsPlainRow(it, def) || it.Quantity >= cap) continue;
                int take = Math.Min(cap - it.Quantity, quantity - placed);
                it.Quantity += take;
                placed += take;
            }
        }

        while (placed < quantity && freeRows > 0)
        {
            int take = def.IsStackable ? Math.Min(cap, quantity - placed) : 1;
            var row = makeRow();
            row.Quantity = take;
            container.Add(row);
            placed += take;
            freeRows--;
        }

        return placed;
    }

    /// <summary>Split every row that sits over its cap into legal ones, in place. A login runs this so
    /// a character saved before the caps existed — or before one was retuned downwards — is migrated
    /// rather than left holding a row the rules say is impossible.
    ///
    /// <para>⚠ It only ever SPLITS; nothing is destroyed and no quantity changes. If the container has
    /// no room for the extra rows, the leftover stays as ONE oversized row rather than being thrown
    /// away — an over-cap stack is a cosmetic wrong, and deleting someone's 4,000 materials to fix it
    /// would not be. It shrinks on its own as they spend it.</para></summary>
    public static void Normalize(List<InventoryItem> container, int maxRows)
    {
        for (int i = 0; i < container.Count; i++)
        {
            var row = container[i];
            if (ItemCatalog.Get(row.DefId) is not ItemDef def) continue;
            int cap = def.MaxStack;
            if (row.Quantity <= cap) continue;

            while (row.Quantity > cap && container.Count < maxRows)
            {
                int spill = Math.Min(cap, row.Quantity - cap);
                row.Quantity -= spill;
                container.Add(new InventoryItem
                {
                    DefId = row.DefId, Quantity = spill,
                    Enchant = row.Enchant, PicksRemaining = row.PicksRemaining,
                    SellPriceOverride = row.SellPriceOverride,
                    TradableOverride = row.TradableOverride,
                    CustomName = row.CustomName,
                    CanStorePrivate = row.CanStorePrivate,
                    CanStoreAccount = row.CanStoreAccount,
                    Attributes = new List<ItemAttribute>(row.Attributes),
                });
            }
        }
    }

    /// <summary>Move an EXISTING row into another container, keeping its instance state and splitting
    /// it across the cap if it has to. The warehouse pair needs this: the row being deposited is a real
    /// object with an InstanceId, and it may not fit in one row on the far side.
    ///
    /// <para>The fast path hands the OBJECT across whenever the whole row fits in one new row, so an
    /// InstanceId, an enchant and rolled attributes survive a round trip untouched — which is what the
    /// warehouse has always done and what gear depends on. Only an oversized or partially-merging move
    /// rebuilds rows.</para>
    ///
    /// <para>Returns the amount moved; the source row is mutated down by it and removed at zero.</para></summary>
    public static int Move(List<InventoryItem> from, List<InventoryItem> to,
                           InventoryItem row, ItemDef def, int freeRows)
    {
        int quantity = row.Quantity;
        if (quantity <= 0) return 0;

        bool mergesInto = def.IsStackable && RoomInExistingRows(to, def) > 0 && IsPlainRow(row, def);
        if (!mergesInto && quantity <= def.MaxStack && freeRows > 0)
        {
            from.Remove(row);
            to.Add(row);
            return quantity;
        }

        int moved = Place(to, def, quantity, freeRows, () => new InventoryItem
        {
            DefId = row.DefId,
            Enchant = row.Enchant,
            SellPriceOverride = row.SellPriceOverride,
            TradableOverride = row.TradableOverride,
            CustomName = row.CustomName,
            CanStorePrivate = row.CanStorePrivate,
            CanStoreAccount = row.CanStoreAccount,
            PicksRemaining = row.PicksRemaining,
            ExpiresAtUtc = row.ExpiresAtUtc,
            Attributes = new List<ItemAttribute>(row.Attributes),
        });

        row.Quantity -= moved;
        if (row.Quantity <= 0) from.Remove(row);
        return moved;
    }
}
