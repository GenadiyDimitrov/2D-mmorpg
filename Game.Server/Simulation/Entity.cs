using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>A timed stat modifier (buff or debuff) on an entity. Carries a
/// flags Effect (one buff can touch several stats) and a per-effect magnitude
/// array with flat/percent modes. Identified by Key; same-Key buffs compare by
/// Rank; a buff also unconditionally removes any active buff in Replaces.</summary>
public class BuffInstance
{
    // Settable: a stacking effect re-snapshots these to the current stack LEVEL each stack.
    public required SkillEffect Effect { get; set; }
    public required EffectMagnitude[] Magnitudes { get; set; }
    public int TicksRemaining { get; set; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";

    public string Key { get; init; } = "";
    public int Rank { get; init; }

    /// <summary>For an IMPROVED (group) buff: the family keys it CONTAINS. The group is one buff at
    /// one rank, but it competes for every family in here — it evicts those singles when it lands and
    /// outranks any that are cast, drunk or read afterwards. Empty for an ordinary single buff, which
    /// competes on <see cref="Key"/> alone. See docs/design/BuffLadders.md.</summary>
    public string[] CoveredKeys { get; init; } = Array.Empty<string>();

    /// <summary>Every family this buff occupies — its own, plus anything it covers.</summary>
    public IEnumerable<string> Families
    {
        get
        {
            yield return Key;
            foreach (var k in CoveredKeys) yield return k;
        }
    }

    /// <summary>The SKILL LEVEL this was applied at. Rank is the stacking-priority number, which is
    /// not the same thing — this is the argument ApplyBuff was called with, kept so a buff can be
    /// rebuilt exactly (magnitudes, DoT power and shield pool are all level-derived) when restoring
    /// a character's buffs on login.</summary>
    public int Level { get; init; } = 1;
    public string[] Replaces { get; init; } = Array.Empty<string>();

    /// <summary>The server tick this buff was (re)applied on — how "oldest" is decided when the
    /// <see cref="GameConstants.MaxBuffSlots"/> cap has to evict one. Settable rather than init-only
    /// because re-applying a buff makes it NEW: a blessing you just recast should not be first out
    /// of the door simply because an older copy of it once sat in that list slot. Not persisted —
    /// on login every restored buff is stamped with the same tick, which is honest (they all arrived
    /// at once) and only matters if a returning player is already at 24.</summary>
    public long AppliedAtTick { get; set; }

    /// <summary>The skill whose icon the buff bar shows, and the id buffs are GROUPED by: for a child
    /// of an improved (group) buff this is the PARENT's id, so the client can collapse the whole
    /// blessing into one square (docs/design/BuffLadders.md). "" for non-skill buffs (e.g. the
    /// synthetic grade-penalty rows, which supply their own icon).</summary>
    public string SourceSkillId { get; init; } = "";

    /// <summary>The skill that actually CREATED this buff — the same as <see cref="SourceSkillId"/>
    /// except for a group buff's child, where the source is the parent. This is the one that can
    /// rebuild the buff, so it is what persistence saves: saving the parent instead would re-apply
    /// every sibling at full duration on the next login (a free refresh for anyone who relogs).</summary>
    public string SkillId { get; init; } = "";

    /// <summary>A TOGGLE/stance buff: never expires on its own (the player clicks the
    /// skill again, or double-clicks the buff, to end it). TickBuffs skips it.</summary>
    public bool Toggle { get; init; }

    /// <summary>This buff hides its owner from UNAGGROED monsters (BL-69, kind 2). Carried on the
    /// buff rather than on the entity so that every way a buff can leave — toggled off,
    /// double-clicked, dispelled, expired, lost on death — ends the stealth without a second
    /// bookkeeping path to keep in step.</summary>
    public bool HidesFromMobs { get; init; }

    /// <summary>Which buff-bar ROW this belongs in (from the granting skill). A debuff overrides
    /// it — see <see cref="Row"/>.</summary>
    public BuffRow SourceRow { get; init; } = BuffRow.Buff;

    /// <summary>The row the client should render this in. Harmful effects always go to the debuff
    /// row no matter what the skill declared, so an offensive skill never has to set it.</summary>
    public BuffRow Row => IsDebuff ? BuffRow.Debuff : SourceRow;

    // ----- Damage-over-time (DoT) stacks -----
    /// <summary>Current stack count (1..MaxStacks). Magnitudes scale with this.</summary>
    public int Stacks { get; set; } = 1;
    /// <summary>Maximum stacks this effect can build (1 = non-stacking).</summary>
    public int MaxStacks { get; set; } = 1;
    /// <summary>DoT damage per stack per second (0 = not a DoT).</summary>
    public int DotPower { get; set; }
    /// <summary>Entity that applied this effect (for DoT damage attribution / kill credit).</summary>
    public Guid SourceId { get; set; }
    /// <summary>An internal mechanic effect (e.g. a DoT stack counter): not shown on the buff
    /// bar and not touched by cure/cancel — only its own burst skill consumes it.</summary>
    public bool Internal { get; set; }
    /// <summary>Can this effect be removed by cure/cancel? (false = immune.)</summary>
    public bool Cancellable { get; set; } = true;
    /// <summary>Remaining absorb pool for a Shield effect (damage soaked before HP). The buff
    /// is removed when it hits 0.</summary>
    public int ShieldPool { get; set; }
    /// <summary>MP-cost reduction this buff grants for PHYSICAL / magic-side skills (fractions).</summary>
    public float PhysMpCostPct { get; init; }
    public float MagicMpCostPct { get; init; }

    /// <summary>BL-06 — chance the owner dodges an incoming PHYSICAL SKILL while this buff is up.
    /// Rides as a field, like the two above, because the SkillEffect flag enum has no bits left.
    /// The rogue's Evasion Boost is the only skill in the game that sets it.</summary>
    public float SkillEvadeChance { get; init; }

    /// <summary>Per-school control resistance while this buff is up — the healer's Clarity (magical,
    /// the SPT-defended school) and Fortitude (physical, the CON-defended one). Rides as fields, like
    /// the one above, because the SkillEffect flag enum has no bits left.</summary>
    public float CcResistMagical { get; init; }
    public float CcResistPhysical { get; init; }

    /// <summary>Magic crit DAMAGE this buff grants its holder, as a fraction of the ×2 base
    /// (0.30 = +30% → ×2.6). Rides as fields, like the ones above, because the SkillEffect flag
    /// enum has no bits left. <see cref="MagicCritDamageDebuff"/> is the `(1 − debuffs)` side.</summary>
    public float MagicCritDamage { get; init; }
    public float MagicCritDamageDebuff { get; init; }

    /// <summary>This buff ends the instant its owner TAKES damage (the healer's Meditation). Carried
    /// on the buff, like <see cref="HidesFromMobs"/>, so it is checked at the one place damage is
    /// applied and needs no second bookkeeping path. See <c>SkillDef.EndsOnDamageTaken</c>.</summary>
    public bool EndsOnDamageTaken { get; init; }

    /// <summary>What this buff does to a monster's payout — the premium reward runes' whole payload
    /// (see <see cref="RewardRates"/>). Default = neutral, which every other buff in the game is.
    /// Rides as a field, like the two above, because the SkillEffect flag enum has no bits left.</summary>
    public RewardRates Rewards { get; init; }

    /// <summary>Angel's Protection / noblesse marker: while a buff with this set is present, DEATH removes
    /// only the protection buff(s) and keeps every other buff (see Kill). No stat effect of its own.</summary>
    public bool KeepsBuffsOnDeath { get; init; }
    /// <summary>A preservation buff that ALSO auto-revives the owner on death (30% HP/MP, no prompt).
    /// The tank self-res and the healer target-auto-res (`BL-35`) set it; Angel's Protection does not.</summary>
    public bool AutoResurrect { get; init; }

    /// <summary>How much of the exp lost to the death penalty an <see cref="AutoResurrect"/> gives back
    /// (0..1), taken from the granting skill's <c>ResExpPct</c> (`BL-35`: his Lightbringer skill is
    /// *"100% exp return"*).
    ///
    /// <para>It has to ride on the BUFF rather than be read at death time, because by then the thing
    /// that knows the number is gone — the caster may be across the map, offline, or a different class
    /// since. The buff is the only surviving record of what was promised when it was cast.</para></summary>
    public float AutoResExpPct { get; init; }

    public bool Has(SkillEffect flag) => (Effect & flag) != 0;

    public bool IsDebuff => (Effect & SkillEffect.AnyDebuff) != 0;

    /// <summary>Sum of this buff's flat entries for an effect. For a stacking effect the
    /// Magnitudes are re-snapshotted to the current stack LEVEL on each stack (see
    /// ApplyBuff + SkillDef.StackLevels), so no per-read scaling is needed here.</summary>
    public float Flat(SkillEffect flag)
    {
        float sum = 0f;
        foreach (var m in Magnitudes)
            if (m.Effect == flag && m.Mode == ModifierMode.Flat) sum += m.Value;
        return sum;
    }

    /// <summary>Sum of this buff's percent entries for an effect.</summary>
    public float Percent(SkillEffect flag)
    {
        float sum = 0f;
        foreach (var m in Magnitudes)
            if (m.Effect == flag && m.Mode == ModifierMode.Percent) sum += m.Value;
        return sum;
    }
}

/// <summary>One item instance in a player's inventory.</summary>
/// <summary>A recently-sold item held for buy-back — enough to restore it exactly (enchant + rolled
/// attributes) and to charge the same price it was sold for.</summary>
public class BuyBackEntry
{
    public required string DefId { get; init; }
    public int Quantity { get; init; } = 1;
    public int Enchant { get; init; }
    public List<Game.Shared.ItemAttribute> Attributes { get; init; } = new();
    /// <summary>Gold paid per unit when it was sold — the buy-back charges the same, so it's a clean undo.</summary>
    public long UnitPrice { get; init; }
}

public class InventoryItem
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public required string DefId { get; init; }
    public bool Equipped { get; set; }
    public int Enchant { get; set; }

    /// <summary>Stack size for consumables/scrolls. Gear is always 1.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Rolled bonus attributes (gear only).</summary>
    public List<ItemAttribute> Attributes { get; set; } = new();

    /// <summary>DB instance id, preserved across saves (null = never persisted).</summary>
    public Guid? PersistentInstanceId { get; set; }

    /// <summary>Wall-clock expiry for a TIMED item (a war/spell rune). Set when its box is opened; the item is
    /// deleted once <c>DateTime.UtcNow &gt;= ExpiresAtUtc</c>. Wall-clock, so it counts down even offline.
    /// null = never expires (everything that isn't a rune).</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    // ===== PER-INSTANCE OVERRIDES (owner, playtest-20 `58d`) ==========================================
    // His rule: *"it is a REAL item with tags — never a new server-side def."* He wants to hand someone a
    // genuine Soulcrystal that happens to be timed and bound, without inventing `soulcrystal_bound`. The
    // 0.54.0 newbie kit was built as cloned `_bound` defs; he accepts that clone but not as the mechanism.
    //
    // Three INDEPENDENT properties, each null = "use the def". The displayed tag is DERIVED from them
    // rather than stored, so there is one truth: sellable+tradable = no tag, neither = `bound`, sellable
    // but not tradable = `private`. A timer composes on top → `(temporary, bound)`.

    /// <summary>Gold this instance sells for, overriding the def. <c>-1</c> = unsellable, any positive
    /// number = that price, <b>null = use the def</b>. His command spells "default" as `0`, which the
    /// parser turns into null here — 0 is not stored, because a stored 0 would mean "worth nothing",
    /// which is a different statement from "no opinion". Independent of <see cref="TradableOverride"/>:
    /// the two together spell the tag.</summary>
    public long? SellPriceOverride { get; set; }

    /// <summary>May THIS instance be traded / warehoused / mailed, overriding the def. null = the def's
    /// own rule. False is what makes an item bound to the character it was given to.</summary>
    public bool? TradableOverride { get; set; }

    /// <summary>A name written for this instance (max <see cref="GameConstants.CustomItemNameMax"/>),
    /// or null to keep the def's. It renames only this copy — the catalog is untouched.</summary>
    public string? CustomName { get; set; }

    /// <summary>Picks still owed by THIS selection box, or null to use the box def's full
    /// <see cref="Game.Shared.BoxDef.PickCount"/> (`BL-20`).
    ///
    /// <para>His ask: *"I'll want to be able to pick 5 and I get my 5 scrolls + the box for the other
    /// 5."* Before this, a partial pick was simply refused — the box demanded all ten in one sitting,
    /// which is the over-correction that came out of playtest-19 `48g` (where a partial pick CONSUMED
    /// the box and forfeited the rest). Neither end of that is what he wants.</para>
    ///
    /// <para>🔑 It is a counter on the INSTANCE, not a family of `box_scrolls_5`/`_3`/`_2` defs, and not
    /// a new item handed back: the box row simply stays in the bag with a smaller number on it. That
    /// keeps ONE box id for one box, keeps the InstanceId stable (so a chooser left open over the split
    /// still refers to the same thing), and needs no free inventory slot at the moment of the split —
    /// there is nothing to hand back, because nothing was taken away.</para>
    ///
    /// <para>⚠ Only meaningful while the def's PickCount &gt; 0, and only sound because a Box is NOT
    /// <see cref="Game.Shared.ItemDef.IsStackable"/> — one row is one box, so one counter cannot be
    /// shared by several copies. <c>HandleSelectBoxItems</c> guards the stacked case anyway.</para></summary>
    public int? PicksRemaining { get; set; }

    /// <summary>May this instance be put in the character's PRIVATE warehouse? null = ask the DEF, whose
    /// answer is yes unless it is SoulBound (the private bank is otherwise just a bigger bag).</summary>
    public bool? CanStorePrivate { get; set; }

    /// <summary>May this instance be put in the ACCOUNT warehouse? null = follow the standing rule, which
    /// is TRADABLE-ONLY — that bank is a door between your characters, so an item bound to the character
    /// that earned it may not walk through it.
    ///
    /// <para>⚠ Deliberately SEPARATE from <see cref="CanStorePrivate"/> (owner, 2026-08-12) rather than one
    /// "storable" flag: the two banks answer different questions, and the Rune of Sinners has to be barred
    /// from BOTH while an ordinary bound item is barred only from the account one.</para></summary>
    public bool? CanStoreAccount { get; set; }

    /// <summary>May this instance go into the private warehouse? (Delegates to <see cref="ItemTag"/>,
    /// like every other instance rule, so the client's card cannot disagree with the keeper.)</summary>
    public bool StorablePrivate(ItemDef def) => ItemTag.StorablePrivate(def, CanStorePrivate);

    /// <summary>May this instance go into the account warehouse? Falls back to the tradable rule.</summary>
    public bool StorableAccount(ItemDef def) =>
        ItemTag.StorableAccount(def, CanStoreAccount, TradableOverride);

    /// <summary>What a vendor pays for THIS instance. Mirrors <see cref="ItemCatalog.SellPrice"/> unless
    /// this copy was given its own price; <c>-1</c> survives as a negative so it reads as unsellable.</summary>
    /// <summary>What a vendor pays for THIS instance. Delegates to <see cref="ItemTag"/> so the server
    /// and the client card can never drift apart on the rule.</summary>
    public long SellPrice(ItemDef def) => ItemTag.SellPrice(def, SellPriceOverride);

    /// <summary>May this instance leave the character (trade / warehouse / mail)?</summary>
    public bool Tradable(ItemDef def) => ItemTag.Tradable(def, TradableOverride);

    /// <summary>May a vendor buy this instance?</summary>
    public bool Sellable(ItemDef def) => ItemTag.Sellable(def, SellPriceOverride, TradableOverride);

    /// <summary>What this instance is called. The def's name unless one was written for this copy.</summary>
    public string Name(ItemDef def) => string.IsNullOrEmpty(CustomName) ? def.Name : CustomName!;

    public InventoryItemDto ToDto() =>
        new(InstanceId, DefId, Equipped, Enchant, Quantity, Attributes.ToArray(), ExpiresAtUtc,
            SellPriceOverride, TradableOverride, CustomName, CanStorePrivate, CanStoreAccount);
}

