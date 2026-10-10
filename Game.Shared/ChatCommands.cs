using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>One typed command, as `/help` lists it.</summary>
/// <param name="Name">The word after the slash, lower case — what the server's command switch matches on.</param>
/// <param name="Usage">The whole line a player types, arguments included (<c>/kick &lt;name&gt; [min]</c>).</param>
/// <param name="What">What it does, one short clause.</param>
/// <param name="MinRole">The lowest rank that may use it. Every higher rank has it too.</param>
/// <param name="Detail">Extra lines only `/help &lt;command&gt;` prints — the examples and fine print the full list
/// has no room for.</param>
public record ChatCommandDef(string Name, string Usage, string What, AccountRole MinRole, params string[] Detail);

/// <summary>
/// EVERY typed command, by the lowest rank that may use it — the one list `/help` prints and the staff allow-lists
/// are read from (0.221.0, owner 2026-10-02: *"Depending on the Game mode of the char the different commands are
/// shown … each lower group is available on the upper one"*).
///
/// 🔑 A command with an ACTION BUTTON is not here: *"no action commands like `/like` they are as action buttons - no
/// need to flood the chat with them"*. Its typed twin is on the action itself (<see cref="ActionDef.Command"/>), which
/// the Actions tab shows. A name-form twin (<c>/block &lt;name&gt;</c>) belongs to its action; the bare toggle
/// (<c>/block</c>) is a different command and is listed.
///
/// ⚠ Adding a STAFF command: a row here, or a Moderator / Chat Moderator cannot use it (the allow-list is this list)
/// and nobody finds it in `/help`. Admin and Owner pass every command regardless.
/// </summary>
public static class ChatCommandCatalog
{
    public static readonly ChatCommandDef[] All =
    {
        // ---- everyone ----
        new("help", "/help [command]", "this list, or everything about one command (/help stat)", AccountRole.Player),
        new("!", "!<text>", "say it in World chat", AccountRole.Player),
        new("where", "/where", "your own coordinates, to tell friends where you are", AccountRole.Player),
        new("return", "/return", "cast Return: back to town", AccountRole.Player),
        new("offline", "/offline", "start offline farming", AccountRole.Player),
        new("target", "/target <name>", "select a player, NPC or mob by name", AccountRole.Player),
        new("title", "/title <text>", "write your own title (needs the right)", AccountRole.Player),
        new("titlecolor", "/titlecolor <colour>", "colour your title (needs the right)", AccountRole.Player),
        new("unstuck", "/unstuck <name>", "send another character of YOUR account to town (you must be in town)", AccountRole.Player),
        new("buff", "/buff", "buff yourself, where the server allows it", AccountRole.Player),
        new("mentor", "/mentor invite|accept|decline <name>", "make a mentor bond (a 76+ 4th class mentors a player under 76)", AccountRole.Player),
        new("mentor", "/mentor remove [name] · /mentor list", "end a bond (may cost a penalty) · your mentor or mentees", AccountRole.Player),
        new("blist", "/blist", "the players you block", AccountRole.Player),
        new("block", "/block", "on/off: hide all player chat", AccountRole.Player),
        new("block-w", "/block-w", "on/off: hide whispers", AccountRole.Player),
        new("block-g", "/block-g", "on/off: hide World chat", AccountRole.Player),
        new("decline-t", "/decline-t", "on/off: refuse trade requests", AccountRole.Player),
        new("decline-p", "/decline-p", "on/off: refuse party invitations", AccountRole.Player),

        // ---- Chat Moderator ----
        new("chatban", "/chatban <name> [min]", "mute a player", AccountRole.ChatModerator),
        new("unchatban", "/unchatban <name>", "lift a mute", AccountRole.ChatModerator),
        new("chatlog", "/chatlog [name] [-p <n>]", "what was said in the public channels", AccountRole.ChatModerator),

        // ---- Moderator ----
        new("jail", "/jail <name> [min]", "send a player to jail", AccountRole.Moderator),
        new("unjail", "/unjail <name>", "release a player from jail", AccountRole.Moderator),
        new("jailed", "/jailed", "who is in jail, and for how long", AccountRole.Moderator),
        new("kick", "/kick <name> [min]", "disconnect a player, locked out for [min] (10)", AccountRole.Moderator),
        new("where", "/where <name>", "where a player is", AccountRole.Moderator),
        new("chatlog", "/chatlog [name] [-w] [around <15m|2h|1d|HH:mm>] [-p <n>]", "-w adds whispers; around jumps to a time", AccountRole.Moderator),

        // ---- Admin ----
        new("role", "/role <name> <player|chatmod|moderator>", "change a character's rank", AccountRole.Admin),
        new("ban", "/ban <name> [min]", "ban the whole account (60)", AccountRole.Admin),
        new("unban", "/unban <name>", "lift an account ban", AccountRole.Admin),
        new("tp", "/tp <name> | <x> <y> | ~<dx> ~<dy>", "teleport yourself", AccountRole.Admin),
        new("tpme", "/tpme <name>", "summon a player to you", AccountRole.Admin),
        new("god", "/god", "on/off: take no damage (survives a relog)", AccountRole.Admin),
        new("invis", "/invis", "on/off: unseen (survives a relog)", AccountRole.Admin),
        new("heal", "/heal [name]", "HP and MP to full, in combat too", AccountRole.Admin),
        new("buff", "/buff [name] [skill] [duration] [level]", "any buff on anyone", AccountRole.Admin,
            "No skill = the full buff set, each at its top rung.",
            "duration: 30s, 15m, 2h, 30d (up to 365d); none = 1 hour.",
            "level: a bare number at the end; none = the top rung.",
            "e.g. /buff 30d  ·  /buff @t might 2h  ·  /buff harmony_mark 1h 2"),
        new("duration", "/duration [name] <buffs|potions|all|skill> <time>", "set how long the buffs already on someone last", AccountRole.Admin,
            "buffs = skill buffs; potions = potions, scrolls and runes; all = both; or one buff by name.",
            "time: 30s, 15m, 2h, 30d (up to 365d) sets it; +2h adds to what is left.",
            "Debuffs and toggles are never touched. A relog keeps the new time.",
            "e.g. /duration buffs 30d  ·  /duration potions 30d  ·  /duration @t all +1h"),
        new("clearbuffs", "/clearbuffs [name]", "every buff off (debuffs stay)", AccountRole.Admin),
        new("stat", "/stat <key> <value>", "force one of your stats; /stat alone clears all, a wrong key lists them", AccountRole.Admin,
            "Uncapped, and applied after gear, passives and buffs — the number you type is the number used.",
            "hpreg/mpreg force the finished per-second regen; 0 stops regen.",
            "e.g. /stat mpreg 500  ·  /stat crate 500  ·  /stat"),
        new("spd", "/spd <m|a|c> <value>", "force move / attack / cast speed; /spd alone resets", AccountRole.Admin,
            "m = move, a = attack, c = cast (333 = 1.0x). Uncapped. e.g. /spd m 250"),
        new("lvl", "/lvl [name] <level|max>", "set a level", AccountRole.Admin, "No name = you."),
        new("exp", "/exp [name] <amount|max>", "give EXP", AccountRole.Admin,
            "No name = you. k/m/b/t suffixes and 1_000_000 both work. e.g. /exp @t 100k"),
        new("sp", "/sp [name] <amount|max>", "give SP", AccountRole.Admin,
            "No name = you. k/m/b/t suffixes and 1_000_000 both work. e.g. /sp max"),
        new("bag", "/bag <name>", "open a player's bag (remove items from it)", AccountRole.Admin),
        new("give", "/give <name> [itemId] [sellPrice] [tradable] [timed]", "give an item; just a name opens the picker", AccountRole.Admin,
            "Full form: /give <player> <itemId> [sellPrice] [tradable] [timed] [\"name\"] [enchant] [canStorePrivate] [canStoreAccount] [amount]",
            "'-' leaves a slot alone. e.g. /give Gena mat_iron - - - - - - - 1000"),
        new("givegold", "/givegold <name> <amount>", "give gold (k/m/b work)", AccountRole.Admin,
            "k/m/b/t suffixes and 1_000_000 both work; a negative amount takes gold away."),
        new("giveplat", "/giveplat <name> <amount>", "give platinum (k/m/b work)", AccountRole.Admin,
            "k/m/b/t suffixes and 1_000_000 both work; a negative amount takes it away. It is ACCOUNT-wide."),
        new("enchant", "/enchant <value>", "pick an item in your bag and set its enchant", AccountRole.Admin),
        new("like", "/like <name> -f <value>", "force a player's current charisma", AccountRole.Admin),
        new("titleright", "/titleright <name> <on|off>", "grant or take the right to /title", AccountRole.Admin),
        new("whatdrops", "/whatdrops <item or mob>", "who drops it, or what it drops", AccountRole.Admin),
        new("who", "/who <name>", "open an online player's character sheet", AccountRole.Admin),
        new("dropindex", "/dropindex", "the drop index's size and build time", AccountRole.Admin),
        new("droprate", "/droprate [group|gear|global|item <id>] [multiplier]", "drop-rate knobs; bare shows them", AccountRole.Admin),
        new("resetlimits", "/resetlimits [name]", "today's dailies, farm allowance, likes, Favor potion", AccountRole.Admin),
        new("farmcap", "/farmcap <name> <autoHours> <offlineHours>", "an account's farm allowance (-1 default, 0 unlimited)", AccountRole.Admin),
        new("testcaps", "/testcaps [off]", "short farm caps for testing", AccountRole.Admin),
        new("server", "/server <shutdown|reboot|on> [min] [adminOnly]", "shut down or reboot on a countdown; on cancels", AccountRole.Admin,
            "minutes: '-', '0' or blank = now; unparseable = 30."),

        // ---- Owner ----
        new("role", "/role <name> admin", "make an Admin (only the Owner may)", AccountRole.Owner),
        new("copy", "/copy <source> <target>", "make <target> an exact copy of <source> (it keeps its name and rank; it must be offline)", AccountRole.Owner),
    };

