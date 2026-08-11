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
    /// The server has been able to craft since 2026-07-06: professions, refinement, finished-item and
    /// consumable recipes, blueprints, and a mats-primary drop table all shipped then. None of it was
    /// reachable, because the phone had no window and the debug panel's profession rows were the only
    /// thing that ever touched it. That is why crafting has sat at the top of the "content blocker"
    /// list through four playtests while being, in code, already built.
    ///
    /// The split of responsibilities is the important part:
    ///   • The SERVER owns the profession and the unlocked blueprints, and re-checks every rule on the
    ///     Craft call. It pushes those two things as <see cref="CraftingUpdate"/> and nothing else.
    ///   • This window reads the RECIPES out of <see cref="RecipeCatalog"/>, which is compiled into the
    ///     client from Game.Shared. So the costs, level gates and success chances it draws are the very
    ///     same values the server crafts from — they cannot drift, and no recipe list has to travel.
    ///
    /// Rows are deliberately "what it makes" over "what it costs, and what I have": the question a
    /// crafter actually has is never "what is the recipe", it is "can I make this yet", and that is a
    /// comparison the window can do for you. Red on an ingredient means that one is what is stopping you.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _craftPanel, _craftList;
        private TextMeshProUGUI _craftTitle;
        private readonly List<Button> _craftTabButtons = new();
        private int _craftRevision = -1;

        /// <summary>Which page of the window is showing. Materials is a page rather than a section
        /// because "how many Rare Ingots do I have" is asked on its own, away from any one recipe.</summary>
        private enum CraftTab { Refine = 0, Gear = 1, Goods = 2, Materials = 3 }
        private CraftTab _craftTab = CraftTab.Refine;
        private static readonly string[] CraftTabNames = { "Refine", "Gear", "Goods", "Mats" };

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

            const float tabW = 150f, tabGap = 6f;
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

            _craftList = UiKit.ScrollArea(inner, out var scroll, 4f);
            UiKit.Stretch((RectTransform)scroll.transform, 14f, chrome + 72f, 14f, 14f);

            _craftPanel.gameObject.SetActive(false);
        }

        public void OpenCraftingWindow()
        {
            _craftRevision = -1;
            OpenWindow(_craftPanel);
        }

        /// <summary>Called by GameBoot when the server pushes new crafting state — a profession pick or
        /// a blueprint unlock has to be visible without waiting for something else to change.</summary>
        public void RefreshCraftingWindow() => _craftRevision = -1;

        /// <summary>Rebuild when anything the list DEPENDS on changed: the tab, the profession, the
        /// blueprints, your level, or the bag (which decides every red/green ingredient). Revision-gated
        /// like the vendor and warehouse — these rows carry captured recipe ids and a per-frame rebuild
        /// would re-register every listener.</summary>
        private void RefreshCraftingList()
        {
            if (_craftPanel == null || !_craftPanel.gameObject.activeSelf) return;

            var bag = Boot.Inventory ?? Array.Empty<InventoryItemDto>();
            int revision = (int)_craftTab * 104729 + (int)Boot.CraftProfession * 31513
                         + Boot.KnownRecipes.Count * 7919 + SelfLevel() * 613;
            foreach (var it in bag) revision = revision * 31 + it.DefId.GetHashCode() + it.Quantity;
            if (revision == _craftRevision) return;
            _craftRevision = revision;

            // The tabs are hidden until a profession exists: every page behind them is defined BY the
            // profession, so before the choice they are four buttons that all lead to the same chooser.
            bool chosen = Boot.CraftProfession != Profession.None;
            for (int i = 0; i < _craftTabButtons.Count; i++)
            {
                _craftTabButtons[i].gameObject.SetActive(chosen);
                _craftTabButtons[i].targetGraphic.color =
                    (int)_craftTab == i ? UiKit.TabActive : UiKit.PanelLight;
            }

            for (int i = _craftList.childCount - 1; i >= 0; i--)
                Destroy(_craftList.GetChild(i).gameObject);

            if (!chosen) { BuildProfessionChooser(); return; }

            var counts = MaterialCounts(bag);
            if (_craftTab == CraftTab.Materials) { BuildMaterialsPage(counts); return; }

            _craftTitle.text = ProfessionName(Boot.CraftProfession) + " — "
                             + "refines " + MaterialWord(RefinedTypeOf(Boot.CraftProfession))
                             + ".  A failed craft still spends the materials.";

            var recipes = RecipeCatalog.ForProfession(Boot.CraftProfession)
                .Where(r => TabOf(r) == _craftTab)
                .OrderBy(r => r.LearnLevel)
                .ThenBy(r => OutputName(r), StringComparer.Ordinal)
                .ToList();

            if (recipes.Count == 0)
            {
                CraftNote(_craftTab == CraftTab.Gear
                    ? "A " + ProfessionName(Boot.CraftProfession) + " makes no equipment."
                    : "Nothing here for a " + ProfessionName(Boot.CraftProfession) + ".");
                return;
            }

            foreach (var recipe in recipes) BuildRecipeRow(recipe, counts);
        }

        // ---- the profession pick -----------------------------------------------------------------

        /// <summary>The one-time, permanent choice. Behind a confirm naming the profession, because
        /// there is no re-spec: this is the only irreversible button in the window.</summary>
        private void BuildProfessionChooser()
        {
            _craftTitle.text = "Choose a profession — this is PERMANENT and cannot be changed.";

            foreach (Profession p in new[]
            {
                Profession.WeaponSmith, Profession.ArmorSmith, Profession.Jeweler,
                Profession.PotionMaster, Profession.ScrollScribe
            })
            {
                Profession pick = p;
                string body = "<size=13><color=#AEB4BE>Refines " + MaterialWord(RefinedTypeOf(p))
                            + ".  Crafts " + MakesWord(p) + ".</color></size>";
                CraftRow(ProfessionName(p) + "\n" + body, true, () =>
                    Ask("Become a " + ProfessionName(pick) + "?\n\n<size=15>A profession is PERMANENT — "
                        + "it cannot be changed or unlearned. You will still be able to trade for what "
                        + "the other four make.</size>",
                        "Choose", () => Boot.ChooseProfession(pick)));
            }

            CraftNote("Every profession refines its own material type; a finished item needs several, so "
                    + "the rest come from drops or trade. Recipes unlock as you level.");
        }

        // ---- a recipe row ------------------------------------------------------------------------

        private void BuildRecipeRow(Recipe recipe, Dictionary<string, int> counts)
        {
            var outDef = ItemCatalog.Get(recipe.OutputId);
            string name = outDef?.Name ?? recipe.OutputId;
            string title = outDef != null ? Coloured(name, outDef.Rarity) : name;
            if (recipe.OutputQty > 1) title += "  x" + recipe.OutputQty;

            // Three ways a recipe can be closed to you, and they want different words: too low a level,
            // a blueprint you have not found, or simply missing materials.
            bool levelLocked = !recipe.DropOnly && SelfLevel() < recipe.LearnLevel;
            bool needsBlueprint = recipe.DropOnly && !Boot.KnownRecipes.Contains(recipe.Id);

            // A DropOnly recipe ALSO spends one blueprint per craft (the owner's rule: one to unlock,
            // one every time after) — so it is listed as an ingredient, not just a gate.
            string blueprintId = recipe.DropOnly ? ItemCatalog.RecipeBookId(recipe.Id) : null;
            if (blueprintId != null && ItemCatalog.Get(blueprintId) == null) blueprintId = null;

            var parts = new List<string>();
            bool haveAll = true;
            foreach (var input in recipe.Inputs)
            {
                int have = counts.TryGetValue(input.ItemId, out var c) ? c : 0;
                bool ok = have >= input.Qty;
                haveAll &= ok;
                parts.Add(Tinted(ShortItemName(input.ItemId) + " " + have + "/" + input.Qty, ok));
            }
            if (blueprintId != null)
            {
                int have = counts.TryGetValue(blueprintId, out var c) ? c : 0;
                bool ok = have >= 1;
                haveAll &= ok;
                parts.Add(Tinted("Blueprint " + have + "/1", ok));
            }

            string second = string.Join("   ", parts);
            string status =
                levelLocked ? Tinted("Needs level " + recipe.LearnLevel, false)
                : needsBlueprint ? Tinted("Blueprint not learned", false)
                : recipe.SuccessChance >= 1f ? Tinted("Guaranteed", true)
                : Tinted(Mathf.RoundToInt(recipe.SuccessChance * 100f) + "% success", true);

            bool enabled = !levelLocked && !needsBlueprint && haveAll;
            string label = title + "   <size=13>" + status + "</size>\n<size=13>" + second + "</size>";

            string id = recipe.Id;                 // captured per row
            float chance = recipe.SuccessChance;
            CraftRow(label, enabled, () =>
            {
                // A guaranteed craft goes straight through; a risky one names the odds first, because
                // failure eats the materials and that is not something to discover by tapping.
                if (chance >= 1f) { Boot.Craft(id); return; }
                Ask("Craft " + name + "?\n\n<size=15>" + Mathf.RoundToInt(chance * 100f)
                    + "% chance to succeed. A failure still consumes the materials.</size>",
                    "Craft", () => Boot.Craft(id));
            });
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

                string owner = Crafting.RefinerOf(type) == Boot.CraftProfession ? "   (yours)" : "";
                CraftNoteRow(MaterialWord(type) + owner + "\n<size=14>" + string.Join("    ", line) + "</size>");
            }
        }

        // ---- helpers ---------------------------------------------------------------------------------

        /// <summary>Bag contents summed by item id. Materials stack, but a stack can still be split
        /// across slots, so this sums rather than taking the first row it finds.</summary>
        private static Dictionary<string, int> MaterialCounts(InventoryItemDto[] bag)
        {
            var counts = new Dictionary<string, int>();
            foreach (var item in bag)
            {
                if (item.Equipped) continue;              // a worn item is not an ingredient
                counts.TryGetValue(item.DefId, out var have);
                counts[item.DefId] = have + Mathf.Max(1, item.Quantity);
            }
            return counts;
        }

        private int SelfLevel() => Boot.ActiveClass?.Level ?? 1;

        private static CraftTab TabOf(Recipe r)
        {
            if (r.Id.StartsWith("refine_", StringComparison.Ordinal)) return CraftTab.Refine;
            var def = ItemCatalog.Get(r.OutputId);
            return def != null && def.Slot is EquipSlot.Weapon or EquipSlot.Armor
                                          or EquipSlot.Shield or EquipSlot.Jewel
                ? CraftTab.Gear : CraftTab.Goods;
        }

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

        private static MaterialType RefinedTypeOf(Profession p) => p switch
        {
            Profession.WeaponSmith => MaterialType.Ingot,
            Profession.ArmorSmith => MaterialType.Leather,
            Profession.Jeweler => MaterialType.Gem,
            Profession.PotionMaster => MaterialType.Wood,
            _ => MaterialType.Thread,
        };

        private static string MaterialWord(MaterialType t) => t switch
        {
            MaterialType.Ingot => "Ingots",
            MaterialType.Leather => "Leather",
            MaterialType.Gem => "Gems",
            MaterialType.Wood => "Wood",
            _ => "Thread",
        };

        private static string MakesWord(Profession p) => p switch
        {
            Profession.WeaponSmith => "weapons",
            Profession.ArmorSmith => "armour and shields",
            Profession.Jeweler => "jewellery",
            Profession.PotionMaster => "potions",
            _ => "enchant and attribute scrolls",
        };

        /// <summary>"WeaponSmith" reads as one word on a button; split it for prose.</summary>
        private static string ProfessionName(Profession p) => p switch
        {
            Profession.WeaponSmith => "Weapon Smith",
            Profession.ArmorSmith => "Armour Smith",
            Profession.PotionMaster => "Potion Master",
            Profession.ScrollScribe => "Scroll Scribe",
            Profession.Jeweler => "Jeweler",
            _ => "None",
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