/// <summary>
/// Live server-side state of one thing in the world.
/// Mutated exclusively by the game-loop thread.
/// </summary>
public class Entity
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required EntityKind Kind { get; init; }

    // RACE is now PER-CLASS (owner, 2026-07-15): a subclass can be a different race, so Race proxies
    // into the active subclass like Level/BaseClass/etc. Setting it (character creation, debug reset)
    // writes the active subclass's race.
    public Race Race
    {
        get => ActiveSubclass.Race;
        set => ActiveSubclass.Race = value;
    }

    // ---- SUBCLASSES ------------------------------------------------------------------------
    //
    // A character owns SEVERAL classes and plays one at a time. Everything CLASS-level (level, XP,
    // skill points, base/2nd/3rd class, the four core stats, learned skills, skill bar) lives on the
    // Subclass; everything CHARACTER-level (race, inventory, gold, karma, quests, profession,
    // auto-hunt, position) stays on the Entity. See Subclass.cs for the full split.
    //
    // The properties below PROXY into the active subclass, so every existing line of game logic that
    // says player.Level / player.BaseClass / player.LearnedSkills keeps working untouched, and a class
    // swap is just moving ActiveSubclassIndex. Mobs get a single implicit subclass and never notice.

    /// <summary>Every class this character owns. Never empty — slot 0 is the class they were created
    /// as and cannot be removed.</summary>
    public List<Subclass> Subclasses { get; } = new() { new Subclass { Slot = 0 } };

    /// <summary>Index into <see cref="Subclasses"/> of the class currently being played.</summary>
    public int ActiveSubclassIndex { get; private set; }

    /// <summary>The class currently being played. Never null.</summary>
    public Subclass ActiveSubclass => Subclasses[Math.Clamp(ActiveSubclassIndex, 0, Subclasses.Count - 1)];

    /// <summary>Switch to another owned class. The caller must RecomputeDerived and re-push state —
    /// every derived stat, and the whole skill list, changes underneath the player.</summary>
    public bool SwitchSubclass(int slot)
    {
        int i = Subclasses.FindIndex(s => s.Slot == slot);
        if (i < 0 || i == ActiveSubclassIndex) return false;
        ActiveSubclassIndex = i;
        return true;
    }

    public BaseClass BaseClass
    {
        get => ActiveSubclass.BaseClass;
        set => ActiveSubclass.BaseClass = value;
    }

    // ---- CLASS UNIQUENESS ------------------------------------------------------------------
    //
    // A character may not own the same DISCIPLINE twice (owner, 2026-07-15). You learn each 3rd-class
    // path once — no two Tempests, no two Vanguards. ARCHETYPE (the 2nd class) is NOT restricted: you
    // may own several Nukers, so long as they branch into DIFFERENT disciplines (Tempest + the other
    // nuker discipline). That is exactly the "own 4 mages — 2 clerics + 2 nukers" case the owner wants.
    //
    // Matched on the DISCIPLINE, not the class id — checking ids would let the same discipline in
    // through a differently-named door (a human vs an elf version of the same path).

    /// <summary>Disciplines (3rd classes) already held by a class OTHER than the active one.</summary>
    public IEnumerable<Discipline> DisciplinesTakenElsewhere =>
        Subclasses.Where(s => s.Slot != ActiveSubclass.Slot && s.ThirdClass > 0)
                  .Select(s => ThirdClassCatalog.Get(s.ThirdClass)?.Discipline)
                  .Where(d => d is not null)
                  .Select(d => d!.Value);

    /// <summary>Every discipline this character owns across ALL its classes (the active one included).
    /// Used when ADDING a new subclass, where the active class must count too.</summary>
    public IEnumerable<Discipline> DisciplinesOwned =>
        Subclasses.Where(s => s.ThirdClass > 0)
                  .Select(s => ThirdClassCatalog.Get(s.ThirdClass)?.Discipline)
                  .Where(d => d is not null)
                  .Select(d => d!.Value);

    /// <summary>Can this character ADD a subclass of the given 3rd class? False if ANY of its classes
    /// (active included) already walks that discipline.</summary>
    public bool CanAddDiscipline(int thirdClassId) =>
        ThirdClassCatalog.Get(thirdClassId) is not { } def
        || !DisciplinesOwned.Contains(def.Discipline);

    /// <summary>Can the class currently being played take this 3rd class? False if one of your OTHER
    /// classes already walks that discipline. (There is deliberately no 2nd-class/archetype limit.)</summary>
    public bool CanTakeThirdClass(int thirdClassId) =>
        ThirdClassCatalog.Get(thirdClassId) is not { } def
        || !DisciplinesTakenElsewhere.Contains(def.Discipline);

    /// <summary>DB character id (null for mobs / unsaved).</summary>
    public int? PersistentId { get; set; }

    /// <summary>DB account id (0 for mobs / unsaved). The key into the shared ACCOUNT warehouse —
    /// the one piece of state that is not this character's.</summary>
    public int AccountId { get; set; }

    /// <summary>Unspent skill points (earned with exp, spent to learn skills). PER CLASS.</summary>
    public int SkillPoints
    {
        get => ActiveSubclass.SkillPoints;
        set => ActiveSubclass.SkillPoints = value;
    }

    /// <summary>Learned skills → the current LEVEL of each (1 for single-level skills). PER CLASS.
    /// A skill is "known" iff it's a key here; its level selects the SkillDef.*At(level)
    /// values (Power/Magnitudes/Passive/MpCost).</summary>
    public Dictionary<string, int> LearnedSkills => ActiveSubclass.LearnedSkills;

    /// <summary>The learned level of a skill, or 0 if not known.</summary>
    public int SkillLevelOf(string id) => LearnedSkills.GetValueOrDefault(id);

    /// <summary>True if the character knows the skill at any level.</summary>
    public bool HasSkill(string id) => LearnedSkills.ContainsKey(id);

    /// <summary>The active class's skill-bar layout ("" = an empty slot). PER CLASS — swap away, swap
    /// back, and the bar is exactly as you left it. The server does not USE the bar (casting is driven
    /// by skill id, not slot); it only owns and persists it, because the bar is character data and must
    /// follow the account to any machine.</summary>
    public string[] ActiveSkillBar
    {
        get => ActiveSubclass.SkillBar;
        set => ActiveSubclass.SkillBar = value ?? Array.Empty<string>();
    }

    /// <summary>Active quests -> progress (step index + counter).</summary>
    public Dictionary<string, CharacterQuestState> ActiveQuests { get; } = new();

    /// <summary>Completed quest ids.</summary>
    public HashSet<string> CompletedQuests { get; } = new();

    /// <summary>Recipe ids the character has learned from a DROP (the DropOnly recipes,
    /// e.g. the A-grade set recipes). Auto-known recipes are gated by level, not this set.</summary>
    public HashSet<string> KnownRecipes { get; } = new();

    /// <summary>Friend CHARACTER names (case-preserved; matched case-insensitively). When a friend comes
    /// online you get a "&lt;friend&gt; is back online" message. Per character. Persisted as a CSV.</summary>
    public HashSet<string> Friends { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ignored (blocked) character names. Chat from these players — whisper, world and local —
    /// is not delivered to you. Persisted as a CSV, like the friend list.</summary>
    public HashSet<string> Blocked { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>What this character refuses to receive: blanket chat blocks and auto-declines
    /// (owner, playtest-19 M2). Persisted as one int. See <see cref="SocialOptions"/> — and note that
    /// none of them apply to STAFF, which is checked where the message is delivered.</summary>
    public SocialOptions Social { get; set; } = SocialOptions.None;

    /// <summary>Does this character refuse <paramref name="option"/>? Always FALSE when the other party
    /// is staff — an admin or moderator must be able to reach and act on anyone.</summary>
    public bool Refuses(SocialOptions option, Entity? from = null) =>
        (from is null || !from.IsStaff) && (Social & option) != 0;

    /// <summary>Recently-SOLD items, re-buyable at any vendor for the sell price you got. In-memory only
    /// (cleared on logout), newest last, capped at <see cref="GameConstants.BuyBackSlots"/>. Stores enough
    /// to restore the item faithfully (enchant + rolled attributes).</summary>
    public List<BuyBackEntry> BuyBack { get; } = new();

    /// <summary>Recently BINNED items, restorable for FREE (playtest-17 C18 — it had already cost him a
    /// real item). Same shape and same in-memory lifetime as <see cref="BuyBack"/>, but a SEPARATE list:
    /// selling and binning are different accidents with different prices, and a shared list would let a
    /// spree of sales push the one thing you meant to undo off the end. Capped at
    /// <see cref="GameConstants.RestoreSlots"/>, newest last. <c>UnitPrice</c> is always 0.</summary>
    public List<BuyBackEntry> Restorable { get; } = new();

    // ----- Charisma (reputation). Two values, neither below 0 (see GameConstants). -----
    /// <summary>The 0–<see cref="GameConstants.CharismaPoolCap"/> bonus POOL — drives the exp/sp bonus.</summary>
    public int Charisma { get; set; }
    /// <summary>Uncapped LIFETIME charisma — what the ranking board uses. Likes raise it; kills (and later
    /// moderation) lower it, so a griefer can't top the board.</summary>
    public long CharismaLifetime { get; set; }
    /// <summary>Likes left to GIVE today (budget, resets daily). </summary>
    public int LikesRemainingToday { get; set; } = GameConstants.DailyLikeBudget;
    /// <summary>UTC date (yyyy-MM-dd) the like budget was last granted; a new day refills it to the budget.</summary>
    public string LikeBudgetDay { get; set; } = "";

    // ----- Wearable title -----
    /// <summary>WHERE the worn title comes from: a leaderboard category, a staff title id,
    /// <see cref="TitleCatalog.Custom"/> for one the player wrote, or "" for none. Persisted.
    /// The source is stored rather than only the text so re-wording a granted title re-words
    /// everyone's, and so a title that is no longer HELD can be recognised and taken back off.</summary>
    public string TitleCategory { get; set; } = "";

    /// <summary>The title's display TEXT, or "" when nothing is worn OR the worn one is no longer held.
    /// Recomputed on the tick thread whenever the title holders are re-read; the snapshot only ever
    /// copies it, so no DB or catalog work happens on the broadcast path. For an NPC this is the ROLE
    /// half of its authored name ("Elder" over "Marius"), set once at spawn.</summary>
    public string Title { get; set; } = "";

    /// <summary>The colour the title is drawn in — RRGGBB, no '#'. "" means
    /// <see cref="TitleCatalog.DefaultHex"/>. Travels with the text because a written title's colour is
    /// its owner's choice and cannot be derived from anything.</summary>
    public string TitleColor { get; set; } = "";

    /// <summary>What the player last WROTE for themselves, kept even while a board title is worn so
    /// they can switch back to it without retyping. Persisted, and independent of
    /// <see cref="TitleCategory"/>.</summary>
    public string CustomTitle { get; set; } = "";
    /// <summary>The colour chosen for <see cref="CustomTitle"/> (RRGGBB, "" = default). Persisted.</summary>
    public string CustomTitleColor { get; set; } = "";

    /// <summary>
    /// May this character write its own title (`/title`)? Persisted per character. Admins always may.
    ///
    /// Granted at **level 76, from the same gate as Angel's Protection** (owner, 2026-08-07): the two
    /// are meant to become rewards of the SAME quest, so they are auto-granted from one place until
    /// that quest exists. `/titleright` is the manual override on top.
    /// </summary>
    public bool MayWriteTitle { get; set; }

    /// <summary>The effective right: an admin never has to grant it to themselves.</summary>
    public bool CanWriteTitle => MayWriteTitle || IsAdmin;

    /// <summary>NPC id this entity represents (NPCs only).</summary>
    public string? NpcId { get; set; }
    public NpcRole NpcRole { get; set; }

    /// <summary>Staff role, held PER CHARACTER (owner) — an admin ACCOUNT can also have plain characters.
    /// Loaded from the character row at EnterWorld.</summary>
    public AccountRole Role { get; set; } = AccountRole.Player;

    /// <summary>Full admin: every command, god mode, no cap enforcement. NOT a moderator.</summary>
    public bool IsAdmin => Role == AccountRole.Admin;

    /// <summary>Any staff (admin OR moderator) — the gate for "may issue moderation commands at all".
    /// WHICH commands is then decided per-command; a moderator gets jail/kick/chatban only.</summary>
    public bool IsStaff => Role != AccountRole.Player;

    /// <summary>God mode: takes no damage (admin only).</summary>
    public bool GodMode { get; set; }

    // ----- Admin speed overrides (`/spd m|a|c <value>`) ----------------------------------------
    // Testing aids: when set, they REPLACE the computed stat outright — no caps, no buffs, no gear —
    // so an admin can dial in an exact number and watch what it does. Runtime only (never persisted);
    // a bare `/spd` clears all three and the normal formulas resume.
    // (Renamed 2026-08-07 from the four `/speed-*` commands — one verb, one letter, bare = reset.)

    /// <summary>Raw cast-speed stat override (333 = 1.0x). null = use the real formula.</summary>
    public float? AdminCastSpeed { get; set; }

    /// <summary>Raw attack-speed stat override (333 = 1.0x). null = use the real formula.</summary>
    public float? AdminAttackSpeed { get; set; }

    /// <summary>Move-speed override in world units/sec. null = use the real formula.</summary>
    public float? AdminMoveSpeed { get; set; }

    /// <summary>True if any admin speed override is in force (shown in the /spd readout).</summary>
    public bool HasSpeedOverride =>
        AdminCastSpeed is not null || AdminAttackSpeed is not null || AdminMoveSpeed is not null;

    // ----- Admin STAT overrides (`/stat <name> <value>`) ---------------------------------------
    // The owner asked for the same thing /spd does, for EVERY stat (playtest-20 `54e`: *"an
    // admin-only stat override for every stat — acc 999999, eva, crit dmg, crit rate… one command,
    // overriding all"*). /spd is kept as the speed shorthand; this is the general form and it
    // covers the speeds too, so there is one command to remember and nothing regressed.
    //
    // ⚠ These REPLACE the finished number, not a base one — the point of "overriding all" is that a
    // buff, a passive, gear and the caps cannot claw it back. That is why the stats which layer
    // their buffs at READ time (evasion, defence, attack) check the override inside their getter
    // rather than in RecomputeDerived: writing the field there would leave the buff to be added on
    // top, and the number the admin typed would not be the number the game used.
    //
    // Runtime only — never persisted, exactly like the speed overrides.

    /// <summary>Forced stat values by key, or null when nothing is forced. See AdminStatKeys.</summary>
    public Dictionary<string, float>? AdminStats { get; set; }

    /// <summary>The forced value for a stat, or null to use the real formula.</summary>
    public float? AdminStat(string key) =>
        AdminStats is not null && AdminStats.TryGetValue(key, out float v) ? v : null;

    public bool HasStatOverride => AdminStats is { Count: > 0 };

    /// <summary>Every stat <c>/stat</c> accepts, and what each one forces. The command's help text is
    /// generated from this, so a new stat is one entry rather than three edits that can disagree.</summary>
    public static readonly (string Key, string What)[] AdminStatKeys =
    {
        ("acc",    "accuracy"),
        ("eva",    "evasion"),
        ("patk",   "physical attack"),
        ("matk",   "magic attack"),
        ("pdef",   "physical defence"),
        ("mdef",   "magic defence"),
        ("crate",  "physical crit rate (0-1000 scale)"),
        ("cdmg",   "crit damage bonus (0.2 = +0.2x)"),
        ("cdflat", "flat crit damage"),
        ("mcrate", "magic crit rate"),
        ("hp",     "max HP"),
        ("mp",     "max MP"),
        ("m",      "move speed"),
        ("a",      "attack speed (333 = 1.0x)"),
        ("c",      "cast speed (333 = 1.0x)"),
    };

    /// <summary>Overrides for the stats that are plain FIELDS — applied at the very end of
    /// RecomputeDerived so no cap, passive or mob multiplier can wash them out. The ones that layer
    /// buffs at read time are handled in their own getters instead.</summary>
    private void ApplyAdminStatOverrides()
    {
        if (!HasStatOverride) return;
        if (AdminStat("acc") is float acc) Accuracy = (int)acc;
        if (AdminStat("crate") is float cr) CritChance = cr;
        if (AdminStat("mcrate") is float mcr) MagicCritChance = mcr;
        if (AdminStat("cdmg") is float cd) CritDamageBonus = cd;
        // "mcdmg" sets the FINISHED magic crit multiplier (2 = the base, 2.6 = one blessing), so
        // /stat mcdmg 3.38 reproduces the fully-blessed 4th-class caster without the 4th class.
        if (AdminStat("mcdmg") is float mcd)
        {
            MagicCritDamageMult = mcd / StatCaps.MagicCritDamageBase;
            MagicCritDamageResist = 0f;
        }
        if (AdminStat("cdflat") is float cdf) CritDamageFlat = cdf;
        if (AdminStat("hp") is float hp) MaxHp = Math.Max(1, (int)hp);
        if (AdminStat("mp") is float mp) MaxMp = Math.Max(0, (int)mp);
    }

    /// <summary>Jailed players are confined to the jail cell and can't chat/whisper/act until this UTC
    /// time. Loaded from the character row so jail SURVIVES a relog (owner). null = free.</summary>
    public DateTime? JailedUntil { get; set; }

    /// <summary>True while the jail sentence is still in effect.</summary>
    public bool Jailed => JailedUntil is DateTime u && u > DateTime.UtcNow;

    /// <summary>Chat-banned until this UTC time — can't type in any channel, but plays normally
    /// otherwise (owner: a lighter punishment than jail). Persisted, so it survives a relog.</summary>
    public DateTime? ChatBannedUntil { get; set; }

    /// <summary>True while the chat ban is still in effect.</summary>
    public bool ChatBanned => ChatBannedUntil is DateTime u && u > DateTime.UtcNow;

    /// <summary>0 = none; otherwise a ClassCatalog id. PER CLASS.</summary>
    public int SecondClass
    {
        get => ActiveSubclass.SecondClass;
        set => ActiveSubclass.SecondClass = value;
    }

    /// <summary>0 = none; otherwise a ThirdClassCatalog id (101-136). PER CLASS.</summary>
    public int ThirdClass
    {
        get => ActiveSubclass.ThirdClass;
        set => ActiveSubclass.ThirdClass = value;
    }

    /// <summary>0 = none; otherwise a FourthClassCatalog id (201-236). PER CLASS.</summary>
    public int FourthClass
    {
        get => ActiveSubclass.FourthClass;
        set => ActiveSubclass.FourthClass = value;
    }

    public Archetype? Archetype =>
        SecondClass > 0 ? ClassCatalog.Get(SecondClass)?.Archetype : null;

    /// <summary>The 3rd-class discipline once chosen (null before lvl-40 change).
    /// Discipline + Race selects the skill list; the parent archetype is unchanged.</summary>
    public Discipline? Discipline =>
        ThirdClass > 0 ? ThirdClassCatalog.Get(ThirdClass)?.Discipline : null;

    public float X { get; set; }
    public float Y { get; set; }

    public float? TargetX { get; set; }
    public float? TargetY { get; set; }

    /// <summary>Computed RUN speed (race+class base + gear/buffs), clamped to
    /// the move cap. This is the value movement uses when running.</summary>
    public float Speed { get; set; } = GameConstants.BasePlayerSpeed;

    /// <summary>Movement/regen state (players). Mobs use Engaged to pick walk/run.</summary>
    public MoveState MoveState { get; set; } = MoveState.Running;

    /// <summary>Ticks remaining in the stand-up recovery after sitting was broken.
    /// While &gt; 0 the player can't move/cast/act.</summary>
    public int StandUpTicks { get; set; }

    /// <summary>Tick the entity last SAT DOWN, so a voluntary stand can tell a genuine rest from
    /// sit/stand spam. Runtime only — sitting does not survive a relog.</summary>
    public long SatDownTick { get; set; }

    /// <summary>Per-entity move-speed ceiling (default 250; a future rogue
    /// ultimate raises it to outrun even a buffed mage).</summary>
    public float MoveSpeedCap { get; set; } = StatCaps.MoveSpeed;

    /// <summary>Mob walk/run speeds (from MobCatalog). Players derive walk from run.</summary>
    public float WalkSpeed { get; set; }
    public float RunSpeed { get; set; }

    // ----- Core stats (CON/ATK/WIT/AGI) --------------------------------------
    // PER CLASS: they are derived from (Race, BaseClass), so swapping a fighter for a mage must swap
    // these too. Mobs use the same fields (they have one implicit subclass) — see Subclass.cs.

    public int Con
    {
        get => ActiveSubclass.Con;
        set => ActiveSubclass.Con = value;
    }
    public int AtkStat
    {
        get => ActiveSubclass.Atk;
        set => ActiveSubclass.Atk = value;
    }
    public int Wit
    {
        get => ActiveSubclass.Wit;
        set => ActiveSubclass.Wit = value;
    }
    public int Agi
    {
        get => ActiveSubclass.Agi;
        set => ActiveSubclass.Agi = value;
    }
    /// <summary>SPT (Spirit) — Max MP, MP regen and M.Def. The retired MEN, now a full stat.</summary>
    public int Spt
    {
        get => ActiveSubclass.Spt;
        set => ActiveSubclass.Spt = value;
    }

    // Primary-stat DELTAS from armor sets (and later dyes/tattoos). Set in RecomputeDerived's
    // pre-pass BEFORE the derived stats are computed, so a set's "CON +3" raises HP, "AGI +1"
    // raises eva/acc/crit, etc. Included in the Effective* getters so live speed getters see them too.
    public int BonusStr { get; set; }
    /// <summary>Delta on the single power stat (ATK), from the level-40 stat-swap passives. ATK
    /// feeds BOTH channels — the WEAPON decides whether it lands as P.Atk or M.Atk — which is what
    /// lets one +ATK skill serve a fighter and a caster alike.</summary>
    public int BonusAtk { get; set; }
    public int BonusAgi { get; set; }
    public int BonusCon { get; set; }
    public int BonusInt { get; set; }
    public int BonusWit { get; set; }
    public int BonusSpt { get; set; }

    /// <summary>SPT actually used by the math: born-with base + the stat-swap passives and set
    /// bonuses. Same rule as <see cref="EffectiveWit"/>.</summary>
    public int EffectiveSpt => Spt + BonusSpt;
    /// <summary>CON including armour-set and stat-swap deltas. Added 2026-08-19 so the contested-debuff
    /// contest reads the same CON that Max HP already did (`Con + BonusCon`, inline in
    /// RecomputeDerived) — his rule: *"an armor con/atk/spt should count and statSwap as well"*.</summary>
    public int EffectiveCon => Con + BonusCon;

    /// <summary>Crafting profession (one per character). Granted by that profession's MASTER after his
    /// joining quest, and quittable at him (`BL-05`).</summary>
    public Profession Profession { get; set; }

    /// <summary>RAW crafting exp — 12 internal points per same-level craft (see
    /// <see cref="Crafting.CraftExpPerCraft"/> for why 12 and not 1). The crafting LEVEL is not stored:
    /// it is <see cref="Crafting.EffectiveLevel"/> of this number against the character's own band, so
    /// there is exactly one source of truth and no way for a stored level to disagree with the exp
    /// beside it. Zeroed when a profession is quit — *"losing all his levels"*.</summary>
    public int CraftExp { get; set; }

    /// <summary>The highest crafting rung this character's PROGRESSION currently allows (0 below level
    /// 20). The freeze the owner asked for lives here: exp accumulates to the top of this band and then
    /// stops dead — *"my exp freezes until i reach the next class … then the l2@100% becomes l3@0%"*.
    ///
    /// 🔑 Read from the BEST subclass, not the active one. <see cref="Level"/> and
    /// <see cref="ThirdClass"/> both proxy to <see cref="ActiveSubclass"/>, so a level-76 main who swaps
    /// to a fresh level-20 subclass would otherwise see his band collapse from 6 to 2 — and since the
    /// freeze CAPS exp rather than banking it, the next craft would have clamped an L6 smith down to
    /// L2 permanently. A profession belongs to the CHARACTER (one per character, quit at a master), so
    /// its band does too. The award path never lowers stored exp either; both guards, because this one
    /// is silent and destroys hours.</summary>
    public int CraftBandCap
    {
        get
        {
            int best = 0;
            foreach (var s in Subclasses)
                best = Math.Max(best, Crafting.BandCap(s.Level, s.ThirdClass > 0, s.FourthClass > 0));
            return best;
        }
    }

    /// <summary>The crafting level actually in force — what the exp is worth, held down to the band.</summary>
    public int CraftLevel =>
        Profession == Profession.None ? 0 : Crafting.EffectiveLevel(CraftExp, CraftBandCap);

    /// <summary>Runtime only: is this crafter standing at HIS OWN master right now? The latch behind the
    /// crafting window's browse-vs-craft mode — see GameLoopService.TickCraftMasterProximity.</summary>
    public bool AtCraftMaster { get; set; }

    /// <summary>WIT used for ALL gameplay math (cast speed, MP, magic crit, interrupt,
    /// heals). Stored <see cref="Wit"/> is the persisted base you were BORN with; the only
    /// thing that moves it is <see cref="BonusWit"/> (the level-40 stat-swap passives).
    /// The old free +1@20…+5@80 "dye stand-in" (LevelStatBonus) is gone — the stat-swap
    /// skills replace it, and stats no longer grow just by levelling.</summary>
    public int EffectiveWit => Wit + BonusWit;

    /// <summary>The power stat (ATK) used for ALL gameplay math: born-with base + the stat-swap
    /// passives. Feeds P.Atk and M.Atk alike; the weapon's channel factors decide which one it
    /// actually lands in.</summary>
    public int EffectiveAtk => AtkStat + BonusAtk;

    /// <summary>AGI used for ALL gameplay math (attack speed, crit, evasion, accuracy).
    /// Same rule as <see cref="EffectiveWit"/>: born-with base + the stat-swap passives.</summary>
    public int EffectiveAgi => Agi + BonusAgi;

    // ----- Derived stats (recomputed on level-up / equip / class change) -------

    /// <summary>PER CLASS — each subclass levels on its own.</summary>
    public int Level
    {
        get => ActiveSubclass.Level;
        set => ActiveSubclass.Level = value;
    }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Mp { get; set; }
    public int MaxMp { get; set; }
    public int AttackPower { get; set; }      // physical attack (pAtk); feeds SKILLS
    public int BasicAttackPower { get; set; } // feeds auto-attacks (archetype-scaled)
    public int MagicAttack { get; set; }      // magic attack (mAtk); feeds spells
    public int Defence { get; set; }
    public int MagicDefence { get; set; }     // magic-only defence (level base + jewels + Anti-Magic)
    public int Accuracy { get; set; }
    public int Evasion { get; set; }
    public WeaponType WeaponType { get; set; } = WeaponType.None;   // encodes hands + type
    /// <summary>The weapon a MOB innately fights with — claws, a club, a blade, a bow. Players leave
    /// this None and get their WeaponType from the equipped item instead.
    ///
    /// It has to be its own field because <see cref="RecomputeDerived"/> resets
    /// <see cref="WeaponType"/> to None and rebuilds it from the equipped weapon, so a mob (which
    /// has no inventory) loses anything assigned directly and would silently fall back to the
    /// weaponless speed on the very next recompute. That is exactly why the Archer role used to
    /// assign WeaponType *after* RecomputeDerived — a workaround that left archer mobs swinging at
    /// bare-hand speed anyway, because WeaponAttackBase had already been derived.
    /// Set once at spawn (GameLoopService.BuildMob); survives every recompute after that.</summary>
    public WeaponType InnateWeaponType { get; set; } = WeaponType.None;
    public float CritChance { get; set; }       // physical crit rate
    public float MagicCritChance { get; set; }  // magic crit rate (from WIT)
    public int InterruptResist { get; set; }    // resist casting interruption (from WIT)
    public int MagicInterruptBonus { get; set; } // OFFENSIVE magic interrupt power (from WIT)
    public int BasicAttackInterruptPower { get; set; } // interrupt power carried by basic attacks (rogues)
    // Defender's MULTIPLIER on the magic-fail formula (StatCalculator.MagicFailChance). 1 = normal,
    // 2 = the tank's Anti-Magic passive. It replaced a flat fizzle FLOOR on 2026-08-10 — see the
    // header comment on MagicFailChance for why a floor was the wrong shape.
    public float MagicFailMod { get; set; } = 1f;
    // "MAGIC EVASION" — flat percentage POINTS added to the fail chance of spells cast AT this entity,
    // after MagicFailMod has multiplied (owner ruling 2026-08-11, `62e`: *"the magic evasion should be
    // magic fail chance like 3-4"*). It is NOT an evasion roll: magic never calls the physical avoid
    // resolver. Only source today is the rogue's Evasion Boost (+4 for 30s).
    public float MagicFailBonus { get; set; }
    // MAGIC RESISTANCE. Stored the way the CSVs author it ("mRes +5%" = 0.05f) and summed across
    // passives/buffs; MagicDefCoef turns it into the defence DIVISOR, the exact shape of
    // PierceDefCoef/BluntDefCoef/BowDefCoef. 0.25 → coef 1.25 → takes ×0.8 magic damage, which is
    // exactly the mob resistance ladder in docs/data/mobs/mobs_passives.csv. It rides inside M.Def,
    // so a defence-ignoring effect bypasses it too. NOT a fizzle chance — see MagicFailMod.
    public float MagicResist { get; set; }
    public float MagicDefCoef => Math.Max(0.01f, 1f + MagicResist);
    public float EvadeFloor { get; set; }        // rogue: guaranteed min chance to dodge physical attacks
    public float HitFloor { get; set; }          // warrior: guaranteed min chance THIS entity lands a physical attack
    // ----- The three SKILL-defence channels (BL-06/07/08). All folded by MAX, like the floors
    //       above: they are class GUARANTEES, so two sources never compound. -----
    /// <summary>BL-06 — chance to dodge an incoming PHYSICAL SKILL. A physical skill is not subject
    /// to the accuracy-vs-evasion roll at all any more (*"normaly no1 can evade a physical skill"*),
    /// so this is the ONLY thing that makes one miss. Rogue ultimate only, today.</summary>
    public float SkillEvadeChance { get; set; }
    /// <summary>BL-07 — chance that a physical SKILL which hits me is reflected at its caster, and
    /// the fraction of its damage that goes back. Basic attacks are the separate MeleeReflect
    /// channel; spells reflect nothing.</summary>
    public float PhysSkillReflectChance { get; set; }
    public float PhysSkillReflectPct { get; set; }
    /// <summary>BL-08 — chance that a DEBUFF aimed at me lands on its caster instead.</summary>
    public float DebuffReflectChance { get; set; }
    public bool Immune { get; set; }             // ultimate total-avoid (future buff); attacks always miss/fail
    public float HpRegenBonus { get; set; }     // flat HP/s from gear attributes
    public float MpRegenBonus { get; set; }     // flat MP/s from gear attributes
    public float HpRegenMult { get; set; } = 1f; // HP-regen multiplier (armor mastery)
    public float MpRegenMult { get; set; } = 1f; // MP-regen multiplier (armor mastery)
    public float CritDamageBonus { get; set; }  // crit-multiplier bonus from gear (e.g. +0.20x)
    // FLAT crit damage (the CSVs' "crit dmg +80", weapon masteries). Joins ATTACK inside the
    // damage ratio on a CRIT only, before the multiplier — never a multiplier itself.
    // See docs/design/CritBlowAndDouble.md §3 and StatCalculator.CritFlatFactor.
    public float CritDamageFlat { get; set; }
    // ----- Crit-RATE chain (his IG model, docs/design/CritBlowAndDouble.md §5) -----
    // crit = (base × MULT + FLAT), clamped once at the end of RecomputeDerived. Every passive
    // and buff MULTIPLIES (×1.2, ×1.3), so a big base is what a multiplier rewards; every gear
    // and flat source ADDS outside all of them, which is what carries the low-crit weapons.
    // Accumulated through RecomputeDerived, then folded into CritChance — read CritChance, not these.
    public float CritRateMult { get; set; } = 1f;
    public float CritRateFlat { get; set; }
    // The MAGIC twins of the two above — same rule, its own channel (owner ruling 2026-08-06).
    // Magic crit shares NOTHING with physical any more: not the rate (WIT, not AGI), not the
    // damage (a flat x3, not CritDamageBonus). Fold into MagicCritChance, read that.
    public float MagicCritRateMult { get; set; } = 1f;
    public float MagicCritRateFlat { get; set; }
    // ----- Magic crit DAMAGE (owner ruling 2026-08-19) -----
    // `base ×2 × multipliers × (1 − debuffs)` — his formula, and the reason the flat ×3 became a
    // base with a knob: the 4th-class buffer/healer blessings are +30% each and COMPOUND (×3.38
    // together). Multipliers compound, debuffs SUM. Read EffectiveMagicCritDamage, not these.
    public float MagicCritDamageMult { get; set; } = 1f;
    public float MagicCritDamageResist { get; set; }

    /// <summary>The finished magic crit multiplier — the ONE thing the damage path should read.
    /// (Its physical twin is spread across CritDamageBonus/CritDamageFlat, which are consumed
    /// differently; magic has a single number, so it gets a single getter.)</summary>
    public float EffectiveMagicCritDamage =>
        StatCalculator.MagicCritMult(MagicCritDamageMult, MagicCritDamageResist);
    // ----- Healer buff/effect layer (folded from buffs + passives in RecomputeDerived) -----
    public float CooldownReduction { get; set; } // spell reuse-delay reduction (0..cap)
    public float CritRateResist { get; set; }    // reduces an attacker's physical crit CHANCE vs you
    public float CritDmgResist { get; set; }     // reduces incoming physical crit EXTRA damage
    public float BowResist { get; set; }         // reduces damage taken from BOW attacks
    public float CcResist { get; set; }          // reduces the LAND chance of contested CC vs you
    // …and the same, but only against ONE school. Composes with CcResist multiplicatively at the roll,
    // so armour's blanket resistance and a healer's targeted blessing never cancel or mask each other.
    public float CcResistMagical { get; set; }   // vs SPT-defended debuffs (Clarity)
    public float CcResistPhysical { get; set; }  // vs CON-defended debuffs (Fortitude)
    // Weapon-TYPE resistance: a multiplier on MY P.Def applied only when the attacker uses
    // that weapon type (the resist rides inside pDef so a def-ignore skill bypasses it).
    // 1 = neutral, >1 = resistant, <1 = weak, ≤0 = no defence (one-shot of that type).
    public float PierceDefCoef { get; set; } = 1f; // vs sword / dual
    public float BluntDefCoef { get; set; } = 1f;  // vs blunt
    public float BowDefCoef { get; set; } = 1f;    // vs bow
    /// <summary>MP-RESTORE RECEIVED multiplier — the MP twin of <see cref="HealReceivedMod"/>. Every
    /// restore that lands on you (a cast, a totem pulse, anything) is scaled by this. 1 = untrained;
    /// the nuker robe mastery raises it to ×1.60 at the top rung.
    /// <para>⚠ It was a FLAT <c>int RestoreMpBonus</c> ("+N MP per restore") until 2026-08-19. A flat
    /// per-EVENT bonus cannot survive a periodic restore — a mana totem pulsing 30 times paid it 30
    /// times — so the owner re-authored it as a percent that scales with whatever actually lands,
    /// exactly as healing already works. See StatMods.RestoreMpPct.</para></summary>
    public float RestoreMpMod { get; set; } = 1f;
    // (MagicFailResist DELETED 2026-08-10 — the owner's magic-landing model has no caster-side
    //  accuracy stat. It was also the field the bow penalty halved, which is why `57d` was invisible.)
    public bool UntrainedCasterWeapon { get; set; }  // Spellcaster Mastery: bow/dual/bare → magic accuracy x0.5
    public float MeleeVamp { get; set; }         // basic (melee) attack lifesteal fraction
    public float SpellVamp { get; set; }         // damage-spell lifesteal fraction
    public float MeleeReflect { get; set; }      // fraction of taken MELEE-basic damage returned to the attacker
    public float PhysMpCostReduction { get; set; }  // reduce PHYSICAL-skill MP cost (fraction)
    public float MagicMpCostReduction { get; set; } // reduce magic/buff/heal-skill MP cost (fraction)
    // ----- Damage-OUT bonuses (fractions): 2×3 matrix context (PvE/PvP) × source
    //       (skill=physical skill / magic / basic). The damage pipeline reads ONE. -----
    public float PveSkillDamageBonus { get; set; }   // +% physical-skill damage vs mobs
    public float PveMagicDamageBonus { get; set; }   // +% magic-skill damage vs mobs
    public float PveBasicDamageBonus { get; set; }   // +% basic-attack damage vs mobs
    public float PvpSkillDamageBonus { get; set; }   // +% physical-skill damage vs players
    public float PvpMagicDamageBonus { get; set; }   // +% magic-skill damage vs players
    public float PvpBasicDamageBonus { get; set; }   // +% basic-attack damage vs players
    /// <summary>The RECEIVING half of the PvP matrix — a MULTIPLIER on damage this entity takes from
    /// another player (1 = neutral, 0.95 = the S heavy/light sets' "PVP Dmg Received x0.95"). The three
    /// bonuses above are all attacker-side; nothing could express "I take less in PvP" until the S set
    /// bonuses needed it (him, gear_sets.csv 2026-08-11). Applied in FinalizeDamage, the one place the
    /// PvP/PvE matrix is read, and never on a mob's hit — this is player-vs-player only.</summary>
    public float PvpDamageTaken { get; set; } = 1f;
    public float CancelResist { get; set; }          // chance each buff resists an enemy cancel

    // ----- REWARD RATES: what a MONSTER pays THIS character, as MULTIPLIERS (1 = untouched). Fed by
    //       the premium reward runes (RewardRunes.cs) and by nothing else today. 0 means "no reward at
    //       all" — the Rune of Sinister zeroes exp+sp, the Rune of Sinners everything. -----
    /// <summary>This character's own reward multipliers — the third and innermost scope, composed with
    /// <see cref="RateConfig.World"/> (and <see cref="RateConfig.Quest"/> on a quest) at every award.
    ///
    /// <para><c>DropChance</c> is fed INTO <see cref="MobCatalog.EffectiveRate"/> rather than applied at
    /// the roll, so the kill roll, the target-inspect list and BalanceMatrix all keep reading one number
    /// (see CLAUDE.md). <c>DropAmount</c> is always 1 — no rune grants stack size, and it stays in the
    /// set only so one type describes every scope.</para></summary>
    public RateSet Runes { get; set; } = RateSet.One;

    public string ActiveArmorSet { get; set; } = ""; // name of the completed armor set bonus, "" if none
    public string ArmorMasteryLabel { get; set; } = ""; // armor-weight mastery status for the UI

    // ----- GRADE PENALTY (see GradePenalty in Game.Shared/Items.cs) -----
    // How many grade STEPS over your own grade the worst piece you're wearing is; 0 = nothing over-grade,
    // which is the normal case at every level. Armour/jewels/shield feed one gap and the weapon another,
    // because they penalise DIFFERENT stat sets. Recomputed from scratch each RecomputeDerived.
    public int GradeArmorGap { get; set; }
    public int GradeWeaponGap { get; set; }

    /// <summary>Future perk hook (owner): "this character may equip gear N levels early". Lifts the
    /// effective level used for GRADE comparisons only — never real level. 0 = no perk, today's behaviour.</summary>
    public int GradeLevelBonus { get; set; }

    /// <summary>What the over-grade ARMOUR/jewels leave of: cast/attack/move speed, P/M defence, evasion.</summary>
    public float GradeArmorPenalty => GradePenalty.FactorForGap(GradeArmorGap);

    /// <summary>What the over-grade WEAPON leaves of: P/M crit rate + crit damage, P/M accuracy, P/M attack.</summary>
    public float GradeWeaponPenalty => GradePenalty.FactorForGap(GradeWeaponGap);

    // ----- Shield / block (0 if no shield equipped) -----
    public bool HasShield { get; set; }
    public float BlockChance { get; set; }       // chance to block a physical hit
    public float BlockReduction { get; set; }    // damage fraction removed on block
    public int ShieldDefense { get; set; }       // flat defence from the shield
    public float ShieldCritDefense { get; set; } // reduces attacker crit chance
    public float BasicAttackRange { get; set; } = GameConstants.MeleeRange;

    /// <summary>Cast-time multiplier from item Cast Speed attributes (0.8 = 20% faster).</summary>
    public float CastSpeedMultiplier { get; set; } = 1f;
    /// <summary>FLAT addition to the casting-speed stat, from passives (the spell rune +40).
    /// Added AFTER the multiplicative chain, so it does not compound with WIT/gear/buffs.</summary>
    public float CastSpeedFlatBonus { get; set; }
    /// <summary>Weapon Proficiency: ×0.5 cast speed while wielding an untrained weapon (not sword/blunt).
    /// 1 = no penalty. Set in RecomputeDerived by WeaponType + the passive.</summary>
    public float CastSpeedPenaltyMult { get; set; } = 1f;
    /// <summary>Divine Focus: heal-output multiplier when NO magic weapon is equipped (Lv1 ×0.5, Lv2 ×0.75).
    /// 1 = no penalty.</summary>
    public float HealOutputMult { get; set; } = 1f;
    /// <summary>Weapon Proficiency: M.Atk multiplier while wielding an UNTRAINED weapon (bow/dual/hands) —
    /// ×0.05, so magic (damage + heals + the shown M.Atk) collapses. 1 = a trained weapon, no penalty.</summary>
    public float MagicWeaponPenaltyMult { get; set; } = 1f;
    /// <summary>True while a magic weapon (wand/staff) is equipped — for Divine Focus.</summary>
    public bool HasMagicWeapon { get; set; }

    // ----- Heal stats (owner 2026-07-17): heals no longer use M.Atk. A heal's OUTPUT is
    // (HealPowerFlat + skillPower)·HealPowerMod; the TARGET then receives (HealReceivedFlat +
    // output)·HealReceivedMod. Defaults 0 flat / ×1, so an untrained healer heals exactly the skill
    // power (nobody overheals). Set by class/gear/passives (and, later, a healer buff). -----
    public int HealPowerFlat { get; set; }
    public float HealPowerMod { get; set; } = 1f;
    public int HealReceivedFlat { get; set; }
    public float HealReceivedMod { get; set; } = 1f;   // anti-heal debuffs lower it; buffs/passives raise it
    /// <summary>Attack-interval multiplier from Attack Speed attributes.</summary>
    public float AttackSpeedMultiplier { get; set; } = 1f;

    /// <summary>PER CLASS — XP earned on one class does not advance another.</summary>
    public long Exp
    {
        get => ActiveSubclass.Exp;
        set => ActiveSubclass.Exp = value;
    }

    /// <summary>Gold wallet (currency). Drops from mobs; spent at vendors / teleports.</summary>
    public long Gold { get; set; }

    // ----- Inventory (players only) ----------------------------------------------

    public List<InventoryItem> Inventory { get; } = new();

    /// <summary>The private WAREHOUSE — a second, separate item list (not the bag). Items here are OUT of
    /// play: not equippable, not sold, and a rune stored here does NOT apply its buff (it still expires).
    /// Kept as its own list so every bag iteration (equip, RecomputeDerived, drops, trade) stays unchanged.
    /// Persisted alongside the bag via the InWarehouse flag on the item record.</summary>
    public List<InventoryItem> Warehouse { get; } = new();

    // ----- Buffs / debuffs ------------------------------------------------------------

    public List<BuffInstance> Buffs { get; } = new();

    /// <summary>Buffs loaded from the DB but not yet re-applied. The loader cannot apply them itself —
    /// ApplyBuff recomputes derived stats and pushes to the client, which is tick-thread work — so it
    /// parks them here and the game loop drains the list on entry to the world.</summary>
    public List<Persistence.PersistenceService.BuffSnapshot> PendingBuffs { get; } = new();

    /// <summary>Held in place by a Root effect — cannot move until it expires.</summary>
    public bool IsRooted
    {
        get
        {
            foreach (var b in Buffs) if (b.Has(SkillEffect.Root)) return true;
            return false;
        }
    }

    /// <summary>Stunned: cannot move, cast or attack while any Stun effect is active.</summary>
    public bool IsStunned
    {
        get
        {
            foreach (var b in Buffs) if (b.Has(SkillEffect.Stun)) return true;
            return false;
        }
    }

    /// <summary>Feared: cannot cast or attack (but may still move) while any Fear is active.</summary>
    public bool IsFeared
    {
        get
        {
            foreach (var b in Buffs) if (b.Has(SkillEffect.Fear)) return true;
            return false;
        }
    }

    /// <summary>Cannot take an action (cast/attack/skill) this tick due to a stun or fear.</summary>
    public bool IsActionLocked => IsStunned || IsFeared;

    /// <summary>Slowed: any Slow debuff is active (for conditional-damage skills).</summary>
    public bool IsSlowed
    {
        get
        {
            foreach (var b in Buffs) if (b.Has(SkillEffect.Slow)) return true;
            return false;
        }
    }


    /// <summary>De-taunt stub (no threat system yet): on a mob, while &gt;0 it will
    /// not re-aggro <see cref="DetauntFromId"/> (the entity that shed it).</summary>
    public int DetauntTicks { get; set; }
    public Guid? DetauntFromId { get; set; }

    /// <summary>Apply all buffs for one effect to a base value using the
    /// standard formula: (base + sum flats) * (1 + sum percents). Optionally a
    /// second (debuff) flag subtracts its percents/flats (used for defence).</summary>
    private float ModifiedStat(float baseValue, SkillEffect plusFlag, SkillEffect minusFlag = SkillEffect.None)
    {
        float flat = 0f, percent = 0f;
        foreach (var buff in Buffs)
        {
            if (buff.Has(plusFlag))
            {
                flat += buff.Flat(plusFlag);
                percent += buff.Percent(plusFlag);
            }
            if (minusFlag != SkillEffect.None && buff.Has(minusFlag))
            {
                flat -= buff.Flat(minusFlag);
                percent -= buff.Percent(minusFlag);
            }
        }
        return Math.Max(0f, (baseValue + flat) * (1f + percent));
    }

    /// <summary>Folds TWO additive buff flags (a shared one + a channel-specific one),
    /// e.g. BuffAtk (both channels) plus BuffPhysAtk / BuffMagAtk (one channel only).</summary>
    private float ModifiedStatDual(float baseValue, SkillEffect plusA, SkillEffect plusB)
    {
        float flat = 0f, percent = 0f;
        foreach (var buff in Buffs)
        {
            if (buff.Has(plusA)) { flat += buff.Flat(plusA); percent += buff.Percent(plusA); }
            if (buff.Has(plusB)) { flat += buff.Flat(plusB); percent += buff.Percent(plusB); }
        }
        return Math.Max(0f, (baseValue + flat) * (1f + percent));
    }

    public float EffectiveAttack =>
        AdminStat("patk") ?? AtkDebuffed(ModifiedStatDual(AttackPower, SkillEffect.BuffAtk, SkillEffect.BuffPhysAtk));

    /// <summary>Buffed magic attack (mAtk), the INTERNAL value — feeds the √ magic-damage/heal formulas
    /// (unchanged; mobs share the same formulas). Magic buffs (shared BuffAtk + magic-only BuffMagAtk) are
    /// applied SQUARED here to cancel that √: a buff authored at its EFFECTIVE % (e.g. +32%) then yields
    /// exactly +32% damage AND +32% on the shrunk display — description = stat = damage. Physical BuffAtk
    /// stays linear on its own channel (EffectiveAttack); only this magic read squares. Owner 2026-07-16.</summary>
    public float EffectiveMagicAttack
    {
        get
        {
            // `/stat matk` forces the INTERNAL value, which is the one the √ formulas read — forcing the
            // shown number instead would move the display and leave the damage alone.
            if (AdminStat("matk") is float forced) return forced;
            // Magic reads ONLY magic-only buffs (BuffMagAtk), applied SQUARED so the stored % is the HONEST
            // effective % (the square cancels the √ in the damage formula). The shared BuffAtk is PHYSICAL-
            // ONLY now — a buff that should boost magic must carry an explicit BuffMagAtk. Only the base
            // M.Atk goes through the √; every modifier is honest (stored = description = effect). Owner 2026-07-16.
            float magFlat = 0f, magPct = 0f;
            foreach (var buff in Buffs)
                if (buff.Has(SkillEffect.BuffMagAtk)) { magFlat += buff.Flat(SkillEffect.BuffMagAtk); magPct += buff.Percent(SkillEffect.BuffMagAtk); }
            // The untrained-weapon penalty (Weapon Proficiency) collapses magic to ×0.05. It lives in the
            // factor (squared alongside the √) so it hits the shown M.Atk, damage AND heals identically.
            float magFactor = (1f + magPct) * MagicWeaponPenaltyMult;
            return AtkDebuffed(Math.Max(0f, (MagicAttack + magFlat) * magFactor * magFactor));
        }
    }

    /// <summary>The DISPLAYED M.Atk. Combat (damage + heals) always uses the INTERNAL EffectiveMagicAttack
    /// with a √; this shrinks only what the player SEES. Path B, refined by the owner 2026-07-25:
    ///
    ///   shown = min(internal, scale·√internal)   (scale = 20)
    ///
    /// i.e. show the HONEST raw internal until it outgrows the shrink, then switch to scale·√internal. The
    /// two are equal at internal = 400 (20·√400 = 400), so the switch is continuous. Below it a small
    /// M.Atk reads as itself (a level-1 wand mage shows ~its real ~13, a fighter's stays tiny); only a
    /// high-M.Atk geared caster — internal past 400, ~level 30-40 — crosses into the shrink that keeps the
    /// endgame number from going cosmic. This makes the display STAT-driven, not level-driven: fighters
    /// stay low on their own because their M.Atk never reaches the crossover. A squared magic buff still
    /// shows its honest % in the shrink regime; in the raw regime it shows squared, which is fine — that
    /// band is the low numbers nobody buffs around. Damage is untouched either way.</summary>
    public float EffectiveMagicAttackShown
    {
        get
        {
            float internalMAtk = Math.Max(0f, EffectiveMagicAttack);
            float shrunk = StatCalculator.MagicAttackDisplayScale * MathF.Sqrt(internalMAtk);
            return MathF.Min(internalMAtk, shrunk);
        }
    }

    /// <summary>Is this a weapon a mage is TRAINED with (sword or blunt — wands/staves are blunt)? An
    /// untrained weapon (bow/dual/bare hands) triggers the Weapon Proficiency cast-speed penalty.</summary>
    private static bool IsMageTrainedWeapon(WeaponType w) =>
        (w & (WeaponType.AnySword | WeaponType.AnyBlunt)) != 0;

    /// <summary>How much of their magic a trained caster keeps while holding a NON-magic weapon (a mace
    /// rather than a wand). Replaces the old per-item MAtkFactor 0.6: the number is the same order, but
    /// it now lives on the CLASS rule instead of on every weapon, so a fighter picking up a wand is not
    /// silently penalised for a caster's problem. Retune here, not in the item catalogue.</summary>
    public const float NonMagicWeaponMagicMult = 0.6f;

    /// <summary>Buffed attack power for BASIC attacks (archetype-scaled). Basic attacks
    /// are physical, so they take the shared BuffAtk plus physical-only BuffPhysAtk.</summary>
    public float EffectiveBasicAttack => AtkDebuffed(ModifiedStatDual(BasicAttackPower, SkillEffect.BuffAtk, SkillEffect.BuffPhysAtk));

    /// <summary>Apply DebuffAtk (e.g. venom) as a multiplicative reduction to an attack value.</summary>
    private float AtkDebuffed(float v)
    {
        float pct = 0f;
        foreach (var b in Buffs) if (b.Has(SkillEffect.DebuffAtk)) pct += b.Percent(SkillEffect.DebuffAtk);
        return Math.Max(0f, v * (1f - pct));
    }

    /// <summary>Move speed including move-speed buffs (flat + percent).</summary>
    /// <summary>Current move speed: 0 if sitting or standing up, walk or run base
    /// by state, plus move-speed buffs, clamped to the (raisable) move cap.</summary>
    public float EffectiveSpeed
    {
        get
        {
            if (IsRooted || IsStunned) return 0f;   // held in place by Root or Stun

            if (AdminMoveSpeed is float adminSpeed) return adminSpeed;   // /spd m: uncapped

            if (Kind == EntityKind.Mob)
            {
                // Mobs walk while wandering, run while aggroed/engaged.
                float mobBase = Engaged ? RunSpeed : WalkSpeed;
                if (mobBase <= 0) mobBase = Speed;
                return ModifiedStat(mobBase, SkillEffect.BuffMoveSpeed) * (1f - SlowFraction);
            }

            // Only SITTING freezes movement. The stand-up recovery (StandUpTicks) gates ACTIONS
            // (attack/cast) but must NOT zero move speed: zeroing it made the client predict a walk at 0
            // while the server held you, which is the "standing rubber-bands me" bug. You can walk the
            // instant you stand; you just can't attack/cast until the recovery elapses.
            if (MoveState == MoveState.Sitting)
                return 0f;
            float baseSpeed = MoveState == MoveState.Walking ? WalkSpeed : RunSpeed;
            if (baseSpeed <= 0) baseSpeed = Speed;   // fallback
            float withBuffs = ModifiedStat(baseSpeed, SkillEffect.BuffMoveSpeed) * (1f - SlowFraction);
            return Math.Min(withBuffs, MoveSpeedCap);
        }
    }

    /// <summary>Total move-speed reduction from Slow debuffs (summed Percent of the Slow
    /// effect), clamped to 90% so a slow never fully stops you (that's Root's job).</summary>
    private float SlowFraction
    {
        get
        {
            float pct = 0f;
            foreach (var b in Buffs) if (b.Has(SkillEffect.Slow)) pct += b.Percent(SkillEffect.Slow);
            return Math.Clamp(pct, 0f, 0.9f);
        }
    }

    /// <summary>Defence including BuffDef (adds) and DebuffDef (subtracts).</summary>
    public float EffectiveDefence =>
        AdminStat("pdef") ?? ModifiedStat(Defence + ShieldDefense, SkillEffect.BuffDef, SkillEffect.DebuffDef);

    /// <summary>Magic defence — the divisor for incoming magic damage. Separate
    /// channel from physical defence; sourced from level base + jewels + the Tank
    /// "Anti Magic" passive, plus any BuffMagicDef (e.g. Warchanter's chant).</summary>
    public float EffectiveMagicDefence => AdminStat("mdef") ?? ModifiedStat(MagicDefence, SkillEffect.BuffMagicDef);

    /// <summary>Evasion including evasion buffs (flat + percent).</summary>
    public float EffectiveEvasion => AdminStat("eva") ?? ModifiedStat(Evasion, SkillEffect.BuffEvasion);

    /// <summary>Weapon's base attack speed stat (333 = normal). Set from the equipped
    /// weapon type in RecomputeDerived. (Cast speed uses class base × weapon factor
    /// directly in EffectiveCastSpeedMultiplier, so it needs no stored field.)</summary>
    public int WeaponAttackBase { get; set; } = StatCalculator.SpeedBaseline;

    /// <summary>Cast-time multiplier (lower = faster). WIT-driven stat (IG-style
    /// 333 = 1.0x), then skill cast-speed buffs shorten it further.</summary>
    public float EffectiveCastSpeedMultiplier
    {
        get
        {
            // /spd c: an exact stat, bypassing formula and cap alike.
            if (AdminCastSpeed is float adminCast)
                return StatCalculator.SpeedBaseline / Math.Max(1f, adminCast);

            // Authentic IG: castSpd = classBase × witModifier × weaponFactor
            //   × gearFactor × ∏(1 + buff%), then time = 333 / castSpd (cap 1999 = 6×).
            // witModifier is EXPONENTIAL (×1.63 per +10 WIT). gearFactor = robe mastery /
            // attributes / passives (CastSpeedMultiplier is their combined TIME multiplier,
            // <1 = faster, so 1/it = speed factor: robe ≈ ×1.4, non-robe ≈ ×0.5). Buffs
            // STACK MULTIPLICATIVELY, matching IG.
            float baseCast = StatCalculator.ClassBaseCastSpeed(Race, BaseClass)
                             * StatCalculator.WeaponCastFactor(WeaponType);
            float witMod = StatCalculator.CastWitModifier(EffectiveWit);
            float gearFactor = 1f / Math.Max(0.05f, CastSpeedMultiplier);
            float buffMult = 1f;
            foreach (var buff in Buffs)
            {
                if (buff.Has(SkillEffect.BuffCastSpeed)) buffMult *= 1f + buff.Percent(SkillEffect.BuffCastSpeed);
                if (buff.Has(SkillEffect.DebuffCastSpeed)) buffMult *= 1f - buff.Percent(SkillEffect.DebuffCastSpeed);
            }

            // The spell rune-style flat bonus is ADDED to the finished stat, not folded into
            // the chain — that's what keeps it from compounding with WIT/gear/buffs.
            float castSpd = baseCast * witMod * gearFactor * buffMult * CastSpeedPenaltyMult + CastSpeedFlatBonus;
            castSpd = Math.Clamp(castSpd, 30f, StatCaps.CastSpeed);
            return StatCalculator.SpeedBaseline / castSpd;   // time multiplier (lower = faster)
        }
    }

    /// <summary>Attack-interval multiplier (lower = faster). AGI-driven stat,
    /// then attack-speed buffs shorten it further.</summary>
    public float EffectiveAttackSpeedMultiplier
    {
        get
        {
            // /spd a: an exact stat, bypassing formula and cap alike.
            if (AdminAttackSpeed is float adminAtk)
                return StatCalculator.SpeedBaseline / Math.Max(1f, adminAtk);

            // Authentic IG: atkSpd = weaponBase × agiModifier × gearFactor × ∏(1+buff%),
            // cap 1500. agiModifier is EXPONENTIAL (baseline 30 AGI = 1.0). Buffs stack
            // multiplicatively (matching cast speed).
            float agiFactor = StatCalculator.AttackAgiModifier(EffectiveAgi);
            float gearFactor = 1f / Math.Max(0.05f, AttackSpeedMultiplier);
            float buffMult = 1f;
            foreach (var buff in Buffs)
            {
                if (buff.Has(SkillEffect.BuffAtkSpeed)) buffMult *= 1f + buff.Percent(SkillEffect.BuffAtkSpeed);
                if (buff.Has(SkillEffect.DebuffAtkSpeed)) buffMult *= 1f - buff.Percent(SkillEffect.DebuffAtkSpeed);
            }

            float atkSpd = WeaponAttackBase * agiFactor * gearFactor * buffMult;
            atkSpd = Math.Clamp(atkSpd, 30f, StatCaps.AttackSpeed);
            return StatCalculator.SpeedBaseline / atkSpd;    // time multiplier (lower = faster)
        }
    }

    // ----- Combat / skill state ----------------------------------------------------------

    public Guid? CombatTargetId { get; set; }
    public bool Engaged { get; set; }
    public int AttackCooldown { get; set; }

    /// <summary>The entity the PLAYER explicitly ordered a basic attack on (second tap, the Attack
    /// action, the target frame's Attack, assist). NOT set by anything the game decides for you.
    ///
    /// It exists because the owner's rule is that nothing ever walks you into melee unless you asked
    /// for it, and a cast wipes <see cref="Engaged"/> — so after the cast the server has to know
    /// whether the melee it would resume was YOUR order or its own idea. Survives a cast; cleared by
    /// a manual move, a follow, disengaging and death. Runtime only.</summary>
    public Guid? AttackCommandTargetId { get; set; }

    /// <summary>FOLLOW: while set, the player is walked toward this entity each tick (auto-repath as it
    /// moves), stopping a short distance away. Cleared by a manual move, attacking, death, or the target
    /// leaving view. Runtime only.</summary>
    public Guid? FollowTargetId { get; set; }

    /// <summary>Threat/aggro table (mobs): attacker entity id → accumulated threat. The mob
    /// targets the highest-threat entity. Taunt spikes it; detaunt drops it.</summary>
    public Dictionary<Guid, float> Threat { get; } = new();

    /// <summary>Has this mob already called its social clan in for this fight (BL-70)?
    ///
    /// The cry fires ONCE, on the first damage it takes, and not again — otherwise every tick of a
    /// DoT would re-scan the grid for clanmates. Cleared when the pull is over (ResetMob) and on
    /// respawn, so the same camp answers the next player who starts something. Runtime only.</summary>
    public bool CriedForHelp { get; set; }

    /// <summary>DAMAGE ledger (mobs): attacker entity id → total damage actually dealt to this mob.
    ///
    /// Deliberately SEPARATE from <see cref="Threat"/>. Threat is a targeting signal that taunt and
    /// detaunt move around on purpose, so it says who the mob wants to hit — not who earned the kill.
    /// Rewards are owed to damage, so they read this instead: the top damager is the "killer" for drops
    /// and, on a contested kill, exp is split by each side's share of the total.
    ///
    /// Cleared on spawn and on reset (a mob that leashed home and re-healed owes nobody anything).</summary>
    public Dictionary<Guid, long> DamageLog { get; } = new();

    /// <summary>Who landed the FINAL blow. Not what rewards are paid on (that is the damage ledger) —
    /// kept because raid/epic bosses want a last-hit counter of their own (owner).</summary>
    public Guid? LastHitterId { get; set; }
    /// <summary>While &gt; 0 a taunt locks the mob onto its taunter (ignores threat retargeting).</summary>
    public int TauntLockTicks { get; set; }

    // ===== INVISIBILITY, IN THREE KINDS (BL-69) ==========================================
    //
    // The owner's spec is explicit that these share a word and nothing else, so they are three
    // separate pieces of state rather than one flag with modes. The distinctions that matter:
    //
    //   HIDE  (HideTicks)      — full. Nobody renders you, nobody can target you, mobs shed their
    //                            aggro. ANYTHING but movement ends it: a hit, a skill, a potion,
    //                            damage taken. A rogue's opener.
    //   STEALTH (a BUFF)       — vs UNAGGROED mobs only. Players still see you and can target you;
    //                            mobs already chasing keep chasing. It does not break when you act —
    //                            only when you stop it. *"toggle-on makes the rogues farm in
    //                            peacefull zones."* Lives on the buff, so removing the buff by any
    //                            route (toggle off, double-click, dispel, expiry, death) ends it.
    //   ADMIN (AdminInvisible) — absolute. No reveal, no AoE and no skill use touches it; it goes
    //                            off only by typing the command again. Not even other staff see it.

    /// <summary>While &gt; 0 the entity is HIDDEN: unrendered, untargetable, and shed by mob aggro.
    /// Decremented each tick and cleared by any action at all except movement.</summary>
    public int HideTicks { get; set; }

    /// <summary>While &gt; 0 this entity CANNOT re-hide — the archer's reveal debuff. Independent of
    /// <see cref="HideTicks"/>: the reveal both ends a hide and bars the next one.</summary>
    public int NoHideTicks { get; set; }

    /// <summary>Admin <c>/invis</c>. Absolute and manual-only — nothing in the simulation clears it.</summary>
    public bool AdminInvisible { get; set; }

    /// <summary>Cached "some buff of mine hides me from unaggroed mobs" — recomputed in
    /// <see cref="RecomputeDerived"/>, which already runs on every buff add, removal and expiry.
    /// Cached because the mob aggro scan asks this about every candidate it considers.</summary>
    public bool StealthFromBuffs { get; set; }

    /// <summary>Unseen by players AND mobs — the two absolute kinds.</summary>
    public bool Hidden => HideTicks > 0 || AdminInvisible;

    /// <summary>Should an UNAGGROED mob decline to start on this entity? True for every kind: a full
    /// hide implies it. This is the predicate the mob AI's aggro scan reads, and it deliberately says
    /// nothing about mobs already in the fight — that difference is the whole point of stealth.</summary>
    public bool Stealthed => Hidden || StealthFromBuffs;

    public string? QueuedSkillId { get; set; }
    public Guid? QueuedTargetId { get; set; }

    public string? CastingSkillId { get; set; }
    public Guid? CastTargetId { get; set; }
    public int CastTicksRemaining { get; set; }

    /// <summary>MP already charged for the in-progress cast (the initial portion),
    /// so we know what was spent if it's interrupted/cancelled and what remains
    /// to charge on completion.</summary>
    public int CastInitialMpPaid { get; set; }

    /// <summary>The inventory item that STARTED this cast, for a consumable with a cast time (a buff
    /// scroll). One unit of it is taken when the cast lands, and nothing is taken if it is interrupted
    /// — which is the whole reason the item is not consumed up front.
    ///
    /// It exists because the older mechanism keyed on the SKILL's <c>ConsumableId</c>, i.e. the skill
    /// had to name its own item. The Return/Resurrection scrolls do; the 48 buff scrolls never did, so
    /// they read for free, for ever. Naming the instance instead means every present and future
    /// channelled consumable is charged for without authoring anything.</summary>
    public Guid? CastFromItemInstance { get; set; }

    public Dictionary<string, int> SkillCooldowns { get; } = new();

    // ----- Auto-hunt / idle farming config (docs/design/AutoHunt.md) -------------------
    public bool AutoHuntEnabled { get; set; }
    public int AutoHpPotionPct { get; set; }
    public int AutoMpPotionPct { get; set; }
    public bool AutoBuffPotions { get; set; }
    public List<AutoSkillDto> AutoSkills { get; } = new();
    public List<string> AutoBuffPotionIds { get; } = new();
    // The auto-potions Potions tab: per-potion on/off + HP% threshold (empty = use AutoHpPotionPct).
    public List<AutoPotionDto> AutoHealPotions { get; } = new();
    // The auto-potions BUFFS tab: one line per buff family (empty = fall back to AutoBuffPotions +
    // AutoBuffPotionIds, the pre-BL-04 behaviour, so an existing character keeps what it had).
    public List<AutoBuffDto> AutoBuffs { get; } = new();

    /// <summary>The region the player was last known to be in, for the "you entered X" notice — only
    /// pushed when this changes. "" = not yet computed / in the wild between regions.</summary>
    public string CurrentRegionId { get; set; } = "";

    /// <summary>Ticks online THIS session (reset on enter), for the "take a break" reminder every 3h.</summary>
    public long SessionOnlineTicks { get; set; }

    /// <summary>Total seconds this character has spent online, persisted — for the online-time leaderboard.</summary>
    public long TotalOnlineSeconds { get; set; }

    /// <summary>Three saved equipment loadouts (A/B/C), each a list of equipped item INSTANCE ids.
    /// Save snapshots what's worn; apply unequips all then re-equips these (skipping any that were
    /// sold/traded/destroyed). Persisted per character. Index 0=A, 1=B, 2=C.</summary>
    public List<Guid>[] EquipPresets { get; } = { new(), new(), new() };
    // Roaming config (docs/design/AutoHunt.md roaming spec).
    public int AutoFarmRange { get; set; } = 1000;
    public bool AutoFarmStatic { get; set; }             // false = roam, true = fixed circle at start
    public bool AutoAttackNormal { get; set; } = true;
    public bool AutoAttackElite { get; set; }
    public bool AutoAttackBoss { get; set; }
    /// <summary>Static-spot centre (the position auto-hunt was last enabled at).</summary>
    public float FarmCenterX { get; set; }
    public float FarmCenterY { get; set; }
    /// <summary>Per-skill earliest auto-recast tick (base reuse + the user's extra delay).</summary>
    public Dictionary<string, long> AutoReadyTick { get; } = new();

    // ----- Auto-hunt skill chains (playtest-15 design #1) -----
    /// <summary>Cyclic chain order: carry on from the last skill used instead of restarting at the top
    /// of the bar every time. See AutoChainPick.</summary>
    public bool AutoCyclic { get; set; }
    /// <summary>HP% under which the auto-HEAL chain runs (0 = never, 100 = heal on cooldown).</summary>
    public int AutoHealPct { get; set; } = 70;
    /// <summary>MP% under which the auto-MPHEAL chain runs (0 = never). The sibling of
    /// <see cref="AutoHealPct"/> and the knob half of <c>BL-67</c>: it used to be a hardcoded 60,
    /// on the argument that a second slider for a one-class skill was UI for nothing. He asked for
    /// the slider (*"an MP bar threshold beside the HP one"*), so 60 survives only as the default.</summary>
    public int AutoMpPct { get; set; } = 60;
    /// <summary>Assist-only: attack what the party leader attacks, and nothing else.</summary>
    public bool AutoAssistLeader { get; set; }
    /// <summary>Cyclic cursor per priority group (index into AutoSkills of the LAST one cast, +1).
    /// Indexed by <c>(int)AutoSkillKind</c>; reset whenever the config changes.
    /// ⚠ Sized to the enum — <c>MpHeal</c> (BL-67) made it 6.</summary>
    public int[] AutoChainCursor { get; } = new int[7];
    /// <summary>Disconnected but still auto-hunting in the world (no connection = no UI pushes).</summary>
    public bool IsOfflineFarming { get; set; }
    /// <summary>Link-dead grace: connection lost while out of combat + not auto-farming. Frozen in
    /// the world (with a "Disconnected" title) for a short window so a reconnect resumes seamlessly.</summary>
    public bool IsDisconnected { get; set; }
    /// <summary>Remaining ticks of the disconnect grace before the normal removal chain runs.</summary>
    public int DisconnectGraceTicks { get; set; }
    /// <summary>Set when this character DIED while offline-farming / link-dead. Persisted; makes the next
    /// login land DEAD (res prompt) instead of healed — anti-exploit. Cleared on respawn.</summary>
    public bool DiedWhileAway { get; set; }
    /// <summary>Exp lost on the most recent death (the 5% death penalty). A resurrection skill/scroll can
    /// restore a fraction of it; a normal town respawn discards it. Runtime only. Cleared on respawn/res.</summary>
    public long LostExp { get; set; }
    /// <summary>A pending resurrection OFFER: the reviving entity, the fraction of lost exp it would restore,
    /// and ticks until it auto-expires. The dead player must ACCEPT before reviving, so they don't stand up
    /// on top of the mob that killed them. Cleared on accept/decline/expire/respawn/revive. Runtime only.</summary>
    public Guid? PendingResFromId { get; set; }
    public float PendingResExpPct { get; set; }
    public int PendingResTicks { get; set; }
    /// <summary>Tick of the last damage dealt or taken — drives the 30s combat-state decay.</summary>
    public long LastCombatTick { get; set; }

    // ----- PvP -----
    /// <summary>Opt-in: my attacks/skills can target and damage other players (outside safe zones).</summary>
    public bool PvpEnabled { get; set; }
    /// <summary>Auto-retaliate against a player who attacks me while I'm auto-hunting / offline.</summary>
    public bool CounterAttack { get; set; }
    /// <summary>The last player who damaged me — the counter-attack retaliation target.</summary>
    public Guid? LastPvpAttackerId { get; set; }

    /// <summary>The autopilot target last PUSHED to this player's client, so the push happens on a
    /// change instead of every tick. Runtime-only; never persisted.</summary>
    public Guid? SentAutoTargetId { get; set; }

    // ----- Pending CLASS CHANGE (`BL-36`) --------------------------------------------------------
    // A swap started outside a town takes GameConstants.SubclassSwapDelaySeconds; one started inside a
    // town or peace zone happens on the spot and never touches these.
    //
    // ⚠ Runtime-only, NOT persisted, and that is deliberate: a class change is a live-session act. If
    // it survived a logout the character would come back as a class the player never saw themselves
    // become — and the swap wipes buffs, cast state and target, none of which mean anything across a
    // relog anyway. Quitting mid-count simply abandons the change; ask again.

    /// <summary>The subclass Slot this character is in the middle of changing to, or -1 for none.</summary>
    public int PendingSubclassSlot { get; set; } = -1;

    /// <summary>Ticks left before <see cref="PendingSubclassSlot"/> takes effect. 🔑 It keeps counting
    /// wherever the character walks, INCLUDING into a town: his rule is that the city neither cancels
    /// nor shortcuts a running timer, only that it never starts one.</summary>
    public int SubclassSwapTicks { get; set; }
    /// <summary>Purple flag: I recently attacked another player and am freely attackable until this
    /// tick (killing me = a PvP kill, not a PK). Refreshed on each PvP action.</summary>
    public long PvpFlagUntilTick { get; set; }
    /// <summary>PK karma. &gt;0 = red name; others attack me without flagging; each of my deaths
    /// lowers it, and at 0 the red flag clears (persisted).</summary>
    public int Karma { get; set; }
    /// <summary>Total innocent kills (PK count) + justified/flagged kills (PvP count). Persisted.</summary>
    public int PkCount { get; set; }
    public int PvpCount { get; set; }
    /// <summary>Consecutive PKs (drives the karma growth); resets when karma redeems to 0.</summary>
    public int ConsecutivePk { get; set; }
    /// <summary>Cached name-flag for the snapshot DTO (recomputed each tick from Karma + flag window).</summary>
    public PvpFlag FlagState { get; set; }
    // The old AutoHuntLocked / AutoIdleElapsedTicks / AutoOfflineElapsedTicks lived here and WERE the
    // defect: per-SESSION counters on the CHARACTER, zeroed at every login. The allowance is a
    // per-ACCOUNT daily balance now — see AccountFarmBudget and World.AccountBudgets.
    /// <summary>Seconds of offline budget left, stamped by the game loop each tick while offline
    /// farming (-1 = uncapped). It exists so the HUB can show it on the character screen without
    /// computing anything: the loop is still the only writer, the hub only reads the last value.</summary>
    public int OfflineSecondsLeft { get; set; } = -1;

    // ----- Potion channel -------------------------------------------------------------
    /// <summary>Shared cooldown across all HEALING potions, in ticks. This is all that's left of
    /// the old potion channel: a potion's lingering effect is now an ordinary BUFF (its skill's),
    /// so TickBuffs/TickHealOverTime run it and the buff bar shows it. The bespoke
    /// PotionRarity / PotionHealPercentPerSecond / PotionEffectTicks / PotionEffectName state is
    /// gone — BuffKey + Rank already express "a stronger potion cancels a weaker one".</summary>
    /// <summary>Drink cooldown PER POTION, keyed by item DefId. A potion shares a cooldown only with
    /// ITSELF (owner, 2026-07-23), so common/uncommon/rare/instant each cool down independently. Ticks
    /// down each tick; absent or ≤0 = ready. Runtime-only.</summary>
    public Dictionary<string, int> PotionCooldowns { get; } = new();

    public bool Dead { get; set; }

    // ----- Mob-only state ----------------------------------------------------------------

    public float HomeX { get; set; }
    public float HomeY { get; set; }

    /// <summary>Spawn zone this mob belongs to (for zone-managed respawn).</summary>
    public string? ZoneId { get; set; }

    /// <summary>Mob template id (MobCatalog) — for drops + quest kill matching.</summary>
    public string? MobTypeId { get; set; }

    /// <summary>Set when this mob came from one of its zone's per-template <see cref="DedicatedSpawn"/>s
    /// (the value is that template id); null for an ordinary mixed-roster spawn.
    ///
    /// The spawner has to be RECORDED rather than worked out from <see cref="MobTypeId"/> at death:
    /// a mixed roll can legitimately produce a template that also has a dedicated spawner, and
    /// crediting that death to the wrong bucket is what would let a guaranteed quest population drift.
    /// Runtime only — mobs are never persisted.</summary>
    public string? SpawnerMobId { get; set; }
    /// <summary>Training dummy: immortal (GodMode), stationary, never attacks/aggroes.</summary>
    public bool TrainingDummy { get; set; }

    /// <summary>What this dummy hits BACK with, if anything (owner, playtest-20 `56c`). None for the
    /// ordinary target dummies, which stay silent. See GameLoopService.StrikeFromDummy.</summary>
    public DummyAttack DummyStrikes { get; set; } = DummyAttack.None;
    /// <summary>The dummy's flat pool and regen. It takes damage so you can read the numbers, and
    /// never dies (ApplyDamage floors it at 1 HP); the regen tops it back up between tests.</summary>
    public const int TrainingDummyHp = 1_000_000;
    public const float TrainingDummyRegen = 10_000f;   // ~10k HP/sec — it is never "engaged", so regen runs

    /// <summary>The mob's PERMANENT stat multipliers — the zone rank and its MobMod passives, composed
    /// at spawn and re-applied at the end of every RecomputeDerived by ApplyMobScale. 1 = no change.
    ///
    /// ⚠ These live on the entity rather than being applied once at spawn precisely because
    /// RecomputeDerived rebuilds a mob's stats from the level curve alone: any buff, debuff or mod
    /// change re-ran it and erased a rank that had been multiplied in afterwards (playtest-20 #7).
    /// Set them in BuildMob and never multiply a mob's MaxHp/attack/defence in place again.</summary>
    public float MobHpScale { get; set; } = 1f;
    public float MobPAtkScale { get; set; } = 1f;
    public float MobMAtkScale { get; set; } = 1f;
    /// <summary>Rank accuracy is FLAT and lands after the template's Accuracy multiplier, so a boss
    /// gets its +20 whole rather than scaled by whatever passive the template happens to carry.</summary>
    public int MobAccFlat { get; set; }
    /// <summary>Caster mob (Mage role): no basic attack — casts the mob spells gated on MP;
    /// out of MP it stands helpless. Set at spawn from MobType.Role.</summary>
    public bool CasterMob { get; set; }

    /// <summary>BL-47 step 2 — this creature's SIX stat bases come from the player formulas (core stats
    /// × the class level curve × the gear it wears) instead of MobBaseStats' authored curve. Set at
    /// spawn from <see cref="MobType.Build"/>; false for every ordinary mob, which is unchanged.
    ///
    /// <para>⚠ It moves the STAT DERIVATION and nothing else. This is still a Mob to the whole rest of
    /// the game — aggro, drops, targeting, the client's plate, PvP, party — and it deliberately does NOT
    /// take the player-only branches around it: no armor SETS (a set bonus is player identity and would
    /// arrive as hidden stats), no learned-passive main-stat loop, no armor-weight masteries, no
    /// race+class speed override (a creature's speed is its template's, so it can still be kited), and
    /// no grade penalty (it wears UNDER-grade gear by design, so the gap is 0 either way). The rank and
    /// <see cref="MobMod"/> multipliers still land on top in ApplyMobScale — that is the whole design:
    /// gear gets you most of the way and the passive carries the remainder.</para></summary>
    public bool PlayerBuilt { get; set; }
    public MobRank Rank { get; set; }
    public bool Aggressive { get; set; }

    /// <summary>Boss combat mechanics. CombatTicks counts how long the boss has been engaged
    /// (drives the enrage timer); Enraged latches once it enrages (so the buff applies once);
    /// BossSkillTicks is the reuse counter for its special skill. Reset when the boss resets.</summary>
    public int CombatTicks { get; set; }
    public bool Enraged { get; set; }
    public int BossSkillCooldown { get; set; }
    /// <summary>How many BossProfile phases have already fired (HP-threshold script cursor).</summary>
    public int BossPhaseIndex { get; set; }
    public int WanderTicks { get; set; }
    public int RespawnTicks { get; set; }

    /// <summary>Interest-management cell. Maintained by CellGrid.</summary>
    public (int Cx, int Cy) Cell { get; set; }

    /// <summary>The COMPLETED armor set the player is wearing (a worn BODY whose SetId matches a set,
    /// plus that set's required accessory slots), or null. A body variant + the tier's shared accessory
    /// line completes the set. Used by RecomputeDerived's pre-pass (primary stats) + set-bonus block.</summary>
    private ArmorSetDef? DetectActiveSet()
    {
        if (Kind != EntityKind.Player) return null;
        string bodySet = "", headSet = "", glovesSet = "", bootsSet = "";
        foreach (var item in Inventory)
        {
            if (!item.Equipped || ItemCatalog.Get(item.DefId) is not ItemDef sd
                || sd.Slot != EquipSlot.Armor || string.IsNullOrEmpty(sd.SetId))
                continue;
            switch (sd.ArmorSlot)
            {
                case ArmorSlot.Body: bodySet = sd.SetId; break;
                case ArmorSlot.Head: headSet = sd.SetId; break;
                case ArmorSlot.Gloves: glovesSet = sd.SetId; break;
                case ArmorSlot.Boots: bootsSet = sd.SetId; break;
            }
        }
        foreach (var set in ArmorSetCatalog.All)
        {
            string accId = string.IsNullOrEmpty(set.AccessorySetId) ? set.Id : set.AccessorySetId;
            var required = set.RequiredSlots ?? ArmorSetCatalog.DefaultSlots;
            bool complete = true;
            foreach (var slot in required)
            {
                string worn = slot switch
                {
                    ArmorSlot.Body => bodySet,
                    ArmorSlot.Head => headSet,
                    ArmorSlot.Gloves => glovesSet,
                    ArmorSlot.Boots => bootsSet,
                    _ => ""
                };
                string need = slot == ArmorSlot.Body ? set.Id : accId;
                if (worn != need) { complete = false; break; }
            }
            if (complete) return set;
        }
        return null;
    }

    /// <summary>BL-47 — the stat block a player-built creature spawns with: a real player base block,
    /// plus his ±5 race lean.
    ///
    /// <para>The lean is a FLAT offset with no level curve, which is his `B1` ruling verbatim
    /// (*"ork have higher con/atk less agi ..while elf have higher agi less atk/con ... No lvl curve.
    /// Can go +-5 same as the swap passives"*). ⚠ Worth being clear-eyed about what that buys: ±5 on a
    /// ~40-point stat is ±12.5%, against reconciliation multipliers of ×1.5-2.0. Race is FLAVOUR here —
    /// what separates a lich from a goblin is its class curve, its gear and its passives, and the lean
    /// is the seasoning on top. Keeping every demo creature on the same <c>StatBase</c> is what makes
    /// the lean the only thing that differs between two of them.</para></summary>
    public static StatCalculator.BaseStats PlayerBuiltStats(MobBuild b)
    {
        var s = StatCalculator.GetBaseStats(b.StatBase, b.Class);
        return new StatCalculator.BaseStats(
            Con: Math.Max(1, s.Con + b.Con),
            Atk: Math.Max(1, s.Atk + b.Atk),
            Wit: Math.Max(1, s.Wit + b.Wit),
            Agi: Math.Max(1, s.Agi + b.Agi),
            Spt: Math.Max(1, s.Spt + b.Spt));
    }

    /// <summary>BL-47 — give this creature its player identity and put its gear ON it. Call BEFORE the
    /// recompute: the equip loop inside <see cref="RecomputeDerived"/> is what turns worn gear into
    /// stats, so a piece added afterwards sits in the bag doing nothing until the next recompute
    /// happens to run.
    ///
    /// <para>🔑 The bag is HELD, never looted. A mob's loot is its DROP TABLE and nothing in the death
    /// path so much as looks at its inventory — which is the shape he asked for (*"not a dropped
    /// one..but just to hold stuff"*) and is why a War Rune can be handed to a creature at all.</para>
    ///
    /// <para>Shared with <c>tools/BalanceMatrix</c> so the measured creature and the spawned one are
    /// built by the same code. A tool that reproduces a construction by hand eventually measures a
    /// creature the server does not spawn.</para></summary>
    public void ApplyMobBuild(MobBuild build)
    {
        PlayerBuilt = true;
        Race = build.StatBase;
        BaseClass = build.Class;
        // The 2nd class is what gives the creature an Archetype and therefore an HP/MP class-level
        // curve — without it a player-built entity falls back to the classless curve and every HP
        // number is wrong. Below 20 there is no 2nd class to have, exactly as for a player.
        if (Level >= 20) SecondClass = build.SecondClass;

        var s = PlayerBuiltStats(build);
        Con = s.Con; AtkStat = s.Atk; Wit = s.Wit; Agi = s.Agi; Spt = s.Spt;

        foreach (var (defId, ench) in build.Pieces())
        {
            if (ItemCatalog.Get(defId) is not ItemDef piece) continue;   // ValidateBuilds fails the boot on a typo
            Inventory.Add(new InventoryItem
            {
                DefId = defId,
                // A rune is HELD, not worn — the rule for players too, and equipping it would do
                // nothing anyway: a rune's power is the buff it keeps up, not a stat line on the item.
                Equipped = piece.Slot != EquipSlot.Rune,
                Enchant = ench
            });
        }
    }

    /// <summary>Recomputes everything derived from core stats, level and
    /// equipped items. Call on creation, level-up, equip changes and class
    /// change.</summary>
    public void RecomputeDerived()
    {
        // STEALTH (BL-69, kind 2) is cached here because this method already runs on every buff add,
        // removal and expiry — so there is no second place for the flag to get out of step — and
        // because the mob aggro scan asks about it once per candidate per pass, which is far too
        // often to walk the buff list for.
        StealthFromBuffs = false;
        for (int i = 0; i < Buffs.Count; i++)
            if (Buffs[i].HidesFromMobs) { StealthFromBuffs = true; break; }

        // ----- Primary-stat PRE-PASS: fold main-stat deltas into the Bonus* stats BEFORE deriving
        // HP/MP/atk/eva/acc/crit, so "CON +3" actually raises HP, "AGI +1" actually raises
        // eva/acc/crit, and "ATK +5" actually raises P.Atk/M.Atk — not just the stat window.
        // TWO sources: the active armor set, and the level-40 STAT-SWAP passives. This has to run
        // here, not in the passive loop below, because that loop happens AFTER everything is derived.
        BonusStr = BonusAgi = BonusCon = BonusInt = BonusWit = BonusSpt = BonusAtk = 0;
        var activeSet = Kind == EntityKind.Player ? DetectActiveSet() : null;
        if (activeSet is not null)
        {
            var pm = activeSet.Mods;
            BonusStr = (int)pm.Str; BonusAgi = (int)pm.Agi; BonusCon = (int)pm.Con;
            BonusInt = (int)pm.Int; BonusWit = (int)pm.Wit; BonusSpt = (int)pm.Spt;
            // ARMOUR POWER LANDS ON ATK (owner ruling 2026-08-19: *"input the armor stats to the
            // effective stats"*). This engine has ONE power stat — ATK, which is STR for a fighter and
            // INT for a mage (StatCalculator.GetBaseStats) — so a set's `Str: 3` and `Int: 2` are the
            // same stat under two names, and until now both landed in BonusStr/BonusInt, which NOTHING
            // reads. Every armour set in the game has been carrying a dead offensive line.
            //
            // All three fold into BonusAtk: STR and INT because they ARE ATK here, and StatMods.Atk for
            // anything authored directly. A set never carries both Str and Int, so summing rather than
            // picking by class needs no branch — and a hybrid that did carry both would want both.
            //
            // ⚠ This raises P.Atk AND M.Atk on every set that authors Str/Int, because EffectiveAtk
            // multiplies the weapon in PhysicalAttackPower / MagicAttackStatScaled. It is a real damage
            // change, not a bookkeeping one — see the CHANGELOG for the measured size.
            BonusAtk = (int)(pm.Str + pm.Int + pm.Atk);
        }
        if (Kind == EntityKind.Player)
        {
            foreach (var (skillId, skillLevel) in LearnedSkills)
            {
                if (SkillCatalog.Get(skillId)?.PassiveAt(skillLevel) is not PassiveEffect pe) continue;
                BonusCon += pe.Con; BonusAgi += pe.Agi; BonusAtk += pe.Atk; BonusWit += pe.Wit; BonusSpt += pe.Spt;
            }
        }

        // Players derive from core stats + class curves; MOBS read the authored per-level
        // BASE curve (docs/data/mobs/mob_base_stats.csv) — the "level modifier" term of the mob
        // formula. CON/passives (MobMod, later masteries) and rank multipliers layer on top
        // in SpawnOneInZone. See MobBaseStats.
        //
        // BL-47 step 2: a PLAYER-BUILT creature takes the player side of all six of these — that is
        // what "built like a player" means and it is the only thing PlayerBuilt changes. See the field.
        bool playerStats = Kind == EntityKind.Player || PlayerBuilt;
        MaxHp = playerStats
            ? StatCalculator.MaxHp(Con + BonusCon, Level,
                StatCalculator.HpClassLevelModifier(BaseClass, Archetype),
                StatCalculator.Level1BaseHp(Race, BaseClass))
            : MobBaseStats.Hp(Level);
        MaxMp = playerStats
            ? StatCalculator.MaxMp(EffectiveSpt, Level,
                StatCalculator.MpClassLevelModifier(BaseClass, Archetype),
                StatCalculator.Level1BaseMp(BaseClass))
            : MobBaseStats.Mp(Level);
        // P.Atk is IG-MULTIPLICATIVE now: the WEAPON is the base, the ATK stat + level MULTIPLY it
        // (StatCalculator.PhysicalAttackPower, applied after the equip loop). So we DON'T seed a stat
        // base here — the weapon's own P.Atk accumulates in the loop, then the multiplier is applied.
        // Unarmed → fist only → feeble, no penalty branch. M.Atk keeps its additive base × levelMod²
        // (the signed-off magic balance) and is UNCHANGED.
        AttackPower = playerStats
            ? 0
            : MobBaseStats.PAtk(Level);
        // Player M.Atk is now WEAPON-based like P.Atk: seed 0 (no additive stat floor), let the equipped
        // weapon's M.Atk accumulate below, then multiply by the stat (StatCalculator.MagicAttackStatScaled)
        // and levelMod². The old additive seed (atkStat + level·2 + INT·3) is what made a level-1 mage read
        // ~40 M.Atk (IG: ~8); removed. INT-via-dye will return as a stat-mult input, not a flat add.
        MagicAttack = playerStats
            ? 0
            : MobBaseStats.MAtk(Level);
        // Defence (authentic IG): players use armor/jewel-driven base + level²/100, no CON
        // term. Mobs use their authored base curve (P.Def and M.Def separately).
        Defence = playerStats
            ? StatCalculator.PhysicalDefenceBase(Level)
            : MobBaseStats.PDef(Level);
        MagicDefence = playerStats
            ? StatCalculator.MagicDefenceBase(Level)   // tank magic identity = his Anti-Magic passive, not a level/2 mDef bonus
            : MobBaseStats.MDef(Level);
        // Resolution "sure" floors come from learned passives (Evasion Mastery / Precision),
        // applied in the passive loop below. Base 0 — the universal 5% land/avoid floor lives in
        // the resolver, not here. Magic has no floor: its defender lever is the ×MULTIPLIER below,
        // whose neutral value is 1, not 0.
        MagicFailMod = 1f;
        MagicFailBonus = 0f;   // "magic evasion" points, buffs only (see the field)
        EvadeFloor = 0f;
        HitFloor = 0f;
        SkillEvadeChance = 0f;
        PhysSkillReflectChance = 0f;
        PhysSkillReflectPct = 0f;
        DebuffReflectChance = 0f;
        Immune = false;
        CooldownReduction = 0f;
        CritRateResist = 0f;
        CritRateMult = 1f;
        CritRateFlat = 0f;
        MagicCritRateMult = 1f;
        MagicCritRateFlat = 0f;
        MagicCritDamageMult = 1f;
        MagicCritDamageResist = 0f;
        CritDmgResist = 0f;
        BowResist = 0f;
        CcResist = 0f;
        CcResistMagical = 0f;
        CcResistPhysical = 0f;
        PierceDefCoef = 1f;
        BluntDefCoef = 1f;
        BowDefCoef = 1f;
        RestoreMpMod = 1f;
        MagicResist = 0f;
        UntrainedCasterWeapon = false;
        MeleeVamp = 0f;
        SpellVamp = 0f;
        MeleeReflect = 0f;
        PhysMpCostReduction = 0f;
        MagicMpCostReduction = 0f;
        PveSkillDamageBonus = 0f;
        PveMagicDamageBonus = 0f;
        PveBasicDamageBonus = 0f;
        PvpSkillDamageBonus = 0f;
        PvpMagicDamageBonus = 0f;
        PvpBasicDamageBonus = 0f;
        PvpDamageTaken = 1f;
        CancelResist = 0f;
        Accuracy = StatCalculator.Accuracy(EffectiveAgi, Level);
        Evasion = StatCalculator.Evasion(EffectiveAgi, Level);
        // Physical crit is set at the WEAPON step below (it multiplies the character base), not here.
        CritChance = 0f;
        // Magic crit has no weapon step (it is WIT + buffs only), so unlike CritChance it can be
        // seeded here. The mult/flat accumulators above then carry it to the single fold at the end.
        MagicCritChance = StatCalculator.MagicCritBase((int)EffectiveWit);
        InterruptResist = StatCalculator.InterruptResist(EffectiveWit, Level);
        MagicInterruptBonus = StatCalculator.MagicInterruptPower(EffectiveWit);
        BasicAttackInterruptPower = 0;   // rogue "cancel on basic" is now a 3rd-class discipline passive (anti-magic rogue), not a base-rogue trait
        BasicAttackRange = GameConstants.MeleeRange;
        // A mob's innate weapon (claws/club/blade/bow) seeds this; an equipped weapon overwrites it
        // below. Players have InnateWeaponType None, so their behaviour is unchanged.
        WeaponType = InnateWeaponType;
        // Base run speed: players from race+class table, mobs from their spawn-set
        // RunSpeed. Gear/buffs raise it below; EffectiveSpeed clamps to the cap.
        if (Kind == EntityKind.Player)
        {
            RunSpeed = SpeedTable.BaseRunSpeed(Race, BaseClass);
            WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor;
        }
        Speed = Kind == EntityKind.Player ? RunSpeed : (RunSpeed > 0 ? RunSpeed : Speed);
        CastSpeedMultiplier = 1f;
        AttackSpeedMultiplier = 1f;
        CastSpeedFlatBonus = 0f;
        CastSpeedPenaltyMult = 1f;
        HealOutputMult = 1f;
        MagicWeaponPenaltyMult = 1f;
        HasMagicWeapon = false;
        HealPowerFlat = 0; HealPowerMod = 1f;
        HealReceivedFlat = 0; HealReceivedMod = 1f;

        HasShield = false;
        BlockChance = 0f;
        BlockReduction = 0f;
        ShieldDefense = 0;
        ShieldCritDefense = 0f;
        HpRegenMult = 1f;
        MpRegenMult = 1f;
        ArmorMasteryLabel = "";
        GradeArmorGap = 0;
        GradeWeaponGap = 0;

        var bodyWeight = ArmorWeight.None;   // equipped BODY-slot armor weight (for masteries)
        int weaponAsBase = 0;                // equipped weapon's attack-speed base override (0 = type default)
        float weaponPFactor = 1f;            // equipped weapon's P.Atk / M.Atk channel factors
        float weaponMFactor = 1f;            // (1 = unarmed: no weapon to shape the split)

        foreach (var item in Inventory)
        {
            if (!item.Equipped || ItemCatalog.Get(item.DefId) is not ItemDef def)
                continue;

            if (def.Slot == EquipSlot.Armor && def.ArmorSlot == ArmorSlot.Body)
                bodyWeight = def.Weight;

            // GRADE PENALTY (owner 2026-07-17): no longer a per-item stat scaler. The gap between your
            // grade and the WORST over-grade piece you wear becomes a CHARACTER-wide debuff, applied at
            // the end of this method (see GradeArmorPenalty / GradeWeaponPenalty). Armour and jewels feed
            // one gap, the weapon another — they debuff different stat sets.
            if (Kind == EntityKind.Player)
            {
                int gap = GradePenalty.Gap(def, Level, GradeLevelBonus);
                if (gap > 0)
                {
                    if (def.Slot == EquipSlot.Weapon)
                        GradeWeaponGap = Math.Max(GradeWeaponGap, gap);
                    else if (def.Slot is EquipSlot.Armor or EquipSlot.Jewel or EquipSlot.Shield)
                        GradeArmorGap = Math.Max(GradeArmorGap, gap);
                }
            }

            // ENCHANT IS A FLAT PER-LEVEL OFFSET, PER SLOT (his table, 0.60.0 — see EnchantRules).
            // It used to be +20% of each base bonus per level, applied to every stat on every slot;
            // the deltas below are authored numbers instead, and only the stats he named scale at all
            // (weapon P.Atk/M.Atk, armour P.Def + Max HP, jewel M.Def + Max MP, shield defence).
            int atkBonus = def.AtkBonus + EnchantRules.AtkDelta(def, item.Enchant);
            AttackPower += atkBonus;
            // Every piece — weapons included — now contributes its OWN authored M.Atk. A weapon used to
            // contribute its single power number to both channels and let the channel factors split it,
            // which meant the CSV's second column never reached the game and the item card never showed
            // an M.Atk line. Weapons that predate the migration have MAtkBonus 0, so they fall back to
            // the old shared-number behaviour and nothing rebalances under them.
            // ⚠ The fallback tests the AUTHORED number, not the enchanted one: an enchant now adds M.Atk
            // to a weapon whose base M.Atk is 0, so testing the total would take a +1 legacy weapon off
            // the legacy path and halve its M.Atk.
            int mAtkBonus = def.MAtkBonus + EnchantRules.MAtkDelta(def, item.Enchant);
            MagicAttack += def.Slot == EquipSlot.Weapon && def.MAtkBonus == 0
                ? atkBonus
                : mAtkBonus;
            Defence += def.DefBonus + EnchantRules.DefDelta(def, item.Enchant);
            MagicDefence += def.MDefBonus + EnchantRules.MDefDelta(def, item.Enchant);  // jewels
            MaxHp += def.HpBonus + EnchantRules.HpDelta(def, item.Enchant);
            MaxMp += def.MpBonus + EnchantRules.MpDelta(def, item.Enchant);
            Evasion += def.EvaBonus;   // evasion no longer scales with enchant

            if (def.Slot == EquipSlot.Weapon)
            {
                WeaponType = def.WeaponType;
                weaponAsBase = def.AttackSpeedBase;   // per-item speed (bow slow/very-slow), 0 = default
                weaponPFactor = def.PAtkFactor;
                weaponMFactor = def.MAtkFactor;
                HasMagicWeapon = def.IsMagicWeapon;    // wand/staff → Divine Focus is satisfied
                // ---- A/S WEAPONS HIT HARDER IN PvP (him, gear_sets.csv 2026-08-11, ruled 2026-08-11):
                // "A/S weapons have increase in pvp dmg 5% ... its separate from the attributes ... if a
                // weapon is enchanted to +4 or more and its A or S to add the 5% pvp bonus, as a price
                // that u risked to break a weapon." So the premium is EARNED, not owned: grade A(76)/S(80)
                // AND +4. It is separate from the weapon's rolled attribute, and applies to all three
                // channels (the note says weapons, not skills). The armour half of the same rule is the
                // −5% PVP damage TAKEN, which lives only in the S set bonuses — set-only, by his design,
                // whereas this one pays on every hit.
                if (def.ItemLevel >= 76 && item.Enchant >= 4)
                {
                    PvpBasicDamageBonus += 0.05f;
                    PvpSkillDamageBonus += 0.05f;
                    PvpMagicDamageBonus += 0.05f;
                }
            }

            if (def.Slot == EquipSlot.Shield)
            {
                HasShield = true;
                BlockChance = def.BlockChance;
                BlockReduction = def.BlockReduction;
                ShieldDefense += def.ShieldDefense + EnchantRules.ShieldDefDelta(def, item.Enchant);
                ShieldCritDefense = def.ShieldCritDefense;
                Evasion -= def.ShieldEvasionPenalty;   // shield lowers evasion
            }

            if (def.WeaponRange > 0)
            {
                float range = def.WeaponRange;
                // Bow range grows by class-change tier (passives):
                //   tier 1 (1-20): base 400; tier 2 (21-40): +200; tier 3 (40+): +500.
                // The ROGUE line owns the bow since the archer merge, so it earns the tier bonus; a
                // mage who picks a bow up still shoots at the flat base range.
                if (Archetype is Game.Shared.Archetype.Archer or Game.Shared.Archetype.Rogue)
                {
                    int tier = SkillMath.RangeTier(Level);
                    float bonus = tier >= 3 ? 500f : tier >= 2 ? 200f : 0f;
                    range = Math.Min(GameConstants.MaxBasicAttackRange, range + bonus);
                }
                BasicAttackRange = range;
            }
        }

        // ----- Weapon channel split + the P.Atk formula -----
        // The equipped weapon decides how much of its power reaches each channel (PAtkFactor /
        // MAtkFactor) — a staff melees poorly (0.6), a sword casts poorly. Mobs have no weapon (factors 1).
        //
        // P.Atk (IG shape): at this point AttackPower holds only the accumulated WEAPON P.Atk bonus (we
        // didn't seed a stat base). Apply the channel factor to it, then run the multiplicative formula
        // — (fist + weaponP) × ATKbonus × levelMod — so the weapon is the base and the stat/level
        // multiply. Unarmed → weaponP 0 → fist only → feeble, with no penalty branch.
        //
        // M.Atk is UNCHANGED: base (atkStat + level·2 + weaponM) × MAtkFactor, then × levelMod² later.
        // That is the signed-off magic balance; only the P channel moved to the multiplicative form.
        //
        // ⚠ A PLAYER-BUILT creature must take this branch or its weapon is worse than useless: the
        // accumulated weapon P.Atk would sit there as a bare additive number over a base of 0, with the
        // ATK stat and the level modifier never applied at all.
        if (playerStats)
        {
            int weaponPAtk = (int)(AttackPower * weaponPFactor);
            AttackPower = StatCalculator.PhysicalAttackPower(weaponPAtk, EffectiveAtk, Level);
            // M.Atk mirrors P.Atk: the accumulated weapon M.Atk is the base, the stat multiplies it.
            // levelMod² is applied later (the magic ² level term), so this stays level-free like the
            // additive base it replaces — set/jewel/passive M.Atk still layer on afterwards.
            int weaponMAtk = (int)(MagicAttack * weaponMFactor);
            MagicAttack = StatCalculator.MagicAttackStatScaled(weaponMAtk, EffectiveAtk);
        }

        // ----- Item attributes (0.45.0: at most one per item, put there by an attribute
        //       scroll — items no longer drop with any. The flat Accuracy/HpRegen/MpRegen
        //       cases only fire for items rolled before the change. -----
        float hpPct = 0, mpPct = 0, speedPct = 0, castPct = 0, atkSpeedPct = 0, atkPct = 0, evaPct = 0, evaFlat = 0, defPct = 0;
        float accFlat = 0, hpRegFlat = 0, mpRegFlat = 0, critRatePct = 0, critDmgPct = 0;
        float mAtkPct = 0, magicCritPct = 0;
        float accPct = 0, hpRegPct = 0, mpRegPct = 0, pAtkPct = 0;
        foreach (var item in Inventory)
        {
            if (!item.Equipped) continue;
            foreach (var attr in item.Attributes)
            {
                switch (attr.Type)
                {
                    case AttributeType.HealthPercent: hpPct += attr.Value; break;
                    case AttributeType.ManaPercent: mpPct += attr.Value; break;
                    case AttributeType.SpeedPercent: speedPct += attr.Value; break;
                    case AttributeType.CastSpeedPercent: castPct += attr.Value; break;
                    case AttributeType.AttackSpeedPercent: atkSpeedPct += attr.Value; break;
                    case AttributeType.AttackPercent: atkPct += attr.Value; break;
                    case AttributeType.EvasionPercent: evaPct += attr.Value; break;   // LEGACY rolls only
                    case AttributeType.Evasion: evaFlat += attr.Value; break;         // dual/dagger, cap 5
                    case AttributeType.DefencePercent: defPct += attr.Value; break;
                    case AttributeType.Accuracy: accFlat += attr.Value; break;
                    case AttributeType.HpRegen: hpRegFlat += attr.Value; break;
                    case AttributeType.MpRegen: mpRegFlat += attr.Value; break;
                    case AttributeType.CritRate: critRatePct += attr.Value; break;
                    case AttributeType.CritDamage: critDmgPct += attr.Value; break;
                    case AttributeType.MagicAttackPercent: mAtkPct += attr.Value; break;   // caster wands/staves
                    case AttributeType.MagicCritRate: magicCritPct += attr.Value; break;
                    case AttributeType.AccuracyPercent: accPct += attr.Value; break;       // bow
                    case AttributeType.HpRegenPercent: hpRegPct += attr.Value; break;      // ring
                    case AttributeType.MpRegenPercent: mpRegPct += attr.Value; break;      // ring
                    case AttributeType.PhysicalAttackPercent: pAtkPct += attr.Value; break; // necklace
                }
            }
        }

        MaxHp += (int)(MaxHp * hpPct / 100f);
        MaxMp += (int)(MaxMp * mpPct / 100f);
        AttackPower += (int)(AttackPower * (atkPct + pAtkPct) / 100f);
        MagicAttack += (int)(MagicAttack * (atkPct + mAtkPct) / 100f);
        // GEAR crit is FLAT and lands outside every multiplier — the magic twin of CritRateFlat
        // below. (No item rolls AttributeType.MagicCritRate today; this is the live hook if one
        // ever should.) The old mid-chain clamp here is GONE: it capped the rate before the
        // Insight buff had multiplied it, which is precisely how a x2 buff bought +3 points.
        MagicCritRateFlat += magicCritPct / 100f;
        // Flat FIRST, then the legacy percent — same order as accuracy below. A flat point is a flat
        // 1% miss at every level, which is the whole reason the dual roll became flat.
        Evasion += (int)evaFlat;
        Evasion += (int)(Evasion * evaPct / 100f);
        Defence += (int)(Defence * defPct / 100f);
        if (Kind == EntityKind.Player)
        {
            RunSpeed = SpeedTable.BaseRunSpeed(Race, BaseClass) * (1f + speedPct / 100f);
            WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor;
            Speed = RunSpeed;   // running by default; EffectiveSpeed picks state + clamps
        }
        CastSpeedMultiplier = Math.Max(0.4f, 1f - castPct / 100f);
        AttackSpeedMultiplier = Math.Max(0.4f, 1f - atkSpeedPct / 100f);
        // Flat first, then the percent — the percent roll multiplies the finished
        // (AGI + level + flats) accuracy, so it keeps pace as you level.
        Accuracy += (int)accFlat;
        if (accPct != 0f) Accuracy += (int)(Accuracy * accPct / 100f);
        HpRegenBonus = hpRegFlat;
        MpRegenBonus = mpRegFlat;
        if (hpRegPct != 0f) HpRegenMult *= 1f + hpRegPct / 100f;
        if (mpRegPct != 0f) MpRegenMult *= 1f + mpRegPct / 100f;
        CritDamageBonus = critDmgPct / 100f;   // e.g. 20 -> +0.20x crit multiplier
        CritDamageFlat = 0f;                   // FLAT crit damage: passives (below) + the S light SET

        // ----- (Flat class bonuses were applied here — DELETED 2026-08-10, owner ruling.) -----
        // "There is no identity. The identity is just the skills/passives kit … no more u change
        // your class and get bonus." A 2nd/3rd class change now grants NOTHING but its skills, so
        // there is nothing to add here. The lean between disciplines returns with the level-40+
        // class CSVs, authored as passives in the discipline's kit — never as a class-def field.
        // See docs/design/Disciplines.md; the deleted table is in git (Classes.Third.cs FlatFor).

        // ----- Armor set bonus (BODY-DRIVEN): the worn BODY's set grants the bonus when
        // Head/Gloves/Boots are filled with that set's accessory line. This lets the
        // light & robe newbie bodies SHARE one accessory line (each body its own bonus).
        // A classic single-id set (AccessorySetId = "") just matches its own id. -----
        // The active set was DETECTED in the pre-pass (its PRIMARY-stat deltas are already folded);
        // here we apply its SECONDARY stats + the legacy flat/percent bonuses.
        ActiveArmorSet = "";
        if (activeSet is ArmorSetDef set)
        {
            MaxHp += set.Bonus.MaxHp;
            MaxMp += set.Bonus.MaxMp;
            Defence += set.Bonus.Defence;
            AttackPower += set.Bonus.Attack;
            MagicAttack += set.Bonus.Attack;   // set Attack feeds both channels
            Evasion += set.Bonus.Evasion;
            Accuracy += set.Bonus.Accuracy;
            // Optional PERCENT set bonuses (e.g. newbie light +2% P.Def, robe +15% cast).
            if (set.DefencePct != 0f) Defence += (int)(Defence * set.DefencePct);
            if (set.CastSpeedPct != 0f)
                CastSpeedMultiplier = Math.Clamp(CastSpeedMultiplier * (1f - set.CastSpeedPct), 0.4f, 2.5f);

            // Full StatMods set bonus (tiered gear) — SECONDARY stats (primary-stat deltas were
            // folded in the pre-pass at the top of this method).
            var m = set.Mods;
            MaxHp = (int)((MaxHp + m.MaxHp) * (1f + m.MaxHpPct));
            MaxMp = (int)((MaxMp + m.MaxMp) * (1f + m.MaxMpPct));
            Defence = (int)((Defence + (int)m.PDef) * (1f + m.PDefPct));
            MagicDefence = (int)((MagicDefence + (int)m.MDef) * (1f + m.MDefPct));
            AttackPower = (int)((AttackPower + (int)m.PAtk) * (1f + m.PAtkPct));
            MagicAttack = (int)((MagicAttack + (int)m.MAtk) * (1f + m.MAtkPct));
            Evasion = (int)((Evasion + (int)m.Evasion) * (1f + m.EvasionPct));
            Accuracy = (int)((Accuracy + (int)m.Accuracy) * (1f + m.AccuracyPct));
            if (m.CastSpeedPct != 0f)
                CastSpeedMultiplier = Math.Clamp(CastSpeedMultiplier / (1f + m.CastSpeedPct), 0.4f, 2.5f);
            if (m.AtkSpeedPct != 0f)
                AttackSpeedMultiplier = Math.Clamp(AttackSpeedMultiplier / (1f + m.AtkSpeedPct), 0.4f, 2.5f);
            // Flat then percent, so the light-S set's "Speed +7"-style flat and its "move speed x1.03"
            // compose the way the passive path already does (RunSpeed + flat) × (1 + pct).
            if (m.MoveSpeed != 0f || m.MoveSpeedPct != 0f)
            {
                RunSpeed = (RunSpeed + m.MoveSpeed) * (1f + m.MoveSpeedPct);
                WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor;
                Speed = RunSpeed;
            }
            if (m.HpRegenPct != 0f) HpRegenMult *= 1f + m.HpRegenPct;
            if (m.MpRegenPct != 0f) MpRegenMult *= 1f + m.MpRegenPct;
            MeleeVamp += m.MeleeVamp;
            MeleeReflect += m.Reflect;
            CcResist += m.CcResist;
            // The four channels the S sets introduced. Crit rate/damage are the FLAT ones on purpose —
            // gear crit lands outside every multiplier (see the crit-model note further down), and the
            // single fold + clamp of CritChance still happens at the end of this method.
            CritRateFlat += m.CritRateFlat;
            CritDamageFlat += m.CritDamageFlat;
            MagicResist += m.MagicResist;
            if (m.PvpDamageTakenPct != 0f) PvpDamageTaken *= 1f + m.PvpDamageTakenPct;
            ActiveArmorSet = set.Name;

            // ---- SHIELD-conditional extra: an ADDITIONAL bonus when the set's own shield is also
            // equipped. Per the gear CSV the shield is never required to complete the set — it just
            // adds this on top. Only the def-oriented heavy sets define one. ----
            bool wearingSetShield = Inventory.Any(it => it.Equipped
                && ItemCatalog.Get(it.DefId) is { Slot: EquipSlot.Shield } shd
                && shd.SetId == set.Id);
            if (wearingSetShield)
            {
                var sb = set.ShieldBonus;
                MaxHp = (int)((MaxHp + sb.MaxHp) * (1f + sb.MaxHpPct));
                Defence = (int)((Defence + (int)sb.PDef) * (1f + sb.PDefPct));
                MagicDefence = (int)((MagicDefence + (int)sb.MDef) * (1f + sb.MDefPct));
                AttackPower = (int)((AttackPower + (int)sb.PAtk) * (1f + sb.PAtkPct));
                ShieldDefense = (int)(ShieldDefense * (1f + sb.ShieldDefPct));
                MeleeReflect += sb.Reflect;
                CcResist += sb.CcResist;
                // Heavy S repeats "PVP Dmg Received x0.95" in its shield clause, so shield-up compounds
                // with the set's own: ×0.95 × ×0.95 = ×0.9025. That is what the CSV writes.
                if (sb.PvpDamageTakenPct != 0f) PvpDamageTaken *= 1f + sb.PvpDamageTakenPct;
                ActiveArmorSet = set.Name + " + Shield";
            }
        }

        // Basic-attack power is now just P.Atk — no per-archetype coefficient. What separates a
        // tank's swing from a warrior's is the WEAPON (1H vs 2H P.Atk, speed, crit factor), and
        // any remaining per-class nudge is data on the Class Balance passive. The crit/evasion
        // leans likewise ride the rogue/archer floor passives (stats-via-skills).
        var arch = Archetype;
        BasicAttackPower = Math.Max(1, AttackPower);
        // Crit RATE — his IG model (docs/design/CritBlowAndDouble.md §5):
        //     crit = (110 × weaponFactor × agiMod × buffs × passives + flat) × debuffs × enemyLightArmor
        // The WEAPON multiplies the character base (dagger/bow 13.2%, sword 8.8%, blunt 4.4%) and AGI
        // is a mild multiplier on top — AGI is no longer the base. Passives and buffs multiply this
        // (CritRateMult); GEAR crit-rate is FLAT and lands OUTSIDE every multiplier (CritRateFlat),
        // which is the whole point of the model: multipliers only reward whoever already has a big
        // base, so the flat term is what carries a blunt warrior. The chain is folded and clamped
        // ONCE, at the end of this method — nothing in between may clamp it.
        CritChance = StatCalculator.PhysicalCritBase(EffectiveAgi, WeaponType);
        // The WEAPON's crit-rate roll MULTIPLIES the weapon's own crit base (owner, 2026-08-07,
        // checklist `0d`) — "Crit Rate +30%" now means x1.30, which is what the tooltip has always
        // said and what AttributeSystem.ToStatMods already assumed.
        // ⚠ It used to land in CritRateFlat as `value / 100`, i.e. a maxed roll was +30 PERCENTAGE
        // POINTS — +300 on his 0-1000 scale, against IG's +109 at S grade and against his own rule
        // for the flat channel, *"a flat 30 is flat 3%"* (the divisor should have been 1000). It
        // took a sword from 8.8% to 38.8% and a dagger from 13.2% to 43.2%, which did not just
        // overpay: being FLAT it collapsed the 3:2:1 weapon identity the crit model exists to
        // create, since the same +30 is worth far more to the weapon with the smaller base.
        // Only weapons roll CritRate (AttributeSystem.PoolFor — armor identity is its SET), so
        // this multiplier is contained to the hand. Armor/buff FLAT crit sources are untouched
        // and still add outside every multiplier, which is what carries the blunt warrior.
        if (critRatePct != 0f) CritRateMult *= 1f + critRatePct / 100f;
        Accuracy += StatCalculator.WeaponAccuracyBonus(WeaponType);

        // Skill-buff Max HP/MP (e.g. HP Boost line, Frenzy): flat add and/or % of max.
        float buffHpPct = 0f, buffMpPct = 0f, buffHpFlat = 0f, buffMpFlat = 0f;
        foreach (var buff in Buffs)
        {
            if (buff.Has(SkillEffect.BuffHp)) { buffHpPct += buff.Percent(SkillEffect.BuffHp); buffHpFlat += buff.Flat(SkillEffect.BuffHp); }
            if (buff.Has(SkillEffect.BuffMp)) { buffMpPct += buff.Percent(SkillEffect.BuffMp); buffMpFlat += buff.Flat(SkillEffect.BuffMp); }
        }
        MaxHp = (int)((MaxHp + buffHpFlat) * (1f + buffHpPct));
        MaxMp = (int)((MaxMp + buffMpFlat) * (1f + buffMpPct));

        // Mobs go through the SAME weapon-speed table as players — their WeaponType is resolved at
        // spawn (GameLoopService.BuildMob) from the Archer role, the template's MobMod.Weapon
        // passive, or the category default. A mob that is genuinely weaponless (plant, magic
        // creature) correctly lands on the weaponless base.
        WeaponAttackBase = weaponAsBase > 0
            ? weaponAsBase
            : StatCalculator.WeaponAttackBaseSpeed(WeaponType);

        // ----- Shield Mastery buffs (tank passives) scale the shield values.
        //  Percent magnitudes add fractionally; flat add directly. Only matter
        //  when a shield is equipped, so a mage's buffed shield is still weak. ---
        if (HasShield)
        {
            foreach (var buff in Buffs)
            {
                if (buff.Has(SkillEffect.BuffBlockChance))
                {
                    BlockChance += buff.Flat(SkillEffect.BuffBlockChance);
                    BlockChance *= 1f + buff.Percent(SkillEffect.BuffBlockChance);
                }
                if (buff.Has(SkillEffect.BuffShieldDef))
                {
                    ShieldDefense += (int)buff.Flat(SkillEffect.BuffShieldDef);
                    ShieldDefense = (int)(ShieldDefense * (1f + buff.Percent(SkillEffect.BuffShieldDef)));
                    // 🔴 A shield-defence buff does NOT thicken the block. HIS RULING, playtest-22 `70b`:
                    // *"Shields dmg reduction is never increased by any means ...only chance."* It used
                    // to add Percent x 0.2 here. See the matching removal in the PASSIVE layer below —
                    // that one is what he actually caught (a 10% shield reading 18%).
                }
                // 🔑 The BUFF side keeps its old magnitude — HIS RULING, 2026-08-12:
                // "sheild_mastery.Shield_PDef will be the only part that will increase 5 times ... other
                // passives, sets and buffs that increase the shieldPdef/chance etc are kept as is."
            }
            BlockChance = Math.Clamp(BlockChance, 0f, StatCaps.BlockChance);
            BlockReduction = Math.Clamp(BlockReduction, 0f, StatCaps.BlockReduction);
        }

        // ----- Armor-weight MASTERY (final layer): each armor-mastery SKILL carries a
        // per-weight StatMods table (bonus for the trained weight, penalty for an untrained
        // one); the worn body weight selects the row. Pure per-level DATA — no character-level
        // / class formula. A class with no mastery skill learned gets nothing (no bonus, no
        // penalty). See docs/design/StatMods.md. ---
        if (Kind == EntityKind.Player)
        {
            // ⚠ Armor masteries STACK (owner, 2026-08-07). This loop used to take the FIRST match
            // and break, so the winner was decided by dictionary ORDER — a nuker whose base Robe
            // Mastery had been removed and re-granted by AutoLearnCoreSkills sat in the freed
            // (earlier) slot, and the level-1 base mastery silently beat Mage Armor Mastery: no
            // +max MP, no P.Def and no mpWhenRestored at all (measured: BalanceMatrix E3 read
            // RestoreMpBonus 0 at every level). It is now a SUM, which is also what the mage
            // restructure needs: Spellcaster Mastery owns the wrong-weight PENALTY and the class
            // mastery owns the BONUS, and a robed nuker must collect both.
            //
            // Percentages compose MULTIPLICATIVELY, one profile at a time — never summed. That is
            // load-bearing for the cleric's light-armor row, which is authored to CANCEL the
            // Spellcaster penalty (cast ×1.90 against ×0.50 = ×0.95): summed they would give
            // 1 + 0.90 − 0.50 = ×1.40, which is not the number he authored.
            // `Replaces` still wins: a superseded base mastery contributes nothing.
            var supersededMasteries = new HashSet<string>();
            foreach (var (skillId, _) in LearnedSkills)
                if (SkillCatalog.Get(skillId)?.Replaces is { } rep)
                    foreach (var r in rep) supersededMasteries.Add(r);

            bool dataMastery = false;
            foreach (var (skillId, skillLevel) in LearnedSkills)
            {
                if (supersededMasteries.Contains(skillId)) continue;
                if (SkillCatalog.Get(skillId)?.ArmorMasteryAt(skillLevel) is not ArmorMasteryProfile prof)
                    continue;
                dataMastery = true;
                ApplyArmorMastery(bodyWeight switch
                {
                    ArmorWeight.Robe  => prof.Robe,
                    ArmorWeight.Light => prof.Light,
                    ArmorWeight.Heavy => prof.Heavy,
                    _ => prof.None,   // no body armor equipped
                });
            }
            ArmorMasteryLabel = dataMastery
                ? (bodyWeight == ArmorWeight.None ? "Armor Mastery" : $"Armor Mastery ({bodyWeight})")
                : "";

            // One armor-mastery StatMods folded in: speed pcts DIVIDE the time multiplier so >0 =
            // faster; regen pcts MULTIPLY the running mult (they used to assign, which silently made
            // the last mastery win); flat def/eva add before the def % factor.
            void ApplyArmorMastery(StatMods sm)
            {
                AttackSpeedMultiplier = Math.Clamp(AttackSpeedMultiplier / (1f + sm.AtkSpeedPct), 0.4f, 2.5f);
                CastSpeedMultiplier = Math.Clamp(CastSpeedMultiplier / (1f + sm.CastSpeedPct), 0.4f, 2.5f);
                // Flat move speed lands BEFORE the percent, so "speed +7" is 7 points of run speed
                // and not 7 points scaled by whatever else is on. StatMods.MoveSpeed already
                // existed here but nothing read it — the rogue light mastery's CSV "speed +7" was
                // being authored as MoveSpeedPct 0.06 (×1.06) instead, which he corrected in
                // `rogue 2nd.csv` during playtest-20 ("Also speed is +7 flat not x1.07").
                RunSpeed = (RunSpeed + sm.MoveSpeed) * (1f + sm.MoveSpeedPct);
                WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor;
                Speed = RunSpeed;
                HpRegenMult *= 1f + sm.HpRegenPct;
                MpRegenMult *= 1f + sm.MpRegenPct;
                MaxHp = (int)((MaxHp + sm.MaxHp) * (1f + sm.MaxHpPct));
                MaxMp = (int)((MaxMp + sm.MaxMp) * (1f + sm.MaxMpPct));
                Evasion += (int)sm.Evasion;
                Accuracy += (int)sm.Accuracy;
                Defence += (int)sm.PDef;
                MagicDefence += (int)sm.MDef;
                if (sm.PDefPct != 0f) Defence = (int)(Defence * (1f + sm.PDefPct));
                if (sm.MDefPct != 0f) MagicDefence = (int)(MagicDefence * (1f + sm.MDefPct));
                InterruptResist += (int)sm.InterruptResist;
                if (sm.CritRate != 0f) CritRateMult *= 1f + sm.CritRate;   // ×1.2, not +20 points
                if (sm.MagicCritRate != 0f) MagicCritRateMult *= 1f + sm.MagicCritRate;   // ditto, magic channel
                if (sm.MagicCritDamage != 0f) MagicCritDamageMult *= 1f + sm.MagicCritDamage;   // ×1.3, not +30 points
                CritDamageBonus += sm.CritDamage;
                CritDmgResist += sm.CritDmgResist;
                CritRateResist += sm.CritRateResist;
                BowResist += sm.BowResist;
                // ×1.6, not +60 points — the same convention as the crit-rate lines above. Masteries
                // COMPOUND here, which is what lets a set bonus ride on top of the robe mastery.
                if (sm.RestoreMpPct != 0f) RestoreMpMod *= 1f + sm.RestoreMpPct;
            }

            // A learned skill can SUPERSEDE another's passive via Replaces[] (e.g. Spell
            // Mastery replaces Weapon Mastery): collect those ids so the base passive
            // doesn't double-apply. (Non-passive replaced skills are harmless no-ops here.)
            var replacedPassives = new HashSet<string>();
            foreach (var (skillId, _) in LearnedSkills)
                if (SkillCatalog.Get(skillId)?.Replaces is { } rep)
                    foreach (var r in rep) replacedPassives.Add(r);

            // Fold one PassiveEffect into the derived stats. Shared by the always-on
            // discipline passives AND the weapon-conditional masteries below (which pass
            // the profile entry for the currently-held weapon). An all-zero pe is inert.
            void ApplyPassive(PassiveEffect pe)
            {
                MaxHp += pe.MaxHp + (int)(MaxHp * pe.MaxHpPct);
                MaxMp += pe.MaxMp + (int)(MaxMp * pe.MaxMpPct);
                Defence += pe.Defence;
                MagicDefence += pe.MagicDefence;
                if (pe.DefencePct != 0f) Defence = (int)(Defence * (1f + pe.DefencePct));
                if (pe.MagicDefencePct != 0f) MagicDefence = (int)(MagicDefence * (1f + pe.MagicDefencePct));
                AttackPower += pe.Attack + (int)(AttackPower * (pe.AttackPct + pe.PhysAtkPct)) + pe.PhysAtk;
                // Magic reads ONLY MagAtkPct (magic-only), applied SQUARED so its stored value is the HONEST
                // effective % (the square cancels the √). AttackPct is SHARED → it raises P.Atk only; it no
                // longer touches M.Atk (a magic boost must use MagAtkPct). Only base M.Atk goes through the √.
                float magPassivePct = (1f + pe.MagAtkPct) * (1f + pe.MagAtkPct) - 1f;
                MagicAttack += pe.Attack + (int)(MagicAttack * magPassivePct) + pe.MagAtk;
                Evasion += pe.Evasion;
                Accuracy += pe.Accuracy;
                if (pe.CritRate != 0f) CritRateMult *= 1f + pe.CritRate;   // ×1.2, not +20 points
                CritDamageBonus += pe.CritDamage;
                CritDamageFlat += pe.CritDamageFlat;
                // Magic crit is MULTIPLICATIVE too now (owner ruling 2026-08-06). The old comment
                // here said it stayed additive because "a mage's base is a 4% WIT figure where a
                // ×1.05 is nothing" — that base is exactly what the rework fixed, and an additive
                // passive on top of it was the single biggest magic-crit source in the game.
                if (pe.MagicCritRate != 0f) MagicCritRateMult *= 1f + pe.MagicCritRate;   // ×1.2, not +20 points
                // Magic crit DAMAGE is its own channel too — pe.CritDamage above is PHYSICAL and
                // must never leak into a spell (owner ruling 2026-08-06, still the rule).
                if (pe.MagicCritDamage != 0f) MagicCritDamageMult *= 1f + pe.MagicCritDamage;   // ×1.3, not +30 points
                HpRegenBonus += pe.HpRegen;
                MpRegenBonus += pe.MpRegen;
                if (pe.HpRegenPct != 0f) HpRegenMult *= 1f + pe.HpRegenPct;
                if (pe.MpRegenPct != 0f) MpRegenMult *= 1f + pe.MpRegenPct;
                if (pe.AtkSpeedPct != 0f) AttackSpeedMultiplier = Math.Clamp(AttackSpeedMultiplier * (1f - pe.AtkSpeedPct), 0.4f, 2.5f);
                if (pe.CastSpeedPct != 0f) CastSpeedMultiplier = Math.Clamp(CastSpeedMultiplier * (1f - pe.CastSpeedPct), 0.4f, 2.5f);
                CastSpeedFlatBonus += pe.CastSpeedFlat;   // spell rune-style flat +cast (added AFTER the multiplicative chain)
                if (pe.MoveSpeedPct != 0f) { RunSpeed *= 1f + pe.MoveSpeedPct; WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor; Speed = RunSpeed; }
                CooldownReduction += pe.CooldownPct;
                CritRateResist += pe.CritRateResist;
                CritDmgResist += pe.CritDmgResist;
                BowResist += pe.BowResist;
                // Bow range bonus applies only while a bow is equipped (rogue/archer mastery).
                if (pe.BowRange != 0f && WeaponType == WeaponType.Bow)
                    BasicAttackRange = Math.Min(GameConstants.MaxBasicAttackRange, BasicAttackRange + pe.BowRange);
                // Shield passive (only with a shield equipped): scale block chance / shield def.
                if (HasShield)
                {
                    if (pe.BlockChancePct != 0f) BlockChance *= 1f + pe.BlockChancePct;
                    // 🔴 A shield-defence passive raises the shield's DEFENCE and nothing else. It used
                    // to also add `ShieldDefPct * 0.04` to BlockReduction, which at a maxed Shield
                    // Mastery (2.00) was a flat +8 points — exactly what he caught in playtest-22 `70b`:
                    // *"The shield says 10 but I see 18% ..the shield says 20 I see 28% ... Shields dmg
                    // reduction is never increased by any means ...only chance."*
                    // 🔑 THE RULE NOW: BlockReduction is the SHIELD's own number, full stop. Nothing —
                    // passive, buff, set or enchant — may raise it; the ladder scales block CHANCE and
                    // shield DEFENCE instead. That is what makes the item card's "10%" readable as the
                    // number that will actually be subtracted.
                    if (pe.ShieldDefPct != 0f)
                        ShieldDefense = (int)(ShieldDefense * (1f + pe.ShieldDefPct));
                }
                MagicResist += pe.MagicResist;
                MagicInterruptBonus += pe.InterruptPower;
                InterruptResist += pe.InterruptResist;
                MeleeVamp += pe.MeleeVamp;
                SpellVamp += pe.SpellVamp;
                PveSkillDamageBonus += pe.PveSkillDamagePct;
                PveMagicDamageBonus += pe.PveMagicDamagePct;
                PveBasicDamageBonus += pe.PveBasicDamagePct;
                PvpSkillDamageBonus += pe.PvpSkillDamagePct;
                PvpMagicDamageBonus += pe.PvpMagicDamagePct;
                PvpBasicDamageBonus += pe.PvpBasicDamagePct;
                CancelResist += pe.CancelResistPct;
                // Resolution floors are GUARANTEES — take the strongest (max), never sum. The magic
                // MULTIPLIER follows the same rule: two anti-magic sources don't compound to ×4.
                EvadeFloor = Math.Max(EvadeFloor, pe.EvadeFloor);
                HitFloor = Math.Max(HitFloor, pe.HitFloor);
                MagicFailMod = Math.Max(MagicFailMod, pe.MagicFailMod);
                // The three skill-defence channels (BL-06/07/08) — same rule. The reflect PCT rides
                // with its own chance rather than being maxed independently, or a 15%-chance-×1.0
                // passive and a 100%-chance-×0.15 one would combine into 100% × 1.0.
                SkillEvadeChance = Math.Max(SkillEvadeChance, pe.SkillEvadeChance);
                if (pe.PhysSkillReflectChance > PhysSkillReflectChance)
                {
                    PhysSkillReflectChance = pe.PhysSkillReflectChance;
                    PhysSkillReflectPct = pe.PhysSkillReflectPct;
                }
                DebuffReflectChance = Math.Max(DebuffReflectChance, pe.DebuffReflectChance);
                // Heal power (output) + heal received (target). No M.Atk in the heal formula.
                HealPowerFlat += pe.HealPowerFlat;
                if (pe.HealPowerPct != 0f) HealPowerMod *= 1f + pe.HealPowerPct;
                HealReceivedFlat += pe.HealReceivedFlat;
                if (pe.HealReceivedPct != 0f) HealReceivedMod *= 1f + pe.HealReceivedPct;
            }

            // ----- Learnable PASSIVES (discipline passives, weapon masteries): each learned
            // skill whose SkillDef carries a PassiveEffect applies it, on top of everything.
            // A weapon mastery applies the entry for the currently-equipped weapon type. -----
            foreach (var (skillId, skillLevel) in LearnedSkills)
            {
                if (replacedPassives.Contains(skillId)) continue;
                var sd = SkillCatalog.Get(skillId);
                if (sd is null) continue;
                if (sd.PassiveAt(skillLevel) is PassiveEffect pe) ApplyPassive(pe);
                if (sd.WeaponMasteryAt(skillLevel) is WeaponMasteryProfile wm)
                    ApplyPassive(wm.For(WeaponType));
            }

            // Conditional weapon passives (keyed on the equipped weapon, so not flat PassiveEffects):
            //  • Weapon Proficiency (all mages): an untrained weapon (not sword/blunt; wand/staff ARE blunt)
            //    halves cast speed.  • Divine Focus (clerics): no magic weapon scales healing down (Lv1 ×0.5,
            //    Lv2 ×0.75 for Warchanters, so buffers stay useful in fighter gear).
            // CAST SPEED is gated on the trained TYPE (sword/blunt — a wand and a mace are both Blunt),
            // and collapses on anything else: bow, dagger, bare hands.
            // ⚠ 2026-08-07: the gate is SPELLCASTER MASTERY now (Weapon Proficiency is retired and
            // superseded by it), and the untrained-weapon magic penalty is the owner's ×0.5 — it was
            // a ×0.05 COLLAPSE. ⚠ 2026-08-10: the "magic accuracy" clause is a ×25 on the FAIL roll
            // (UntrainedCasterWeapon → StatCalculator.MagicWeaponFailMod), not a halving of a
            // caster stat — the old form halved MagicFailResist, which is 0 on every character.
            bool spellcaster = HasSkill(SkillCatalog.SpellcasterMastery) || HasSkill(SkillCatalog.WeaponProficiency);
            if (spellcaster && !IsMageTrainedWeapon(WeaponType))
            {
                CastSpeedPenaltyMult = 0.5f;
                MagicWeaponPenaltyMult = 0.5f;    // bow / dual / bare hands → half magic, not a collapse
                UntrainedCasterWeapon = true;     // ...and half magic accuracy, applied after the buffs
            }
            // M.ATK is gated on the weapon being an actual MAGIC weapon, which the type cannot tell you:
            // a wand and a mace are both Blunt, so the old type check waved a mace-swinging caster
            // through at full magic power. That leak used to be plugged by MAtkFactor 0.6 on the ITEM —
            // an invisible multiplier on the whole channel, with no in-game explanation for "why is my
            // M.Atk 60%?". The weapon now just carries its authored M.Atk, and the CLASS's own passive
            // states the rule instead: train with a magic weapon or lose most of your magic. Same
            // outcome, but it is data a player can read rather than a constant they cannot.
            else if (spellcaster && !HasMagicWeapon)
                MagicWeaponPenaltyMult = NonMagicWeaponMagicMult;
            int divineFocus = SkillLevelOf(SkillCatalog.DivineFocus);
            if (divineFocus > 0 && !HasMagicWeapon)
                HealOutputMult = divineFocus >= 2 ? 0.75f : 0.5f;

            // (The combat-training attack bonus is now a normal LEVELED passive — its
            // per-level AttackPct flows through the loop above, no special-casing.)

            // Shield-passive scaling above can push block over caps — re-clamp.
            if (HasShield)
            {
                BlockChance = Math.Clamp(BlockChance, 0f, StatCaps.BlockChance);
                BlockReduction = Math.Clamp(BlockReduction, 0f, StatCaps.BlockReduction);
            }
        }

        // ----- The two MAGIC level-scaling terms (authentic IG; see StatCalculator) -----
        //   M.Atk = base × levelMod²   (squared — cancels the √M.Atk in the damage formula,
        //                               so magic grows linearly in level like physical)
        //   M.Def = base × MEN × levelMod
        // Both multiply the finished flat pool (base + gear + jewels + passives). Buffs
        // layer on afterwards in the Effective* getters. PLAYERS ONLY — a mob's M.Atk/M.Def
        // come from its own authored curve (MobBaseStats), which is already a final number.
        // A player-built creature has no such curve, so it takes the terms like a player (BL-47).
        if (playerStats)
        {
            MagicAttack = (int)(MagicAttack * StatCalculator.MagicAttackLevelMod(Level));
            // M.Atk stays the INTERNAL (base·levelMod²) value — the √ magic formulas depend on it and mobs
            // share it. Only the DISPLAY is shrunk (EffectiveMagicAttackShown = scale·√internal). Path B.
            MagicDefence = (int)(MagicDefence
                * StatCalculator.SptModifier(EffectiveSpt)
                * StatCalculator.MagicDefenceLevelMod(Level));
        }

        // ----- Timed-buff contributions to BAKED stats (the stats computed once here;
        // atk/def/speed read buffs live in their Effective* getters instead). Re-folded on
        // every buff apply/expire because both ApplyBuff and TickBuffs call this. Fraction
        // effects accept either Flat or Percent magnitudes (summed as a fraction). -----
        // REWARD rates are folded by MAX, not by sum: holding a +50% and a +20% Exp rune gives +50%.
        // Summing them would make "one rune per channel" a lie — a player would carry the whole ladder.
        // The two zeroing runes are HARD OVERRIDES applied after the max, below, so no pile of bonus
        // runes can dilute a punishment.
        float bestExp = 0f, bestSp = 0f, bestGold = 0f, bestDrop = 0f;
        bool stopExpSp = false, stopGoldDrop = false;

        foreach (var buff in Buffs)
        {
            if (!buff.Rewards.IsNeutral)
            {
                var r = buff.Rewards;
                bestExp = MathF.Max(bestExp, r.Exp);
                bestSp = MathF.Max(bestSp, r.Sp);
                bestGold = MathF.Max(bestGold, r.Gold);
                bestDrop = MathF.Max(bestDrop, r.Drop);
                stopExpSp |= r.StopsExpSp;
                stopGoldDrop |= r.StopsGoldDrop;
            }
            if (buff.Has(SkillEffect.BuffAccuracy)) Accuracy += (int)buff.Flat(SkillEffect.BuffAccuracy);
            // A crit-rate buff's PERCENT multiplies (Focus ×1.30, Harmony ×1.75); its FLAT part
            // lands outside every multiplier — "a flat 30 is flat 3%, not increased by buffs".
            if (buff.Has(SkillEffect.BuffCritRate))
            {
                CritRateMult *= 1f + buff.Percent(SkillEffect.BuffCritRate);
                CritRateFlat += buff.Flat(SkillEffect.BuffCritRate);
            }
            if (buff.Has(SkillEffect.BuffMagicCritRate))
            {
                MagicCritRateMult *= 1f + buff.Percent(SkillEffect.BuffMagicCritRate);
                MagicCritRateFlat += buff.Flat(SkillEffect.BuffMagicCritRate);
            }
            if (buff.Has(SkillEffect.BuffCritDamage))
                CritDamageBonus += buff.Flat(SkillEffect.BuffCritDamage) + buff.Percent(SkillEffect.BuffCritDamage);
            if (buff.Has(SkillEffect.BuffCritRateResist)) CritRateResist += buff.Flat(SkillEffect.BuffCritRateResist) + buff.Percent(SkillEffect.BuffCritRateResist);
            if (buff.Has(SkillEffect.BuffCritDmgResist)) CritDmgResist += buff.Flat(SkillEffect.BuffCritDmgResist) + buff.Percent(SkillEffect.BuffCritDmgResist);
            if (buff.Has(SkillEffect.BuffBowResist)) BowResist += buff.Flat(SkillEffect.BuffBowResist) + buff.Percent(SkillEffect.BuffBowResist);
            if (buff.Has(SkillEffect.BuffMagicResist)) MagicResist += buff.Flat(SkillEffect.BuffMagicResist) + buff.Percent(SkillEffect.BuffMagicResist);
            if (buff.Has(SkillEffect.BuffMagicEvasion)) MagicFailBonus += buff.Flat(SkillEffect.BuffMagicEvasion);
            if (buff.Has(SkillEffect.BuffMeleeVamp)) MeleeVamp += buff.Flat(SkillEffect.BuffMeleeVamp) + buff.Percent(SkillEffect.BuffMeleeVamp);
            if (buff.Has(SkillEffect.BuffSpellVamp)) SpellVamp += buff.Flat(SkillEffect.BuffSpellVamp) + buff.Percent(SkillEffect.BuffSpellVamp);
            if (buff.Has(SkillEffect.BuffReflect)) MeleeReflect += buff.Flat(SkillEffect.BuffReflect) + buff.Percent(SkillEffect.BuffReflect);
            if (buff.Has(SkillEffect.DebuffHealRecv)) HealReceivedMod *= 1f - buff.Percent(SkillEffect.DebuffHealRecv);   // anti-heal
            PhysMpCostReduction += buff.PhysMpCostPct;   // MP-cost reduction (rides as buff fields, not a flag)
            MagicMpCostReduction += buff.MagicMpCostPct;
            // BL-06 skill evasion — a buff field for the same reason (the flag enum is full), and
            // MAXed with the passive side rather than added: it is a guarantee, not a stat.
            SkillEvadeChance = Math.Max(SkillEvadeChance, buff.SkillEvadeChance);
            // Per-school control resistance ADDS (unlike SkillEvadeChance, which is a guarantee and
            // takes the max): these are ordinary stats stacked from gear and blessings, and the sum is
            // clamped below like CcResist is.
            CcResistMagical += buff.CcResistMagical;
            CcResistPhysical += buff.CcResistPhysical;
            // Magic crit damage — the blessings COMPOUND (×1.3 × ×1.3 = ×1.69 on the ×2 base, the
            // owner's own ×3.38), the debuffs SUM. Both ride as buff fields; the flag enum is full.
            if (buff.MagicCritDamage != 0f) MagicCritDamageMult *= 1f + buff.MagicCritDamage;
            MagicCritDamageResist += buff.MagicCritDamageDebuff;
            if (buff.Has(SkillEffect.BuffCooldown)) CooldownReduction += buff.Flat(SkillEffect.BuffCooldown) + buff.Percent(SkillEffect.BuffCooldown);
            if (buff.Has(SkillEffect.BuffPveSkillDamage)) PveSkillDamageBonus += buff.Flat(SkillEffect.BuffPveSkillDamage) + buff.Percent(SkillEffect.BuffPveSkillDamage);
            if (buff.Has(SkillEffect.BuffPveMagicDamage)) PveMagicDamageBonus += buff.Flat(SkillEffect.BuffPveMagicDamage) + buff.Percent(SkillEffect.BuffPveMagicDamage);
            if (buff.Has(SkillEffect.BuffPveBasicDamage)) PveBasicDamageBonus += buff.Flat(SkillEffect.BuffPveBasicDamage) + buff.Percent(SkillEffect.BuffPveBasicDamage);
            if (buff.Has(SkillEffect.BuffPvpSkillDamage)) PvpSkillDamageBonus += buff.Flat(SkillEffect.BuffPvpSkillDamage) + buff.Percent(SkillEffect.BuffPvpSkillDamage);
            if (buff.Has(SkillEffect.BuffPvpMagicDamage)) PvpMagicDamageBonus += buff.Flat(SkillEffect.BuffPvpMagicDamage) + buff.Percent(SkillEffect.BuffPvpMagicDamage);
            if (buff.Has(SkillEffect.BuffPvpBasicDamage)) PvpBasicDamageBonus += buff.Flat(SkillEffect.BuffPvpBasicDamage) + buff.Percent(SkillEffect.BuffPvpBasicDamage);
            if (buff.Has(SkillEffect.BuffCancelResist)) CancelResist += buff.Flat(SkillEffect.BuffCancelResist) + buff.Percent(SkillEffect.BuffCancelResist);
            if (buff.Has(SkillEffect.BuffInterruptPower)) MagicInterruptBonus += (int)buff.Flat(SkillEffect.BuffInterruptPower);
            if (buff.Has(SkillEffect.BuffInterruptResist)) InterruptResist += (int)buff.Flat(SkillEffect.BuffInterruptResist);
        }

        // The reward multipliers, from the best rune held in each channel. A STOP wins outright: it is
        // the point of the Rune of Sinister ("no lvl up") and of the Rune of Sinners (nothing at all).
        Runes = new RateSet(
            Exp:        stopExpSp    ? 0f : 1f + bestExp,
            Sp:         stopExpSp    ? 0f : 1f + bestSp,
            Gold:       stopGoldDrop ? 0f : 1f + bestGold,
            DropChance: stopGoldDrop ? 0f : 1f + bestDrop,
            DropAmount: 1f);
        // Fold the crit-RATE chain exactly once: base × (every passive/buff multiplier) + (every
        // flat source), then the single cap — StatCaps.PhysicalCritRate = his 500 on the 0-1000
        // scale. (The three 0.75 clamps that used to sit along the chain are gone: they clamped
        // intermediate values and contradicted the cap the design has always stated.)
        CritChance = Math.Clamp(CritChance * CritRateMult + CritRateFlat, 0f, StatCaps.PhysicalCritRate);
        MagicCritChance = Math.Clamp(MagicCritChance * MagicCritRateMult + MagicCritRateFlat, 0f, StatCaps.MagicCritRate);
        // The magic crit-DAMAGE chain has no cap of its own here: StatCalculator.MagicCritMult
        // applies StatCaps.MagicCritDamageCap at the point of use, so a debuff can still bite a
        // stack that would otherwise be pinned to the ceiling. Only the debuff sum is bounded.
        MagicCritDamageResist = Math.Clamp(MagicCritDamageResist, 0f, 0.9f);
        CritRateResist = Math.Clamp(CritRateResist, 0f, 1f);
        CritDmgResist = Math.Clamp(CritDmgResist, 0f, 0.9f);
        BowResist = Math.Clamp(BowResist, 0f, 0.9f);
        // (Spellcaster Mastery's untrained-weapon magic penalty is NOT folded in here any more.
        //  It was `MagicFailResist *= 0.5f` — inert, because MagicFailResist was 0 on everyone.
        //  The bow now multiplies the FAIL side at the roll: UntrainedCasterWeapon feeds
        //  StatCalculator.MagicWeaponFailMod at each of the three magic call sites.)
        MagicResist = Math.Clamp(MagicResist, -0.9f, 0.9f);   // negative = a magic WEAKNESS
        MagicFailMod = Math.Max(1f, MagicFailMod);            // never below neutral
        CooldownReduction = Math.Clamp(CooldownReduction, 0f, 0.8f);
        MeleeReflect = Math.Clamp(MeleeReflect, 0f, 0.5f);   // never reflect more than half
        // The skill-defence channels. Evade and the two reflect CHANCES stop short of 1 on purpose —
        // "never dodges anything, ever" and "a skill user can never touch this class" are both
        // degenerate, and his own top rung is 0.90, not 1.0. The reflected FRACTION is uncapped at 1
        // because he authored exactly ×1 ("0.15 chance x1 reflected").
        SkillEvadeChance = Math.Clamp(SkillEvadeChance, 0f, 0.95f);
        PhysSkillReflectChance = Math.Clamp(PhysSkillReflectChance, 0f, 0.95f);
        PhysSkillReflectPct = Math.Clamp(PhysSkillReflectPct, 0f, 1f);
        DebuffReflectChance = Math.Clamp(DebuffReflectChance, 0f, 0.95f);
        CcResist = Math.Clamp(CcResist, 0f, 0.8f);           // never fully CC-immune from gear
        // Each school's own resistance is capped the same way, and because the two multiply with the
        // blanket one at the roll, the floor on a landing debuff is still the contest's own 10%.
        CcResistMagical = Math.Clamp(CcResistMagical, 0f, 0.8f);
        CcResistPhysical = Math.Clamp(CcResistPhysical, 0f, 0.8f);
        // MP cost: −2 … +0.8, i.e. from THREE TIMES the price up to a 80% discount. The floor used to
        // be 0, which quietly made a cost-RAISING effect impossible — and that is exactly Mana Strain
        // (owner 2026-08-19: *"a debuff that increases mana consumption of the enemy"*). One number
        // covers both directions because a discount and a surcharge are the same multiplier read from
        // opposite ends; a second field would only give the two a chance to disagree.
        PhysMpCostReduction = Math.Clamp(PhysMpCostReduction, -2f, 0.8f);
        MagicMpCostReduction = Math.Clamp(MagicMpCostReduction, -2f, 0.8f);
        MeleeVamp = Math.Clamp(MeleeVamp, 0f, 1f);
        SpellVamp = Math.Clamp(SpellVamp, 0f, 1f);

        ApplyGradePenalty();

        ApplyMobScale();

        // LAST, on purpose — `/stat` means "this is the number", so it runs after the caps, the grade
        // penalty and the mob scale have all had their say (playtest-20 `54e`).
        ApplyAdminStatOverrides();

        Hp = Math.Min(Hp, MaxHp);
        Mp = Math.Min(Mp, MaxMp);
    }

    /// <summary>The zone-rank, MobMod and training-dummy multipliers, re-applied on TOP of the freshly
    /// recomputed base curve — the mob equivalent of the grade penalty above, and for the same reason:
    /// it must run last so nothing can wash it out.
    ///
    /// ⚠ playtest-20 #7 — *"Frost bind collapses a training dummy's HP, 1kk → 5k, and elites lose their
    /// bonus too."* These multipliers used to be applied ONCE, in BuildMob, on top of whatever
    /// RecomputeDerived had just produced (the note by the mob base curve still described that as the
    /// design). RecomputeDerived runs again on every buff, debuff and gear change, and it rebuilds
    /// MaxHp/MaxMp/attack/defence from the level curve alone — so the first debuff that landed on a
    /// ranked mob silently deleted its rank. Frost Bind on a dummy is only the loudest case: a 1,000,000
    /// HP pool fell back to the level curve's ~5k. It was never specific to Frost Bind, or to dummies.
    ///
    /// Keeping the factors ON the entity makes the recompute idempotent, which is the actual fix: run it
    /// a hundred times and a champion is still a champion.</summary>
    private void ApplyMobScale()
    {
        if (Kind != EntityKind.Mob) return;

        // 1. The ZONE's rank multipliers (champion/elite/boss).
        MaxHp = Math.Max(1, (int)(MaxHp * MobHpScale));
        AttackPower = Math.Max(1, (int)(AttackPower * MobPAtkScale));
        MagicAttack = Math.Max(1, (int)(MagicAttack * MobMAtkScale));
        BasicAttackPower = Math.Max(1, (int)(BasicAttackPower * MobPAtkScale));

        var mobType = MobTypeId is { } id ? MobCatalog.Get(id) : null;

        // 2. The TEMPLATE's "passive skills" — magic monster, armored brute, boss, …
        if (mobType?.Mod is MobMod mod)
        {
            MaxHp = Math.Max(1, (int)(MaxHp * mod.Hp));
            MaxMp = Math.Max(1, (int)(MaxMp * mod.MaxMp));
            Defence = Math.Max(1, (int)(Defence * mod.PDef));
            MagicDefence = Math.Max(1, (int)(MagicDefence * mod.MDef));
            AttackPower = Math.Max(1, (int)(AttackPower * mod.PAtk));
            MagicAttack = Math.Max(1, (int)(MagicAttack * mod.MAtk));
            BasicAttackPower = Math.Max(1, (int)(BasicAttackPower * mod.PAtk));
            Evasion = (int)(Evasion * mod.Evasion) + mod.EvaFlat;
            Accuracy = (int)(Accuracy * mod.Accuracy);
            // Leveled-mastery extras: attack speed (>1 = faster → shorter interval), HP/MP regen.
            if (mod.AtkSpeed != 1f) AttackSpeedMultiplier /= mod.AtkSpeed;
            if (mod.HpRegen != 1f) HpRegenMult *= mod.HpRegen;
            if (mod.MpRegen != 1f) MpRegenMult *= mod.MpRegen;
            BowResist = Math.Clamp(mod.BowResist, 0f, 0.9f);
            CritRateResist = Math.Clamp(mod.CritResist, 0f, 1f);
            // Weapon-type resistance coefficients (P.Def route; applied per-hit by attacker weapon).
            PierceDefCoef = mod.PierceResist;
            BluntDefCoef = mod.BluntResist;
            BowDefCoef = mod.BowDefResist;
            // BL-11 — MAGIC RESISTANCE, the missing sibling of the three weapon resists above.
            // *"We had a anti magic mobs (lower pdef more mdef) and anty physical (less m def more
            // pdef) — this should feed your mres passive."* Until now a template could only raise
            // M.DEF, which is a flat divisor a mage out-scales; mRes is the percentage channel his
            // own mob ladder is written in, and it is what a player's Anti-Magic passives already
            // read. A NEGATIVE value is a magic WEAKNESS, which is what makes the anti-PHYSICAL
            // half of his pair mean something: the armoured brute is the mage's target.
            MagicResist = Math.Clamp(mod.MagicResist, -0.9f, 0.9f);
            if (mod.Boss)   // raid-boss passive: resists crits + arrows
            {
                CritRateResist = Math.Max(CritRateResist, 0.3f);
                BowResist = Math.Max(BowResist, 0.3f);
            }
        }

        // BL-14 — the weapon a mob holds now decides its per-hit POWER as well as its rate, the way
        // a player's weapon item does. Applied after the template's own P.Atk passive so a hand-tuned
        // champion multiplier is not diluted, and before the ROLE, which has its own trade.
        //
        // ⚠ NOT for a player-built creature (BL-47): this factor exists to give a mob the per-hit power
        // a player gets free from the weapon ITEM, and a player-built creature is holding the actual
        // item. Applying it too would pay for the same weapon twice.
        float weaponPower = PlayerBuilt ? 1f : StatCalculator.MobWeaponPowerFactor(WeaponType);
        if (weaponPower != 1f)
        {
            AttackPower = Math.Max(1, (int)(AttackPower * weaponPower));
            BasicAttackPower = Math.Max(1, (int)(BasicAttackPower * weaponPower));
        }

        if (MobAccFlat != 0) Accuracy += MobAccFlat;

        // 3. The mob's ROLE — ranged/caster archetypes on top of base + passives.
        //
        // ⚠ For a PLAYER-BUILT creature the role keeps its BEHAVIOUR (how far it reaches, whether it
        // swings at all) and loses its STAT LEAN (BL-47). Those multipliers are compensation aimed at
        // the authored mob curve; a player-built caster already gets the caster's shape from the things
        // a player gets it from — a robe, a staff, a mage class curve — and stacking the lean on top
        // would measure the two systems at once and credit the wrong one.
        bool roleStats = !PlayerBuilt;
        switch (mobType?.Role)
        {
            case MobRole.Archer:
                // Fires from ~450 range with a bow; higher P.Atk but light armor (less P.Def, a
                // little more evasion). Uses the normal auto-attack — just at longer range.
                BasicAttackRange = 450f;
                if (roleStats)
                {
                    AttackPower = Math.Max(1, (int)(AttackPower * 2f));
                    BasicAttackPower = Math.Max(1, (int)(BasicAttackPower * 2f));
                    Defence = Math.Max(1, (int)(Defence * 0.85f));
                    Evasion += 8;
                }
                break;
            case MobRole.Mage:
                // No basic attack — casts the mob spells gated on MP; out of MP it stands helpless.
                if (roleStats)
                {
                    MagicAttack = Math.Max(1, (int)(MagicAttack * 1.5f));
                    AttackPower = Math.Max(1, (int)(AttackPower * 0.5f));
                    Defence = Math.Max(1, (int)(Defence * 0.7f));
                }
                BasicAttackPower = 1;
                BasicAttackRange = 0f;
                break;
        }

        // 4. The dummy's pool is a flat override, not a factor: it must read the same 1,000,000 at
        // every level, because it exists to be hit for ten seconds and show you the numbers.
        if (TrainingDummy)
        {
            MaxHp = TrainingDummyHp;
            HpRegenBonus = TrainingDummyRegen;
        }
    }

    /// <summary>GRADE PENALTY — the LAST layer of RecomputeDerived (owner, 2026-07-17).
    ///
    /// Wearing gear above your grade doesn't shrink that ITEM any more; it debuffs YOU. It runs last, on
    /// top of every gear/set/mastery/passive/buff bonus, precisely so it cannot be out-stacked: a level-1
    /// in A-grade keeps a tenth of the affected stats no matter what else he piles on (owner: "his whole
    /// stats window is multiplied by 0.1 — his 100 stats become 10").
    ///
    /// Two independent gaps, because each kind of gear penalises what it grants:
    ///   ARMOUR + jewels + shield → cast/attack/move speed, P.Def, M.Def, evasion
    ///   WEAPON                   → P/M attack, P/M crit rate, crit damage, accuracy
    /// The speed multipliers are TIMES (lower = faster), so a penalty DIVIDES them.
    ///
    /// In normal play both gaps are 0 and this whole method is a no-op — a level-40 wears level-40 gear.
    /// Deliberately NOT applied to mobs: they have no grade.</summary>
    private void ApplyGradePenalty()
    {
        if (Kind != EntityKind.Player) return;

        float armor = GradeArmorPenalty;
        if (armor < 1f)
        {
            Defence = (int)(Defence * armor);
            MagicDefence = (int)(MagicDefence * armor);
            Evasion = (int)(Evasion * armor);
            ShieldDefense = (int)(ShieldDefense * armor);
            // Speed multipliers are TIME factors: dividing by the penalty makes you slower.
            CastSpeedMultiplier = Math.Clamp(CastSpeedMultiplier / armor, 0.4f, 6f);
            AttackSpeedMultiplier = Math.Clamp(AttackSpeedMultiplier / armor, 0.4f, 6f);
            RunSpeed *= armor;
            WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor;
            Speed = RunSpeed;
        }

        float weapon = GradeWeaponPenalty;
        if (weapon < 1f)
        {
            AttackPower = Math.Max(1, (int)(AttackPower * weapon));
            MagicAttack = Math.Max(1, (int)(MagicAttack * weapon));
            BasicAttackPower = Math.Max(1, (int)(BasicAttackPower * weapon));
            CritChance *= weapon;
            MagicCritChance *= weapon;
            CritDamageBonus *= weapon;
            CritDamageFlat *= weapon;
            // Only the EXCESS shrinks: MagicCritDamageMult is 1-based, so scaling it whole would
            // penalise the ×2 base that every caster has, which no other line here does.
            MagicCritDamageMult = 1f + (MagicCritDamageMult - 1f) * weapon;
            Accuracy = (int)(Accuracy * weapon);
        }
    }

    public EntityDto ToDto() =>
        new(Id, Name, Kind, Race, BaseClass, X, Y, Speed, Level,
            Hp, MaxHp, Mp, MaxMp, SecondClass, ThirdClass, Dead, IsDisconnected, FlagState,
            Kind == EntityKind.Mob && Aggressive, Title, TitleColor, SocialClanShown);

    /// <summary>The social clan the target frame prints (playtest 23), or "" for a loner, a
    /// non-mob — or for EVERY mob while the clan system is switched off (`BL-73`). The frame must
    /// never advertise a behaviour the simulation is not running: with the switch off nothing answers
    /// a cry, so nothing is social, and saying otherwise would teach the player a rule that is false
    /// today and true again later.</summary>
    private string SocialClanShown =>
        Kind == EntityKind.Mob && GameConstants.MobClansEnabled && MobTypeId is string id
            ? MobCatalog.Get(id).Clan : "";

    /// <summary>The tick-to-tick DYNAMIC fields only (see EntityLean) — position, vitals, dead/dc/flag.
    /// Sent while an entity is already in view; the static fields ride the full spawn DTO.</summary>
    public EntityLean ToLean() =>
        // EffectiveSpeed, NOT the raw base Speed: the client PREDICTS self-movement at this value, and
        // the server MOVES at EffectiveSpeed (walk state, slows, stun/sit = 0, the /spd m admin
        // override). Sending raw Speed made the client predict ~150 while the server moved at 1 (or at
        // half while walking, or at all while stunned) — which is exactly the "set speed to 1 and it
        // rubber-bands" report. Now the two run the same number.
        new(Id, X, Y, EffectiveSpeed, Hp, Mp, Dead, IsDisconnected, FlagState);

    /// <summary>True if the STATIC parts of two DTOs match — i.e. the difference (if any) is purely
    /// dynamic and can go out as an EntityLean. A static change (level-up, class change, name) instead
    /// forces a fresh full spawn DTO so the client updates those fields too.</summary>
    public static bool StaticFieldsEqual(EntityDto a, EntityDto b) =>
        a.Name == b.Name && a.Kind == b.Kind && a.Race == b.Race && a.BaseClass == b.BaseClass &&
        a.Level == b.Level && a.MaxHp == b.MaxHp && a.MaxMp == b.MaxMp &&
        a.SecondClass == b.SecondClass && a.ThirdClass == b.ThirdClass && a.Aggressive == b.Aggressive &&
        // Title is static too: changing it (or losing the board, or recolouring it) must force a full
        // DTO, or the new title would only reach the people who walked into view after it changed.
        a.Title == b.Title && a.TitleColor == b.TitleColor && a.SocialClan == b.SocialClan;
}
