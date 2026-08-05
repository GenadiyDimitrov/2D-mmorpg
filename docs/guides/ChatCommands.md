# Chat commands — the complete list (0.47.0)

Everything you can type into the command bar. Read out of the code, not from memory: the player side is
parsed in `GameBoot.Say`, the staff side in `GameLoopService.HandleAdmin`.

Two rules that apply to everything below:

- **Names are case-insensitive** and, where a command can reach someone offline, resolved to the
  character's canonical spelling — `/fadd elder` finds `Elderin`.
- **A slash you are not allowed to use is refused locally**, not sent as chat. Type `/tp Bob` as an
  ordinary player and you get `Unknown command: /tp` — you never accidentally shout it in local chat.

---

## For every player

There is no `/say`, `/local` or `/exit` — plain text **is** local chat, and you leave the world through
character select, not a command. The rest:

| What you type | What it does | Example |
|---|---|---|
| `<anything>` | **Local chat.** White, Local tab. Heard by players near you. | `where is the smith?` |
| `!<message>` | **World chat.** Gold, tagged `[W]`, World tab — everyone online sees it. ⚠ Requires **level 10**; staff are exempt. | `!selling Feretite Blade 200k` |
| `/w <name> <message>` | **Whisper** one player. Violet, tagged `[PM]`, PM tab. Both of you see the line. They must be **online**, and if they have blocked you it is refused. | `/w Aricel meet me at the gate` |
| `/fadd <name>` | **Add a friend.** Works on an offline character. Friendship is MUTUAL: until they add you back it reads `[pending]` and you get no presence info about them — and they are *not* told you added them. | `/fadd Aricel` |
| `/frem <name>` | **Remove a friend.** | `/frem Aricel` |
| `/flist` | **List your friends**, each tagged `[online]`, `[offline]` or `[pending]`. | `/flist` |
| `/offline` | **Start offline farming.** Puts you into offline farm and drops you to character select; that character's row shows the time it has left. Same as Menu → Offline. | `/offline` |
| `/ptinv <name>` | **Invite to your party.** The player must be **nearby** (they have to be on your screen for the client to find them). | `/ptinv Aricel` |
| `/ptleave` | **Leave your party.** | `/ptleave` |
| `/ptkick <name>` | **Kick a party member.** Leader only — the server re-checks. | `/ptkick Aricel` |
| `/ptcl <name>` | **Change leader** to that party member. Leader only. | `/ptcl Aricel` |

Every one of the party commands also exists as a button (target frame / party window); the typed forms
are there for when the buttons are awkward on a phone.

> ⚠ **`/block`, `/unblock`, `/blocklist` and `/like` do NOT exist as typed commands** — this is
> playtest-17 `B11` and it is still open. The block list and the charisma "like" are real and work, but
> only from the **Actions** tab and the target frame. Typing `/block Bob` today just tells you it is an
> unknown command. Blocking is one-sided and silent: it filters what *you* receive, the other person is
> never told, and it does not stop you messaging them.

---

## For staff

Roles are **per character**, not per account, so an admin account can still have ordinary characters.
Authorization is re-checked on the server for every command — these ship in release builds, so nothing
here relies on a debug flag.

**One rule governs every moderation action: you can only act on someone ranked BELOW you.** An admin can
jail a moderator or a player but not another admin; a moderator can only act on players; and because your
own role is never below itself, **nobody can jail, kick or ban themselves** (`/jail admin` used to lock
the owner in their own cell).

### Moderators — behaviour policing only

A moderator is a **trusted player, not a GM**. The allow-list is exactly eight commands; everything else
answers `Moderators can't use /<command>.` **No god mode, no teleporting, no items, no gold.**

| What you type | What it does | Example |
|---|---|---|
| `/help` | Prints the moderator list (admins get their own, longer one). | `/help` |
| `/jail <name> [minutes]` | **Jail** a character — moved to the cell, silenced, can't leave. Default **30** minutes. Works on an offline character; it persists and is enforced when they log in. | `/jail Griefer 60` |
| `/unjail <name>` | Release them early. | `/unjail Griefer` |
| `/jailed` | List everyone currently jailed, with time left and a ready-made `/unjail` for each. | `/jailed` |
| `/kick <name> [minutes]` | **Kick** — disconnect and keep them out for a bit. Default **10** minutes. | `/kick Griefer` |
| `/chatban <name> [minutes]` | **Silence** without the cell: no chat, no whispers. Default **30** minutes. | `/chatban Spammer 120` |
| `/unchatban <name>` | Let them speak again. | `/unchatban Spammer` |
| `/where <name>` | That player's coordinates. Online only. | `/where Griefer` |

