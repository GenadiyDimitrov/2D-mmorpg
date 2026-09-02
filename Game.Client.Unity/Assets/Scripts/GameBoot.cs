using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>What the client is currently doing. The HUD renders one screen per phase, so
    /// "is it connected / did login work / am I in the world" is always answerable ON the device.</summary>
    public enum ClientPhase
    {
        Offline,        // nothing attempted yet, or we dropped
        Connecting,     // SignalR handshake in flight
        Authenticating, // login/register in flight
        CharacterSelect,// authenticated, choosing a character
        Entering,       // EnterWorld in flight
        InWorld,
    }

    /// <summary>
    /// The client's orchestrator and single source of truth for connection state: connect → login →
    /// pick a character → enter the world → stream deltas into the EntityManager.
    ///
    /// Everything the HUD needs to answer "is this thing actually working?" is a public field here.
    /// The scene only needs THIS component — the EntityManager, CameraRig, TouchInput and HUD are
    /// created at runtime if they aren't wired in the Inspector, so a fresh scene can't be
    /// half-wired into a silent black screen.
    /// </summary>
    public class GameBoot : MonoBehaviour
    {
        [Header("Server")]
        [Tooltip("Emulator: http://10.0.2.2:5238/game   Cabled phone (adb reverse): http://127.0.0.1:5238/game   Same Wi-Fi: http://<PC-LAN-IP>:5238/game")]
        public string ServerUrl = "http://127.0.0.1:5238/game";

        // admin/admin is the DEBUG seed account — a level-90 character in full gear with every skill,
        // which is what you want on the phone when the point is to test a window rather than to level
        // something up. It only exists in a DEBUG server build.
        [Header("Dev login (prefilled into the login screen)")]
        public string Username = "admin";
        public string Password = "admin";
        public string CharacterName = "Pathfinder";
        public Race Race = Race.Human;
        public BaseClass BaseClass = BaseClass.Fighter;

        [Tooltip("Skip the login screen and use the credentials above (handy in the Editor).")]
        public bool AutoLogin = false;

        [Header("Scene refs (auto-created when empty)")]
        public EntityManager Entities;
        public CameraRig CameraRig;
        public MoveMarker Marker;
        public ZoneOverlay Zones;
        /// <summary>The ground circles — totem footprints and area-skill flashes.</summary>
        public GroundDecals Decals;

        // ----- State the HUD reads -------------------------------------------------------------
        public ClientPhase Phase { get; private set; } = ClientPhase.Offline;
        public string StatusMessage { get; private set; } = "Not connected";
        public string LastError { get; set; }
        public CharacterSlot[] Characters { get; private set; } = Array.Empty<CharacterSlot>();
        public Guid SelfId => _selfId;
        public Guid? TargetId { get; set; }

        /// <summary>The target we have observed ALIVE — the only one whose death clears the selection
        /// (owner, playtest-21 `65d`). See OnDelta; it is what makes the rule a transition rather than
        /// a state, so deliberately selecting a corpse sticks.</summary>
        private Guid? _targetSeenAlive;

        /// <summary>Who has hit US recently, and when (<see cref="Time.realtimeSinceStartup"/>). Fed by
        /// <see cref="OnCombat"/> and read by <see cref="TargetClosest"/> so that a creature actually
        /// fighting you outranks a nearer one that is not (`BL-43`).
        ///
        /// <para>🔑 This is CLIENT-side on purpose and needs no protocol change: the combat feed already
        /// carries every blow landed on us, with the attacker's id. The server's autopilot has its own,
        /// separate notion of retaliation (<c>RetaliationTarget</c>) and the two are deliberately not
        /// shared — the autopilot picks what to FIGHT, this picks what the player is LOOKING at.</para>
        ///
        /// <para>Entries are pruned lazily on read, so a mob that has stopped hitting you drops back to
        /// plain distance order after <see cref="RetaliationMemory"/> seconds rather than sticking to
        /// the top of the cycle for the rest of the session.</para></summary>
        private readonly Dictionary<Guid, float> _recentAttackers = new Dictionary<Guid, float>();

        /// <summary>How long (seconds) a blow keeps its author at the front of the target cycle. Long
        /// enough to span a caster's wind-up between hits, short enough that a mob you walked away from
        /// stops claiming priority.</summary>
        private const float RetaliationMemory = 10f;

        /// <summary>Server frames received, and when the last one landed — the honest answer to
        /// "is the connection alive?", which a connected-but-silent socket would otherwise fake.</summary>
        public int FramesReceived { get; private set; }
        public float LastFrameTime { get; private set; }
        public float FramesPerSecond { get; private set; }
        public float SecondsSinceFrame => FramesReceived == 0 ? -1f : Time.realtimeSinceStartup - LastFrameTime;

        public StatsUpdate Stats { get; private set; }
        public ProgressUpdate Progress { get; private set; }
        public long Gold { get; private set; }

        /// <summary>Staff role of the character in world — mirrors the WPF client, which only bothers
        /// sending admin commands when it believes it's allowed (the server re-checks regardless).</summary>
        public AccountRole Role { get; private set; } = AccountRole.Player;

        /// <summary>STAFF — may TYPE a slash command at all. Every rank above Player is included; which
        /// commands each may actually use is the server's business (see `AllowedCommands`), and a chat
        /// moderator typing `/jail` gets told so rather than being told the command does not exist.</summary>
        public bool IsAdmin => Role != AccountRole.Player;

        /// <summary>May use the ADMIN TOOLBOX (the former debug menu — free levels, gold, items, class
        /// changes). Admin ONLY, deliberately narrower than <see cref="IsAdmin"/>, because that is exactly
        /// what the server's gate is (<c>Entity.IsAdmin</c> is <c>Role == Admin</c>). Showing a moderator a
        /// menu whose every button answers "that is an admin-only command" is worse than not showing it:
        /// moderators moderate, they do not cheat.</summary>
        public bool CanUseAdminTools => Role >= AccountRole.Admin;

        /// <summary>Your own otherwise-undrawable state (`BL-82`) — god mode, forced speeds, and which
        /// of the three kinds of invisibility you are in. Null until the first push, which arrives on
        /// the first tick after entering the world; treat null as "ordinary, nothing on".</summary>
        public SelfStateDto SelfState { get; private set; }

        /// <summary>
        /// The skill bar, exactly as the SERVER sent it — 60 slots of skill id / "action:…" / "item:…"
        /// / null. **This client never authors a bar.** The server owns it and does the placement; a
        /// client that wrote back a bar it had not been told to write is what destroyed real layouts
        /// in the WPF client twice (a Learned push arriving while the client held a different bar).
        /// So: render this, and only send SetSkillBar when the PLAYER edits a slot.
        /// </summary>
        public string[] SkillBar { get; private set; } = new string[GameConstants.SkillBarSlots];

        /// <summary>Learned skill id → level, for greying out what isn't castable.</summary>
        public readonly Dictionary<string, int> Learned = new Dictionary<string, int>();

        /// <summary>A running reuse timer, as the BAR sees it: when it ends (client clock) and how long
        /// it ran for, which is the denominator of the shrinking overlay.</summary>
        public struct Reuse { public float EndsAt; public float Total; }

        /// <summary>Action-bar token → its running reuse. Fed by the server's Cooldowns push and then
        /// counted down LOCALLY: the server sends one message when a timer starts, not one per tick, so
        /// the bar can animate at frame rate without any extra traffic. Entries are dropped as they
        /// expire, so an empty dictionary means "everything is ready".</summary>
        public readonly Dictionary<string, Reuse> Cooldowns = new Dictionary<string, Reuse>();

        /// <summary>The bag, as last sent by the server (it pushes the whole thing on any change).</summary>
        public InventoryItemDto[] Inventory { get; private set; } = new InventoryItemDto[0];
        public InventoryItemDto[] Warehouse { get; private set; } = new InventoryItemDto[0];

        /// <summary>The ACCOUNT bank — shared by every character on the account.</summary>
        public InventoryItemDto[] AccountWarehouse { get; private set; } = new InventoryItemDto[0];
        public BuyBackEntryDto[] BuyBack { get; private set; } = new BuyBackEntryDto[0];

        /// <summary>Recently BINNED items, restorable for free (C18). Separate from BuyBack, and pushed
        /// on login and on every delete rather than when a vendor opens — the undo has to work where the
        /// accident happens, which is in the field.</summary>
        public BuyBackEntryDto[] Restorable { get; private set; } = new BuyBackEntryDto[0];

        /// <summary>Party roster (empty when you are not in one) and the agreed loot rule.</summary>
        public PartyMemberDto[] Party { get; private set; } = new PartyMemberDto[0];
        public LootMode PartyLoot { get; private set; } = LootMode.Random;

        /// <summary>The roster row for an entity id, or null. This is the ONLY thing that still knows an
        /// ally who has walked out of interest range — the world snapshot has dropped them (B7).</summary>
        public PartyMemberDto FindPartyMember(Guid id)
        {
            if (Party != null)
                foreach (var m in Party)
                    if (m != null && m.Id == id) return m;
            return null;
        }

        /// <summary>A pending party invitation, or null.</summary>
        public PartyInviteDto PendingInvite { get; private set; }

        public async void PartyInvite(Guid targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.PartyInviteAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Invite: " + ex.Message); }
        }

        /// <summary>Invite by NAME — the SERVER resolves it, over every online player. We must not look
        /// the name up here: the client only holds the entities in view, which is what made `/ptinv`
        /// answer "no player x nearby" for a party member who had walked off screen (46d).</summary>
        public async void PartyInviteByName(string name)
        {
            if (Phase != ClientPhase.InWorld || string.IsNullOrWhiteSpace(name)) return;
            try { await _net.PartyInviteByNameAsync(name.Trim()); }
            catch (Exception ex) { ClientLog.Warn("Invite: " + ex.Message); }
        }

        /// <summary>Open a box/chest from the inventory (random grants loot; a selection box replies with
        /// a "Selection" push that the UI turns into a chooser).</summary>
        public async void OpenBox(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.OpenBoxAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("Open: " + ex.Message); }
        }

        /// <summary>Break a piece of gear down into crafting materials (`BL-22`).</summary>
        public async void DisassembleItem(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.DisassembleItemAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("Disassemble: " + ex.Message); }
        }

        /// <summary>Confirm the picked item(s) from a selection box.</summary>
        public async void SelectBoxItems(Guid instanceId, string[] itemIds)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.SelectBoxItemsAsync(instanceId, itemIds); }
            catch (Exception ex) { ClientLog.Warn("Select: " + ex.Message); }
        }

        /// <summary>Use an enchant scroll on a piece of gear.</summary>
        public async void Enchant(Guid scrollInstanceId, Guid targetInstanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.EnchantAsync(scrollInstanceId, targetInstanceId); }
            catch (Exception ex) { ClientLog.Warn("Enchant: " + ex.Message); }
        }

        /// <summary>ADMIN: set an item's enchant outright (the `/enchant &lt;value&gt;` picker).</summary>
        public async void AdminEnchant(Guid instanceId, int value)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.AdminEnchantAsync(instanceId, value); }
            catch (Exception ex) { ClientLog.Warn("Enchant: " + ex.Message); }
        }

        /// <summary>Use an attribute scroll on a weapon or jewel.</summary>
        public async void RerollAttributes(Guid scrollInstanceId, Guid targetInstanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.RerollAttributesAsync(scrollInstanceId, targetInstanceId); }
            catch (Exception ex) { ClientLog.Warn("Attribute: " + ex.Message); }
        }

        /// <summary>Walk after a player until you move or they leave (null stops following).</summary>
        public async void Follow(Guid? targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            // Same reasoning as Attack: the server steers this walk (it re-paths as they move), so a
            // predicted straight line to the last tap point would diverge and snap.
            CancelMoveOrder();
            try { await _net.FollowAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Follow: " + ex.Message); }
        }

        /// <summary>Attack whatever the targeted player is attacking.</summary>
        public async void Assist(Guid targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.AssistAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Assist: " + ex.Message); }
        }

        public async void AnswerPartyInvite(bool accept)
        {
            PendingInvite = null;
            try { await _net.PartyRespondAsync(accept); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        public async void PartyLeave()
        {
            try { await _net.PartyLeaveAsync(); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        public async void PartyKick(Guid targetId)
        {
            try { await _net.PartyKickAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        public async void PartyChangeLeader(Guid targetId)
        {
            try { await _net.PartyChangeLeaderAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        /// <summary>Save the currently-worn gear into preset slot 0/1/2 (A/B/C).</summary>
        public async void SaveEquipPreset(int slot)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.SaveEquipPresetAsync(slot); }
            catch (Exception ex) { ClientLog.Warn("Preset: " + ex.Message); }
        }

        /// <summary>Re-equip preset slot 0/1/2. Server refuses in combat + reports skipped items.</summary>
        public async void ApplyEquipPreset(int slot)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.ApplyEquipPresetAsync(slot); }
            catch (Exception ex) { ClientLog.Warn("Preset: " + ex.Message); }
        }

        /// <summary>Fetch a leaderboard board and hand the result back on the main thread.</summary>
        public async void RequestLeaderboard(string category, Action<LeaderboardDto> onResult)
        {
            if (Phase != ClientPhase.InWorld || _net == null) return;
            try
            {
                var dto = await _net.RequestLeaderboardAsync(category);
                if (dto != null) Main(() => onResult(dto));
            }
            catch (Exception ex) { ClientLog.Warn("Rank: " + ex.Message); }
        }

        /// <summary>Find a current party member's id by name (for /ptkick, /ptcl). Null if not in party.</summary>
        public Guid? PartyMemberId(string name)
        {
            if (Party == null || string.IsNullOrWhiteSpace(name)) return null;
            foreach (var m in Party)
                if (m.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)) return m.Id;
            return null;
        }

        /// <summary>The instance id of an unequipped bag item with this def id (any stack), or null —
        /// for the quick-use bar, where a slot holds "item:&lt;defId&gt;" and any matching stack works.</summary>
        public Guid? FindBagItem(string defId)
        {
            if (Inventory == null || string.IsNullOrEmpty(defId)) return null;
            foreach (var it in Inventory)
                if (!it.Equipped && it.DefId == defId) return it.InstanceId;
            return null;
        }

        /// <summary>How many of this item are in the bag, counting stack quantities and summing the
        /// separate stacks a split can leave behind. Drives the hotbar's consumable count (32n).</summary>
        public int BagCount(string defId)
        {
            if (Inventory == null || string.IsNullOrEmpty(defId)) return 0;
            int n = 0;
            foreach (var it in Inventory)
                if (!it.Equipped && it.DefId == defId) n += Mathf.Max(1, it.Quantity);
            return n;
        }

        /// <summary>Send a friend command. The hub takes a NAME rather than an id on purpose — friendship
        /// has to work on someone who is offline or out of view, which no entity id can express.</summary>
        public async void FriendCommand(string action, string name)
        {
            try { await _net.FriendCommandAsync(action, name); }
            catch (Exception ex) { ClientLog.Warn("Friend: " + ex.Message); }
        }

        public async void BlockCommand(string action, string name)
        {
            try { await _net.BlockCommandAsync(action, name); }
            catch (Exception ex) { ClientLog.Warn("Block: " + ex.Message); }
        }

        /// <summary>This character's social toggles, as the SERVER last stated them (playtest-19 M2).
        /// Never written optimistically on a tap — the window redraws when the push comes back.</summary>
        public SocialOptions Social { get; private set; } = SocialOptions.None;

        /// <summary>The character's ONE permanent crafting profession, as the server states it.
        /// <see cref="Profession.None"/> until it has been picked.</summary>
        public Profession CraftProfession { get; private set; } = Profession.None;

        /// <summary>The DropOnly recipes unlocked from a blueprint. Auto-known recipes are NOT in here —
        /// those are gated on character level and the window works that out from the catalog.</summary>
        public HashSet<string> KnownRecipes { get; private set; } = new HashSet<string>();

        /// <summary>The crafting LEVEL in force (1-6, 0 with no profession), the RAW crafting exp behind
        /// it, and the highest rung this character's progression currently allows (`BL-05`).
        ///
        /// <para>All three come from the server and none is recomputed here: the band depends on the
        /// third class and on every subclass, and a client that worked it out for itself would be a
        /// second implementation of the freeze rule that could disagree with the one enforcing it.</para></summary>
        public int CraftLevel { get; private set; }
        public int CraftExp { get; private set; }
        public int CraftBandCap { get; private set; }

        /// <summary>Is the character standing at HIS OWN master right now? The craft buttons are live
        /// only here — away from him the same window is a read-only browse of what to farm.</summary>
        public bool AtCraftMaster { get; private set; }

        /// <summary>Craft one unit. Same rule as every other action: nothing is applied locally — the
        /// inventory push that follows is what tells us it happened.</summary>
        public async void Craft(string recipeId)
        {
            try { await _net.CraftAsync(recipeId); }
            catch (Exception ex) { ClientLog.Warn("Craft: " + ex.Message); }
        }

        /// <summary>Re-take a master's profession you have already been taught (`BL-05`). A FIRST
        /// profession comes from finishing his joining quest, never from here — the server refuses this
        /// unless the quest is already in <c>CompletedQuests</c>.</summary>
        public async void JoinProfession()
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.JoinProfessionAsync(DialogNpcId); }
            catch (Exception ex) { ClientLog.Warn("JoinProfession: " + ex.Message); }
        }

        /// <summary>Buy one SP Bottle at an SP broker. The server re-checks SP, gold and inventory
        /// space; this only asks.</summary>
        public async void BuySpBottle()
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.BuySpBottleAsync(DialogNpcId); }
            catch (Exception ex) { ClientLog.Warn("BuySpBottle: " + ex.Message); }
        }

        /// <summary>Quit your profession at your own master. Every crafting level is lost.</summary>
        public async void QuitProfession()
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.QuitProfessionAsync(DialogNpcId); }
            catch (Exception ex) { ClientLog.Warn("QuitProfession: " + ex.Message); }
        }

        public async void Like(string name)
        {
            try { await _net.LikeAsync(name); }
            catch (Exception ex) { ClientLog.Warn("Like: " + ex.Message); }
        }

        /// <summary>The `@target` / `@self` tokens. Playtest 26 asked for the first: *"we should make
        /// @target or %target or ~ so admin/players commands to work on the target (take the name from
        /// the target window) because a player named "IlIlllIIllI" for a human is impossible to read."*
        /// Playtest 27 fixed the spellings: *"other symbols than @t/target or @s/self should not work"*.
        ///
        /// 🔑 It is a CLIENT substitution, done once before any command is parsed, and it therefore works
        /// on EVERY command that takes a name — `/jail @target`, `/w @t hello`, `/ptinv @target`,
        /// `/give @target sword1h_t10`, `/buff @s aim 1` — including ones written later, with no server
        /// change at all. Targeting is client-side in this game (the server is only ever told a target id
        /// when you act on something), so the client is the only place that knows the answer.
        ///
        /// TWO tokens, two spellings each:
        ///   • `@target` / `@t` → the name of whatever you have targeted.
        ///   • `@self` / `@s` → your own character's name.
        ///
        /// ⚠ **`~` and `%target` were REMOVED here.** `%target` was a spelling nobody asked to keep, and
        /// `~` is now the RELATIVE-COORDINATE prefix (`/tp ~100 ~-50`) — one character cannot mean both
        /// "my target" and "relative to me" in the same command line. Whole tokens only, so a name or a
        /// message containing one of these characters is untouched.
        ///
        /// Returns false when a token was used with nothing targeted — the command is then not sent at
        /// all, rather than reaching the server as the literal word "@target".</summary>
        private bool TrySubstituteTargetToken(string raw, out string result)
        {
            result = raw;
            if (raw.IndexOf('@') < 0) return true;

            var parts = raw.Split(' ');
            bool changed = false;
            string targetName = null;
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                bool wantsSelf = p.Equals("@self", StringComparison.OrdinalIgnoreCase)
                              || p.Equals("@s", StringComparison.OrdinalIgnoreCase);
                bool wantsTarget = p.Equals("@target", StringComparison.OrdinalIgnoreCase)
                                || p.Equals("@t", StringComparison.OrdinalIgnoreCase);
                if (!wantsSelf && !wantsTarget) continue;

                if (wantsSelf)
                {
                    // Your own name always exists, so @self can never fail the way @target can.
                    string me = SelfName();
                    if (string.IsNullOrEmpty(me))
                    {
                        ClientLog.Warn("Your character is not loaded yet — " + p + " has no one to stand for.");
                        return false;
                    }
                    parts[i] = me;
                    changed = true;
                    continue;
                }

                if (targetName == null)
                {
                    // Whatever is targeted, not just a player: an admin typing `/where @target` on an NPC
                    // should get the server's "no character" answer, not a silent wrong guess here.
                    if (TargetId.HasValue && TargetId.Value != SelfId && Entities != null
                        && Entities.TryGetState(TargetId.Value, out var e))
                        targetName = e.Name;
                }
                if (string.IsNullOrEmpty(targetName))
                {
                    ClientLog.Warn("Nothing targeted — " + p + " has no one to stand for.");
                    return false;
                }
                parts[i] = targetName;
                changed = true;
            }
            if (changed) result = string.Join(" ", parts);
            return true;
        }

        /// <summary>Your own character's name, or null before the world state has arrived.</summary>
        private string SelfName() =>
            Entities != null && Entities.TryGetState(SelfId, out var me) ? me.Name : null;

        /// <summary>The NAME of the currently targeted player, or null when the target is missing, is a
        /// mob, or is yourself. Used by the name-only actions, which take a target instead of typing.</summary>
        public string TargetPlayerName()
        {
            if (!TargetId.HasValue || Entities == null) return null;
            if (TargetId.Value == SelfId) return null;
            return Entities.TryGetState(TargetId.Value, out var e)
                   && e.Kind == EntityKind.Player ? e.Name : null;
        }

        /// <summary>A PLAYER entity IN VIEW, by name. ⚠ Never use this to address a player — it can
        /// only see what is on screen, which is exactly the 46d bug. It is kept for UI that is already
        /// talking about something visible.</summary>
        public Guid? FindPlayerByName(string name)
        {
            if (Entities == null || string.IsNullOrWhiteSpace(name)) return null;
            foreach (var kv in Entities.States)
                if (kv.Value.Kind == EntityKind.Player &&
                    kv.Value.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return kv.Key;
            return null;
        }

        /// <summary>True if <paramref name="raw"/> is exactly this command, or this command followed by
        /// its argument. A bare StartsWith would make `/title` swallow `/titleright`.</summary>
        private static bool IsCommand(string raw, string command) =>
            raw.Equals(command, StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith(command + " ", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// `/target &lt;name&gt;` — select whoever is nearest with that name.
        ///
        /// The case this exists for (owner): a crowd around the gatekeeper, whose plate is behind three
        /// other players', and no finger can reach it. Matching is deliberately generous — an NPC's
        /// authored name is "Gatekeeper Pell" but its plate now reads `Gatekeeper` over `Pell`, so both
        /// halves, and a prefix of either, have to work or the command would need you to know which
        /// half the server kept as the name.
        ///
        /// EXACT matches win over prefix ones, and nearest wins within each: with a Pell and a Pellon
        /// in the square, "/target Pell" must mean Pell.
        /// </summary>
        public void TargetByName(string name)
        {
            if (Phase != ClientPhase.InWorld || Entities == null) return;
            string needle = (name ?? "").Trim();
            if (needle.Length == 0) { ClientLog.Warn("Usage: /target <name>"); return; }
            if (!Entities.TryGetState(SelfId, out var self)) return;

            Guid bestId = Guid.Empty; float bestDistSq = float.MaxValue; bool bestExact = false;
            foreach (var kv in Entities.States)
            {
                var e = kv.Value;
                if (e.Id == SelfId) continue;

                // The title line counts as part of the name for matching only — "Gatekeeper" finds Pell.
                bool exact = Matches(e.Name, needle) || Matches(e.Title, needle)
                          || Matches((e.Title + " " + e.Name).Trim(), needle);
                bool prefix = !exact && (StartsWith(e.Name, needle) || StartsWith(e.Title, needle));
                if (!exact && !prefix) continue;

                float dx = e.X - self.X, dy = e.Y - self.Y;
                float d2 = dx * dx + dy * dy;
                // An exact match always beats a prefix one, however far away it is.
                if (bestExact && !exact) continue;
                if (exact && !bestExact) { bestId = e.Id; bestDistSq = d2; bestExact = true; continue; }
                if (d2 < bestDistSq) { bestId = e.Id; bestDistSq = d2; bestExact = exact; }
            }

            if (bestId == Guid.Empty)
            {
                // "in sight" is the honest limit: this searches what the server has told us about, and
                // that is only ever what is in view range.
                ClientLog.Warn("No one called \"" + needle + "\" in sight.");
                return;
            }

            TargetId = bestId;
            if (Entities.TryGetState(bestId, out var picked))
                ClientLog.Info("Targeting " + (string.IsNullOrEmpty(picked.Title)
                                               ? picked.Name : picked.Title + " " + picked.Name) + ".");

            static bool Matches(string have, string want) =>
                !string.IsNullOrEmpty(have) && have.Equals(want, StringComparison.OrdinalIgnoreCase);
            static bool StartsWith(string have, string want) =>
                !string.IsNullOrEmpty(have) && have.StartsWith(want, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>How many enemies the cycle holds. His number: *"targeting closest/retaliate 5 and
        /// cycling through them"* — a short ring you can tap around blind is the point, and a cycle over
        /// every mob in a 2500 radius is not one.</summary>
        private const int TargetRingSize = 5;

        /// <summary>NextTarget (`BL-43`). Select the best living enemy nearby; pressing again steps to
        /// the next one, wrapping — so you can flick between the handful in front of you without
        /// looking. Owner, playtest note 5: *"Need NextTarget (targeting closest/retaliate 5 and cycling
        /// through them)"*, deferred then and built now.
        ///
        /// <para>🔑 **Retaliation outranks distance.** Anything that has hit you inside
        /// <see cref="RetaliationMemory"/> sorts ahead of everything that has not, and only then does
        /// distance decide. This is the same complaint that produced the AUTOPILOT's retaliate rule
        /// (playtest note 4, *"I'm getting ganked by orc archers and still kill the nearest"*) — the
        /// manual selector had the identical hole, one tap instead of one autopilot decision.</para>
        ///
        /// <para>The ring is rebuilt on every press rather than cached, so it always reflects where you
        /// are standing now. That means the ring can change under you as you move or as something new
        /// starts swinging; that is wanted — a mob that opens up on you should join the front of the
        /// cycle immediately, not after the ring is exhausted. If the current target has fallen out of
        /// the ring, <c>FindIndex</c> returns -1 and the press lands on the best entry, which is also
        /// the right answer.</para></summary>
        public void TargetClosest()
        {
            const float maxRange = 2500f, maxRangeSq = maxRange * maxRange;
            if (Entities == null || !Entities.TryGetState(SelfId, out var self)) return;

            // Prune the retaliation memory first, so a stale entry can never win a sort below.
            float now = Time.realtimeSinceStartup;
            if (_recentAttackers.Count > 0)
            {
                var expired = new List<Guid>();
                foreach (var kv in _recentAttackers)
                    if (now - kv.Value > RetaliationMemory) expired.Add(kv.Key);
                foreach (var id in expired) _recentAttackers.Remove(id);
            }

            var enemies = new List<(Guid Id, bool Hitting, float DistSq)>();
            foreach (var kv in Entities.States)
            {
                var e = kv.Value;
                if (e.Kind != EntityKind.Mob || e.Dead) continue;
                float dx = e.X - self.X, dy = e.Y - self.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 <= maxRangeSq) enemies.Add((kv.Key, _recentAttackers.ContainsKey(kv.Key), d2));
            }
            if (enemies.Count == 0) { ClientLog.Warn("No enemy in range."); return; }

            // Retaliators first, then nearest. Ties inside each group are broken by distance alone, so
            // the order is stable between presses as long as nothing moves.
            enemies.Sort((a, b) => a.Hitting != b.Hitting
                                   ? (a.Hitting ? -1 : 1)
                                   : a.DistSq.CompareTo(b.DistSq));
            if (enemies.Count > TargetRingSize) enemies.RemoveRange(TargetRingSize,
                                                                    enemies.Count - TargetRingSize);

            int idx = TargetId.HasValue ? enemies.FindIndex(x => x.Id == TargetId.Value) : -1;
            TargetId = enemies[(idx + 1) % enemies.Count].Id;   // -1 → best; else the next one round
        }

        public async void PartySetLoot(LootMode mode)
        {
            try { await _net.PartySetLootModeAsync(mode); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        /// <summary>The open NPC conversation, or null. Everything an NPC offers — quests, class
        /// change, shop, teleport, buffs, skill reset — arrives in this ONE push.</summary>
        public NpcDialog Dialog { get; private set; }

        /// <summary>The NPC being talked to. Every dialog action needs it, because the server checks
        /// you are still standing in front of that specific NPC.</summary>
        public Guid DialogNpcId { get; private set; }

        public async void TalkToNpc(Guid npcEntityId)
        {
            if (Phase != ClientPhase.InWorld) return;
            DialogNpcId = npcEntityId;
            try { await _net.TalkToNpcAsync(npcEntityId); }
            catch (Exception ex) { ClientLog.Warn("Talk: " + ex.Message); }
        }

        /// <summary>An NPC we are walking TOWARDS in order to talk, or empty (owner, playtest-19 M13).
        /// The server refuses a Talk outside <see cref="GameConstants.TalkRange"/>, so "tap the NPC,
        /// get told you are too far, walk, tap again" was the whole interaction — the second tap now
        /// walks you there and opens the conversation on arrival.</summary>
        private Guid _walkToTalk;

        /// <summary>Walk to an NPC and talk when you get there. Talks immediately if already in range,
        /// so the near case costs nothing.</summary>
        public void ApproachAndTalk(Guid npcEntityId)
        {
            if (Phase != ClientPhase.InWorld || Entities == null) return;
            if (!Entities.TryGetState(npcEntityId, out var npc) || npc.Kind != EntityKind.Npc) return;

            if (WithinTalkRange(npc))
            {
                _walkToTalk = Guid.Empty;
                TalkToNpc(npcEntityId);
                return;
            }

            _walkToTalk = npcEntityId;
            // Stop SHORT of the NPC — walking onto their exact spot leaves you standing inside them.
            MoveTowards(npc.X, npc.Y, GameConstants.TalkRange * 0.6f);
        }

        /// <summary>Am I close enough to the NPC for the server to accept a Talk?</summary>
        private bool WithinTalkRange(EntityDto npc)
        {
            if (Entities == null || !Entities.TryGetState(SelfId, out var me)) return false;
            float dx = npc.X - me.X, dy = npc.Y - me.Y;
            return dx * dx + dy * dy <= GameConstants.TalkRange * GameConstants.TalkRange;
        }

        /// <summary>Move to a point <paramref name="stopShort"/> units before (x, y).</summary>
        private void MoveTowards(float x, float y, float stopShort)
        {
            if (Entities == null || !Entities.TryGetState(SelfId, out var me)) return;
            float dx = x - me.X, dy = y - me.Y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist <= stopShort) { Move(x, y); return; }
            float f = (dist - stopShort) / dist;
            Move(me.X + dx * f, me.Y + dy * f);
        }

        /// <summary>Drive a pending walk-to-talk: talk the moment we are in range, and give up if the
        /// NPC vanishes or the player takes a different action (which clears _walkToTalk).</summary>
        private void TickWalkToTalk()
        {
            if (_walkToTalk == Guid.Empty) return;
            if (Entities == null || !Entities.TryGetState(_walkToTalk, out var npc))
            {
                _walkToTalk = Guid.Empty;
                return;
            }
            if (!WithinTalkRange(npc)) return;

            var npcId = _walkToTalk;
            _walkToTalk = Guid.Empty;
            TalkToNpc(npcId);
        }

        /// <summary>Abandon a pending walk-to-talk — the player did something else.</summary>
        public void CancelWalkToTalk() => _walkToTalk = Guid.Empty;

        public void CloseDialog() { Dialog = null; DialogNpcId = Guid.Empty; }

        /// <summary>Accept / complete / change-class are NPC conversations: the server re-checks you are
        /// standing in front of that specific NPC, so sending them without one is pointless and the guard
        /// below drops them.
        ///
        /// ABANDON IS NOT. It is issued from the QUEST LOG, where there is no dialog open and
        /// <see cref="DialogNpcId"/> is therefore <c>Guid.Empty</c> — so the guard swallowed it and the
        /// button did nothing but show its confirmation (owner, playtest-14). The server's AbandonQuest
        /// never reads the npc id at all.</summary>
        public async void QuestAction(string action, string id)
        {
            if (Phase != ClientPhase.InWorld) return;
            if (action != "abandon" && DialogNpcId == Guid.Empty) return;
            try { await _net.QuestActionAsync(action, id, DialogNpcId); }
            catch (Exception ex) { ClientLog.Warn("Quest: " + ex.Message); }
        }

        public async void BufferAction(string action, string skillId)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.BufferActionAsync(DialogNpcId, action, skillId ?? ""); }
            catch (Exception ex) { ClientLog.Warn("Buffer: " + ex.Message); }
        }

        public async void BuyItem(string defId, int quantity)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.BuyItemAsync(DialogNpcId, defId, quantity); }
            catch (Exception ex) { ClientLog.Warn("Buy: " + ex.Message); }
        }

        public async void SellItem(Guid instanceId, int quantity)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.SellItemAsync(DialogNpcId, instanceId, quantity); }
            catch (Exception ex) { ClientLog.Warn("Sell: " + ex.Message); }
        }

        public async void BuyBackItem(int index)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.BuyBackAsync(DialogNpcId, index); }
            catch (Exception ex) { ClientLog.Warn("BuyBack: " + ex.Message); }
        }

        /// <summary>Undo a bin-delete (C18). No NPC check — unlike buy-back, this one has to work in
        /// the field, because that is where you bin things by mistake.</summary>
        public async void RestoreItem(int index)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.RestoreItemAsync(index); }
            catch (Exception ex) { ClientLog.Warn("Restore: " + ex.Message); }
        }

        public async void OpenWarehouse()
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.OpenWarehouseAsync(); }
            catch (Exception ex) { ClientLog.Warn("Warehouse: " + ex.Message); }
        }

        public async void WarehouseDeposit(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.WarehouseDepositAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("Deposit: " + ex.Message); }
        }

        public async void WarehouseWithdraw(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.WarehouseWithdrawAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("Withdraw: " + ex.Message); }
        }

        public async void OpenAccountWarehouse()
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.OpenAccountWarehouseAsync(); }
            catch (Exception ex) { ClientLog.Warn("AccountWarehouse: " + ex.Message); }
        }

        public async void AccountWarehouseDeposit(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.AccountWarehouseDepositAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("Deposit: " + ex.Message); }
        }

        public async void AccountWarehouseWithdraw(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.AccountWarehouseWithdrawAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("Withdraw: " + ex.Message); }
        }

        public async void TeleportTo(string zoneId)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.TeleportAsync(DialogNpcId, zoneId); }
            catch (Exception ex) { ClientLog.Warn("Teleport: " + ex.Message); }
        }

        public async void ForgetSkill(string skillId)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.ForgetSkillAsync(DialogNpcId, skillId); }
            catch (Exception ex) { ClientLog.Warn("Forget: " + ex.Message); }
        }

        /// <summary>The quest log, as last pushed by the server.</summary>
        public QuestLog Quests { get; private set; }

        /// <summary>Which NPCs have a quest marker for me, from the server. Arrives with every quest-log
        /// push, so it is always in step with the log.</summary>
        public QuestMark[] QuestMarks { get; private set; } = new QuestMark[0];

        /// <summary>The expanded target window's contents, or null. Arrives only after asking.</summary>
        public TargetDetails Details { get; private set; }

        /// <summary>A pending resurrect offer, or null.</summary>
        public ResurrectOffer PendingResurrect { get; private set; }

        public async void InspectTarget(Guid targetId, bool withDrops)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.InspectTargetAsync(targetId, withDrops); }
            catch (Exception ex) { ClientLog.Warn("Inspect: " + ex.Message); }
        }

        public async void AnswerResurrect(bool accept)
        {
            PendingResurrect = null;   // clear locally; the server sends no "offer withdrawn" push
            try { await _net.ResurrectResponseAsync(accept); }
            catch (Exception ex) { ClientLog.Warn("Resurrect: " + ex.Message); }
        }

        /// <summary>Active buffs and debuffs. The server pushes these once a second while any are
        /// running, and once more when the last one drops.</summary>
        public BuffDto[] Buffs { get; private set; } = new BuffDto[0];

        /// <summary>Cancel a buff you no longer want (a movement-speed buff before a stealth approach,
        /// a mistaken toggle). Debuffs are NOT cancellable — that would defeat the point of them.</summary>
        public async void RemoveBuff(string buffKey)
        {
            if (Phase != ClientPhase.InWorld || string.IsNullOrEmpty(buffKey)) return;
            try { await _net.RemoveBuffAsync(buffKey); }
            catch (Exception ex) { ClientLog.Warn("RemoveBuff: " + ex.Message); }
        }

        /// <summary>Unspent skill points. Fed by BOTH the stats push (login, learning a skill) and the
        /// progress push (every kill) — SP is earned on the kill event, which never sends stats, so
        /// reading it from StatsUpdate alone left the figure frozen at its login value for the whole
        /// session and only right again after a relog.</summary>
        public int SkillPoints { get; private set; }

        /// <summary>The class currently being PLAYED — race, base/second/third class and its own level.
        /// A subclass swap replaces it, which is why the skills window reads this rather than anything
        /// remembered from the character screen.</summary>
        public SubclassDto ActiveClass { get; private set; }

        /// <summary>Every class this character owns (server-pushed). Drives the debug Class tab.</summary>
        public SubclassDto[] Subclasses { get; private set; } = System.Array.Empty<SubclassDto>();

        /// <summary>The UI, so a server push can refresh a panel that is currently showing stale data.</summary>
        public GameUi Ui { get; private set; }

        /// <summary>The server's last-reported tuning values (rates/karma/caps). Null until requested.</summary>
        public DebugConfigDto DebugConfig { get; private set; }

        private NetworkChannel _net;
        private Guid _selfId;

        // Remembered so a silent reconnect can restore the session (see NetworkChannel.Reconnected).
        private string _authUser, _authPass;
        private int _lastCharacterId = -1;
        private float _fpsWindowStart;
        private int _fpsWindowCount;
        private float _enteredAt;
        private float _lastResync = -99f;
        private bool _busy;

        public bool IsBusy => _busy;
        public bool IsConnected => _net != null && _net.IsConnected;

        private const string PrefUrl = "l2clone.serverUrl";
        private const string PrefUser = "l2clone.username";
        private const string PrefPass = "l2clone.password";
        private const string PrefRemember = "l2clone.rememberLogin";

        /// <summary>G4 (playtest-18): does the login screen keep the credentials between launches?
        /// ON stores username AND password after a login that actually succeeded; OFF stores neither
        /// and comes up blank. ⚠ PlayerPrefs is not a secret store — on Android it is a plain XML file
        /// in the app's private data. That is the same guarantee every "remember me" on a phone gives,
        /// and it is why this is a CHOICE rather than the unconditional behaviour.</summary>
        public bool RememberLogin { get; private set; } = true;

        /// <summary>Flip the remember-me choice. Turning it OFF wipes what is already stored rather
        /// than merely stopping future writes — "don't save my login" has to mean the one on disk too,
        /// or the box is a lie until the next successful login.</summary>
        public void SetRememberLogin(bool on)
        {
            RememberLogin = on;
            PlayerPrefs.SetInt(PrefRemember, on ? 1 : 0);
            if (!on) { PlayerPrefs.DeleteKey(PrefUser); PlayerPrefs.DeleteKey(PrefPass); }
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            ClientLog.Hook();
            _ = UnityMainThreadDispatcher.Instance;

            // 🔴 UNITY CAPS ANDROID AT 30 FPS BY DEFAULT. Nothing in this project ever set
            // targetFrameRate, so the client had been running at 30 the whole time — and 30fps is
            // visible as judder on a phone whose panel does 60+, no matter how good the network is.
            // Every "the movement is choppy" report had this underneath it.
            //
            // -1 would mean "as fast as the platform allows", which on mobile means cooking the
            // battery for frames nobody asked for. 60 is the honest target for a game this simple.
            Application.targetFrameRate = 60;

            // KEEP THE SCREEN ON. An MMO is watched as much as it is touched — you stand still while
            // regenerating, while auto-hunting, while reading a drop list — and the phone reads all of
            // that as "idle" and dims out. Tapping every ten seconds to stop the screen sleeping is
            // not gameplay. Unity restores the system setting when the app loses focus, so this only
            // applies while the game is in front.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Typing an IP and a username on a phone keyboard every launch is its own punishment.
            // The ADDRESS is remembered either way — it is not a credential, and re-typing an IP is
            // the punishment this line exists to end. Only the account is governed by the checkbox.
            ServerUrl = PlayerPrefs.GetString(PrefUrl, ServerUrl);
            RememberLogin = PlayerPrefs.GetInt(PrefRemember, 1) == 1;
            // `BL-93` — before ANYTHING spawns. Read late and the first creatures into view would be
            // built with the wrong shape and keep it until they walked out and back.
            EntityManager.LoadModelPreference();
            if (RememberLogin)
            {
                // The inspector values are the FALLBACK, so a fresh install still comes up on the
                // admin/admin debug seed the testing rig depends on. Once you log in as anyone else,
                // that is what comes back — which is the whole of his complaint.
                Username = PlayerPrefs.GetString(PrefUser, Username);
                Password = PlayerPrefs.GetString(PrefPass, Password);
            }
            else
            {
                Username = "";
                Password = "";
            }

            EnsureSceneRefs();
        }

        private void EnsureSceneRefs()
        {
            if (Entities == null)
            {
                Entities = FindAnyObjectByType<EntityManager>();
                if (Entities == null)
                    Entities = new GameObject("Entities").AddComponent<EntityManager>();
            }
            Entities.MissingEntity += RequestResync;

            if (CameraRig == null)
            {
                CameraRig = FindAnyObjectByType<CameraRig>();
                if (CameraRig == null && Camera.main != null)
                    CameraRig = Camera.main.gameObject.AddComponent<CameraRig>();
            }

            var input = FindAnyObjectByType<TouchInput>();
            if (input == null) input = gameObject.AddComponent<TouchInput>();
            input.Boot = this;

            var ui = FindAnyObjectByType<GameUi>();
            if (ui == null) ui = gameObject.AddComponent<GameUi>();
            ui.Boot = this;
            Ui = ui;   // so server pushes can refresh a panel that is showing stale data

            if (FindAnyObjectByType<GroundGrid>() == null)
                new GameObject("GroundGrid").AddComponent<GroundGrid>();

            // The scene's default 10×10 plane is far smaller than the world and almost the same grey
            // as the entity markers. WorldGround fixes both, on whatever object is already there.
            if (FindAnyObjectByType<WorldGround>() == null)
            {
                var ground = GameObject.Find("Ground");
                if (ground != null) ground.AddComponent<WorldGround>();
            }

            // HELD, not looked up on demand: FindAnyObjectByType skips INACTIVE objects, so once the
            // overlay was switched off the next lookup returned null and it could never be switched
            // back on. A toggle that only works in one direction is worse than no toggle.
            if (Zones == null)
            {
                Zones = FindAnyObjectByType<ZoneOverlay>();
                if (Zones == null) Zones = new GameObject("ZoneOverlay").AddComponent<ZoneOverlay>();
            }

            if (Marker == null)
            {
                Marker = FindAnyObjectByType<MoveMarker>();
                if (Marker == null) Marker = new GameObject("MoveMarker").AddComponent<MoveMarker>();
            }

            if (Decals == null)
            {
                Decals = FindAnyObjectByType<GroundDecals>();
                if (Decals == null) Decals = new GameObject("GroundDecals").AddComponent<GroundDecals>();
            }
        }

        private async void Start()
        {
            ClientLog.Info("Client v" + GameConstants.GameVersion + " ready. Server: " + ServerUrl);
            if (AutoLogin) await ConnectAndLogin(Username, Password, register: false);
        }

        private void Update()
        {
            // The destination ring lives exactly as long as the walk it describes. Prediction ends when
            // you arrive, when something cancels the walk, or when the server turns out to be taking you
            // somewhere else entirely (a skill on a far target makes it close on the ENEMY instead) —
            // and in every one of those cases a ring still sitting on the ground is a promise the game
            // is no longer keeping.
            if (Phase == ClientPhase.InWorld && Marker != null && Marker.IsShown
                && Entities != null && !Entities.SelfIsPredicting)
                Marker.Hide();

            if (_mobCasts.Count > 0) PruneMobCasts();

            if (Phase == ClientPhase.InWorld) TickWalkToTalk();

            // Frames/sec over a rolling second: 10/s means a healthy server tick reaching us.
            if (Time.realtimeSinceStartup - _fpsWindowStart >= 1f)
            {
                FramesPerSecond = _fpsWindowCount / Mathf.Max(0.0001f, Time.realtimeSinceStartup - _fpsWindowStart);
                _fpsWindowStart = Time.realtimeSinceStartup;
                _fpsWindowCount = 0;
            }

            // Watchdog for the one failure the delta feed cannot recover from on its own: frames are
            // arriving, we are in the world, and YOUR OWN entity isn't among them. A standing player is
            // byte-identical every tick, so the server never re-sends it and the HUD would sit on
            // "waiting for your entity …" indefinitely. Ask for a resync instead of waiting forever.
            if (Phase == ClientPhase.InWorld && FramesReceived > 0 && Entities != null
                && !Entities.States.ContainsKey(_selfId)
                && Time.realtimeSinceStartup - _enteredAt > 2f)
                RequestResync();
        }

        /// <summary>Tell the server to forget its per-connection diff state so the next frame re-sends
        /// every visible entity in full. Throttled: the conditions that trigger it persist for a few
        /// frames, and one request per tick would be a stampede.</summary>
        public async void RequestResync()
        {
            if (Phase != ClientPhase.InWorld || _net == null || !_net.IsConnected) return;
            if (Time.realtimeSinceStartup - _lastResync < 3f) return;
            _lastResync = Time.realtimeSinceStartup;
            try
            {
                ClientLog.Warn("World state out of sync — asking the server to re-send it.");
                await _net.RequestResyncAsync();
            }
            catch (Exception ex) { ClientLog.Warn("Resync: " + ex.Message); }
        }

        // ----- Flow ----------------------------------------------------------------------------

        public async Task ConnectAndLogin(string username, string password, bool register)
        {
            if (_busy) return;
            _busy = true;
            LastError = null;
            try
            {
                if (!IsConnected)
                {
                    Phase = ClientPhase.Connecting;
                    StatusMessage = "Connecting to " + ServerUrl + " …";
                    ClientLog.Info(StatusMessage);
                    await Connect();
                    ClientLog.Good("Connected.");

                    // REMEMBER THE URL AS SOON AS IT CONNECTS, not after a successful login.
                    //
                    // It used to be saved only in the login-success branch below, which meant a URL
                    // that reached the server but failed to authenticate — wrong password, an account
                    // that does not exist yet, a version refusal — was forgotten, and the address had
                    // to be typed again on a PHONE KEYBOARD next launch. That is exactly the situation
                    // when moving between networks, which is the only time the address changes at all.
                    //
                    // Connecting is the right test: it proves the address is reachable, and that is
                    // the only thing the address field is responsible for being right about.
                    PlayerPrefs.SetString(PrefUrl, ServerUrl);
                    PlayerPrefs.Save();
                }

                Phase = ClientPhase.Authenticating;
                StatusMessage = register ? "Registering …" : "Logging in …";

                var auth = register
                    ? await _net.RegisterAsync(username, password)
                    : await _net.LoginAsync(username, password);

                if (!auth.Success)
                {
                    Fail(auth.Error ?? (register ? "Registration failed." : "Login failed."));
                    return;
                }

                Username = username;
                _authUser = username;
                _authPass = password;
                PlayerPrefs.SetString(PrefUrl, ServerUrl);
                // Only credentials that WORKED are stored — storing them on submit would happily
                // remember a typo and hand it back on every launch.
                if (RememberLogin)
                {
                    PlayerPrefs.SetString(PrefUser, username);
                    PlayerPrefs.SetString(PrefPass, password);
                }
                PlayerPrefs.Save();

                ClientLog.Good((register ? "Registered" : "Logged in") + " as " + username + ".");
                await RefreshCharacters();
            }
            catch (Exception ex)
            {
                Fail(Describe(ex));
            }
            finally { _busy = false; }
        }

        private async Task Connect()
        {
            _net = new NetworkChannel();
            _net.SnapshotDeltaReceived += OnDelta;
            _net.SnapshotReceived += OnFullSnapshot;
            _net.StatsReceived += s => Main(() => { Stats = s; if (s != null) SkillPoints = s.SkillPoints; });
            _net.ProgressReceived += p => Main(() =>
            {
                Progress = p;
                // Keep ActiveClass.Level in step with the live level. The Subclasses push (which sets
                // ActiveClass) only fires on login and class add/swap — NOT on a normal level-up — so
                // ActiveClass.Level went STALE the moment you levelled. The Skills window keys both its
                // rebuild stamp AND the Learn tab's level-gating off it, so newly-learnable skills didn't
                // appear and Learn looked dead until a relog (device playtest: "dead when I levelled to
                // 7, had to relog"). SubclassDto is a record, so update it with a fresh copy.
                if (ActiveClass != null && ActiveClass.Level != p.Level)
                    ActiveClass = ActiveClass with { Level = p.Level };
                SkillPoints = p.SkillPoints;   // SP is earned on this event; stats aren't pushed here
                if (p.LeveledUp) ClientLog.Good("Level up! Now level " + p.Level + ".");
            });
            _net.GoldReceived += g => Main(() => Gold = g.Gold);
            // Auto-hunt drives the target window. The server owns the choice while the autopilot is on,
            // so this simply adopts it; when auto-hunt stops the server sends null and the window clears
            // rather than freezing on the last mob it killed.
            _net.AutoTargetReceived += t => Main(() => TargetId = t.TargetId);
            _net.CooldownsReceived += c => Main(() => ApplyCooldowns(c));
            _net.BuffsReceived += b => Main(() => Buffs = b?.Buffs ?? new BuffDto[0]);
            _net.TargetDetailsReceived += d => Main(() => Details = d);
            _net.PvpStateReceived += p => Main(() =>
            {
                if (p == null) return;
                PvpEnabled = p.Pvp;          // authoritative — the server may have refused the toggle
                Karma = p.Karma;
                PkCount = p.PkCount;
                PvpCount = p.PvpCount;
            });
            // `BL-82`. Two consumers, and they read DIFFERENT halves: the badge reads the staff flags,
            // the world reads the visibility ones. The role is taken from here too — this push is the
            // live one, so a /role while you are standing there moves the toolbox without a relog.
            _net.SelfStateReceived += s => Main(() =>
            {
                if (s == null) return;
                SelfState = s;
                Role = s.Role;
                ApplySelfVisibility();
            });
            _net.TitlesReceived += t => Main(() =>
            {
                HeldTitles = t?.Held ?? new string[0];
                WornTitle = t?.Worn ?? "";
                MayWriteTitle = t?.MayWrite ?? false;
                CustomTitle = t?.CustomText ?? "";
                CustomTitleColor = t?.CustomColor ?? "";
                TitlesRevision++;
            });
            _net.QuestLogReceived += q => Main(() => Quests = q);
            _net.QuestMarksReceived += m => Main(() => QuestMarks = m?.Marks ?? new QuestMark[0]);
            _net.DialogReceived += d => Main(() => Dialog = d);
            _net.AutoConfigReceived += c => Main(() =>
            {
                if (c == null) return;
                AutoConfig = c;                 // the authoritative, already-clamped config
                AutoHunting = c.Enabled;
                // The server's list is the truth, INCLUDING when it is empty (playtest-17 B1). This used
                // to be guarded by `c.Skills.Length > 0` to protect a client-side default of "basic
                // attack on" — a default that no longer exists (see AutoSkills), so all the guard did was
                // let one character's marks survive into the next one's session.
                AutoSkills.Clear();
                if (c.Skills != null)
                    foreach (var s in c.Skills) if (s.Enabled) AutoSkills.Add(s.SkillId);
            });
            _net.AutoHuntStatusReceived += st => Main(() =>
            {
                if (st == null) return;
                AutoHunting = st.Enabled;
                FarmCenter = new Vector2(st.FarmCenterX, st.FarmCenterY);
                AutoIdleSecondsLeft = st.IdleSecondsLeft;
                AutoOfflineSecondsLeft = st.OfflineSecondsLeft;
                _autoBudgetStamp = Time.unscaledTime;
            });
            _net.SocialOptionsReceived += s => Main(() =>
            {
                if (s == null) return;
                Social = (SocialOptions)s.Options;
                Ui?.RefreshOptionsWindow();
            });
            _net.CraftingReceived += c => Main(() =>
            {
                if (c == null) return;
                CraftProfession = (Profession)c.Profession;
                KnownRecipes = new HashSet<string>(c.KnownRecipes ?? new string[0]);
                CraftLevel = c.Level;
                CraftExp = c.Exp;
                CraftBandCap = c.BandCap;
                AtCraftMaster = c.AtMaster;
                Ui?.RefreshCraftingWindow();
            });
            _net.RegionReceived += r => Main(() => Ui?.ShowRegionNotice(r));
            _net.NoticeReceived += m => Main(() => Ui?.ShowToast(m));
            _net.SelectionReceived += o => Main(() => Ui?.ShowBoxSelection(o));
            _net.TitleColorsReceived += o => Main(() => Ui?.ShowTitleColorPicker(o));
            _net.PartyReceived += p => Main(() =>
            {
                Party = p?.Members ?? new PartyMemberDto[0];
                if (p != null) PartyLoot = p.LootMode;
            });
            _net.PartyInviteReceived += i => Main(() =>
            {
                PendingInvite = i;
                // The loot rule is named in the invite ON PURPOSE: joining a party silently changes
                // who gets the drops, and that is not something to discover after the fact.
                ClientLog.Good(i.InviterName + " invites you to a party (loot: " + i.LootMode + ").");
            });
            _net.ResurrectOfferReceived += o => Main(() =>
            {
                PendingResurrect = o;
                ClientLog.Good(o.FromName + " offers to resurrect you (" + (int)(o.ExpPct * 100f) + "% exp back).");
            });
            _net.InventoryReceived += i => Main(() =>
                Inventory = i?.Items ?? new InventoryItemDto[0]);
            _net.WarehouseReceived += w => Main(() =>
                Warehouse = w?.Items ?? new InventoryItemDto[0]);
            _net.AccountWarehouseReceived += w => Main(() =>
                AccountWarehouse = w?.Items ?? new InventoryItemDto[0]);
            _net.BuyBackReceived += b => Main(() =>
                BuyBack = b?.Items ?? new BuyBackEntryDto[0]);
            _net.RestoreReceived += r => Main(() =>
                Restorable = r?.Items ?? new BuyBackEntryDto[0]);
            _net.LearnedReceived += l => Main(() =>
            {
                Learned.Clear();
                if (l?.Skills != null)
                    foreach (var s in l.Skills) Learned[s.Id] = s.Level;
            });
            _net.SubclassesReceived += s => Main(() =>
            {
                if (s?.Classes == null) return;
                // Keep the WHOLE list, not just the active one: the debug Class tab lists every class
                // you own so you can swap between them, which is the owner's way of comparing two
                // builds in the same gear without relogging.
                Subclasses = s.Classes;
                foreach (var c in s.Classes) if (c.Active) { ActiveClass = c; break; }
                Ui?.RefreshDebugPanel();   // a swap that already happened must not still be offered
            });
            _net.DebugConfigReceived += c => Main(() =>
            {
                DebugConfig = c;
                Ui?.FillTuning(c);
            });
            _net.TradeRequestReceived += t => Main(() => Ui?.OnTradeRequest(t));
            _net.TradeStateReceived += t => Main(() => Ui?.OnTradeState(t));
            _net.SkillBarReceived += b => Main(() =>
            {
                // Copy into a fixed 60 rather than trusting the length: the bar is rendered by index
                // and a short array from an older server would throw on every frame.
                var slots = new string[GameConstants.SkillBarSlots];
                if (b?.Slots != null)
                    for (int i = 0; i < slots.Length && i < b.Slots.Length; i++) slots[i] = b.Slots[i];
                SkillBar = slots;
            });
            _net.ChatReceived += m => Main(() => AppendChat(m));
            _net.CombatReceived += OnCombat;
            // The ground circles. Both go straight to the decal renderer — nothing here decides
            // anything, because the server already decided what is visible and what colour it is.
            _net.TotemsReceived += t => Main(() => Decals?.SetTotems(t));
            _net.WhispsReceived += w => Main(() => Decals?.SetWhisps(w));   // `BL-109`
            _net.AreaEffectReceived += a => Main(() => Decals?.Flash(a));
            _net.CastReceived += c => Main(() =>
            {
                // Seconds <= 0 is the server saying the cast ENDED (finished or was cancelled), not a
                // zero-length cast — treating it as one would leave the bar stuck full.
                // `BL-93` — your own casting pose, off the cast bar's own message. Unlike a mob's,
                // this one IS told when the cast ends, so it needs no expiry of its own.
                var self = Entities != null ? Entities.Find(_selfId) : null;
                if (self != null) self.SetCasting(c != null && c.Seconds > 0f);

                if (c == null || c.Seconds <= 0f) { CastingSkill = null; return; }
                CastingSkill = c.SkillName;
                CastStartedAt = Time.realtimeSinceStartup;
                CastEndsAt = CastStartedAt + c.Seconds;

                // The walk is over HERE — when the SERVER confirms a cast started and roots you — and
                // not when the button was tapped. See UseSkill for why guessing was wrong.
                CancelMoveOrder();
            });
            _net.MobCastReceived += c => Main(() =>
            {
                if (c == null) return;
                // Seconds 0 = the mob's cast was cancelled or interrupted — drop the bar at once.
                // A cast that COMPLETES sends nothing (the server has no "finished" push), so a
                // finished bar has to expire on its own clock: see PruneMobCasts.
                var caster = Entities != null ? Entities.Find(c.CasterId) : null;
                if (c.Seconds <= 0f)
                {
                    _mobCasts.Remove(c.CasterId);
                    if (caster != null) caster.SetCasting(false);   // `BL-93`
                    return;
                }
                float now = Time.realtimeSinceStartup;
                _mobCasts[c.CasterId] = new MobCast
                {
                    SkillName = c.SkillName, StartedAt = now, EndsAt = now + c.Seconds,
                };
                if (caster != null) caster.SetCasting(true);        // `BL-93`
            });
            _net.Disconnected += m => Main(() =>
            {
                Phase = ClientPhase.Offline;
                StatusMessage = "Disconnected: " + m;
                ClientLog.Error(StatusMessage);
                if (Entities != null) Entities.Clear();
                Decals?.ClearTotems();
                Decals?.ClearWhisps();
            });
            _net.ForceDisconnected += m => Main(() =>
            {
                // The same message carries a kick AND the "you are now farming offline for 2h" reply
                // to our own /offline. Only one of those is bad news.
                if (_offlineFarmRequested) { _offlineFarmRequested = false; StatusMessage = m; ClientLog.Info(m); }
                else ClientLog.Error("Kicked by server: " + m);
            });
            _net.Reconnecting += () => Main(() =>
            {
                StatusMessage = "Connection dropped — reconnecting …";
                ClientLog.Warn(StatusMessage);
            });
            _net.Reconnected += () => Main(() => { _ = RestoreSession(); });

            await _net.ConnectAsync(ServerUrl);
        }

        /// <summary>
        /// Fold a server cooldown snapshot into <see cref="Cooldowns"/>. The snapshot is authoritative
        /// about WHAT is running, so anything absent from it is dropped — that is what clears an
        /// overlay when a timer was wiped rather than ticked away.
        ///
        /// The "total" (the overlay's denominator) is inferred, not sent: the push happens the tick a
        /// timer starts, so the first Seconds seen for a token IS its full reuse. It is only replaced
        /// when Seconds comes back HIGHER than what we were counting down to — i.e. the timer restarted.
        /// </summary>
        /// <summary>Translate the visibility half of <see cref="SelfState"/> into how YOUR OWN marker
        /// draws (`BL-82`). His rule, verbatim: *"the players in shtealt will see themselves with
        /// opacity to 0.7 and in invis 0.4 (for them selves only - for others stealth does nothing,
        /// invis vanishes them)"* — plus a golden ring for an admin in god mode.
        ///
        /// <para>Both invisibilities share the 0.4, because they make the same promise to the player:
        /// nobody can see you. That they end differently (a hide breaks when you act, <c>/invis</c>
        /// never does) is not something an opacity can say, and the chat line already does.</para>
        ///
        /// <para>🔑 This is a LOCAL effect on ONE marker — the self one. It must never be derived from
        /// anything the server says about another player, and it cannot be: nothing on the wire
        /// describes another player's stealth. A hidden character is simply not sent.</para></summary>
        private void ApplySelfVisibility()
        {
            if (Entities == null) return;
            var s = SelfState;
            float alpha = s == null ? 1f
                        : s.Invisible || s.Hidden ? 0.4f
                        : s.Stealthed ? 0.7f
                        : 1f;
            Entities.SetSelfVisual(alpha, s != null && s.GodMode);
        }

        private void ApplyCooldowns(CooldownUpdate update)
        {
            var entries = update?.Entries;
            if (entries == null || entries.Length == 0) { Cooldowns.Clear(); return; }

            float now = Time.unscaledTime;
            _cooldownSeen.Clear();
            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Id) || e.Seconds <= 0f) continue;
                _cooldownSeen.Add(e.Id);

                float total = e.Seconds;
                Reuse existing;
                if (Cooldowns.TryGetValue(e.Id, out existing))
                {
                    float remaining = existing.EndsAt - now;
                    // Still counting down the same run → keep the denominator we already have.
                    if (e.Seconds <= remaining + 0.15f) total = existing.Total;
                }
                Cooldowns[e.Id] = new Reuse { EndsAt = now + e.Seconds, Total = total };
            }

            // Drop what the server no longer lists.
            _cooldownDrop.Clear();
            foreach (var key in Cooldowns.Keys)
                if (!_cooldownSeen.Contains(key)) _cooldownDrop.Add(key);
            foreach (var key in _cooldownDrop) Cooldowns.Remove(key);
        }

        private readonly HashSet<string> _cooldownSeen = new HashSet<string>();
        private readonly List<string> _cooldownDrop = new List<string>();

        /// <summary>Seconds left on a bar token's reuse, and 0..1 of how much of it is still to run
        /// (1 = just started). Both 0 when the token is ready. Expired entries are reaped here, so the
        /// bar asking is also what keeps the dictionary small.</summary>
        public bool ReuseOf(string token, out float secondsLeft, out float fraction)
        {
            secondsLeft = 0f; fraction = 0f;
            if (string.IsNullOrEmpty(token)) return false;

            string key = token;
            Reuse r;
            if (!Cooldowns.TryGetValue(key, out r))
            {
                // A consumable has TWO reuse channels: a healing potion's per-item drink timer (which
                // the server keys by the item token) and a scroll's own skill reuse (keyed by the skill
                // the item grants). The bar only holds the item token, so resolve the second one here
                // or a Return scroll would look ready the whole time it isn't.
                if (!GameConstants.IsItemSlot(token)) return false;
                var idef = ItemCatalog.Get(GameConstants.ItemSlotDefId(token));
                if (idef == null || string.IsNullOrEmpty(idef.UseSkillId)) return false;
                key = idef.UseSkillId;
                if (!Cooldowns.TryGetValue(key, out r)) return false;
            }

            secondsLeft = r.EndsAt - Time.unscaledTime;
            if (secondsLeft <= 0f) { Cooldowns.Remove(key); secondsLeft = 0f; return false; }
            fraction = r.Total > 0f ? Mathf.Clamp01(secondsLeft / r.Total) : 1f;
            return true;
        }

        /// <summary>After a transport reconnect the new connection has no server session, so silently
        /// re-login and re-enter the character we were on. This is what stops a phone-link blip from
        /// dumping you to an empty, "not logged in" character screen.</summary>
        private async Task RestoreSession()
        {
            if (string.IsNullOrEmpty(_authUser)) return;
            Restoring = true;
            try
            {
                ClientLog.Info("Reconnected — restoring session …");
                var auth = await _net.LoginAsync(_authUser, _authPass);
                if (!auth.Success) { Fail("Re-login failed: " + auth.Error); return; }

                if (_lastCharacterId >= 0 && Phase == ClientPhase.InWorld)
                    await EnterWorld(_lastCharacterId, silent: true);
                else
                    await RefreshCharacters();
                ClientLog.Good("Session restored.");
            }
            catch (Exception ex) { Fail("Restore failed: " + Describe(ex)); }
            finally { Restoring = false; }
        }

        /// <summary>
        /// True while a silent reconnect is re-logging in and re-entering the world.
        ///
        /// Backgrounding the app on a phone drops the socket, so coming back always runs this — and it
        /// USED to show the character-select screen for the length of the round trip, because the
        /// restore called EnterWorld and the UI treats <see cref="ClientPhase.Entering"/> as "choosing
        /// a character". Nothing was broken; it just wore the wrong screen. The UI now keeps the world
        /// up and puts a "Reconnecting" notice over it.
        /// </summary>
        public bool Restoring { get; private set; }

        public async Task RefreshCharacters()
        {
            try
            {
                var list = await _net.ListCharactersAsync();
                Characters = list.Characters ?? Array.Empty<CharacterSlot>();
                Phase = ClientPhase.CharacterSelect;
                StatusMessage = Characters.Length + " character(s) on this account.";
                ClientLog.Info(StatusMessage);
            }
            catch (Exception ex) { Fail(Describe(ex)); }
        }

        public async Task CreateCharacter(string name, Race race, BaseClass baseClass)
        {
            if (_busy) return;
            // Same rule the server enforces, read from Game.Shared so the two cannot disagree — this is
            // only here to say WHY before a round trip, never to decide. The server is the authority and
            // re-runs the identical check (GameConstants.IsValidCharacterName).
            if (!GameConstants.IsValidCharacterName(name, out string nameError))
            { Fail("Create failed: " + nameError); return; }
            _busy = true;
            try
            {
                var err = await _net.CreateCharacterAsync(name, race, baseClass);
                if (!string.IsNullOrEmpty(err)) { Fail("Create failed: " + err); return; }
                ClientLog.Good("Created " + name + ".");
                await RefreshCharacters();
            }
            catch (Exception ex) { Fail(Describe(ex)); }
            finally { _busy = false; }
        }

        /// <summary>Delete a character (32e). The SERVER decides whether that is immediate or a
        /// scheduled deletion with a grace window — the client only asks and re-reads the list, so the
        /// rule lives in one place. There was no way to do this from the phone at all, which is also
        /// what made the admin fast-delete untestable.</summary>
        public async Task DeleteCharacter(int characterId)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                var err = await _net.DeleteCharacterAsync(characterId);
                if (!string.IsNullOrEmpty(err)) { Fail("Delete failed: " + err); return; }
                ClientLog.Good("Character deleted.");
                await RefreshCharacters();
            }
            catch (Exception ex) { Fail(Describe(ex)); }
            finally { _busy = false; }
        }

        /// <summary>Undo a pending deletion.</summary>
        public async Task CancelDeleteCharacter(int characterId)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                var err = await _net.CancelDeleteCharacterAsync(characterId);
                if (!string.IsNullOrEmpty(err)) { Fail("Restore failed: " + err); return; }
                ClientLog.Good("Character restored.");
                await RefreshCharacters();
            }
            catch (Exception ex) { Fail(Describe(ex)); }
            finally { _busy = false; }
        }

        /// <param name="silent">A re-entry after a transport blip rather than a player choosing a
        /// character. It keeps the phase at InWorld so the UI does not flash the character-select
        /// screen for the length of the round trip.</param>
        public async Task EnterWorld(int characterId, bool silent = false)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                if (!silent) Phase = ClientPhase.Entering;
                StatusMessage = silent ? "Reconnecting …" : "Entering world …";

                // Wipe the old world BEFORE the request goes out, not after it returns. The server starts
                // streaming the moment the character is in the world, and those frames can land while we
                // are still awaiting the reply — clearing afterwards threw away the one full spawn of
                // your own entity, which is then never re-sent because a standing player never changes.
                // That is the "waiting for your entity …" bug: mobs trickled back in as they wandered,
                // you never did.
                if (Entities != null) { Entities.Clear(); Entities.SetSelf(Guid.Empty); }
                Decals?.ClearTotems();
                Decals?.ClearWhisps();
                if (CameraRig != null) CameraRig.Target = null;   // re-acquire on the next frame
                if (Marker != null) { Marker.Follow = null; Marker.Hide(); }

                var result = await _net.EnterWorldAsync(characterId);
                if (!result.Success) { Fail("Enter failed: " + result.Error); return; }

                _selfId = result.EntityId;
                _lastCharacterId = characterId;
                Role = result.Role;

                // The server has always SENT its clock epoch on login and the client has always
                // thrown it away. Keeping it lets the status bar show in-game time off the SHARED
                // GameClock formula rather than a second copy of it — one epoch, one TimeScale, no
                // way for the two sides to disagree about what time it is.
                // ⚠ Normalise the Kind: over the wire this can come back Unspecified, and
                // `DateTime.UtcNow - Unspecified` would silently be out by the timezone offset.
                if (result.ServerEpochUtc != default)
                    GameClock.Epoch = result.ServerEpochUtc.Kind == DateTimeKind.Utc
                        ? result.ServerEpochUtc
                        : result.ServerEpochUtc.Kind == DateTimeKind.Local
                            ? result.ServerEpochUtc.ToUniversalTime()
                            : DateTime.SpecifyKind(result.ServerEpochUtc, DateTimeKind.Utc);
                Main(() =>
                {
                    if (Entities != null) Entities.SetSelf(_selfId);   // re-tints anything already spawned
                    // Hand the chat buffer to THIS character — restores what he was saying last time
                    // and nobody else's (playtest 28; see ClientLog.SwitchCharacter for why this and
                    // C1's "reset on exit" are the same rule). Keyed by character id, not name, so a
                    // rename keeps the history and two characters can never share a file.
                    ClientLog.SwitchCharacter(characterId.ToString());
                    _enteredAt = Time.realtimeSinceStartup;
                    Phase = ClientPhase.InWorld;
                    StatusMessage = "In world";
                    ClientLog.Good("In world at (" + Mathf.RoundToInt(result.X) + ", " + Mathf.RoundToInt(result.Y) + ")."
                                   + (IsAdmin ? "  [" + Role + "]" : ""));
                });
            }
            catch (Exception ex) { Fail(Describe(ex)); }
            finally { _busy = false; }
        }

        /// <summary>Clear everything that belongs to the CHARACTER you were playing, so re-entering the
        /// world (as the same or a different character) never shows a ghost from the last session — the
        /// party roster that hung around after a relog, a half-open NPC dialog, a stale target window.
        /// The server re-pushes what still applies on entry; this just makes "nothing" the default.</summary>
        private void ResetWorldTransients()
        {
            TargetId = null;
            _targetSeenAlive = null;
            // `BL-43`: entity ids are per-spawn, so a retaliation entry from the last session can only
            // ever be dead weight — and on the tiny chance of a reused id, a wrong priority.
            _recentAttackers.Clear();
            Party = new PartyMemberDto[0];
            PendingInvite = null;
            PendingResurrect = null;
            Dialog = null;
            Details = null;
            DialogNpcId = Guid.Empty;
            // Buffs and BuyBack are the two per-character caches the server pushes CONDITIONALLY —
            // buffs only while some are running, buy-back only when a vendor opens. Everything else
            // here (inventory, warehouse, stats, learned, quests, gold) is re-pushed on login, so it
            // corrects itself. These two do not: switching from a character with 30-day runes to
            // one with nothing left the first character's buffs sitting on the bar until some unrelated
            // push happened to replace them (using a potion "fixed" it). Same for the sold-items list.
            Buffs = new BuffDto[0];
            BuyBack = new BuyBackEntryDto[0];
            Restorable = new BuyBackEntryDto[0];   // per CHARACTER, like the sold list
            // Nobody in the world you are LEAVING is still casting at you. Entity ids are per-session,
            // so a leftover entry could otherwise land a bar on an unrelated mob after a relog.
            _mobCasts.Clear();
            Cooldowns.Clear();   // same conditional-push reason as Buffs: reuse is per CHARACTER
            SkillPoints = 0;   // its own field now, so it no longer clears with Stats
            // 🔴 The auto-hunt marks are per CHARACTER, and this set is a singleton that outlives the
            // character (playtest-17 B1). Leaving them behind is what made a freshly created character
            // arrive with the deleted one's actions already auto-on and firing — the flag looked like it
            // was stored "per account" because nothing on the client ever forgot it.
            AutoSkills.Clear();
            AutoHunting = false;
            AutoConfig = new AutoHuntConfigDto(false, 60, 40, false, new AutoSkillDto[0], new string[0]);
            // C1: the chat log is per CHARACTER too. A new character inherited the DELETED one's chat
            // (owner) for the same reason the auto-hunt marks above did — the buffer is a singleton
            // that outlives whoever was talking.
            //
            // ⚠ It is FILED now, not wiped (playtest 28: *"chat again is saved between logins. Don't
            // reset"*). SwitchCharacter("") stores the outgoing character's chat on disk and empties the
            // buffer, so the character screen and the next character still see a clean one — C1's
            // requirement — while the conversation is waiting when you come back. The System tab is
            // untouched either way: it is the diagnostics trail, not per-character.
            ClientLog.SwitchCharacter("");
        }

        public async void LeaveWorld()
        {
            try
            {
                // The server REFUSES this in combat (including while a DoT ticks), so stay put and say
                // why — going to the character screen anyway would leave the entity in the world and
                // then refuse to let us back into our own character.
                string refused = await _net.LeaveWorldAsync();
                if (!string.IsNullOrEmpty(refused))
                {
                    Main(() => { StatusMessage = refused; ClientLog.Warn(refused); });
                    return;
                }
                // Fetch the FRESH list BEFORE showing the select screen, then switch in ONE step.
                //
                // This used to flip the phase first and refresh after, which meant the select screen came
                // up holding the array captured at LOGIN — so your level and class read stale for exactly
                // one round trip (owner, playtest-13, and still visible in playtest-14). The server side
                // was never the problem: GameHub.LeaveWorld already awaits the character SAVE, so the row
                // on disk is correct by the time we get here. The only fault was drawing before asking.
                //
                // A failed list is not a reason to strand the player in a world we are about to clear —
                // fall back to the array we have and switch anyway.
                CharacterSlot[] fresh;
                try
                {
                    var list = await _net.ListCharactersAsync();
                    fresh = list.Characters ?? Array.Empty<CharacterSlot>();
                }
                catch (Exception ex)
                {
                    ClientLog.Warn("Leave (character list): " + ex.Message);
                    fresh = Characters;
                }

                Main(() =>
                {
                    if (Entities != null) Entities.Clear();
                    Decals?.ClearTotems();
                    Decals?.ClearWhisps();
                    ResetWorldTransients();
                    Characters = fresh;
                    Phase = ClientPhase.CharacterSelect;
                    StatusMessage = "Left the world.";
                });
            }
            catch (Exception ex) { ClientLog.Warn("Leave: " + ex.Message); }
        }

        /// <summary>
        /// Go offline-farming: the character STAYS in the world under the autopilot while this client
        /// returns to character select. The WPF harness had a button for this and the phone never got
        /// one, so since 0.42.8 there has been no way to start an offline session at all (playtest-17
        /// C12) — the whole server side was already here and simply had no caller.
        ///
        /// The server unbinds the character from this connection and answers with ForceDisconnect, but
        /// it does NOT drop the account session (that is keyed separately), so the same socket can go
        /// straight to character select — where the row now shows the offline timer.
        /// </summary>
        public async void StartOfflineFarm()
        {
            if (Phase != ClientPhase.InWorld) return;
            try
            {
                _offlineFarmRequested = true;
                await _net.StartOfflineFarmAsync();

                // The unbind happens on the server's next TICK. Asking for the list before that comes
                // back with no offline timer on the row we just left, which reads as "it didn't work".
                await Task.Delay(400);

                CharacterSlot[] fresh;
                try { fresh = (await _net.ListCharactersAsync()).Characters ?? Array.Empty<CharacterSlot>(); }
                catch (Exception ex)
                {
                    ClientLog.Warn("Offline farm (character list): " + ex.Message);
                    fresh = Characters;
                }

                Main(() =>
                {
                    if (Entities != null) Entities.Clear();
                    Decals?.ClearTotems();
                    Decals?.ClearWhisps();
                    ResetWorldTransients();
                    Characters = fresh;
                    Phase = ClientPhase.CharacterSelect;
                    StatusMessage = "Farming offline — your character keeps hunting.";
                });
            }
            catch (Exception ex)
            {
                _offlineFarmRequested = false;
                ClientLog.Warn("Offline farm: " + ex.Message);
            }
        }

        /// <summary>Set while our OWN /offline request is in flight, so the ForceDisconnect it provokes
        /// is not reported as "kicked by server".</summary>
        private bool _offlineFarmRequested;

        /// <summary>Sign out of the account and go back to the login screen. The connection itself is
        /// dropped, because the server's session (which account, which character) is keyed by the
        /// CONNECTION — keeping the socket open would leave us signed in on the server while the client
        /// showed a login form. The stored credentials are cleared too, or the reconnect handler would
        /// helpfully log us straight back in.</summary>
        public async void Logout()
        {
            _authUser = _authPass = null;
            _lastCharacterId = -1;
            Role = AccountRole.Player;
            SelfState = null;   // or the next character inherits this one's badge until its first tick

            try { if (_net != null && _net.IsConnected && Phase == ClientPhase.InWorld) await _net.LeaveWorldAsync(); }
            catch (Exception ex) { ClientLog.Warn("Logout (leave): " + ex.Message); }

            try { if (_net != null) await _net.DisposeAsync(); }
            catch (Exception ex) { ClientLog.Warn("Logout (close): " + ex.Message); }

            _net = null;
            Main(() =>
            {
                if (Entities != null) { Entities.Clear(); Entities.SetSelf(Guid.Empty); }
                Decals?.ClearTotems();
                Decals?.ClearWhisps();
                if (CameraRig != null) CameraRig.Target = null;
                if (Marker != null) { Marker.Follow = null; Marker.Hide(); }
                ResetWorldTransients();
                Stats = null; Progress = null; Gold = 0;
                Characters = Array.Empty<CharacterSlot>();
                LastError = null;
                Phase = ClientPhase.Offline;
                StatusMessage = "Signed out.";
                ClientLog.Info(StatusMessage);
            });
        }

        // ----- Server events -------------------------------------------------------------------

        private void OnDelta(SnapshotDelta delta)
        {
            Main(() =>
            {
                CountFrame();
                if (Entities == null) return;
                Entities.ApplyDelta(delta);
                AcquireCamera();
                // A despawned target is no longer a target — otherwise the HUD shows a ghost.
                // 🔴 Except a PARTY MEMBER (playtest-17 B7). Interest management stops sending an ally
                // who walks out of view, so this cleared the target ~10× a second and made assist, heal,
                // buff, kick and change-leader unreachable at exactly the range they matter. They are not
                // a ghost — the roster still carries them, and the target frame draws from it.
                if (TargetId.HasValue && !Entities.States.ContainsKey(TargetId.Value)
                    && !IsPartyMember(TargetId.Value))
                    TargetId = null;

                // MY target DIED — drop it (owner, playtest-21 `65d`:
                // *"if(target == current target && current target dies){ closes }"*). This lives here,
                // client-side, because only the client knows what is selected: the server used to do it
                // by pushing a null AutoTarget when ITS combat target went stale, which wiped a manual
                // selection made while the old one was still alive.
                //
                // ⚠ It is the alive→dead TRANSITION, not the dead STATE — his *"'DIES' not 'DEAD'"*.
                // Hence _targetSeenAlive: only a target we have actually watched breathing gets
                // dropped when it stops. Tapping a corpse on purpose (a necromancer will want to)
                // selects something already dead, was never watched, and so stays selected.
                if (TargetId.HasValue && Entities.TryGetState(TargetId.Value, out var tgt))
                {
                    if (!tgt.Dead) _targetSeenAlive = TargetId;
                    else if (_targetSeenAlive == TargetId) { TargetId = null; _targetSeenAlive = null; }
                }
            });
        }

        private void OnFullSnapshot(WorldSnapshot snapshot)
        {
            Main(() =>
            {
                CountFrame();
                if (Entities == null) return;
                Entities.ApplySnapshot(snapshot.Entities);
                AcquireCamera();
            });
        }

        /// <summary>Raised on the main thread for every combat event that involves YOU — the HUD turns
        /// these into floating damage numbers.</summary>
        public event Action<CombatEvent> CombatHappened;

        /// <summary>What this character is casting, and when it finishes (realtime). Name is null when
        /// nothing is being cast.</summary>
        public string CastingSkill { get; private set; }
        public float CastStartedAt { get; private set; }
        public float CastEndsAt { get; private set; }

        /// <summary>One MOB's cast in progress — what it is casting and the window it lands in.</summary>
        public class MobCast
        {
            public string SkillName;
            public float StartedAt, EndsAt;
        }

        /// <summary>Casts in progress by nearby MOBS, keyed by caster. The nameplate layer draws a bar
        /// from this over each one's head.</summary>
        private readonly Dictionary<Guid, MobCast> _mobCasts = new Dictionary<Guid, MobCast>();

        /// <summary>Scratch list for the prune below — reused so a per-frame sweep allocates nothing.</summary>
        private readonly List<Guid> _mobCastsDone = new List<Guid>();

        /// <summary>What <paramref name="id"/> is casting, or false if it is casting nothing right now.
        /// An entry past its end time counts as nothing: the cast landed.</summary>
        public bool TryGetMobCast(Guid id, out MobCast cast) =>
            _mobCasts.TryGetValue(id, out cast) && Time.realtimeSinceStartup < cast.EndsAt;

        /// <summary>
        /// Forget casts that have finished, and casts by mobs that are no longer here.
        ///
        /// Both are needed because the server only ever pushes a CANCELLATION: a spell that actually
        /// goes off says nothing, and a mob that dies or wanders out of the grid mid-cast says nothing
        /// either. Without this the dictionary would grow for the whole session and a killed caster
        /// would leave a bar hanging over its corpse.
        /// </summary>
        private void PruneMobCasts()
        {
            float now = Time.realtimeSinceStartup;
            _mobCastsDone.Clear();
            foreach (var kv in _mobCasts)
                if (now >= kv.Value.EndsAt || Entities == null || !Entities.States.ContainsKey(kv.Key))
                    _mobCastsDone.Add(kv.Key);
            foreach (var id in _mobCastsDone)
            {
                _mobCasts.Remove(id);
                // `BL-93` — drop the casting pose with the bar. A mob's finished cast sends nothing
                // (see the MobCastReceived handler), so this expiry is the ONLY thing that ever ends
                // it — miss it and the creature stands there mid-incantation until it dies.
                if (Entities != null) { var v = Entities.Find(id); if (v != null) v.SetCasting(false); }
            }
        }

        /// <summary>The COMBAT feed's colours (D5). Damage you DEAL is green and damage dealt to YOU is
        /// red, so the direction of a fight reads off the colour alone — a wall of one colour was the
        /// state before, and on a phone you are not reading the words mid-fight. Deliberately a deeper
        /// green than <see cref="ClientLog.Good"/>'s lime (owner: "green, not lime"), which stays the
        /// System tab's "that worked" colour and should not be confused with it.</summary>
        private static readonly Color DealtColour  = new Color(0.36f, 0.85f, 0.45f);
        private static readonly Color TakenColour  = new Color(1f, 0.42f, 0.42f);
        private static readonly Color LootColour   = new Color(0.95f, 0.82f, 0.35f);
        private static readonly Color RewardColour = new Color(0.60f, 0.78f, 1f);

        private void OnCombat(CombatEvent e)
        {
            Main(() =>
            {
                // `BL-93` — the swing, BEFORE the self-filter below. Everything after that filter is
                // about YOUR combat log, and rightly ignores two strangers trading blows; an animation
                // is the opposite — the whole point is that the fight across the clearing looks like a
                // fight. The server already sends this event to everyone nearby, so a full attack
                // animation for every visible creature costs no new message and no new field.
                if (Entities != null && e.AttackerId != Guid.Empty)
                {
                    var attacker = Entities.Find(e.AttackerId);
                    if (attacker != null) attacker.PlayAttack();
                }

                if (e.AttackerId != _selfId && e.TargetId != _selfId) return;
                // `BL-43`: remember who is hitting US, so the target cycle can put them first. Stamped
                // on every inbound blow regardless of outcome — a MISS still tells you something has
                // decided to fight you, which is exactly what you want to be able to select.
                if (e.TargetId == _selfId && e.AttackerId != _selfId)
                    _recentAttackers[e.AttackerId] = Time.realtimeSinceStartup;
                if (CombatHappened != null) CombatHappened(e);
                bool mine = e.AttackerId == _selfId;
                string verb = mine ? "You → " + e.TargetName : e.AttackerName + " → you";
                // Tab.Combat, not System (D5): one fight writes a line per swing, which is what buried
                // every whisper and every refusal in the one console this used to share.
                ClientLog.Chat(verb + "  " + e.Outcome + (e.Damage != 0 ? " " + e.Damage : "")
                               + (string.IsNullOrEmpty(e.Skill) ? "" : " (" + e.Skill + ")"),
                               mine ? DealtColour : TakenColour, ClientLog.Tab.Combat);
            });
        }

        private void CountFrame()
        {
            FramesReceived++;
            _fpsWindowCount++;
            LastFrameTime = Time.realtimeSinceStartup;
        }

        private void AcquireCamera()
        {
            if (CameraRig == null || CameraRig.Target != null) return;
            var self = Entities.Find(_selfId);
            if (self != null)
            {
                CameraRig.Target = self.transform;
                if (Marker != null) Marker.Follow = self.transform;   // so it clears on arrival
            }
        }

        // ----- Commands ------------------------------------------------------------------------

        /// <summary>True while a cast is in progress — casting ROOTS you (commit on start), so the
        /// server rejects movement until it finishes or you cancel it.</summary>
        public bool IsCasting => !string.IsNullOrEmpty(CastingSkill)
                                 && Time.realtimeSinceStartup < CastEndsAt;

        public async void Move(float serverX, float serverY)
        {
            if (Phase != ClientPhase.InWorld) return;

            // The server DROPS a move command while casting (HandleMove returns early). Sending one
            // anyway and dropping a destination ring on the ground advertises an order that was thrown
            // away — the character stands still next to a marker promising otherwise. Say why instead.
            //
            // Deliberately NOT queued-until-the-cast-ends: that is rare in the genre (WoW/FFXIV cancel
            // the cast on move; IG roots you and ignores the click) and this project's design is the
            // IG one — "casting roots you, ESC cancels".
            if (IsCasting)
            {
                ClientLog.Warn("Can't move while casting — tap the cast bar (or press Back) to cancel.");
                return;
            }

            // An NPC conversation PINS you where you stand (owner, playtest-19 M13). Every dialog action
            // — teleport, buy, deposit, learn — is re-checked against the distance to that NPC, so a
            // ground tap made just before opening the window walked you out of range and the teleport
            // you then chose answered "Too far". The walk-to-talk below is the sanctioned way in.
            if (Ui != null && Ui.NpcWindowOpen)
            {
                ClientLog.Warn("Close the window first — you can't walk away mid-conversation.");
                return;
            }

            // DEAD is not a movement state, it is the absence of one (owner, playtest-19 M4). The server
            // already refuses and rubber-bands you back, but that is the safety net, not the fix: the
            // corpse slid around on your own screen while standing still on everyone else's.
            if (Entities != null && Entities.TryGetState(SelfId, out var deadCheck) && deadCheck.Dead)
            {
                ClientLog.Warn("You are dead — you can't move.");
                return;
            }

            // Don't predict a walk the server will DROP — that mismatch is what rubber-bands you. It
            // drops one while you're SITTING, and whenever your effective speed is 0 (stun / root): the
            // last delta's self-speed is that number, so a 0 there means "you can't move right now".
            bool sitting = Stats != null && Stats.MoveState == MoveState.Sitting;
            bool immobile = Entities != null && Entities.TryGetState(SelfId, out var selfState)
                            && selfState.Speed <= 0.01f;
            if (sitting || immobile)
            {
                ClientLog.Warn(sitting ? "Stand up first — you can't move while sitting."
                                       : "You can't move right now.");
                return;
            }

            // CLIENT-SIDE WALLS (playtest-11 item 23 / `B10`, the owner's 2026-07-24 architecture call).
            // The client is the half that must STOP YOU AT THE SURFACE and never emit an out-of-world
            // coordinate; the server's ConfineToDomain stays as the anti-cheat backstop. Until now only
            // the backstop existed, so the everyday experience of a wall was a rubber-band — you walked
            // through it and were yanked. Both halves read the SAME geometry (WorldDomain, Game.Shared),
            // which is the only reason it is safe for the client to decide anything here.
            //
            // Two different answers, because they are two different mistakes:
            //   • tapping PAST the edge of the world you're in → walk to the edge and stop (clamp);
            //   • tapping INSIDE another world → don't issue the order at all, and say why. Crossing is
            //     teleport-only, so silently walking you to the nearest wall would be a lie.
            if (Entities != null && Entities.TryGetState(SelfId, out var hereState))
            {
                var domain = WorldDomain.At(hereState.X, hereState.Y);
                if (!domain.Contains(serverX, serverY))
                {
                    var tapped = WorldDomain.At(serverX, serverY);
                    if (tapped != domain && tapped.Contains(serverX, serverY))
                    {
                        ClientLog.Warn("You can't walk to " + tapped.Name + " — only a teleport goes there.");
                        return;
                    }
                    var stop = domain.Clamp(serverX, serverY);
                    serverX = stop.X; serverY = stop.Y;
                }
            }

            // Drop the destination ring here rather than in TouchInput, so EVERY move order shows one
            // — including any future ones that don't come from a tap.
            if (Marker != null) Marker.ShowAt(WorldMapper.ToUnity(serverX, serverY));

            // PREDICT IMMEDIATELY. Waiting for the server's first position means every step you take
            // starts a round trip late, and no amount of smoothing hides that on the character you are
            // driving. The walk is deterministic — straight toward the target at Speed — so the client
            // can run exactly the same simulation the server will, and be corrected if it is wrong.
            if (Entities != null) Entities.PredictSelfMoveTo(serverX, serverY);

            try { await _net.MoveAsync(serverX, serverY); }
            catch (Exception ex) { ClientLog.Warn("Move: " + ex.Message); }
        }

        /// <summary>
        /// Fire one skill-bar slot. A slot holds a skill id, an "action:…" token or an "item:…" token;
        /// the server never interprets them, the CLIENT dispatches. Actions the mobile client hasn't
        /// grown yet say so out loud rather than doing nothing — a dead button that stays silent is
        /// indistinguishable from a broken one.
        /// </summary>
        public void UseSlot(string token)
        {
            if (Phase != ClientPhase.InWorld || string.IsNullOrEmpty(token)) return;

            if (ActionCatalog.FromToken(token) is ActionDef action)
            {
                // No blanket CancelMoveOrder here. "Every action either stops you or retargets you" is
                // simply untrue: a Run/Walk toggle changes your SPEED and you keep going, a basic
                // attack with no target does nothing at all, and standing up is not a stop. Each case
                // below already cancels the walk when it really ends it (Attack and SetMoveState do),
                // so the blanket call could only ever be wrong — and it was: tapping a bar slot cut the
                // walk's prediction dead while the server walked on.
                switch (action.Id)
                {
                    case GameConstants.ActionBasicAttack:
                        // Same verb as the second tap — including "a party member is followed, not hit".
                        if (TargetId.HasValue) AttackOrFollow(TargetId.Value);
                        else ClientLog.Warn("No target.");
                        break;
                    case GameConstants.ActionSitStand:
                        SetMoveState(Stats != null && Stats.MoveState == MoveState.Sitting
                                     ? MoveState.Running : MoveState.Sitting);
                        break;
                    case GameConstants.ActionRunWalk:
                        // Walk/run only changes SPEED, and only once you're standing — toggling it while
                        // seated must NOT stand you up (owner). Stand first with Sit/Stand.
                        if (Stats != null && Stats.MoveState == MoveState.Sitting)
                            ClientLog.Warn("Stand up first — walk/run only changes speed while standing.");
                        else
                            SetMoveState(Stats != null && Stats.MoveState == MoveState.Walking
                                         ? MoveState.Running : MoveState.Walking);
                        break;
                    case GameConstants.ActionTargetClosest:
                        TargetClosest();
                        break;
                    case GameConstants.ActionTradeTarget:
                        if (TargetId.HasValue) { var id = TargetId.Value; Trade(n => n.TradeRequestAsync(id), "request"); }
                        else ClientLog.Warn("Target a player to trade.");
                        break;
                    case GameConstants.ActionPartyInvite:
                        if (TargetId.HasValue) PartyInvite(TargetId.Value);
                        else ClientLog.Warn("Target a player to invite.");
                        break;
                    case GameConstants.ActionFollowTarget:
                        if (TargetId.HasValue) Follow(TargetId.Value);
                        else ClientLog.Warn("Target a player to follow.");
                        break;
                    case GameConstants.ActionAssistTarget:
                        if (TargetId.HasValue) Assist(TargetId.Value);
                        else ClientLog.Warn("Target a player to assist.");
                        break;

                    // ---- Name-only commands: the TARGET supplies the name, so nothing is typed. The
                    //      friend hub takes a NAME (friendship must work on someone who is offline), so
                    //      these resolve the target to its name first; the party ones take an id. ----
                    case GameConstants.ActionFriendAdd:
                        if (TargetPlayerName() is string addName) FriendCommand("add", addName);
                        else ClientLog.Warn("Target a player to add as a friend.");
                        break;
                    case GameConstants.ActionFriendRemove:
                        if (TargetPlayerName() is string remName) FriendCommand("remove", remName);
                        else ClientLog.Warn("Target a player to remove from your friends.");
                        break;
                    case GameConstants.ActionFriendList:
                        FriendCommand("list", "");
                        break;
                    // The one action that can't finish the job: a whisper needs a MESSAGE, and no button
                    // can supply one. So it does the half a button CAN do — the name, which is the part
                    // that is miserable to type on a phone — and hands you the caret.
                    case GameConstants.ActionWhisperTarget:
                        if (TargetPlayerName() is string wspName) ComposeWhisper(wspName);
                        else ClientLog.Warn("Target a player to whisper.");
                        break;
                    case GameConstants.ActionLike:
                        if (TargetPlayerName() is string likeName) Like(likeName);
                        else ClientLog.Warn("Target a player to like.");
                        break;
                    case GameConstants.ActionBlock:
                        if (TargetPlayerName() is string blkName) BlockCommand("block", blkName);
                        else ClientLog.Warn("Target a player to block.");
                        break;
                    case GameConstants.ActionUnblock:
                        if (TargetPlayerName() is string ublkName) BlockCommand("unblock", ublkName);
                        else ClientLog.Warn("Target a player to unblock.");
                        break;
                    case GameConstants.ActionPartyLeave:
                        PartyLeave();
                        break;
                    case GameConstants.ActionPartyKick:
                        if (TargetId.HasValue) PartyKick(TargetId.Value);
                        else ClientLog.Warn("Target a party member to remove.");
                        break;
                    case GameConstants.ActionPartyLeader:
                        if (TargetId.HasValue) PartyChangeLeader(TargetId.Value);
                        else ClientLog.Warn("Target a party member to pass leadership to.");
                        break;
                    default:
                        ClientLog.Warn(action.Name + " isn't available on the phone yet.");
                        break;
                }
                return;
            }

            if (GameConstants.IsItemSlot(token))
            {
                // Drink/use one of that item from the bag. The slot holds "item:<defId>", so any stack
                // of that potion satisfies it — this is the quick-use bar (owner).
                string defId = token.Substring(GameConstants.SkillBarItemPrefix.Length);
                if (FindBagItem(defId) is Guid iid) UsePotion(iid);
                else ClientLog.Warn("You have no " + (ItemCatalog.Get(defId)?.Name ?? defId) + " to use.");
                return;
            }

            if (GameConstants.IsPresetSlot(token))
            {
                if (int.TryParse(token.Substring(GameConstants.SkillBarPresetPrefix.Length), out int ps))
                    ApplyEquipPreset(ps);
                return;
            }

            UseSkill(token);
        }

        /// <summary>
        /// Whether this character is willing to fight other PLAYERS.
        ///
        /// With it OFF you can only hit mobs — the server refuses a swing at a player outright, which
        /// is what stops a stray tap in a crowd from starting a fight you did not want. It is not a
        /// client-side courtesy: <c>CanPvpHit</c> re-checks it, along with safe zones.
        ///
        /// Set optimistically on tap and then CORRECTED by the server's PvpState push, which is the
        /// authority — it also refuses the toggle outright in a safe zone, and a button that keeps
        /// claiming "PvP ON" after a refusal is worse than one that lags by a tick.
        /// </summary>
        public bool PvpEnabled { get; private set; }

        /// <summary>Reputation, straight from the PvpState push. Karma &gt; 0 makes you a PK: guards
        /// attack, you drop gear on death, and towns stop being safe for you.</summary>
        public int Karma { get; private set; }
        public int PkCount { get; private set; }
        public int PvpCount { get; private set; }

        // ----- Wearable titles -----------------------------------------------------------------
        /// <summary>Leaderboard categories this character currently TOPS — the titles it may wear. The
        /// server decides; the picker only ever offers what is in here.</summary>
        public string[] HeldTitles { get; private set; } = Array.Empty<string>();

        /// <summary>The category whose title is being worn, "" for none. Reported as "" by the server
        /// when the choice is no longer held, so this and the plate always agree.</summary>
        public string WornTitle { get; private set; } = "";

        /// <summary>Bumped on every Titles push so the Rank window redraws only when something changed.</summary>
        public int TitlesRevision { get; private set; }

        /// <summary>Has this character been granted the right to write its own title? The picker's
        /// custom row and the `/title` hint are shown only when it has — offering either to someone who
        /// cannot use them is an advertisement, not a feature.</summary>
        public bool MayWriteTitle { get; private set; }

        /// <summary>What this character last wrote for itself, and in what colour ("" = never wrote
        /// one). Kept by the server even while a board title is worn, so the picker can offer it back.</summary>
        public string CustomTitle { get; private set; } = "";
        public string CustomTitleColor { get; private set; } = "";

        public async void SetTitle(string category)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.SetTitleAsync(category ?? ""); }
            catch (Exception ex) { ClientLog.Warn("Title: " + ex.Message); }
        }

        /// <summary>`/title &lt;text&gt;` — write your own title and wear it; "" clears it. The text is
        /// checked HERE only to save a round trip on an obvious mistake; the server validates it again
        /// and is the authority (a hand-rolled client could skip this entirely).</summary>
        public async void SetCustomTitle(string text)
        {
            if (Phase != ClientPhase.InWorld) return;
            text = (text ?? "").Trim();
            if (text.Length > 0 && !TitleCatalog.IsValidCustom(text, out string reason))
            { ClientLog.Warn(reason); return; }
            try { await _net.SetCustomTitleAsync(text); }
            catch (Exception ex) { ClientLog.Warn("Title: " + ex.Message); }
        }

        /// <summary>`/titlecolor &lt;name&gt;` — recolour the title you wrote.</summary>
        public async void SetTitleColor(string color)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.SetTitleColorAsync(color ?? ""); }
            catch (Exception ex) { ClientLog.Warn("Title colour: " + ex.Message); }
        }

        public async void TogglePvp()
        {
            if (Phase != ClientPhase.InWorld) return;
            PvpEnabled = !PvpEnabled;
            ClientLog.Info(PvpEnabled
                ? "PvP ON — you can hit other players, and they can hit you back."
                : "PvP off — mobs only.");
            try { await _net.TogglePvpAsync(PvpEnabled); }
            catch (Exception ex) { ClientLog.Warn("PvP: " + ex.Message); }
        }

        /// <summary>
        /// Idle farming. The AUTOPILOT LIVES ON THE SERVER — targeting, skill choice, potions, the
        /// lot — so this is genuinely one flag, not a client bot. That is also why it keeps running
        /// when the app is closed.
        /// </summary>
        public bool AutoHunting { get; private set; }

        /// <summary>Where the STATIC farm circle is anchored, in world units, as the server reports it.
        /// Only meaningful while AutoConfig.StaticSpot is on — the roaming mode has no anchor because
        /// the scan follows the character.</summary>
        public Vector2 FarmCenter { get; private set; }

        // ---- Auto-hunt time budgets (32q). The server pushes them with every AutoHunt status (each
        //      regen tick); between pushes we count down locally off the stamp, so the button ticks
        //      smoothly instead of jumping in 3-second steps. -1 = uncapped.
        public int AutoIdleSecondsLeft { get; private set; } = -1;
        public int AutoOfflineSecondsLeft { get; private set; } = -1;
        private float _autoBudgetStamp;

        /// <summary>Idle seconds left, interpolated since the last push. -1 when uncapped. The clock
        /// only runs while auto-hunt is ON — that is the budget the server actually spends.</summary>
        public int AutoIdleSecondsLeftNow => AutoIdleSecondsLeft < 0 ? -1
            : Mathf.Max(0, AutoIdleSecondsLeft - (AutoHunting
                ? Mathf.FloorToInt(Time.unscaledTime - _autoBudgetStamp) : 0));

        /// <summary>Skill ids the player has marked for auto-use (the per-slot Auto toggle writes here).
        /// Starts EMPTY (owner): only what you explicitly mark Auto is auto-used. Basic attack used to be
        /// seeded in here, which made auto-hunt swing a weapon the moment it was enabled even though the
        /// player never asked — the owner's rule is that nothing is auto unless it was marked, so basic
        /// attack must be opted in like any skill.</summary>
        public readonly HashSet<string> AutoSkills = new HashSet<string>();

        /// <summary>The last config the SERVER confirmed (potions %, buff potions, farm range, ranks).
        /// The auto-potions and auto-farm windows read and edit THIS; every push preserves the fields
        /// it does not own, because <c>SetAutoHuntConfig</c> replaces the whole config wholesale —
        /// hardcoding the untouched half (as the toggle used to) silently reset the player's settings on
        /// every on/off.</summary>
        public AutoHuntConfigDto AutoConfig { get; private set; } =
            new AutoHuntConfigDto(false, 60, 40, false, new AutoSkillDto[0], new string[0]);

        /// <summary>Build a full config that flips Enabled and reflects the current auto-skill marks,
        /// while carrying every other field (potions, farm) forward from the cached config.</summary>
        private AutoHuntConfigDto BuildAutoConfig(bool enabled)
        {
            var skills = new List<AutoSkillDto>();
            foreach (var id in AutoSkills)
                if (id == AutoHuntIds.BasicAttack || Learned.ContainsKey(id))
                {
                    int extra = 0;   // preserve any per-skill reuse the server already knows
                    if (AutoConfig.Skills != null)
                        foreach (var s in AutoConfig.Skills)
                            if (s.SkillId == id) { extra = s.ExtraDelayTicks; break; }
                    skills.Add(new AutoSkillDto(id, true, extra));
                }
            return AutoConfig with { Enabled = enabled, Skills = skills.ToArray() };
        }

        public async void ToggleAutoHunt()
        {
            if (Phase != ClientPhase.InWorld) return;
            AutoHunting = !AutoHunting;

            try
            {
                // The CONFIG carries the actions: the server's autopilot only uses skills it was GIVEN,
                // and an empty list is why auto-hunt "just wandered". Basic attack is the pseudo-skill
                // that makes it melee at all; without it a fighter walks up to a mob and stares at it.
                // Sending the WHOLE config (not a bare toggle) keeps the potion/farm settings the player
                // configured in their windows — a bare enable used to overwrite them with defaults.
                await _net.SetAutoHuntConfigAsync(BuildAutoConfig(AutoHunting));
                ClientLog.Info(AutoHunting
                    ? "Auto-hunt ON (" + AutoSkills.Count + " action(s))."
                    : "Auto-hunt off.");
            }
            catch (Exception ex)
            {
                AutoHunting = !AutoHunting;   // the server never heard us; keep the button honest
                ClientLog.Warn("AutoHunt: " + ex.Message);
            }
        }

        /// <summary>Push a config edited by the auto-potions / auto-farm windows. Optimistic on the
        /// cache; the server's echo confirms the clamped values.</summary>
        public async void PushAutoConfig(AutoHuntConfigDto cfg)
        {
            if (Phase != ClientPhase.InWorld || cfg == null) return;
            AutoConfig = cfg;
            AutoHunting = cfg.Enabled;
            try { await _net.SetAutoHuntConfigAsync(cfg); }
            catch (Exception ex) { ClientLog.Warn("Auto config: " + ex.Message); }
        }

        /// <summary>Mark/unmark a skill for auto-use and push it.
        ///
        /// <para>The push is unconditional. It used to happen only while auto-hunt was RUNNING, which
        /// left a mark made with auto-hunt off living nowhere but this client — and the marks are per
        /// CHARACTER, so the server has to be the one holding them (playtest-17 B1).</para></summary>
        public void ToggleAutoSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return;
            if (!AutoSkills.Remove(skillId)) AutoSkills.Add(skillId);
            PushAutoConfig(BuildAutoConfig(AutoHunting));
        }

        /// <summary>The auto-hunt id a BAR TOKEN maps to, or null when the autopilot cannot repeat it.
        ///
        /// <para>A skill is its own id. The BASIC ATTACK is the exception: it is an action token on the
        /// bar but the server knows it as the pseudo-skill <see cref="AutoHuntIds.BasicAttack"/>, the
        /// entry that decides whether the autopilot melees at all.</para>
        ///
        /// <para>It lives here rather than in the UI because the BAR owns the mark: a token that leaves
        /// the bar has to take its auto flag with it, and <see cref="AssignSlot"/> is where that
        /// happens.</para></summary>
        public static string AutoIdFor(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            var action = ActionCatalog.FromToken(token);
            if (action != null)
                return action.Id == GameConstants.ActionBasicAttack ? AutoHuntIds.BasicAttack : null;

            var def = SkillCatalog.Get(token);
            if (def == null) return null;
            return def.Passive != null || def.Category == SkillCategory.Passive ? null : token;
        }

        /// <summary>Drop the auto mark of a token that has just left the bar, and tell the server.
        /// His rule (playtest-17 B1): *"removing something from the bar automatically disables the
        /// auto-on ... when u put it back u need to reactivate it."* Without this the autopilot goes on
        /// firing an action the bar no longer shows, which is unexplainable from the screen.</summary>
        private void ClearAutoMarkFor(string token, string[] barAfter)
        {
            var autoId = AutoIdFor(token);
            if (autoId == null || !AutoSkills.Contains(autoId)) return;

            // Only if it is gone from the bar ENTIRELY — the same skill may sit in a second slot, and
            // moving a token between slots must not disarm it.
            if (barAfter != null)
                foreach (var t in barAfter)
                    if (AutoIdFor(t) == autoId) return;

            AutoSkills.Remove(autoId);
            PushAutoConfig(BuildAutoConfig(AutoHunting));
        }

        public bool CounterAttack { get; private set; }

        public async void ToggleCounterAttack()
        {
            if (Phase != ClientPhase.InWorld) return;
            CounterAttack = !CounterAttack;
            ClientLog.Info(CounterAttack ? "Counter-attack ON." : "Counter-attack off.");
            try { await _net.ToggleCounterAttackAsync(CounterAttack); }
            catch (Exception ex) { ClientLog.Warn("CounterAttack: " + ex.Message); }
        }

        /// <summary>ESC/cancel a cast in progress. The server keeps the initial MP and starts the
        /// cooldown — cancelling is a choice with a cost, not a free undo.</summary>
        public async void CancelCast()
        {
            if (Phase != ClientPhase.InWorld) return;
            // Clear it locally too: the server has no "cast cancelled" push, so waiting for one would
            // leave the bar sitting there until the original duration ran out.
            CastingSkill = null;
            try { await _net.CancelCastAsync(); }
            catch (Exception ex) { ClientLog.Warn("CancelCast: " + ex.Message); }
        }

        public async void LearnSkill(string skillId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.LearnSkillAsync(skillId); }
            catch (Exception ex) { ClientLog.Warn("Learn: " + ex.Message); }
        }

        /// <summary>Commit a planned set of stat-swap rungs in one charge (the Stats tab). The server
        /// re-validates and re-prices the whole basket — the tab's own totals are a PREVIEW, never the
        /// bill — and answers with the usual Stats/Learned/Gold pushes.</summary>
        public async void BuyStatSwaps(StatSwapPurchaseDto[] picks)
        {
            if (Phase != ClientPhase.InWorld || picks == null || picks.Length == 0) return;
            try { await _net.BuyStatSwapsAsync(picks); }
            catch (Exception ex) { ClientLog.Warn("BuyStatSwaps: " + ex.Message); }
        }

        /// <summary>
        /// Put a token in a bar slot and send the WHOLE bar back.
        ///
        /// This is the one write the client is allowed to make, and only because the PLAYER moved
        /// something. Writing a bar the client authored itself — reacting to a Learned push, say —
        /// is what destroyed real layouts in the WPF client twice: it looked right on screen while
        /// the server's copy was already wrong.
        /// </summary>
        public async void AssignSlot(int index, string token)
        {
            if (Phase != ClientPhase.InWorld) return;
            if (SkillBar == null || index < 0 || index >= SkillBar.Length) return;

            var slots = (string[])SkillBar.Clone();
            var displaced = slots[index];   // Remove (token == null) or an overwrite — either way it left
            slots[index] = token;
            SkillBar = slots;               // optimistic: the server echoes the bar back anyway
            ClearAutoMarkFor(displaced, slots);   // playtest-17 B1: off the bar = auto off
            try { await _net.SetSkillBarAsync(slots); }
            catch (Exception ex) { ClientLog.Warn("SkillBar: " + ex.Message); }
        }

        /// <summary>Swap two bar slots and send the bar ONCE. Two AssignSlot calls would send two
        /// bars, and the second would be built from a copy that never saw the first.</summary>
        public async void SwapSlots(int from, int to)
        {
            if (Phase != ClientPhase.InWorld || SkillBar == null) return;
            if (from < 0 || to < 0 || from >= SkillBar.Length || to >= SkillBar.Length || from == to) return;

            var slots = (string[])SkillBar.Clone();
            var moved = slots[from];
            slots[from] = slots[to];
            slots[to] = moved;
            SkillBar = slots;
            try { await _net.SetSkillBarAsync(slots); }
            catch (Exception ex) { ClientLog.Warn("SkillBar: " + ex.Message); }
        }

        /// <summary>
        /// Ask the server to cast. Deliberately does NOT cancel the walk.
        ///
        /// 🔴 It used to, and that was a guess about a decision only the server makes. Tap a skill with
        /// no target and the server REFUSES it — you keep walking — but the client had already dropped
        /// the destination ring and killed the prediction. The ring vanished while the character walked
        /// on (reported: "when I click the skill the ground move-flag disappears"), and the abandoned
        /// prediction handed the character back to the interpolator mid-walk, which is where the visible
        /// judder came from.
        ///
        /// The walk now ends when the server says a cast STARTED (see the CastReceived handler), which
        /// is the same moment it actually roots you. Refused casts change nothing, because nothing
        /// happened.
        /// </summary>
        public async void UseSkill(string skillId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.UseSkillAsync(skillId, TargetId); }
            catch (Exception ex) { ClientLog.Warn("UseSkill: " + ex.Message); }
        }

        /// <summary>
        /// Drop the destination ring, because the walk it described is over.
        ///
        /// The rule is: the ring means "I am going THERE". Anything that stops the character or sends
        /// them somewhere else makes it a lie — casting (which roots you), attacking (you chase the
        /// target instead), sitting, dying, teleporting. Leaving it on the ground pointing at a place
        /// you are no longer walking to is worse than not having it.
        /// </summary>
        public void CancelMoveOrder()
        {
            if (Marker != null) Marker.Hide();
            // Stop predicting too, or the character keeps walking locally toward a destination the
            // server has already thrown away — which then reads as a rubber-band when it corrects.
            if (Entities != null) Entities.CancelSelfPrediction();
        }

        /// <summary>Equip or unequip — the SERVER decides which, from the item's current state, so the
        /// client can't disagree with it about what is worn.</summary>
        public async void EquipItem(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.EquipItemAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("Equip: " + ex.Message); }
        }

        /// <summary>Drink/read a consumable. 🔴 It now always carries the CURRENT TARGET, and that is the
        /// whole of the playtest-23 resurrection-scroll bug: *"cannot use scroll of resurrection ... but
        /// scroll says 'need a fallen ally as its target'."* The server has had a targeted path since the
        /// scroll shipped (<c>UsePotionOn</c>) — this client only ever called the untargeted one, so a
        /// res scroll validated a target that was never sent and refused itself every time. The cleric's
        /// skill worked because a CAST has always carried its target.
        ///
        /// <para>Sending the target on every consumable is safe and deliberate: the server reads it only
        /// for a skill with <c>Resurrect</c>, and everything else channels on the user regardless. A
        /// null target still reaches the untargeted overload, so drinking with nothing selected is
        /// unchanged.</para></summary>
        public async void UsePotion(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.UsePotionAsync(instanceId, TargetId); }
            catch (Exception ex) { ClientLog.Warn("UsePotion: " + ex.Message); }
        }

        /// <summary>Drop/destroy an item. all=true bins the whole stack, false a single unit — the
        /// server enforces which is legal (a non-stackable ignores the distinction).</summary>
        public async void RemoveItem(Guid instanceId, bool all, int quantity = 0)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.RemoveItemAsync(instanceId, all, quantity); }
            catch (Exception ex) { ClientLog.Warn("RemoveItem: " + ex.Message); }
        }

        /// <summary>Fire-and-forget trade call. Same shape as <see cref="Debug"/>: the trade window
        /// never applies anything itself, it just tells the server and redraws from what comes back.</summary>
        public async void Trade(Func<NetworkChannel, Task> call, string what)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await call(_net); }
            catch (Exception ex) { ClientLog.Warn("Trade " + what + ": " + ex.Message); }
        }

        /// <summary>Fire-and-forget debug call. Every one of these is re-checked server-side against
        /// the account role — <see cref="IsAdmin"/> only decides whether we bother SHOWING the panel.</summary>
        public async void Debug(Func<NetworkChannel, Task> call, string what)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await call(_net); }
            catch (Exception ex) { ClientLog.Warn(what + ": " + ex.Message); }
        }

        public async void Attack(Guid targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            TargetId = targetId;
            // Attacking supersedes the walk order, so the destination ring has to go. You chase the
            // TARGET, not the spot you last tapped, so the ring would never be reached and would sit
            // on the ground pointing at nothing.
            //
            // CancelMoveOrder rather than just hiding the ring: the prediction has to stop for the
            // same reason the ring does. Chasing a target is a walk the SERVER steers — it follows a
            // moving mob — so predicting a straight line to the old tap point would diverge and snap.
            CancelMoveOrder();
            try { await _net.AttackAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Attack: " + ex.Message); }
        }

        /// <summary>True if that player is in YOUR party. Party members can never be attacked
        /// (the server enforces it); the client uses this to offer FOLLOW in place of the swing.</summary>
        public bool IsPartyMember(Guid id)
        {
            for (int i = 0; i < Party.Length; i++)
                if (Party[i].Id == id) return true;
            return false;
        }

        /// <summary>THE attack verb, for every way of asking: the second tap on a target, the Attack
        /// action on the skill bar, the target frame's Attack button. One method because they are one
        /// command and must not drift apart — the owner's rule is "attack button = same logic as the
        /// second tap".
        ///
        /// A party member cannot be fought (the server refuses it outright), so the order becomes the
        /// thing you almost certainly meant: follow them. Everything else routes to Attack and the
        /// SERVER decides — safe zones, the PvP opt-in and flags all live in CanPvpHit, which answers a
        /// refused swing with a system message. The client checks party membership only to pick which
        /// command to send, never as the rule.</summary>
        public void AttackOrFollow(Guid targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            if (IsPartyMember(targetId)) Follow(targetId);
            else Attack(targetId);
        }

        public async void SetMoveState(MoveState state)
        {
            if (state == MoveState.Sitting) CancelMoveOrder();   // sitting cancels the walk
            try { await _net.SetMoveStateAsync(state); }
            catch (Exception ex) { ClientLog.Warn("MoveState: " + ex.Message); }
        }

        public async void Respawn()
        {
            try { await _net.RespawnAsync(); }
            catch (Exception ex) { ClientLog.Warn("Respawn: " + ex.Message); }
        }

        /// <summary>The one entry point for the chat/command box. Mirrors the WPF client's routing so
        /// slash commands actually DO something instead of being broadcast as chat text:
        ///   !text            → world chat
        ///   /w Name message  → whisper
        ///   /fadd|/frem Name, /flist → friends (any player)
        ///   /anything else   → admin command (only if this character has a staff role; the server
        ///                      re-checks). A non-admin slash is reported locally, not sent as chat.
        ///   plain text       → local chat</summary>
        public async void Say(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            raw = raw.Trim();
            if (!TrySubstituteTargetToken(raw, out raw)) return;
            try
            {
                if (raw.StartsWith("!"))
                {
                    var body = raw.Substring(1).Trim();
                    if (body.Length > 0) await _net.ChatAsync(body, ChatChannel.World);
                    return;
                }

                if (raw.StartsWith("/w ", StringComparison.OrdinalIgnoreCase))
                {
                    var rest = raw.Substring(3).Trim();
                    int sp = rest.IndexOf(' ');
                    if (sp <= 0) { ClientLog.Warn("Usage: /w <name> <message>"); return; }
                    await _net.ChatAsync(rest.Substring(sp + 1).Trim(), ChatChannel.Whisper, rest.Substring(0, sp));
                    return;
                }

                if (raw.StartsWith("/fadd ", StringComparison.OrdinalIgnoreCase))
                { await _net.FriendCommandAsync("add", raw.Substring(6).Trim()); return; }
                if (raw.StartsWith("/frem ", StringComparison.OrdinalIgnoreCase))
                { await _net.FriendCommandAsync("remove", raw.Substring(6).Trim()); return; }
                if (raw.Equals("/flist", StringComparison.OrdinalIgnoreCase))
                { await _net.FriendCommandAsync("list", ""); return; }

                // Social filters (owner, playtest-19 M2). `/block <name>` is the per-person list that
                // already existed; the rest are blanket TOGGLES — the same word turns each back off, so
                // there is no second command to remember. ⚠ None of them can silence staff (server-side).
                // These belong in an Options window, which is still to build (B11) — the commands are
                // the interim, and will stay as the typed twins of those switches.
                if (raw.StartsWith("/block ", StringComparison.OrdinalIgnoreCase))
                { await _net.BlockCommandAsync("block", raw.Substring(7).Trim()); return; }
                if (raw.StartsWith("/unblock ", StringComparison.OrdinalIgnoreCase))
                { await _net.BlockCommandAsync("unblock", raw.Substring(9).Trim()); return; }
                if (raw.Equals("/blist", StringComparison.OrdinalIgnoreCase))
                { await _net.BlockCommandAsync("list", ""); return; }
                if (raw.Equals("/block", StringComparison.OrdinalIgnoreCase))
                { await _net.BlockCommandAsync("all", ""); return; }
                if (raw.Equals("/block-w", StringComparison.OrdinalIgnoreCase))
                { await _net.BlockCommandAsync("whispers", ""); return; }
                if (raw.Equals("/block-g", StringComparison.OrdinalIgnoreCase))
                { await _net.BlockCommandAsync("global", ""); return; }
                if (raw.Equals("/decline-t", StringComparison.OrdinalIgnoreCase))
                { await _net.BlockCommandAsync("trades", ""); return; }
                if (raw.Equals("/decline-p", StringComparison.OrdinalIgnoreCase))
                { await _net.BlockCommandAsync("party", ""); return; }

                // The typed twin of the Menu's [Offline] button. It has to be here rather than in the
                // admin passthrough below: offline farming is a PLAYER command, and everything that
                // reaches the admin branch is refused for a non-staff character.
                if (raw.Equals("/offline", StringComparison.OrdinalIgnoreCase))
                { StartOfflineFarm(); return; }

                // Write your own title, if you have been granted the right. Player commands, so they
                // must sit ABOVE the admin passthrough or a non-staff character would be told
                // "unknown command" for the one thing the grant exists to let them do.
                // ⚠ Matched on "exact, or followed by a space" rather than a bare StartsWith: the ADMIN
                // command is `/titleright`, and a prefix match would have swallowed it and tried to
                // wear "right Bob on" as a title.
                if (IsCommand(raw, "/titlecolor"))
                { SetTitleColor(raw.Substring("/titlecolor".Length).Trim()); return; }
                if (IsCommand(raw, "/title"))
                { SetCustomTitle(raw.Substring("/title".Length).Trim()); return; }

                // `/target <name>` — select by NAME instead of by tapping (owner): in a crowd around the
                // gatekeeper the NPC you want is behind three other players' plates and simply cannot be
                // hit with a finger. Client-side, because targeting IS client-side — the server is only
                // ever told a target id when you act on it.
                if (raw.StartsWith("/target ", StringComparison.OrdinalIgnoreCase))
                { TargetByName(raw.Substring(8).Trim()); return; }

                // Party target-commands (leader-only ones are re-checked server-side). Every one has an
                // action button too (target frame / party window); these are the typed equivalents.
                if (raw.Equals("/ptleave", StringComparison.OrdinalIgnoreCase))
                { PartyLeave(); return; }
                if (raw.StartsWith("/ptinv ", StringComparison.OrdinalIgnoreCase))
                {
                    // Straight to the server — see PartyInviteByName. Resolving the name here limited
                    // the invite to whoever happened to be on screen (46d).
                    PartyInviteByName(raw.Substring(7).Trim());
                    return;
                }
                if (raw.StartsWith("/ptkick ", StringComparison.OrdinalIgnoreCase))
                {
                    var id = PartyMemberId(raw.Substring(8).Trim());
                    if (id is Guid g) PartyKick(g); else ClientLog.Warn("Not a party member.");
                    return;
                }
                if (raw.StartsWith("/ptcl ", StringComparison.OrdinalIgnoreCase))
                {
                    var id = PartyMemberId(raw.Substring(6).Trim());
                    if (id is Guid g) PartyChangeLeader(g); else ClientLog.Warn("Not a party member.");
                    return;
                }

                // ADMIN `/enchant <value>` (D2). Handled here rather than passed through to the server
                // like the other staff commands because it needs a PICKER, and the picker is a client
                // window over the bag the client already holds — the same reason /ptinv and /offline
                // live here. The server still re-checks the staff role on AdminEnchantCmd.
                if (raw.StartsWith("/enchant", StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsAdmin) { ClientLog.Warn("Unknown command: " + raw); return; }
                    var argText = raw.Substring("/enchant".Length).Trim();
                    if (!int.TryParse(argText, out int value) || value < 0)
                    { ClientLog.Warn("Usage: /enchant <value>  (e.g. /enchant 16)"); return; }
                    Ui?.BeginAdminEnchant(value);   // Say() runs on the UI thread, like /offline
                    return;
                }

                if (raw.StartsWith("/"))
                {
                    var body = raw.Substring(1).Trim();
                    int sp = body.IndexOf(' ');
                    string cmd = sp < 0 ? body : body.Substring(0, sp);
                    string arg = sp < 0 ? "" : body.Substring(sp + 1).Trim();
                    // Almost every slash command is staff-only, and the client refuses the rest rather
                    // than letting them travel — but a few are for EVERYONE, and the server is the one
                    // that decides how far each goes. `/where` with no argument is the first (owner,
                    // playtest 27: *"/where should work for anyone - they can see their own map
                    // coordinates -> to tell friends where to find them -> while /where player-name
                    // should work only for admins+"*), so the ARGUMENT form still lands on the staff
                    // gate server-side and is refused there.
                    bool playerAllowed = cmd.Equals("where", StringComparison.OrdinalIgnoreCase)
                                         && arg.Length == 0;
                    if (!IsAdmin && !playerAllowed) { ClientLog.Warn("Unknown command: " + raw); return; }
                    await _net.AdminCommandAsync(cmd, arg);
                    return;
                }

                await _net.ChatAsync(raw, ChatChannel.Local);
            }
            catch (Exception ex) { ClientLog.Warn("Chat: " + ex.Message); }
        }

        /// <summary>
        /// Route one incoming chat message to its tab, with its channel's colour.
        ///
        /// Every line goes to exactly ONE tab; "All" is a filter that accepts them all rather than a
        /// second copy of each line. That is why the channel tag ("[W]", "[PM]") is baked into the text
        /// here — in All the tabs cannot tell you where a line came from, and without the tag world
        /// chat and local chat look identical.
        ///
        /// Colours are the WPF harness's, which the owner played for months: world gold, whisper
        /// violet, local white, system green.
        /// </summary>
        private void AppendChat(ChatMessage m)
        {
            if (m == null) return;
            switch (m.Channel)
            {
                case ChatChannel.World:
                    ClientLog.Chat("[W] " + m.From + ": " + m.Text,
                                   new Color(1f, 0.84f, 0.35f), ClientLog.Tab.World);
                    break;
                case ChatChannel.Whisper:
                    // Both directions land here — the server echoes your own whisper back — so the line
                    // says who spoke to whom rather than assuming it was sent TO you.
                    RememberWhisper(string.Equals(m.From, CharacterName, StringComparison.OrdinalIgnoreCase)
                                    ? (m.To ?? "") : m.From);
                    ClientLog.Chat("[PM] " + m.From + " -> " + (m.To ?? "?") + ": " + m.Text,
                                   new Color(0.85f, 0.6f, 1f), ClientLog.Tab.Whisper);
                    break;
                case ChatChannel.System:
                    // Through Good() rather than Chat(): a server system line is also a diagnostic
                    // ("you can't do that here", a refusal, a ban notice), and logcat should keep it.
                    ClientLog.Good(m.From + ": " + m.Text);
                    break;
                case ChatChannel.Combat:
                    // D5. From is a colour TAG here, not a speaker, so it is not printed: loot gold,
                    // the kill's Exp/SP/Gold line a calmer blue. Not through Good() — this feed is
                    // several lines per kill and mirroring it into logcat is pure noise.
                    ClientLog.Chat(m.Text, m.From == "LOOT" ? LootColour : RewardColour,
                                   ClientLog.Tab.Combat);
                    break;
                default:
                    ClientLog.Chat(m.From + ": " + m.Text,
                                   new Color(0.92f, 0.94f, 0.96f), ClientLog.Tab.Local);
                    break;
            }
        }

        /// <summary>Put "/w &lt;name&gt; " in the command box and open the keyboard on it. Used by the
        /// Whisper action and by the chat window's Reply button.</summary>
        public void ComposeWhisper(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { ClientLog.Warn("No one to whisper."); return; }
            if (Ui != null) Ui.ComposeCommand("/w " + name.Trim() + " ");
        }

        /// <summary>The last person a whisper passed between you and — what the Whisper tab's "Reply"
        /// fills in. One name, not a list: on a phone the case that matters is answering the message
        /// you are looking at.</summary>
        public string LastWhisperName { get; private set; } = "";

        private void RememberWhisper(string name)
        {
            if (!string.IsNullOrWhiteSpace(name)) LastWhisperName = name;
        }

        // ----- Helpers -------------------------------------------------------------------------

        private void Fail(string message)
        {
            LastError = message;
            StatusMessage = message;
            ClientLog.Error(message);
            if (Phase == ClientPhase.Connecting || Phase == ClientPhase.Authenticating)
                Phase = IsConnected ? ClientPhase.Authenticating : ClientPhase.Offline;
            else if (Phase == ClientPhase.Entering)
                Phase = ClientPhase.CharacterSelect;
        }

        /// <summary>Turn transport exceptions into something a human staring at a phone can act on.
        /// "No connection could be made" on a device almost always means the URL points at the
        /// phone's own localhost without an `adb reverse`, or at a PC firewall.</summary>
        private string Describe(Exception ex)
        {
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException;
            string msg = inner.Message;

            if (msg.Contains("refused") || msg.Contains("No connection") || msg.Contains("unreachable"))
                msg += "  — is the server running, and is the URL reachable FROM THE PHONE? "
                     + "(cable: adb reverse tcp:5238 tcp:5238 then use 127.0.0.1; Wi-Fi: use the PC's LAN IP)";
            return msg;
        }

        private static void Main(Action a) => UnityMainThreadDispatcher.Instance.Enqueue(a);

        /// <summary>Flush the chat to disk when Android backgrounds us.
        ///
        /// ⚠ This is the case that actually matters on a phone: the OS kills a backgrounded app without
        /// running OnDestroy, so waiting for a tidy exit would mean the chat only survived the relogs
        /// you did on purpose — not the ones the phone did for you, which is most of them.</summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused && Phase == ClientPhase.InWorld && _lastCharacterId > 0)
                ClientLog.SaveChat(_lastCharacterId.ToString());
        }

        private async void OnDestroy()
        {
            if (Phase == ClientPhase.InWorld && _lastCharacterId > 0)
                ClientLog.SaveChat(_lastCharacterId.ToString());
            if (_net != null) await _net.DisposeAsync();
        }
    }
}
