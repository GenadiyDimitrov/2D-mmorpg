namespace Game.Shared;

/// <summary>NPC "newbie buffer" buffs — strong, long (1 hour) versions that represent a
/// 3rd-class buffer's MAX-level buffs (a 4th class will give more; the buffer class also
/// gets a Prophecy). They share the SAME BuffKey as the player self-buffs so they OVERRIDE
/// the weaker self-cast versions (high Rank). Not learnable — applied by an NpcRole.Buffer
/// NPC. Channel-split atk (Might = P.Atk, Force = M.Atk) via the buff/effect layer.</summary>
public static partial class SkillCatalog
{
    // The five ORIGINAL blessings. Each was one monolithic multi-effect buff on its own key —
    // which is exactly why a Might potion could stack on top of the buffer's Might. They are now
    // SINGLES too (owner 2026-07-31): npc_might hands out the P.Atk rung and nothing else, and its
    // old companions have their own buttons below. Ids kept (append-only), meaning changed.
    public const string NpcMight  = "npc_might";     // Might   — % P.Atk
    public const string NpcForce  = "npc_force";     // Force   — % M.Atk
    public const string NpcFocus  = "npc_focus";     // Focus   — % crit rate
    public const string NpcSpeed  = "npc_speed";     // the old GROUP — no longer offered
    public const string NpcBody   = "npc_body";      // Body    — % Max HP
    public const string NpcFrenzy = "npc_frenzy";    // Frenzy  — the whole trade-off buff
    // The four SPEED singles, one hour each — the scroll tier handed out one buff at a time.
    // They replaced the "Improved Speed" GROUP the buffer used to give (owner 2026-07-31).
    public const string NpcSwift    = "npc_swift";
    public const string NpcAlacrity = "npc_alacrity";
    public const string NpcAgility  = "npc_agility";
    public const string NpcHaste    = "npc_haste";
    // The rest of the singles, one per family, so every effect the buffer used to bundle can now
    // be taken (and cancelled) on its own — and so a potion of that family competes with it.
    public const string NpcBulwark   = "npc_bulwark";     // % P.Def
    public const string NpcVampirism = "npc_vampirism";   // % melee vampirism
    public const string NpcAccuracy  = "npc_accuracy";    // flat accuracy
    public const string NpcWard      = "npc_ward";        // % M.Def
    public const string NpcResolve   = "npc_resolve";     // interrupt resistance, as a PERCENT (IG formula, 2026-08-26)
    public const string NpcFerocity  = "npc_ferocity";    // % crit damage
    public const string NpcInsight   = "npc_insight";     // % magic crit rate
    public const string NpcSoul      = "npc_soul";        // % Max MP
    public const string NpcVigor     = "npc_vigor";       // % HP regeneration
    public const string NpcSerenity  = "npc_serenity";    // % MP regeneration
    // The IMPROVED (group) versions at max rungs. No NPC hands these out — a buffer CLASS does —
    // but the admin buff button grants them so the group shape can be seen and tested (owner).
    public const string NpcMightGroup = "npc_might_group";   // Might and Bulwark
    public const string NpcForceGroup = "npc_force_group";   // Force and Ward
    public const string NpcFocusGroup = "npc_focus_group";   // Focus and Ferocity
    public const string NpcBodyGroup  = "npc_body_group";    // Body and Soul
    // "Harmony" GREATER buffs (max-level; owner 2026-07-03). They STACK on top of the six above
    // (distinct BuffKeys). NO LONGER OFFERED by the newbie buffer — see NewbieBuffSet. The defs
    // stay because a real 3rd-class buffer is meant to have them; nothing grants them today.
    public const string NpcHarmonyProtection = "npc_harmony_protection";
    public const string NpcHarmonyWarrior    = "npc_harmony_warrior";
    public const string NpcHarmonyWizard     = "npc_harmony_wizard";
    // ---- `BL-160`, the EIGHT SINGLE HARMONIES the NPC buffer sells (owner, 2026-09-04). Each lifts
    //      ONE effect out of a Warchanter harmony and unlocks at the exact level the Warchanter gains
    //      that effect, so the NPC is always precisely one rung behind the class.
    // 🔑 THESE ARE NOT THE THREE ABOVE. `npc_harmony_warrior`/`_wizard`/`_protection` are the
    //    Warchanter's own multi-rung CLASS harmonies (append-only ids). His first CSV draft reused
    //    `npc_harmony_warrior` here, which would have overwritten a class skill; he corrected the file
    //    himself the same day. Read the id, not the name.
    public const string NpcHWard     = "npc_harmony_ward";       // 44 — Protection r1: +30% M.Def
    public const string NpcHForce    = "npc_harmony_force";      // 48 — Wizard r1:     +10% M.Atk
    public const string NpcHSwift    = "npc_harmony_swift";      // 48 — Speed r1:      +20 move
    public const string NpcHAlacrity = "npc_harmony_alacrity";   // 52 — Wizard r2:     +30% cast speed
    public const string NpcHBulwark  = "npc_harmony_bulwark";    // 56 — Protection r3: +25% P.Def
    public const string NpcHMight    = "npc_harmony_might";      // 56 — Warrior r4:    +12% P.Atk
    public const string NpcHFury     = "npc_harmony_fury";       // 58 — Warrior r5:    +15% attack speed
    public const string NpcHBody     = "npc_harmony_body";       // 66 — Protection r4: +30% Max HP

    // ---- Their BUFF KEYS. Separate strings from the ids above and deliberately so: the id is what a
    //      table grants, the key is what the bar competes on. They are consts rather than literals at
    //      the def site because the four CLASS harmonies have to name them (`CoveredKeys`), and a key
    //      spelled twice by hand is exactly how `BL-183`'s dead rule survived review — it *looked*
    //      right at both ends. `SkillCatalog` validates at startup that each is really in use.
    public const string KeyHWard     = "npc_h_ward";
    public const string KeyHForce    = "npc_h_force";
    public const string KeyHSwift    = "npc_h_swift";
    public const string KeyHAlacrity = "npc_h_alacrity";
    public const string KeyHBulwark  = "npc_h_bulwark";
    public const string KeyHMight    = "npc_h_might";
    public const string KeyHFury     = "npc_h_fury";
    public const string KeyHBody     = "npc_h_body";