### Admins — everything above, plus

| What you type | What it does | Example |
|---|---|---|
| `/ban <name> [minutes]` | **Account ban** — no login at all until it expires. Default **60** minutes. Per ACCOUNT, not per character, and offline-safe. | `/ban Cheater 1440` |
| `/unban <name>` | Lift the account ban. | `/unban Cheater` |
| `/role <name> <player\|moderator\|admin>` | **Grant or revoke a staff role** on a character. Works offline. Dropping someone below admin also turns their god mode off. `mod` and `none` are accepted spellings. | `/role Aricel moderator` |
| `/god` | Toggle **god mode** — you take no damage. Shows a permanent on-screen indicator, not just one chat line. | `/god` |
| `/tp <name>` | **Teleport yourself** to an online player (lands a few metres off, cancels your move order). | `/tp Aricel` |
| `/tpme <name>` | The reverse — **summon** an online player to you. They are told who did it. | `/tpme Aricel` |
| `/speed-cast <value>` | Force your **cast speed** stat. | `/speed-cast 1500` |
| `/speed-attack <value>` | Force your **attack speed** stat. `/speed-atack` also works (the misspelling is deliberate — it was in the original list). | `/speed-attack 1200` |
| `/speed-move <value>` | Force your **move speed**. | `/speed-move 250` |
| `/speed-reset` | Drop all three overrides back to your real stats. | `/speed-reset` |
| `/bag <name>` | Open an **online player's inventory** — read-only-ish, with a remove button per row. | `/bag Aricel` |
| `/give <name>` | Open **your own** bag as a picker to hand something to that online player. Ignores tradability on purpose: staff can hand over anything, quest items included. | `/give Aricel` |
| `/enchant <value>` | Open **your own** bag as a picker and set the chosen weapon/armor/jewel to that exact enchant. Unrestricted on purpose — no grade band, no scroll, no success roll and no +16 ceiling, so it reaches states the scroll ladder cannot. `/enchant 999999` on an F weapon works. | `/enchant 16` |
| `/givegold <name> <amount>` | Give or **take** gold. `k`/`m`/`b`/`t` suffixes and `1_000_000` both parse; a **negative** amount takes it away and clamps at zero, so you don't need the exact figure to empty someone. | `/givegold Aricel 5m` |
| `/farmcap <player> <autoHours> <offlineHours>` | The **premium knob**, per account: the daily auto-hunt and offline-farm allowances. `-1` = server default (free is 8/2), `0` = unlimited. | `/farmcap Aricel 12 4` |
| `/testcaps [off]` | Debug: shrink the farm caps to **30s idle / 20s offline / 15s grace** so you can test them in a minute instead of eight hours. `off` restores 8h/2h/180s. Both refill the allowances. | `/testcaps` |
| `/droprate` | With no argument, **print** the current rates: the global multiplier, every group, and any per-item overrides. | `/droprate` |
| `/droprate <group> <mult>` | Set one **group's** multiplier. Groups: `armor`, `accessory`, `weapon`, `jewel`, `mats`, `scrolls`, `always`, `other`. | `/droprate weapon 0.05` |
| `/droprate gear <mult>` | Shorthand — sets **all four gear groups** (armor, accessory, weapon, jewel) at once. | `/droprate gear 0.025` |
| `/droprate global <mult>` | The **global** multiplier. ⚠ `mats`, `scrolls` and `always` are **exempt** from it — those are authored as absolutes. | `/droprate global 3` |
| `/droprate item <id or name> <mult>` | Tune a **single item**, on top of its group's rate. Accepts the display name; a wrong one suggests near matches. `1` clears the override. | `/droprate item Scroll of Resurrect 5` |

`/droprate` is a chat command rather than a row in the tuning panel on purpose: the panel's payload is a
wire DTO, and adding eight fields to it would bump the protocol and need a matching Unity build — for a
knob whose entire value is being adjustable mid-playtest, on the phone, with nothing rebuilt.

---

## Where the rest of the commands went

Most things that *could* be a command are **actions** instead — Skills → Actions, and they can be
dragged onto the skill bar: attack, target-closest, sit/stand, walk/run, trade, party invite/leave/
kick/leader, follow, assist, friend add/remove/list, like, block/unblock, whisper. The Whisper action
fills the command box with `/w <name> ` and hands you the caret, because that is the one that genuinely
needs typed text.
