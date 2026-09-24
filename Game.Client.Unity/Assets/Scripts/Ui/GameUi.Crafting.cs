using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: CRAFTING — the window the whole crafting system was missing.
    ///
    /// `BL-273` part 2 (0.203.0): there are no professions. A character becomes a crafter through the
    /// Master Crafter's trial at level 40 and then crafts everything; the window shows the LEARNED recipes
    /// (one slot each, gear at a %), the Master's generic recipes to learn, the slots to forget, and the mats.
    ///
    /// The split of responsibilities is the important part:
    ///   • The SERVER owns the crafter flag, the learned recipes, the craft points and the slot count, and
    ///     re-checks every rule on the Craft call. It pushes those as <see cref="CraftingUpdate"/>.
    ///   • This window reads the RECIPES out of <see cref="RecipeCatalog"/>, compiled into the client from
    ///     Game.Shared, so the costs, % scaling and chances it draws are the ones the server crafts from.
    ///
    /// Red on an ingredient means that one is what is stopping you.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _craftPanel, _craftList;
        private TextMeshProUGUI _craftTitle;
        private readonly List<Button> _craftTabButtons = new();
        private int _craftRevision = -1;

        /// <summary>Which page of the window is showing. Materials is a page rather than a section
        /// because "how many Rare Ingots do I have" is asked on its own, away from any one recipe.</summary>
        private enum CraftTab { Craft = 0, Learn = 1, Slots = 2, Materials = 3 }
        private CraftTab _craftTab = CraftTab.Craft;
        private static readonly string[] CraftTabNames = { "Craft", "Learn", "Points", "Mats" };

        // ----- `BL-245`: the keeper's shelf counts too ---------------------------------------------
        //
        //  *"crafter should see mats in private wharehouse -> maybe the crafting window can have a
        //  toggle button (on by default) [show keeper items]"*.
        //
        //  🔑 IT COUNTS **AND SPENDS**. The toggle does not merely tint a row green — it rides the
        //  Craft call, and the server takes the shortfall out of the warehouse. A window that showed
        //  4/4 Rare Ingots and then refused would be worse than one that never offered.
        //
        //  Bag first, bank second, on both sides. It is a CLIENT preference like the [ORDER] cycle —
        //  PlayerPrefs, never part of the character — because it changes what this window offers, not
        //  what the character is.
        private const string CraftKeeperPref = "l2c.craftKeeper";
        private Button _craftKeeperToggle;
        private bool _craftUseKeeper = true;

        private void BuildCraftingWindow()
        {
            _craftPanel = UiKit.PanelBox(_worldRoot, "Crafting");
            UiKit.Place(_craftPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(700f, 540f));
            var inner = _craftPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_craftPanel, "Crafting", () => CloseWindow(_craftPanel));

            _craftTitle = UiKit.Label(inner, "", 15f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_craftTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 6f), new Vector2(660f, 22f));

            // The four tabs lost 20px each to make room for [Keeper] on the same row (`BL-245`). A
            // third row would have cost the list 40px of height for one toggle; the tab captions are
            // four to six letters and never came close to filling 150.
            const float tabW = 130f, tabGap = 6f;
            for (int i = 0; i < CraftTabNames.Length; i++)
            {
                int index = i;
                var button = UiKit.TextButton(inner, CraftTabNames[i], () =>
                {
                    _craftTab = (CraftTab)index;
                    _craftRevision = -1;
                }, 15f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(16f + i * (tabW + tabGap), -chrome - 32f), new Vector2(tabW, 34f));
                _craftTabButtons.Add(button);
            }

            _craftUseKeeper = PlayerPrefs.GetInt(CraftKeeperPref, 1) != 0;
            _craftKeeperToggle = UiKit.TextButton(inner, "", () =>
            {
                _craftUseKeeper = !_craftUseKeeper;
                PlayerPrefs.SetInt(CraftKeeperPref, _craftUseKeeper ? 1 : 0);
                _craftRevision = -1;
            }, 14f);
            UiKit.Place(UiKit.Rect(_craftKeeperToggle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f + CraftTabNames.Length * (tabW + tabGap), -chrome - 32f),
                        new Vector2(126f, 34f));

            _craftList = UiKit.ScrollArea(inner, out var scroll, 4f);
            UiKit.Stretch((RectTransform)scroll.transform, 14f, chrome + 72f, 14f, 14f);

            _craftPanel.gameObject.SetActive(false);
        }

        public void OpenCraftingWindow() => OpenCraftingWindow(_craftTab);

        /// <summary>Open on a given page — the Master's "Learn" row lands on the Learn tab.</summary>
        private void OpenCraftingWindow(CraftTab tab)
        {
            _craftTab = tab;
            _craftRevision = -1;
            OpenWindow(_craftPanel);
        }

        /// <summary>Called by GameBoot when the server pushes new crafting state — a learned recipe or a
        /// level has to be visible without waiting for something else to change.</summary>
        public void RefreshCraftingWindow() => _craftRevision = -1;

        /// <summary>Rebuild when anything the list DEPENDS on changed: the tab, the crafter state, the
        /// learned recipes, your level and gold, or the bag (which decides every red/green ingredient).
        /// Revision-gated like the vendor and warehouse — these rows carry captured recipe ids and a
        /// per-frame rebuild would re-register every listener.</summary>
        private void RefreshCraftingList()
        {
            if (_craftPanel == null || !_craftPanel.gameObject.activeSelf) return;

            var bag = Boot.Inventory ?? Array.Empty<InventoryItemDto>();
            // `BL-245`: the KEEPER is an input to every red/green ingredient, so both the toggle and the
            // warehouse's own contents belong in the stamp.
            var keeper = _craftUseKeeper ? (Boot.Warehouse ?? Array.Empty<InventoryItemDto>())
                                         : Array.Empty<InventoryItemDto>();
            int revision = (int)_craftTab * 104729 + (Boot.IsCrafter ? 31513 : 0)
                         + SelfLevel() * 613 + Boot.CraftPoints * 65537
                         + Boot.CraftTypeLevels.Aggregate(0, (h, v) => h * 11 + v) * 7 + Boot.CraftRespecs * 13
                         + Boot.CraftSlots * 17 + (int)(Boot.Gold % 1000003)
                         + (Boot.AtCraftMaster ? 1046527 : 0) + (Boot.DialogNpcId != Guid.Empty ? 3 : 0)
                         + (_craftUseKeeper ? 15485863 : 0);
            foreach (var kv in Boot.KnownRecipes) revision = revision * 29 + kv.Key.GetHashCode() + kv.Value;
            foreach (var it in bag) revision = revision * 31 + it.DefId.GetHashCode() + it.Quantity;
            foreach (var it in keeper) revision = revision * 37 + it.DefId.GetHashCode() + it.Quantity;
            if (revision == _craftRevision) return;
            _craftRevision = revision;

            UiKit.SetButtonText(_craftKeeperToggle, _craftUseKeeper ? "Keeper: ON" : "Keeper: off");
            _craftKeeperToggle.targetGraphic.color = _craftUseKeeper ? UiKit.TabActive : UiKit.PanelLight;

            // The tabs are hidden until the trial is done — except while the trial's own recipe is held,
            // because its craft happens in this window too.
            bool open = Boot.IsCrafter || Boot.KnownRecipes.ContainsKey(Crafting.HammerRecipeId);
            for (int i = 0; i < _craftTabButtons.Count; i++)
            {
                _craftTabButtons[i].gameObject.SetActive(open);
                _craftTabButtons[i].targetGraphic.color =
                    (int)_craftTab == i ? UiKit.TabActive : UiKit.PanelLight;
            }

            for (int i = _craftList.childCount - 1; i >= 0; i--)
                Destroy(_craftList.GetChild(i).gameObject);

            if (!open) { BuildCrafterInvitation(); return; }

            var counts = MaterialCounts(bag, keeper);
            _craftTitle.text = CraftHeader();
            switch (_craftTab)
            {
                case CraftTab.Materials: BuildMaterialsPage(counts); return;
                case CraftTab.Learn: BuildLearnPage(); return;
                case CraftTab.Slots: BuildSlotsPage(); return;
            }

            var recipes = Boot.KnownRecipes.Keys
                .Select(RecipeCatalog.Get)
                .Where(r => r != null)
                .OrderBy(r => r.IsGear ? 0 : 1)
                .ThenBy(r => r.GearItemLevel)
                .ThenBy(r => OutputName(r), StringComparer.Ordinal)
                .ToList();
            if (recipes.Count == 0)
            {
                CraftNote("You know no recipes yet. Use a recipe item from your bag to learn it (anywhere), "
                        + "or learn potion, scroll and refine recipes from a Master Crafter (the Learn tab).");
                return;
            }
            foreach (var recipe in recipes) BuildRecipeRows(recipe, counts);
        }

        // ---- the header, and the invitation before you are a crafter -----------------------------

        /// <summary>Who you are as a crafter, how many slots, and whether the buttons below are live.</summary>
        private string CraftHeader()
        {
            string where = Boot.AtCraftMaster
                ? "<color=#8CD98C>at the anvil</color>"
                : Tinted("browsing — craft at a Master Crafter", false);
            if (!Boot.IsCrafter) return "The Master's Trial   " + where;
            return "Crafting L" + Boot.CraftLevel
                 + (Boot.CraftPointsFree > 0 ? "  <color=#E6C35C>" + Boot.CraftPointsFree + " point(s) to spend</color>" : "")
                 + "  (" + string.Join(" · ", Crafting.SpendableTypes.Select(t => TypeShort(t) + " " + Boot.CraftTypeLevel(t))) + ")"
                 + "   slots " + Boot.CraftSlotsUsed + "/" + Boot.CraftSlots + "   " + where;
        }

        /// <summary>Not a crafter yet (`BL-273` part 2): there are no professions, just one trial.</summary>
        private void BuildCrafterInvitation()
        {
            _craftTitle.text = "You are not a crafter yet.";
            CraftNote("Every town's Master Crafter gives a trial at level " + Crafting.CrafterQuestLevel
                    + ": gather materials, learn a recipe, and forge a hammer at his anvil. Finish it and "
                    + "you craft everything — weapons, armour, jewels, potions and scrolls — with "
                    + Crafting.BaseSlots + " recipe slots, growing with your crafting level.");
        }

        // ---- the Craft page: one row per learned recipe (and per usable recipe %) -----------------

        private void BuildRecipeRows(Recipe recipe, Dictionary<string, int> counts)
        {
            var outDef = ItemCatalog.Get(recipe.OutputId);
            string name = outDef?.Name ?? recipe.OutputId;
            string title = outDef != null ? Coloured(name, outDef.Rarity) : name;
            if (recipe.OutputQty > 1) title += "  x" + recipe.OutputQty;
            int learned = Boot.KnownRecipes.TryGetValue(recipe.Id, out var l) ? l : 100;

            if (recipe.IsGear)
            {
                // One row per recipe % you may spend: every % its tier has, at or below the learned one.
                foreach (int pct in Crafting.RecipePercentsFor(recipe.GearItemLevel))
                {
                    if (pct > learned) continue;
                    float bonus = Crafting.GearSuccessBonus(Boot.CraftTypeLevel(recipe.Type));
                    float chance = Mathf.Min(1f, pct / 100f + bonus);
                    BuildOneRow(recipe, title + "  <size=13>(learned " + learned + "%)</size>", name,
                                pct, chance, ItemCatalog.RecipeBookId(recipe.Id, pct), counts);
                }
                return;
            }
            if (recipe.QuestOnly)
            {
                BuildOneRow(recipe, title + "  <size=13>(the trial)</size>", name, Crafting.HammerRecipePercent,
                            recipe.SuccessChance, ItemCatalog.CrafterQuestRecipe, counts);
                return;
            }
            BuildOneRow(recipe, title, name, 0, recipe.SuccessChance, null, counts);
        }

        private void BuildOneRow(Recipe recipe, string title, string name, int pct, float chance,
                                 string recipeItemId, Dictionary<string, int> counts)
        {
            var parts = new List<string>();
            bool haveAll = true;
            foreach (var input in recipe.Inputs)
            {
                int need = recipe.IsGear ? Crafting.ScaledQty(input.Qty, pct) : input.Qty;
                int have = counts.TryGetValue(input.ItemId, out var c) ? c : 0;
                bool ok = have >= need;
                haveAll &= ok;
                parts.Add(Tinted(ShortItemName(input.ItemId) + " " + have + "/" + need, ok));
            }
            if (recipeItemId != null)
            {
                int have = counts.TryGetValue(recipeItemId, out var c) ? c : 0;
                bool ok = have >= 1;
                haveAll &= ok;
                parts.Add(Tinted("Recipe " + pct + "% " + have + "/1", ok));
            }
            int gold = recipe.GoldAt(Boot.CraftTypeLevel(recipe.Type));
            if (gold > 0)
            {
                bool ok = Boot.Gold >= gold;
                haveAll &= ok;
                parts.Add(Tinted(gold.ToString("N0") + " " + GameConstants.CurrencyName, ok));
            }

            int shown = Mathf.RoundToInt(chance * 100f);
            // LOCKED after a respec (0.204.0): the recipe keeps its slot but will not craft below its gate.
            bool locked = !recipe.QuestOnly && Boot.CraftTypeLevel(recipe.Type) < recipe.UnlockLevel;
            string status = locked ? Tinted("Locked: " + GateName(recipe), false)
                : chance >= 1f ? Tinted("Guaranteed", true) : Tinted(shown + "% success", true);
            haveAll &= !locked;

            // ⚠ AWAY FROM THE MASTER every row is dead — the browse mode. The have/need colouring is the
            // whole point of reading this in the field.
            bool enabled = haveAll && Boot.AtCraftMaster;
            string label = title + "   <size=13>" + status + "</size>\n<size=13>" + string.Join("   ", parts) + "</size>";

            string id = recipe.Id;                 // captured per row
            int usePct = pct;
            bool spendsRecipe = recipeItemId != null;
            CraftRow(label, enabled, () =>
            {
                // A guaranteed craft goes straight through; anything that can fail names the odds first,
                // because failure eats the materials (and the recipe) and that is not something to learn
                // by tapping.
                bool keeperOn = _craftUseKeeper;    // captured, so the tap spends what the row promised
                if (chance >= 1f) { Boot.Craft(id, keeperOn, usePct); return; }
                Ask("Craft " + name + "?\n\n<size=15>" + shown + "% chance to succeed. A failure still consumes "
                    + (spendsRecipe ? "the materials and the recipe." : "the materials.") + "</size>",
                    "Craft", () => Boot.Craft(id, keeperOn, usePct));
            });
        }

        // ---- the Learn page: the Master's generic recipes -----------------------------------------

        /// <summary>The potion, scroll and refine recipes a Master Crafter teaches for gold (`BL-273` part
        /// 2). Listed everywhere so a crafter can plan; the Learn buttons are live only with the Master's
        /// dialog open. ⚠ Unlock levels and prices are placeholders until step 9b's table.</summary>
        private void BuildLearnPage()
        {
            bool atMaster = Boot.AtCraftMaster && Boot.DialogNpcId != Guid.Empty;
            if (!Boot.IsCrafter) { CraftNote("Only a crafter can learn recipes."); return; }
            if (!atMaster) CraftNote("Talk to a Master Crafter to learn these.");
            bool slotFree = Boot.CraftSlotsUsed < Boot.CraftSlots;
            foreach (var recipe in RecipeCatalog.GenericForSale)
            {
                var outDef = ItemCatalog.Get(recipe.OutputId);
                string name = outDef?.Name ?? recipe.OutputId;
                string title = (outDef != null ? Coloured(name, outDef.Rarity) : name)
                             + (recipe.OutputQty > 1 ? "  x" + recipe.OutputQty : "");
                bool known = Boot.KnownRecipes.ContainsKey(recipe.Id);
                bool lvlOk = SelfLevel() >= recipe.LearnLevel;
                bool craftOk = Boot.CraftTypeLevel(recipe.Type) >= recipe.UnlockLevel;
                bool goldOk = Boot.Gold >= recipe.LearnPrice;
                string status = known ? Tinted("known", true)
                    : !craftOk ? Tinted(GateName(recipe), false)
                    : !lvlOk ? Tinted("Needs level " + recipe.LearnLevel, false)
                    : !slotFree ? Tinted("No free slot", false)
                    : Tinted(recipe.LearnPrice.ToString("N0") + " " + GameConstants.CurrencyName, goldOk);
                bool enabled = atMaster && !known && lvlOk && craftOk && goldOk && slotFree;
                string id = recipe.Id;
                int price = recipe.LearnPrice;
                CraftRow(title + "   <size=13>" + status + "</size>", enabled, () =>
                    Ask("Learn the " + name + " recipe for " + price.ToString("N0") + " "
                        + GameConstants.CurrencyName + "?\n\n<size=15>It takes one recipe slot.</size>",
                        "Learn", () => Boot.LearnRecipeAtMaster(id)));
            }
        }

        // ---- the Slots page: forget a recipe --------------------------------------------------------

        /// <summary>The crafter-points model (0.204.0): spend the generic level's points on the five types, respec
        /// at a Master, then every learned recipe, one slot each; tap one to forget it (anywhere, nothing
        /// refunded).</summary>
        private void BuildSlotsPage()
        {
            int free = Boot.CraftPointsFree;
            CraftNote("Each crafting level gives one point to spend on a type (" + free + " free). Smiths: T52 needs L2, "
                    + "T61 L4, T76 L6, T80 L8; L9 and L10 add +5% each. Scribe and Apothecary unlock their uncommon "
                    + "lines by the same tiers and craft cheaper each level (x0.90 down to x0.55).");
            foreach (var type in Crafting.SpendableTypes)
            {
                var t = type;                        // captured per row
                int lvl = Boot.CraftTypeLevel(t);
                bool can = free > 0 && lvl < Crafting.MaxCraftLevel;
                CraftRow(TypeName(t) + "  L" + lvl + "   <size=13>" + (can ? "tap to spend a point" : "") + "</size>",
                         can, () => Ask("Spend a point on " + TypeName(t) + " (L" + lvl + " -> L" + (lvl + 1) + ")?"
                                        + "\n\n<size=15>Points only come back with a respec at a Master Crafter.</size>",
                                        "Spend", () => Boot.SpendCraftPoint(t)));
            }
            int used = Boot.CraftRespecs;
            bool anySpent = Crafting.SpendableTypes.Any(t => Boot.CraftTypeLevel(t) > 0);
            bool atMaster = Boot.AtCraftMaster && Boot.DialogNpcId != Guid.Empty;
            if (used < Crafting.MaxRespecs)
            {
                long price = Crafting.RespecPrices[used];
                string why = !anySpent ? "nothing spent" : !atMaster ? "at a Master Crafter"
                           : price.ToString("N0") + " " + GameConstants.CurrencyName;
                CraftRow("Respec (" + used + "/" + Crafting.MaxRespecs + " used)   <size=13>"
                         + Tinted(why, anySpent && atMaster && Boot.Gold >= price) + "</size>",
                         anySpent && atMaster && Boot.Gold >= price,
                         () => Ask("Respec your crafting points for " + price.ToString("N0") + " "
                                   + GameConstants.CurrencyName + "?\n\n<size=15>Every point comes back. Recipes above "
                                   + "your new levels stay in their slots, LOCKED, until you reach them again. You get "
                                   + Crafting.MaxRespecs + " respecs a lifetime.</size>",
                                   "Respec", () => Boot.RespecCraft()));
            }
            else CraftNote("All " + Crafting.MaxRespecs + " respecs used.");

            CraftNote("Slots " + Boot.CraftSlotsUsed + "/" + Boot.CraftSlots + ". Each crafting level adds "
                    + Crafting.SlotsPerLevel + ". Forgetting a recipe frees its slot and refunds nothing.");
            foreach (var kv in Boot.KnownRecipes.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (kv.Key == Crafting.HammerRecipeId) continue;
                var recipe = RecipeCatalog.Get(kv.Key);
                if (recipe == null) continue;
                string name = OutputName(recipe);
                string id = kv.Key;
                CraftRow(name + "   <size=13>" + (recipe.IsGear ? kv.Value + "%" : "generic") + " — tap to forget</size>",
                         true, () => Ask("Forget the " + name + " recipe?\n\n<size=15>Nothing is refunded; you "
                                         + "would have to learn it again from a new recipe.</size>",
                                         "Forget", () => Boot.ForgetRecipe(id)));
            }
        }

        // ---- the materials page --------------------------------------------------------------------

        /// <summary>Every material in the game with what you hold of it, laid out type by type. This is
        /// the page you read BEFORE a farm session, so the ones you have none of are still listed —
        /// showing only what is in the bag would hide exactly the thing you are short of.</summary>
        private void BuildMaterialsPage(Dictionary<string, int> counts)
        {
            _craftTitle.text = "Materials — every rarity drops; refining is the other way up the ladder.";

            foreach (var type in Crafting.MaterialTypes)
            {
                var line = new List<string>();
                foreach (var rarity in Crafting.MaterialRarities)
                {
                    string id = Crafting.MaterialId(type, rarity);
                    int have = counts.TryGetValue(id, out var c) ? c : 0;
                    var colour = ItemCatalog.Get(id) is ItemDef d ? d.Rarity : rarity;
                    line.Add(Coloured(rarity.ToString(), colour) + " "
                             + (have > 0 ? have.ToString() : "<color=#8A9099>0</color>"));
                }

                CraftNoteRow(MaterialWord(type) + "\n<size=14>" + string.Join("    ", line) + "</size>");
            }
        }

        // ---- helpers ---------------------------------------------------------------------------------

        private static string TypeName(CraftType t) => t switch
        {
            CraftType.Weapon => "Weaponsmith",
            CraftType.Armour => "Armoursmith",
            CraftType.Jewels => "Jeweler",
            CraftType.Apothecary => "Apothecary",
            CraftType.Scribe => "Scribe",
            _ => "Crafting",
        };

        private static string TypeShort(CraftType t) => t switch
        {
            CraftType.Weapon => "weapon",
            CraftType.Armour => "armour",
            CraftType.Jewels => "jewels",
            CraftType.Apothecary => "apothecary",
            CraftType.Scribe => "scribe",
            _ => "generic",
        };

        /// <summary>What a recipe's gate reads, e.g. "Scribe L7" (or "Crafting L3" for a refine).</summary>
        private static string GateName(Recipe recipe) => TypeName(recipe.Type) + " L" + recipe.UnlockLevel;

        /// <summary>Everything a craft may spend, summed by item id: the bag, plus the private
        /// warehouse when [Keeper] is on (`BL-245`). Materials stack, but a stack can still be split
        /// across slots, so this sums rather than taking the first row it finds.
        ///
        /// <para>⚠ It must count exactly what <c>CraftCount</c> on the server counts. The server holds
        /// the same two containers behind the same flag; if this window summed a third source, or
        /// skipped one, the rows would go green on a craft the server then refuses.</para></summary>
        private static Dictionary<string, int> MaterialCounts(InventoryItemDto[] bag,
                                                              InventoryItemDto[] keeper)
        {
            var counts = new Dictionary<string, int>();
            foreach (var item in bag)
            {
                if (item.Equipped) continue;              // a worn item is not an ingredient
                counts.TryGetValue(item.DefId, out var have);
                counts[item.DefId] = have + Mathf.Max(1, item.Quantity);
            }
            foreach (var item in keeper)
            {
                counts.TryGetValue(item.DefId, out var have);
                counts[item.DefId] = have + Mathf.Max(1, item.Quantity);
            }
            return counts;
        }

        private int SelfLevel() => Boot.ActiveClass?.Level ?? 1;

        private static string OutputName(Recipe r) => ItemCatalog.Get(r.OutputId)?.Name ?? r.OutputId;

        /// <summary>An ingredient's name, short enough for a phone row. A material is "Rare Ingot"
        /// rather than its full catalog name so a four-ingredient line still fits.</summary>
        private static string ShortItemName(string itemId)
        {
            var def = ItemCatalog.Get(itemId);
            return def?.Name ?? itemId;
        }

        private static string Tinted(string text, bool ok) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(ok ? UiKit.Good : UiKit.Bad) + ">" + text + "</color>";

        private static string MaterialWord(MaterialType t) => t switch
        {
            MaterialType.Ingot => "Ingots",
            MaterialType.Leather => "Leather",
            MaterialType.Gem => "Gems",
            MaterialType.Wood => "Wood",
            _ => "Thread",
        };

        // ---- row primitives ---------------------------------------------------------------------

        private void CraftRow(string text, bool enabled, Action onTap)
        {
            var button = UiKit.TextButton(_craftList, text, enabled ? onTap : null, 16f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.Left;
                label.color = enabled ? UiKit.Text : UiKit.TextDim;
            }
            // A locked row stays TAPPABLE-looking but does nothing, rather than being hidden: knowing a
            // recipe exists three levels ahead is most of what a crafting list is for.
            button.interactable = enabled;
            button.targetGraphic.color = enabled ? UiKit.PanelLight : UiKit.Panel;
            button.gameObject.AddComponent<LayoutElement>().minHeight = 62f;
        }

        private void CraftNoteRow(string text)
        {
            var label = UiKit.Label(_craftList, text, 16f, UiKit.Text, TextAlignmentOptions.TopLeft);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 54f;
        }

        private void CraftNote(string text)
        {
            var label = UiKit.Label(_craftList, text, 14f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 56f;
        }
    }
}