    /// <summary>`BL-160` — the eight, in shelf order (cheapest level first). Each has its OWN BuffKey,
    /// so all eight can sit on the bar together — but every one of them is contained in a Warchanter
    /// CLASS harmony, which since `BL-183` covers it: the class version evicts the single it contains
    /// and refuses it afterwards, exactly as an improved group does to its singles. Owner, 2026-09-06:
    /// *"Think of them as single harmonies and group harmonies -> group buffs replaces singles."*</summary>
    public static readonly string[] NpcSingleHarmonySet =
        { NpcHWard, NpcHForce, NpcHSwift, NpcHAlacrity, NpcHBulwark, NpcHMight, NpcHFury, NpcHBody };

    /// <summary>The rank a CLASS harmony competes at, one above <see cref="NpcBuffRank"/>.
    ///
    /// <para>🔑 `BL-183` — this number is what makes covering actually bite. Both shelves used to sit
    /// at <see cref="NpcBuffRank"/>, and at EQUAL rank <c>ApplyBuff</c> keeps whichever has the longer
    /// time left: an NPC single harmony runs an HOUR, a class harmony five minutes, so declaring the
    /// covering alone would have inverted the rule and let the bought single refuse the Warchanter's
    /// own. One above, and the class version wins by rank at every rung, both directions.</para>
    ///
    /// <para>⚠ Every rung shifts together (<c>BuffPlan</c> adds <c>level - 1</c> to a childless
    /// multi-level buff), so this does not change how a harmony competes with ITSELF — only with the
    /// eight singles it now covers, which is the whole point.</para></summary>
    public const int HarmonyRank = NpcBuffRank + 1;

    /// <summary>`BL-161` — the three Marks, sold at 78 for 300,000 each. They are the Lightbringer's own
    /// 4th-class skills at RUNG 1 (she learns rung 1 at 78 and rung 2 at 83, which the NPC never sells),
    /// so nothing new is authored here. They do not stack with each other — their own text says so —
    /// which is why one Mark, not three, is what a full endgame set costs.</summary>
    public static readonly string[] NpcMarkSet = { HolyMark, LifeMark, BloodMark };
    // (Shield Bless and Harden — his `buffer 3rd.csv` @66 — is NOT here. It is a real improved GROUP
    // over the two shield families, so it lives with the other groups in Skills.Healer.cs as
    // `HolyShield`. AdminBuffSet still hands it out.)

    public const int NpcBuffTicks = 36000;   // 1 hour @ 10 ticks/s
    public const int NpcBuffRank  = 100;     // overrides player self-buffs (rank 1-4)

    /// <summary>The buffs the newbie buffer NPC grants: the BASIC tier only, one hour each — the
    /// scroll tier (owner 2026-07-31: "not the improved and harmonies … just the scroll buffs,
    /// 1h of single basic buff"). So no GROUP buff and no Harmony: the buffer's edge over a potion
    /// is the DURATION, and its ceiling stays below a real buffer class, which keeps the improved
    /// groups. Frenzy stays in (it's a FULL buffer; cancel that one buff if you don't want its
    /// −10% Max HP/MP).
    ///
    /// 🔑 CUT FROM NINETEEN TO ELEVEN, playtest 28. He named the survivors by hand — *"npc buffer
    /// should have only: Body, vigor, resolve, alacrity, might, bulwarc, vamp, ward, force, fury,
    /// frenzy -> (p/m.Def, p/m.atk, p/m.speed, hp max/regen, cast interrupt/vamp, frenzy) … those 11
    /// buffs are enough for the start of the game"* — and his parenthesis is the shape: ONE buff per
    /// axis that a levelling character actually feels, and nothing per axis that only matters once you
    /// are optimising. So the eight that went are the optimiser's row: Aim, Focus, Ferocity and Insight
    /// (the whole accuracy/crit block), Soul and Serenity (the MP pair — the HP pair stayed because
    /// dying is the thing a new character does), Swift (move speed — Dash and a mount cover it) and
    /// Agility (evasion).
    ///
    /// ⚠ This is also the other half of the buff CAP (`BL-87`). Nineteen NPC singles against a cap of
    /// twenty meant taking the full set left you one free slot and no room for a real buffer's groups;
    /// eleven leaves nine, so the NPC set and a party buffer can now sit on the same bar — which is
    /// what makes a buffer worth grouping with instead of a cheaper substitute for one.
    ///
    /// ⚠ "Fury" is <see cref="NpcHaste"/>: the ATTACK-SPEED family is named Fury on the ladder and its
    /// NPC single kept the older id. Alacrity is CAST speed. Neither is a typo for the other.</summary>
    /// 🔑 SWIFT CAME BACK, 2026-08-27 — *"add swift in the NPC buffer - i missed it apparently"*. It
    /// was cut above on the reasoning that Dash and a mount cover move speed; his correction is that
    /// they do not, at the level this set is for. TWELVE against the cap of twenty, so a real buffer's
    /// groups still fit beside the full NPC set — which was the other half of why the list was trimmed.
    ///
    /// 🔑 AND FOUR MORE, 2026-08-28 (`BL-95`) — SIXTEEN. He listed the set by hand again and added
    /// *"serenity, soul, aim, agility"*, with the reason attached to each pair: *"players to not be so
    /// overwhelmed by mobs (serenity, soul — longer mage sessions; agility + aim — fighter less misses,
    /// dagger less hits taken)"*. That is the trim of playtest 28 partly reversed, and knowingly: the
    /// eight cut then were called "the optimiser's row", but two of those axes turn out to decide how
    /// long a SESSION is rather than how good a parse is — a mage out of MP and a dagger eating every
    /// swing both stop playing. Focus, Ferocity and Insight (the crit block) stay out; they are still
    /// the optimiser's row.
    ///
    /// ⚠ Sixteen against the cap of twenty leaves FOUR free slots, not eight. That is the cost, and it
    /// is why the two role presets below exist.
    ///
    /// ═══ EVERYTHING ABOVE THIS LINE IS HISTORY, NOT THE CURRENT RULE ═══
    /// It is kept because the REASONING still decides things (which axes a levelling character feels,
    /// why the crit block was once called "the optimiser's row"), but every COUNT in it — eleven,
    /// twelve, sixteen — and the cap arithmetic that goes with it were superseded on 2026-09-03. Read
    /// the `BL-150` block below as the spec.
    /// 🔑 NINETEEN, and split into TWO TIERS — 2026-09-03, `BL-150`. He re-ruled the whole shape:
    /// *"i would like npc to give fury/alacrity/force/mght/bulwark/swift/vamp/resolve from 6+,
    /// body,soul,vigor,serenity,agility,aim,ward,frenzy 40+"*, and separately *"add and the
    /// focus,ferocity,insight to the npc 40+ as well"*.
    ///
    /// <para>🔑 <b>THE FREE/PAID LINE IS THE BUFF, NOT THE PLAYER'S LEVEL.</b> That is the whole
    /// reversal, and it is easy to read the other way round: the old rule was "everyone is free below
    /// 75, everyone pays above". The new one is *"6~75 (starting ones bulwark/might etc) buffs be
    /// free, and 40~75 (the 40+ buffs aim/agi etc) be paid"* — so a level 80... a level 74 character
    /// still pays nothing for Might and 15,000 for Aim, and the two answers never depend on who is
    /// asking. See <see cref="FreeNpcBuffSet"/>.</para>
    ///
    /// <para>⚠ NINETEEN AGAINST A BUFF CAP OF TWENTY, which is exactly the state playtest 28 trimmed
    /// the set from nineteen down to eleven to escape. It is deliberate this time and the reasoning
    /// has changed: a real buffer's GROUPS evict the singles they cover (18 of these 19 collapse into
    /// 5 group squares), so the squeeze is only ever felt by a player buffing SOLO — who has nothing
    /// else to put in those slots anyway, except their own class self-buffs. Taking all nineteen is a
    /// choice to fill your own bar. Raised with him on the day; if it bites, the cap moves, not this
    /// list.</para></summary>
    /// ✅ `BL-163`, 2026-09-17 — <b>THE LIST IS THE FILE NOW.</b> It used to be this hand-written array
    /// concatenated with the harmony and Mark sets: nineteen + eight + three = thirty ids, in
    /// buff-bar order. All thirty (and their levels, prices and the RUNG each one hands out) live in
    /// `docs/data/npc_buff_shelf.csv`, and the file's row order IS this order — his ask:
    /// *"that table can be a file with min lvl,skill_id_rung,price (editable from outside — so a pvp
    /// server won't require new npc just change of id's)"*.
    /// <para>⚠ A PROPERTY, never a field. The shelf loads the file on first access, and a static field
    /// initialiser here would run during <see cref="SkillCatalog"/>'s own construction — which is when
    /// the file's validator wants to read <see cref="FreeNpcBuffSet"/> back. That is the cross-file
    /// static-initialiser trap `BL-237` already cost a build to; a property cannot fall into it.</para>
    /// <para>⚠ THIRTY OFFERS AGAINST A BUFF CAP OF TWENTY, and that is deliberate — his ruling when it
    /// was put to him: *"Its a strategy -> deside what u want .. a fighter wont get magic buffs no need
    /// cast/insight/force etc"*. He then wrote both role loadouts by hand and each is EIGHTEEN, leaving
    /// room for self-buffs. The cap does not move; choosing is the content.</para>
    public static IReadOnlyList<string> NewbieBuffSet => NpcBuffShelf.Order;