    /// <summary>The commands a STAFF rank below Admin may send to the server's staff switch: its own rank's rows and
    /// every staff rank below it, plus <c>help</c>. Player rows are left out on purpose — they are handled before the
    /// staff gate, and a Player-level name like <c>buff</c> must not let a Moderator into the ADMIN half of it.
    /// Null for Admin and Owner, who may use everything.</summary>
    public static HashSet<string>? StaffAllowList(AccountRole role)
    {
        if (role >= AccountRole.Admin) return null;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "help" };
        foreach (var c in All)
            if (c.MinRole != AccountRole.Player && c.MinRole <= role) names.Add(c.Name);
        return names;
    }

    /// <summary>Every row named <paramref name="name"/> (no slash, any case) that <paramref name="role"/> may use — what
    /// `/help &lt;command&gt;` prints. Several rows can share a name (`/where`, `/mentor`, `/chatlog`), one per rank or form.</summary>
    public static IReadOnlyList<ChatCommandDef> For(string name, AccountRole role) =>
        All.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.MinRole <= role
                       && !(c.Name == "buff" && c.MinRole == AccountRole.Player && !RateConfig.FreeBuffs))
           .ToList();

    private static readonly (AccountRole Role, string Header)[] Sections =
    {
        (AccountRole.Owner, "--- Owner only ---"),
        (AccountRole.Admin, "--- Admin only ---"),
        (AccountRole.Moderator, "--- Moderator ---"),
        (AccountRole.ChatModerator, "--- Chat Moderator ---"),
        (AccountRole.Player, "--- Commands ---"),
    };

    /// <summary>What `/help` prints for <paramref name="role"/>, one line each: the target-token note, then every
    /// section from the caller's own rank down to the plain commands.</summary>
    public static IReadOnlyList<string> HelpLines(AccountRole role)
    {
        var lines = new List<string> { "<name> can be swapped for @s (self) or @t (your target)." };
        foreach (var (sectionRole, header) in Sections)
        {
            if (sectionRole > role) continue;
            lines.Add(header);
            foreach (var c in All.Where(c => c.MinRole == sectionRole))
            {
                // The player `/buff` works only while the server hands out free buffs (`BL-126`), so `/help`
                // offers it only then (his 128a: *"remove the /buff command … or check if server allows it"*).
                if (c.Name == "buff" && c.MinRole == AccountRole.Player && !RateConfig.FreeBuffs) continue;
                lines.Add($"{c.Usage}  —  {c.What}");
            }
        }
        lines.Add("Party, friends, whisper, like, block a player: on the Actions tab (each shows its typed command).");
        return lines;
    }
}
