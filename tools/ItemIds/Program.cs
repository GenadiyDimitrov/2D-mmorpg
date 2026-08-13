using System.Globalization;
using System.Text;
using Game.Shared;

// Regenerates docs/guides/ItemIds.md — the `/give` reference the owner asked for in playtest-22:
// *"Need a grouped list (in a file - like the commands one) with each equip/item ID."*
//
// 🔑 GENERATED, never hand-written. A hand-kept id list is wrong the first time anyone adds an item,
// and a wrong id list is worse than none: it sends him hunting for a typo in the command when the
// item simply does not exist. Re-run this after touching ItemCatalog.

var outPath = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "guides", "ItemIds.md"));

var all = ItemCatalog.AllItems.ToList();
var sb = new StringBuilder();

sb.AppendLine("# Item ids — the complete `/give` reference");
sb.AppendLine();
sb.AppendLine("**Generated from `ItemCatalog`** by `tools/ItemIds` — do not hand-edit; re-run");
sb.AppendLine("`dotnet run --project tools/ItemIds` after adding or removing an item. Every id below is a real");
sb.AppendLine("id the server will accept today.");
sb.AppendLine();
sb.AppendLine($"**{all.Count} items.** Generated {DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
sb.AppendLine();
sb.AppendLine("```");
sb.AppendLine("/give <player> <itemId> [sellPrice] [tradable] [timed] [\"name\"] [enchant] [canStorePrivate] [canStoreAccount] [amount]");
sb.AppendLine();
sb.AppendLine("/give Gena mat_iron - - - - - - - 1000     # a thousand of a material, in one bag slot");
sb.AppendLine("```");
sb.AppendLine();
sb.AppendLine("Everything after the item id is optional and **positional**; `-` in any slot means *no opinion,");
sb.AppendLine("use the catalog*. See [ChatCommands.md](ChatCommands.md) for what each argument does.");
sb.AppendLine();
sb.AppendLine("**`[amount]`** defaults to 1 and is capped at 10,000. A **stackable** (materials, potions,");
sb.AppendLine("scrolls, quest items — the `stacks` note below) arrives as ONE bag row carrying the quantity;");
sb.AppendLine("**gear** cannot stack, so an amount there is that many separate rows and stops when the bag");
sb.AppendLine("is full (it tells you how many fit).");
sb.AppendLine();
sb.AppendLine("> 🔑 **Ids are also on the item card in game**, under the enchant line, for staff only —");
sb.AppendLine("> so you can read one off the thing in your bag instead of coming here.");
sb.AppendLine();

// Gear is grouped by SLOT then TIER, because that is how he shops for a test subject ("a level 40
// heavy body"). Everything else groups by slot alone — there is no ladder to sort it along.
static string Tier(ItemDef d) => d.ItemLevel > 0
    ? $"Lv {d.ItemLevel}"
    : "no tier (training / one-off)";

void Section(string title, IEnumerable<ItemDef> defs, bool byTier)
{
    var items = defs.ToList();
    if (items.Count == 0) return;
    sb.AppendLine($"## {title}  ({items.Count})");
    sb.AppendLine();

    IEnumerable<IGrouping<string, ItemDef>> groups = byTier
        ? items.GroupBy(Tier).OrderBy(g => g.First().ItemLevel)
        : new[] { items.GroupBy(_ => "").First() }.Concat(Array.Empty<IGrouping<string, ItemDef>>());

    if (!byTier)
    {
        Table(items.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase));
        return;
    }

    foreach (var g in groups)
    {
        sb.AppendLine($"### {g.Key}");
        sb.AppendLine();
        Table(g.OrderBy(d => (int)d.Grade).ThenBy(d => (int)d.Rarity).ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase));
    }
}

void Table(IEnumerable<ItemDef> defs)
{
    sb.AppendLine("| id | name | grade | rarity | notes |");
    sb.AppendLine("|---|---|---|---|---|");
    foreach (var d in defs)
    {
        var notes = new List<string>();
        if (!d.Tradable) notes.Add("untradable");
        if (d.SoulBound) notes.Add("**soulbound**");
        if (d.IsStackable) notes.Add("stacks");
        if (d.WeaponType != WeaponType.None) notes.Add(d.WeaponType.ToString());
        if (d.Weight != ArmorWeight.None) notes.Add(d.Weight.ToString());
        if (d.ArmorSlot != ArmorSlot.None) notes.Add(d.ArmorSlot.ToString());
        if (d.JewelType != JewelType.None) notes.Add(d.JewelType.ToString());
        sb.AppendLine($"| `{d.Id}` | {d.Name} | {d.Grade} | {d.Rarity} | {string.Join(", ", notes)} |");
    }
    sb.AppendLine();
}

Section("Weapons", all.Where(d => d.Slot == EquipSlot.Weapon), byTier: true);
Section("Shields", all.Where(d => d.Slot == EquipSlot.Shield), byTier: true);
Section("Armor", all.Where(d => d.Slot == EquipSlot.Armor), byTier: true);
Section("Jewels", all.Where(d => d.Slot == EquipSlot.Jewel), byTier: true);
Section("Runes", all.Where(d => d.Slot == EquipSlot.Rune), byTier: false);
Section("Consumables (potions)", all.Where(d => d.Slot == EquipSlot.Consumable), byTier: false);
Section("Scrolls", all.Where(d => d.Slot == EquipSlot.Scroll), byTier: false);
Section("Boxes", all.Where(d => d.Slot == EquipSlot.Box), byTier: false);
Section("Materials", all.Where(d => d.Slot == EquipSlot.Material), byTier: false);
Section("Quest items", all.Where(d => d.Slot == EquipSlot.QuestItem), byTier: false);

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
File.WriteAllText(outPath, sb.ToString());
Console.WriteLine($"Wrote {all.Count} items to {outPath}");