    /// <summary>`BL-163` — EVERY BLESSING THE SHELF IS ALLOWED TO NAME, in C#, and not the same thing
    /// as <see cref="NewbieBuffSet"/>: this is the UNIVERSE, that is what is actually ON OFFER (the
    /// file, in the file's order). The shelf may drop a blessing, re-price it, move its levels or point
    /// it at a different rung; it may not invent a blessing this assembly has never heard of, and
    /// <c>NpcBuffShelf</c> refuses to load a file whose SHELF_IDs are not all in here.
    ///
    /// <para>🔑 IT EXISTS BECAUSE TWO THINGS NEED THE LIST WITHOUT THE FILE. The admin Buffs menu is
    /// built on the UNITY CLIENT (`GameUi.Debug`), where there is no `docs/data/` to read — and a
    /// DEBUG menu wants every blessing that exists anyway, including one an operator has edited off
    /// the shelf while testing it. Keeping the universe in code and the tuning in the file is what
    /// makes the file safe to hand to a server operator.</para>
    ///
    /// <para>⚠ The order here is the historical shelf order and is NOT what the window uses — the
    /// FILE's row order is. It is kept because the admin menu dedupes by display name and takes the
    /// first, so a reshuffle here would change which def a shared name resolves to.</para></summary>
    public static readonly string[] NpcShelfCatalogue =
        new[]
        { NpcMight, NpcBulwark, NpcVampirism,
          NpcForce, NpcWard, NpcResolve,
          NpcBody, NpcVigor, NpcSoul, NpcSerenity,
          NpcAlacrity, NpcHaste, NpcSwift, NpcAgility,
          NpcAccuracy,
          NpcFrenzy,
          NpcFocus, NpcFerocity, NpcInsight }
        .Concat(NpcSingleHarmonySet).Concat(NpcMarkSet).ToArray();

    /// <summary>`BL-150` — the EIGHT blessings that are free, and available from level 6. His list, in
    /// his order: *"fury/alacrity/force/mght/bulwark/swift/vamp/resolve from 6+"*.
    ///
    /// <para>🔑 The shape is one buff per axis a LEVELLING character feels — attack and cast speed,
    /// magic and physical attack, physical defence, movement, sustain, and not being interrupted. The
    /// eleven that wait until 40 are the ones that only matter once you are optimising or once your
    /// pools are big enough for a percentage of them to be worth anything.</para>
    ///
    /// <para>⚠ Membership of THIS set is the entire price rule. A buff in it is free at every level;
    /// a buff outside it costs the full <c>BuffCostPerLevel × 5</c> at every level. There is no
    /// third case and no per-player discount — see <c>GameLoopService.SingleBuffCost</c>.</para></summary>
    public static readonly string[] FreeNpcBuffSet =
        { NpcHaste, NpcAlacrity, NpcForce, NpcMight, NpcBulwark, NpcSwift, NpcVampirism, NpcResolve };

