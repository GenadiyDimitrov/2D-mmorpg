# Playtest-14 — owner's report (2026-07-30, after 0.33.1)

Verbatim, as given. Reached level ~25 with **3kk gold purely from selling trash** — the headline
finding: the economy is a faucet with no drain, driven by drop rate × sell price.

---

## Items (the economy / drop batch)

1. **Training weapons still have no M.Atk** — and audit for any other item in the same state.

2. **Lower the SELL price of weapons/armor.** At level 25, 3kk gold from selling trash alone
   (Common at ~20 % drop rate × ~20k sell price). Lower it **at least 3×**.

3. **Lower the drop chances.** Now roughly 20 / 12 / 5. Target:
   - **Normal monsters** — C 5 %, U 2 %, R 0.2 %, E 0.01 % (below level 74 also drop a recipe at 0.1 %)
   - **Elite / dungeon / instance** — U 10 %, R 2 %, E 0.2 %, recipe 0.1 %
   - **Boss** — E 70 %, L 40 %, M 2 %, armor recipe 50 %, weapon recipe 40 %, jewel 60 %

4. **Drops must be GROUPED and grade-locked.**
   - **Grade lock:** a mob drops only ITS OWN grade. A level-40 mob drops D-grade recipe / armor /
     weapon — never E or C.
   - **Groups:** armor · accessories · weapons · jewels · crafting mats · recipes ·
     scrolls+buff-pots · always · gold.
   - Without groups you can get 20 light armors off one lucky kill.
   - Each group has a **trigger chance**; on a trigger it rolls a **rarity**; on a rarity hit it
     **randomises which item of that slot family** at the mob's grade+rarity.
   - Percentages below are examples for a NORMAL mob (the group chance and the inner rarity chance
     multiply out to the target in §3):

   | Group | Trigger | Inner rarity roll | Randomise among |
   |---|---|---|---|
   | Armor | 50 % | C 10 · U 4 · R 0.4 · E 0.02 | Light / Heavy / Robe |
   | Accessories | 50 % | C 10 · U 4 · R 0.4 · E 0.02 | Helmet / Boots / Gloves / Shield |
   | Weapons | 33 % | C 15 · U 6 · R 0.6 · E 0.03 | blade / fangs / longbow / wand / … |
   | Jewels | 100 % | C 5 · U 2 · R 0.2 · E 0.01 | Ring / Earring / Necklace |
   | Mats | 100 % | wood/iron/… — 50 % → 1, 40 % → 2, 9 % → 4, 1 % → 10 | **rarity = the AMOUNT** |
   | Scrolls | 100 % | C 40 · U 20 · R 10 | a buff potion (not healing) or a scroll of the grade |
   | Always | 100 % | <75: C 70 · U 30 · 75+: C 55 · U 40 · R 5 | health potion / escape / resurrect; 75+ adds rare pot + Ultimate escape/res |
   | Gold | always rolls (70 % or whatever it is now) | — | **its own group** — inside "Always" it would compete and you could never get both |

   > Mats note (owner): either roll the material and let rarity BE the amount, or roll the material
   > then roll the amount separately — whichever the current code makes easier.

## General

1. **Make `/givegold` and every other admin command work on the phone.**
2. **Visual for skill cooldowns.**
3. **Passives still not re-worded** — they show a brief description, not the actual stats.
4. **Add the exp / SP / gold chat row.**
5. **Increase the mob spawn limit** — more mobs, at least the ones quests need.
   - Give Werewolves / Ashen Wolves / Ork Archers / Grunts (etc.) **their own spawner** on top of
     the one they currently spawn at, and **remove them from that one**.
   - Then killing a Werewolf respawns a Werewolf in 30 s — not a Skeleton. Today you kill 50 mobs
     just to make the quest target reappear.
   - Once that holds, quest requirements can go up (15 archers, not 5) — **later**, with the quest
     rework, not now.

## Not working

1. **The Abandon button does nothing** except show its confirmation.
2. **Char-select is still briefly stale** — level and class update only after a delay.