    private static readonly HashSet<string> _freeNpcBuffs = new(FreeNpcBuffSet);

    /// <summary>The level this NPC blessing unlocks at: 6 for the free eight, 40 for the other eleven.
    ///
    /// <para>🔑 This is what makes his saved-preset rule work with no extra machinery: *"if some1 buff
    /// me with body or soul and i save it and im &lt;40lvl they will not activate .. they will activate
    /// after 40+ from npc buffer"*. A preset is a SHOPPING LIST, so the gate is applied when the list
    /// is expanded, not when it is saved — the ids stay in the preset and start landing the day you
    /// reach 40, with nothing to re-save.</para></summary>
    /// ✅ `BL-158`, 2026-09-04 — IT READS THE TIER TABLE NOW, not a two-way `? 6 : 40`. A blessing's
    /// first tier IS its unlock level, so Insight's 62 and the Marks' 78 are answered by the same line
    /// that answers Might's 6, and the greyed-out button and the refused cast can never disagree.
    /// The 40 is gone as a constant: eleven blessings no longer share one gate.
    /// ✅ `BL-163`, 2026-09-17 — it reads the FILE's first row for the blessing. Same answer, one less
    /// hand-written table behind it.
    public static int NpcBuffMinLevel(string skillId) => NpcBuffShelf.MinLevel(skillId);

    /// <summary>Is this blessing one of the free eight? See <see cref="FreeNpcBuffSet"/>.</summary>
    public static bool IsFreeNpcBuff(string skillId) => _freeNpcBuffs.Contains(skillId);

    /// <summary>`BL-95` — the MAGE preset the NPC offers as one button.
    ///
    /// 🔑 It is a SHOPPING LIST, not a rule — every buff in it is still available singly, and nothing
    /// checks your class. A buffer taking the mage set and cancelling Force is exactly the workflow his
    /// custom preset is for. What the preset buys is the taps.
    ///
    /// ⚠ The order here is the order they land in, which is the order they appear on the buff bar.
    /// Kept as he wrote it.
    ///
    /// 🔑 RE-RULED 2026-09-03, `BL-150` — FOUR, and they are *"the two fighter and mage sets that i
    /// give you and they do not change"*: *"magic buffs -> alacrity,force,bulwark,resolve"*.
    ///
    /// <para>🔑 <b>THE TWO PRESETS ARE EXACTLY THE FREE EIGHT, PARTITIONED.</b> Fighter ∪ Mage =
    /// might, bulwark, vamp, fury, swift, alacrity, force, resolve — precisely
    /// <see cref="FreeNpcBuffSet"/>, with Bulwark AND SWIFT the two buffs both roles want (`BL-162`,
    /// 2026-09-04 — it was Bulwark alone until Swift joined the mage five). That is not a
    /// coincidence to be preserved by hand; it is what makes his instruction for building a full set
    /// work: *"if you want 'full buff' you buff fighter+mage sets and buy all 40+ then save your own"*.
    /// Pressing both buttons costs nothing and lands all eight free blessings.</para>
    ///
    /// <para>⚠ THE [FULL BUFF] BUTTON IS GONE (same ruling). There is no longer a one-press way to
    /// take all nineteen: the shipped buttons hand out the free tier, the paid eleven are bought one
    /// at a time, and a player who wants the lot saves a CUSTOM preset once and presses that. Which is
    /// also what stops "fill all twenty squares" from being the default action at this window.</para></summary>
    /// ✅ `BL-162`, 2026-09-04 — SWIFT JOINED, making it FIVE. His correction: *"mage - swift, alacrity,
    /// resolve, bulwark, force - 5 out of 8"*, and the split he wants is *"3-fighter(might,vamp,fury),
    /// 3-mage(force,resolv,alac), 2-shared(swift,bulwark)"*. So the invariant below is now **Bulwark AND
    /// Swift** are the two both roles want — it used to say Bulwark was the only one, and that sentence
    /// stopped being true the moment Swift was added. 5 + 5 = 8 + 2 overlaps.
    public static readonly string[] MageBuffSet =
        { NpcAlacrity, NpcForce, NpcBulwark, NpcResolve, NpcSwift };

    /// <summary>`BL-95` — the FIGHTER preset the NPC offers as one button.
    ///
    /// 🔑 RE-RULED 2026-09-03, `BL-150` — FIVE: *"fighter buffs -> might,bulwark,vamp,fury,swift"*.
    /// See <see cref="MageBuffSet"/> for why these two lists are the free tier split in half.
    ///
    /// ⚠ "Fury" is <see cref="NpcHaste"/> — named for the FAMILY on the buff ladder, keeping an older
    /// id. Not a typo, and not Alacrity (which is CAST speed and belongs to the mage's four).</summary>
    public static readonly string[] FighterBuffSet =
        { NpcMight, NpcBulwark, NpcVampirism, NpcHaste, NpcSwift };
    /// <summary>What the ADMIN buff button and `/buff` hand out: EVERYTHING a max-level BUFFER can
    /// give, at that buffer's TOP rung — the groups, the harmonies, Great Might and the Harmony Mark.
    /// Those top layers are the ones no NPC sells and no consumable can reach, so this is the only way
    /// to see a fully-buffed character, which is the state the balance numbers are read at.
    ///
    /// 🔑🔑 <b>THIS AND THE SPIRIT HELPER'S SHELF ARE TWO SEPARATE THINGS</b> (owner, 2026-09-03):
    /// *"The npc buffer that is the spirit helper gives the singles we decided … the admin-full and
    /// /buff gives the real full buffs. Both are separate. Altering the one should not break the
    /// other."* <see cref="NewbieBuffSet"/> is HIS shelf and answers to `BL-150`; this is the buffer
    /// CLASS's own kit and answers to the CSVs. Neither list may be built out of the other — see the
    /// note at the bottom of <c>BuildAdminBuffSet</c> for the version where one was, and what it cost.
    ///
    /// 🔑 It is DERIVED from the Warchanter's own class tables, not hand-listed (owner, playtest 26:
    /// *"admin fullbuff should give the new buffs and should fallow buffers buf changes.. Meaning if
    /// new buff/harmony should be added as well in the fullbuff and max effect"*). The hand-written
    /// array it replaces had gone stale in exactly the way he predicted: it named the nine lane groups
    /// and four harmonies that existed the day it was typed, and it applied every one of them at
    /// **level 1**, which is why he saw L1 harmonies. Add a rung, a harmony or a whole new group to
    /// `buffer 3rd.csv` and it appears here with no second edit.
    ///
    /// ⚠ The GROUPS come first, and they simply WIN: a group covers its families at group rank, so
    /// every single it contains is refused when it arrives after. The bar ends up as the group squares
    /// + the Harmonies + War Frenzy — which is the point of the improved tier. The order is kept
    /// deliberate anyway: reversed, the singles would land first and then be evicted, which is the
    /// same picture through twice the work.
    ///
    /// 🔴 THAT SENTENCE IS A CONTRACT WITH <c>GrantFullBuffSet</c>, and 0.107.0 broke it for two
    /// versions: "refused when it arrives after" is only true while the singles are applied WITHOUT
    /// `force`. See the note there before changing either side. Measure the result with
    /// <c>dotnet run --project tools/BalanceMatrix -- --buffs</c>, which models the same rule.
    ///
    /// ⚠ It is LAZY (a property, not a field). It reads <see cref="Get"/> and <c>ClassSkills</c>, both
    /// of which have to be built first; a static field initialiser here would run inside the catalog's
    /// own construction and see an empty table.</summary>
    public static IReadOnlyList<string> AdminBuffSet => _adminBuffSet ??= BuildAdminBuffSet();
    private static string[]? _adminBuffSet;

    /// <summary>Buffs a max-level buffer CAN cast that the admin set deliberately skips. War Bulwark
    /// shares one buff key with War Might on purpose (an ally wears one or the other, never both), so
    /// granting both would only show whichever landed last.</summary>
    private static readonly HashSet<string> AdminBuffSkip = new()
    {
        WcWarBulwark,
        // 🔑 THE TWO HE DOES NOT WANT IN A FULL BUFF (owner, 2026-09-03): *"don't want a Shrouding
        //    hymn in the full buff. And bow expertise."* Both are situational rather than wrong, and
        //    both actively spoil the thing a full buff is FOR — reading a fully-buffed character's
        //    numbers and then fighting with them. Shrouding Hymn is party STEALTH: unaggroed creatures
        //    ignore you, so a buffed test character cannot be attacked without picking the fight
        //    himself. Bow Expertise does nothing at all unless a bow is held, so on every other build
        //    it is a square on the bar that means nothing. Neither is lost — both are still one
        //    `/buff <name>` away when a test actually wants them.
        ShroudingHymn,
        WcBowExpertise,
    };

    private static string[] BuildAdminBuffSet()
    {
        // Every skill every RACE of Warchanter can learn — three races, because the kit is split by
        // race and the admin wants the union, not one race's half.
        var learnable = new List<string>();
        foreach (var third in ThirdClassCatalog.Playable)
        {
            if (third.Discipline != Discipline.Warchanter) continue;
            if (ClassCatalog.Get(third.ParentSecondClassId) is not SecondClassDef parent) continue;
            // 🔴 `fourth: true` — WITHOUT IT THIS SET STOPPED AT THE 3RD CLASS, and the summary above
            //    ("everything a MAX-LEVEL buffer can give") was simply false (found 2026-09-03, his
            //    playtest: the full buff owes *"group buffs + harmonies + harmony mark + great might"*
            //    and Harmony Mark was never in it). `Cumulative` defaults the flag to FALSE because for
            //    a real character it means "has paid the 100kk Rite"; the admin set is not a character
            //    and wants the whole kit. Everything the 4th tier adds beyond buffs — the sigils, the
            //    shared passives — is dropped by the IsGrantableBuff test below, as the 3rd tier's is.
            foreach (var cs in ClassSkills.Cumulative(third.Race, parent.Base, parent.Archetype,
                                                      third.Discipline, fourth: true))
                learnable.Add(cs.SkillId);
        }

        // Keep only what is actually a TIMED BUFF. One test drops his attack skills, his heals, his
        // totems and every passive — and it is the test that keeps this honest as the kit grows.
        bool IsGrantableBuff(string id) =>
            !AdminBuffSkip.Contains(id)
            && Get(id) is { Category: SkillCategory.Buff, DurationTicks: > 0 };

        bool IsGroup(string id) =>
            Get(id) is SkillDef d && d.ChildBuffsAt(d.MaxLevel) is { Length: > 1 };

        var classBuffs = learnable.Where(IsGrantableBuff).Distinct().ToList();

        // 🔑🔑 THE NPC BUFFER'S LIST IS **NOT** APPENDED HERE — owner, 2026-09-03: *"The npc buffer that
        //     is the spirit helper gives the singles we decided … the admin-full and /buff gives the real
        //     full buffs — groups + harmonies + great might + harmony mark. Both are separate. Altering
        //     the one should not break the other."* This set used to end `.Concat(NewbieBuffSet)` "for
        //     anything uncovered", and that one line WAS the coupling: `BL-150` grew his shelf from 16
        //     singles to 19 and all three walked straight into the admin bar, where (once `BL-131` forced
        //     the set) they evicted the very groups this command exists to show. Nothing was ever gained
        //     by it either — all 19 were refused as covered, every version it shipped in.
        // ⚠ So the safety it was pretending to provide is now MEASURED instead of assumed: `--buffs`
        //     prints any NPC family the buffer's own kit fails to cover. If that list is ever non-empty,
        //     the answer is a missing buff in the CLASS kit, not a re-import of the NPC's shelf.
        return classBuffs.Where(IsGroup)           // groups first — they cover and evict the singles
            .Concat(classBuffs.Where(id => !IsGroup(id)))
            .Distinct()
            .ToArray();
    }

    private static SkillDef NpcBuff(string id, string name, string buffKey,
        SkillEffect effect, EffectMagnitude[] mags, string desc,
        float physMpCost = 0f, float magicMpCost = 0f) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: NpcBuffTicks, BuffKey: buffKey, Rank: NpcBuffRank,
            Category: SkillCategory.Buff, Magnitudes: mags,
            PhysMpCostPct: physMpCost, MagicMpCostPct: magicMpCost,
            Description: desc + " (buffer's blessing, 1 hour).");

    /// <summary>An NPC-buffer buff that is a GROUP: ONE buff carrying the numbers of every child
    /// named here, covering each child's family. It evicts those singles and outranks anything the
    /// player can drink or read afterwards — a group is by definition the max version of its parts
    /// (docs/design/BuffLadders.md). NpcBuffRank does not apply: a group's rank is the GROUP rank.</summary>
    private static SkillDef NpcBuffGroup(string id, string name, SkillEffect effect,
        string[] children, string desc) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: NpcBuffTicks, ChildBuffs: children,
            Category: SkillCategory.Buff,
            Description: desc + " (buffer's blessing, 1 hour).");

    /// <summary>An NPC-buffer buff that hands out exactly ONE single buff (the scroll tier) for an
    /// hour. Mechanically the same shape as a Scroll — one child, the wrapper owns the duration —
    /// so it competes on the child's family key by Rank and can never stack with a potion or a
    /// cleric's rung of the same effect.</summary>
    private static SkillDef NpcSingle(string id, string name, string child,
        SkillEffect effect, string desc) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: NpcBuffTicks, ChildBuffs: new[] { child },
            Category: SkillCategory.Buff,
            Description: desc + " (buffer's blessing, 1 hour).");

    // ═══ `BL-158` — THE SHELF LEVELS UP WITH THE CHARACTER ═══════════════════════════════════════
    //
    // Owner, 2026-09-04: *"NPC Buffer will 'LVL UP' with the character .. if a player asks the npc for
    // buffs he will receive only buffs available to the same lvl bugger/healer - the npc no longer will
    // provide @40 buff that is learned at 74 (except the 8 free)"*. Its purpose: *"help single players
    // that dont want to spend time in party and or lvl up a buffer"*.
    //
    // 🔑 ONE TABLE IS THE WHOLE FEATURE. A blessing's tiers are read off his
    // `docs/data/classes_skills_csv/buffs.csv` (`NPC LVL` / `NPC Price`), and the SAME array drives all
    // four questions the buffer window asks: what level unlocks it, which rung lands, what it costs,
    // and whether the button is greyed out. There is no second list to keep in step.
    //
    // 🔑 TIER INDEX == SkillLevel INDEX. Tier n (1-based) is `SkillLevel` n of the wrapper, whose
    // `ChildBuffs` names the family rung for that tier — `ApplyBuff(def, tier)` then does the rest via
    // `SkillDef.ChildBuffsAt(level)`. That is why the wrappers were KEPT rather than deleted: the
    // player still receives `npc_might`, so `SourceSkillId` is unchanged and [Save], the presets and
    // their saved rows all keep working with no migration.
    //
    // ⚠ THE FREE EIGHT DO NOT LADDER — one tier, level 6, price 0, TOP rung, forever. His explicit
    // exception (*"except the 8 free"*), and it is what the code already did, so nothing about them
    // moves. A level-6 character wears +33% attack speed and that is intended.
    //
    // ⚠ THE PAID ELEVEN DELIBERATELY SKIP RUNGS — Body and Soul take rungs 1, 4 and 6 of six, Aim
    // skips its first. The gaps are what a real buffer fills, and they are the mechanism rather than an
    // oversight. **Never "fill them in" for tidiness.**
    // ✅ `BL-163`, 2026-09-17 — THE TABLE IS GONE FROM C#. `NpcBuffTier`, `NpcBuffTiers` (thirty rows
    // of level/price), `HarmonyPrice`, `MarkPrice` and the `NpcLadder` factory all lived here and are
    // now `docs/data/npc_buff_shelf.csv` + `NpcBuffShelf`. What follows is the same four questions the
    // game has always asked the shelf, forwarded, so the thirty call sites did not have to move:
    //
    // 🔑 AND THE ONE REAL CHANGE IS THAT THE ROW NAMES THE RUNG. A blessing's tier used to be an INDEX
    //    into the wrapper's `Levels`, which is why the wrapper had to carry a ladder that agreed with
    //    the table row-for-row — the invariant `BL-158`'s startup check existed to police. Now the file
    //    says `npc_ward,44,buff_def_mag_3,1,10000` and the NPC grants `buff_def_mag_3` the way `/buff`
    //    does. See `NpcBuffRung` below; the wrappers kept here are one-rung labels for `/buff` and the
    //    admin menu, and carry no ladder that can drift.

    /// <summary>The tier INDEX (1-based) this character qualifies for, or 0 if the blessing is still
    /// out of reach. The highest rung at or below their level wins.</summary>
    public static int NpcBuffTierFor(string skillId, int playerLevel) =>
        NpcBuffShelf.TierFor(skillId, playerLevel);

    /// <summary>What this character pays for the rung they qualify for. 0 for a free blessing — and 0
    /// for one they cannot buy yet, which never reaches a charge because the level gate refuses
    /// first.</summary>
    public static long NpcBuffPrice(string skillId, int playerLevel) =>
        NpcBuffShelf.Price(skillId, playerLevel);

    /// <summary>`BL-163` — THE BUFF THAT ACTUALLY LANDS, and the level to apply it at. Every caller
    /// that grants or price-checks a blessing must resolve it through this one method: asking the
    /// shelf twice is how a refusal and an outcome come to disagree.
    /// <para>⚠ Whoever grants it must pass the SHELF id as <c>sourceSkillId</c>, or [Save] will store
    /// the rung and freeze the player at the rung they saved.</para></summary>
    public static (SkillDef Def, int Level)? NpcBuffRung(string skillId, int playerLevel) =>
        NpcBuffShelf.RungDefFor(skillId, playerLevel);

    /// <summary>What the buffer window writes on the button. The rung's own name, so a row added to
    /// the file needs no label authored in code — see <see cref="NpcBuffShelf.DisplayName"/>.</summary>
    public static string NpcBuffName(string skillId) => NpcBuffShelf.DisplayName(skillId);

    /// <summary>Does the NPC sell this at all? The guard against a hand-made packet.</summary>
    public static bool NpcSells(string skillId) => NpcBuffShelf.Sells(skillId);

    /// <summary>`BL-160` — one of the eight NPC single harmonies. Its own BuffKey, so all eight can sit
    /// on the bar at once (his fighter list wants six of them together); rank <see cref="NpcBuffRank"/>,
    /// so a potion cannot touch it.
    ///
    /// <para>🔑 `BL-183` — WHAT REMOVES IT IS THE WARCHANTER'S OWN HARMONY, which covers it
    /// (<c>SkillDef.CoveredKeys</c>) at <see cref="HarmonyRank"/>, one rank above this. Owner,
    /// 2026-09-06: *"Harmony of swift should not stack with harmony of speed … Think of them as single
    /// harmonies and group harmonies -&gt; group buffs replaces singles."* Both directions hold: the
    /// class harmony tears this off when it lands, and the Spirit Helper refuses to sell it — before
    /// charging, <c>BuffWouldLand</c> is asked — while the class one is up.</para>
    ///
    /// <para>🔴 That was the INTENT from day one and it did not work for three versions. The rule was
    /// authored as <c>Replaces</c>, which <c>ApplyBuff</c> matches against buff KEYS while the entries
    /// were skill IDs (`npc_harmony_swift` vs `npc_h_swift`), so it matched nothing and the two tiers
    /// stacked in silence. The lesson is the one that keeps recurring here: <b>when a wrapper and its
    /// buff both carry a string, name which of the two a list is holding</b> — and if a rule cannot be
    /// measured, it is not a rule. This one is measured now, by
    /// <c>dotnet run --project tools/BalanceMatrix -- --buffs</c>.</para>
    ///
    /// <para>⚠ SINGLE-TARGET, unlike the class harmony it is lifted from. His CSV's `party/aoe` describes
    /// the Warchanter's skill shape, which the row was copied from; the NPC hands every other blessing
    /// to the one player who asked and paid, and an AoE here would let one player buy for a whole party
    /// at 50k. Flagged rather than assumed.</para></summary>
    private static SkillDef NpcHarmonySingle(string id, string name, string buffKey,
        SkillEffect effect, EffectMagnitude[] mags, string desc) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: NpcBuffTicks, BuffKey: buffKey, Rank: NpcBuffRank,
            Category: SkillCategory.Buff, Magnitudes: mags,
            // ⚠ The second sentence is not decoration — this is a 50,000-gold button, and a player who
            // already has the class harmony has to be able to see why it greys out.
            Description: desc + " (buffer's harmony, 1 hour). Replaced by the buffer class's own "
                       + "harmony that contains it — the two never stack.");

    private static SkillDef[] BufferSkills() => new SkillDef[]
    {
        // ---- The MIGHT family, one button each (was a single four-effect "Might"). ----
        NpcSingle(NpcMight, "Might", BuffPAtk3, SkillEffect.BuffPhysAtk, "+15% P.Atk"),
        NpcSingle(NpcBulwark, "Bulwark", BuffPDef3, SkillEffect.BuffDef, "+15% P.Def"),
        NpcSingle(NpcVampirism, "Vampirism", BuffVamp5, SkillEffect.BuffMeleeVamp, "9% melee vampirism"),
        // "Aim", not "Accuracy" — the FAMILY is Aim on every other rung (the ladder buff, all three
        // potions, all three scrolls), and the owner asks for it by that name (`BL-95`). This single
        // was the one place the stat's name was showing instead of the buff's, which read as a
        // different blessing on the same bar. Only the display name moved; the id is append-only.
        NpcSingle(NpcAccuracy, "Aim", BuffAcc4, SkillEffect.BuffAccuracy, "+4 Accuracy"),

        // ---- The FORCE family (was a single three-effect "Force"). ----
        NpcSingle(NpcForce, "Force", BuffMAtk4, SkillEffect.BuffMagAtk, "+32% M.Atk"),
        NpcSingle(NpcWard, "Ward", BuffMDef4, SkillEffect.BuffMagicDef, "+30% M.Def"),
        NpcSingle(NpcResolve, "Resolve", BuffIntr7, SkillEffect.BuffInterruptResist,
            "+54% interrupt resistance"),

        // ---- The FOCUS family (was a single three-effect "Focus"). ----
        NpcSingle(NpcFocus, "Focus", Rung(FamCritRate, 6), SkillEffect.BuffCritRate, "+30% critical rate"),
        NpcSingle(NpcFerocity, "Ferocity", Rung(FamCritDmg, 6), SkillEffect.BuffCritDamage, "+35% critical damage"),
        NpcSingle(NpcInsight, "Insight", Rung(FamMagCrit, 6), SkillEffect.BuffMagicCritRate, "double magic critical rate"),

        // Speed used to be an IMPROVED (group) buff here. The owner cut it (2026-07-31): the NPC
        // buffer gives the SCROLL tier — four separate single buffs, bought and cancelled one at a
        // time — and the improved GROUP is what a buffer CLASS gives. The def below is kept (nothing
        // grants it) so the group shape stays documented in one place; NewbieBuffSet no longer lists
        // it. The buffer's edge is the DURATION (1 hour vs a potion's 20 minutes), which the
        // equal-rank "longer time wins" rule in ApplyBuff protects. See docs/design/BuffLadders.md.
        NpcBuffGroup(NpcSpeed, "Swift and Sure",
            SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed | SkillEffect.BuffCastSpeed | SkillEffect.BuffEvasion,
            new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityR, BuffHasteR },
            "+33% attack speed, +30% cast speed, +33 move, +4 evasion"),

        // ---- The IMPROVED (group) versions at max rungs. Nothing in the world grants these — the
        //      admin buff button does, so the collapsed-group display and the group's own numbers
        //      can be checked without levelling a Warchanter to 74. ----
        NpcBuffGroup(NpcMightGroup, "Might and Bulwark",
            SkillEffect.BuffPhysAtk | SkillEffect.BuffDef | SkillEffect.BuffMeleeVamp | SkillEffect.BuffAccuracy,
            new[] { BuffPAtk3, BuffPDef3, BuffVamp5, BuffAcc4 },
            "+15% P.Atk & P.Def, 9% melee vampirism, +4 accuracy"),
        NpcBuffGroup(NpcForceGroup, "Force and Ward",
            SkillEffect.BuffMagAtk | SkillEffect.BuffMagicDef | SkillEffect.BuffInterruptResist,
            new[] { BuffMAtk4, BuffMDef4, BuffIntr7 },
            "+32% M.Atk, +30% M.Def, +54% interrupt resistance"),
        NpcBuffGroup(NpcFocusGroup, "Focus and Ferocity",
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffMagicCritRate,
            new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 6), Rung(FamMagCrit, 6) },
            "+30% critical rate, +35% critical damage, double magic critical rate"),
        NpcBuffGroup(NpcBodyGroup, "Body and Soul",
            SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffHpRegen | SkillEffect.BuffMpRegen,
            new[] { Rung(FamMaxHp, 6), Rung(FamMaxMp, 6), Rung(FamHpRegen, 6), Rung(FamMpRegen, 6) },
            "+35% Max HP & MP, +20% HP & MP regeneration"),

        // ---- The four speed singles the buffer actually offers, one hour each. ----
        NpcSingle(NpcSwift, "Swift", BuffSwiftR, SkillEffect.BuffMoveSpeed, "+33 Move Speed"),
        NpcSingle(NpcAlacrity, "Alacrity", BuffAlacrityR, SkillEffect.BuffCastSpeed, "+30% Cast Speed"),
        NpcSingle(NpcAgility, "Agility", BuffAgilityR, SkillEffect.BuffEvasion, "+4 Evasion"),
        NpcSingle(NpcHaste, "Fury", BuffHasteR, SkillEffect.BuffAtkSpeed, "+33% Attack Speed"),

        // ---- The BODY family (was a single four-effect "Body"). ----
        // ⚠ Body and Soul take rungs 1, 4 and 6 of SIX. The skipped rungs are his and are the point —
        // they are what a real buffer fills in. Do not "complete" this ladder.
        NpcSingle(NpcBody, "Body", Rung(FamMaxHp, 6), SkillEffect.BuffHp, "+35% Max HP"),
        NpcSingle(NpcSoul, "Soul", Rung(FamMaxMp, 6), SkillEffect.BuffMp, "+35% Max MP"),
        NpcSingle(NpcVigor, "Vigor", Rung(FamHpRegen, 6), SkillEffect.BuffHpRegen, "+20% HP regeneration"),
        NpcSingle(NpcSerenity, "Serenity", Rung(FamMpRegen, 6), SkillEffect.BuffMpRegen, "+20% MP regeneration"),

        // Frenzy — a reckless trade-off buff, and the one family whose rung is a whole buff rather
        // than one stat. INCLUDED in the full set (it's a FULL buffer); a player who doesn't want
        // the -10% Max HP/MP can just cancel this one buff.
        // ⚠ RUNG 2, NOT 6 (2026-09-04). The family was cut to two rungs on his ruling and rung 6 was
        // byte-for-byte identical to rung 2, so this hands out exactly the numbers it always did.
        NpcSingle(NpcFrenzy, "Frenzy", Rung(FamFrenzy, 2), SkillEffect.BuffPhysAtk,
            "-10% Max HP/MP but +8% offence and speed, +8 move, -8 evasion"),

        // ----- `BL-160`: THE EIGHT SINGLE HARMONIES -----------------------------------------------
        // Each carries the SAME payload as the Warchanter rung it is lifted from, at the level she
        // learns it. Verified 8/8 against `buffer 3rd.csv`, which is what makes the NPC exactly one
        // rung behind the class rather than a cheaper substitute for it.
        NpcHarmonySingle(NpcHWard, "Harmony of Ward", KeyHWard, SkillEffect.BuffMagicDef,
            new EffectMagnitude[] { new(SkillEffect.BuffMagicDef, 0.30f) }, "+30% M.Def"),
        NpcHarmonySingle(NpcHForce, "Harmony of Force", KeyHForce, SkillEffect.BuffMagAtk,
            new EffectMagnitude[] { new(SkillEffect.BuffMagAtk, 0.10f) }, "+10% M.Atk"),
        NpcHarmonySingle(NpcHSwift, "Harmony of Swift", KeyHSwift, SkillEffect.BuffMoveSpeed,
            new EffectMagnitude[] { new(SkillEffect.BuffMoveSpeed, 20, ModifierMode.Flat) }, "+20 Move Speed"),
        NpcHarmonySingle(NpcHAlacrity, "Harmony of Alacrity", KeyHAlacrity, SkillEffect.BuffCastSpeed,
            new EffectMagnitude[] { new(SkillEffect.BuffCastSpeed, 0.30f) }, "+30% Cast Speed"),
        NpcHarmonySingle(NpcHBulwark, "Harmony of Bulwark", KeyHBulwark, SkillEffect.BuffDef,
            new EffectMagnitude[] { new(SkillEffect.BuffDef, 0.25f) }, "+25% P.Def"),
        NpcHarmonySingle(NpcHMight, "Harmony of the Might", KeyHMight, SkillEffect.BuffPhysAtk,
            new EffectMagnitude[] { new(SkillEffect.BuffPhysAtk, 0.12f) }, "+12% P.Atk"),
        NpcHarmonySingle(NpcHFury, "Harmony of the Fury", KeyHFury, SkillEffect.BuffAtkSpeed,
            new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.15f) }, "+15% Attack Speed"),
        NpcHarmonySingle(NpcHBody, "Harmony of Body", KeyHBody, SkillEffect.BuffHp,
            new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.30f) }, "+30% Max HP"),

        // ----- The three original "Harmony" blessings MOVED OUT on 2026-08-21 -----
        // They are now LADDERS (Protection 5 rungs, Warrior 6, Wizard 2) on his `buffer 3rd.csv`
        // rows, with a 5-minute duration and a 2-minute reuse, and they live beside the fourth one
        // (Harmony of Speed) in Skills.Warchanter3rd.cs. Their IDS DID NOT CHANGE — `npc_harmony_*`
        // is append-only and AdminBuffSet still names them.
    };
}
