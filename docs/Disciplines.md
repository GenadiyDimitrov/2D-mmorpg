
   * physical skills can do double dmg - same logic as magic crit chance just dependent on DEX/ATK (whichever is higher is used) - maximum 30% to do x2 dmg -> not all can crit -> [Double] added behind skills that can double
   * magic skills crit - the max 20% magic critical chance x3 dmg
   * there should be difference between physical debuff - ATK vs CON, and magical debuff - ATK vs WIT
   * stun/pull are always physical
   * root/slow/fear can be magical or physical
   * bleed/venom are always physical
   * poison is always magical


# Tanks - taunt mobs

## DISCIPLINE: Bulwark — defensive wall: near-immortal, minimal damage

  ### Human — flavor: shiled + defence
   - Main 1 [DMG/TAUNT]: <name> — single-target dmg with shield, taunt (shiled is required)
   - Main 2 [AOE/TAUNT]: <name> — multi-target shout that taunts nearby enemies, gets a shield (for each enemy hit) - stack up to 30% of max hp 
   - Buff [self]: <name> — self shield - duration 15s, cd 15s, 8% of max hp  (insta-cast)
   - Passive [always-on]: <name> — increase defence,max hp, and mobs aggression (on-hit)  (can be 2 separate passives)
  ### Elf — flavor: heal + defence
   - Main 1 [DMG/TAUNT]: <name> — single-target dmg with shield, taunt (shiled is required)
   - Main 2 [AOE/TAUNT]: <name> — multi-target provoke(taunt), heals self (for each enemy hit) - stack up to 30% of max hp 
   - Buff [self]: <name> — cd 1 min, 30% of max hp self heal (insta-cast)
   - Passive [always-on]: <name> — increase -> healing received, defence, max hp
  ### Ork — flavor: immortality
   - Main 1 [DMG/TAUNT]: <name> — single-target hit with shield, taunt (shiled is required)
   - Main 2 [AOE/TAUNT]: <name> — multi-target ground stomp that taunts and lowers enemies attack
   - Buff [self]: <name> —  immune to dmg for 10s - cd 5min - recovers 25% over the duration  (insta-cast)
   - Passive [always-on]: <name> — when lethal dmg recovers 50% hp - 5 min cd, also increases -> defence, max hp (can be 2 separate passives)
   
   * Taunt always work - for mobs increases aggro value against the tank - for players tank changes enemy target to himself
   
## DISCIPLINE: Vanguard — offensive tank: high defence + real damage

  ### Human — flavor: more dmg than his tanky descipline
   - Main 1 [DMG/TAUNT]: <name> — single-target hit with shield, taunt (shiled is required)
   - Main 2 [DMG]: <name> — single-target, charges forward to dmg and stun enemy (at mele range just dmg+stun)
   - Buff [self]: <name> — increase dmg, and deffence ,max hp
   - Passive [always-on]: <name> — increase attack ,max hp, and mobs aggression (on-hit)  (can be 2 separate passives)
  ### Elf — flavor: 
   - Main 1 [DMG/TAUNT]: <name> — single-target hit with shield, taunt (shiled is required)
   - Main 2 [DMG]: <name> — single-target, ranged lightning strike that dmg and stuns enemy
   - Buff [self]: <name> — increase atk speed, speed and defence
   - Passive [always-on]: <name> — increases attack, max hp, on hit vamp(heal)
  ### Ork — flavor: lower 
   - Main 1 [DMG/TAUNT]: <name> — single-target hit with shield, taunt (shiled is required)
   - Main 2 [DMG/DEF]: <name> — single-target pull -> pulls enemy to himself and dmg and stunn (if pull is resisted stun can still land)  (at mele range just dmg+stun)
   - Buff [self]: <name> — increases atack, crit rate, crit dmg
   - Passive [always-on]: <name> — increases attack, max hp, lower hp more dmg (flat formula - % + lvl ? ) and dmg reduction (up to 50% at 25% hp)
   
   * ATK increases stun chance - CON increases stun resist - Bosses are immune - Stun is like other debuffs -> min 10% max 90% - same ATK vs CON 50%?
   * ATK increases pull chance - CON increases pull resist - Bosses are immune - Pull is like other debuffs -> min 10% max 90% - same ATK vs CON 50%?
   
# Warriors

## DISCIPLINE: Ravager — pure single-target burst, low survivability

  ### Human — flavor: 2h sword critical master
   - Main 1 [DMG]: <name> — big single-target sword slash [Double]
   - Main 2 [DMG/SLOW]: <name> — big horizontal frontal strike (can hit multimple enemies - upfront) slow enemies (90% - 10s)
   - Buff [self]: <name> — for a short period of time increases attack,crit rate,skill crit rate, crit dmg (15-30s) cd 1-2min
   - Passive [always-on]: <name> —increases atack and max hp, when 2h sword is equiped increases crit rate, crit dmg
  ### Elf — flavor: 2h weapon / utility 
   - Main 1 [DMG]: <name> — big single-target sword slash [Double]
   - Main 2 [DMG/HOLD]: <name> — big horizontal frontal strike (can hit multimple enemies - upfront) root enemies (10s)
   - Buff [self]: <name> — increases attack, atack speed (standart 20min buff)
   - Passive [always-on]: <name> —increases atack and max hp, when 2h weapon (blunt/sword) is equiped increases ms and as
  ### Ork — flavor: 2h blunt berserker
   - Main 1 [DMG]: <name> — big single-target sword slash
   - Main 2 [DMG/FEAR]: <name> — big horizontal frontal strike (can hit multimple enemies - upfront) fear (enemys cannot attack for 5 sec)
   - Buff [self]: <name> — for a short period of time increases attack, each attack can stun - 15-30s cd1-2min
   - Passive [always-on]: <name> —increases atack and max hp, when 2h blunt is equiped increases attack when hp gets lower 
   
   * warriors apply physical root/slow/frear
   
## DISCIPLINE: Warlord — balanced bruiser with AoE

  ### Human — flavor: AOE crit
   - Main 1 [DMG/AOE]: <name> — big aoe slash dmg [Double]
   - Main 2 [AOE/STUN]: <name> — slow+dmg enemies arround - 50% for 10s
   - Buff [self]: <name> — Increases attack,crit,crit rate
   - Passive [always-on]: <name> —increases atack and max hp, when 2h sword is equiped critical hit can vamp
  ### Elf — flavor: AOE
   - Main 1 [DMG/AOE]: <name> — big aoe slash dmg [Double]
   - Main 2 [AOE/STUN]: <name> — root around + dmg for 5s
   - Buff [self]: <name> — Increases attack,as/ms
   - Passive [always-on]: <name> —increases atack and max hp, when 2h weapon (blunt/sword) each basic attack chance to heal for % of max hp
  ### Ork — flavor: dmg + max hp
   - Main 1 [DMG/AOE]: <name> — big aoe slash dmg
   - Main 2 [AOE/STUN]: <name> — fear+dmg enemies arround for 3s
   - Buff [self]: <name> — Increases attack,max hp,hp regen
   - Passive [always-on]: <name> — increases atack and max hp, when 2h blunt increases dmg reduction
   
   
   
# Rogues

## DISCIPLINE: Phantom — stealth, high evasion, ambush burst

  ### Human — flavor: anti magic
   - Main 1 [DMG]: <name> — single-target dmg- increased dmg if stelth was active [Double]
   - Main 2 [DMG]: <name> — blink behind enemy and dmg - increased dmg if stelth was active [Double]
   - Buff [self]: <name> — enters stelth for 30sec - atacking cancel the effect - after the effect ends/cancel for 30sec increases magic fail chance
   - Passive [always-on]: <name> —if magic fails next skill gets the full stelth bonus dmg, when dual is equiped increases crit rate crit dmg, when light is equipped increases magi fail chance
  ### Elf — flavor: anti phys
   - Main 1 [DMG]: <name> — single-target dmg- increased dmg if stelth was active [Double]
   - Main 2 [DMG]: <name> — blink behind enemy and dmg - increased dmg if stelth was active [Double]
   - Buff [self]: <name> — enters stelth for 30sec - atacking cancel the effect - after the effect ends/cancel for 30sec increases evasion
   - Passive [always-on]: <name> —if phys atack is evaded next skill gets the half stelth bonus dmg (more chance to evade phys than magic to fail so dmg is halved), when dual is equiped increases atck speed,speed, when light is equipped increases evasion additionally
  ### Ork — flavor: brute dmg
   - Main 1 [DMG]: <name> — single-target dmg- increased dmg if stelth was active [Double]
   - Main 2 [DMG]: <name> — blink behind enemy and dmg - increased dmg if stelth was active [Double]
   - Buff [self]: <name> — enters stelth for 30sec - atacking cancel the effect - after the effect ends/cancel for 30sec increases skill dmg,max hp,def
   - Passive [always-on]: <name> — chance to increase skill dmg on hit, when dual is equiped increases skill dmg, when light is equipped increases max hp,def
   
   * human "evades" magic, the elf evades phys. the ork should outlive the target
   * increased dmg if stelth was active => enter stelth -> uses skill while hidden -> more dmg (not when the after-effect is active - only the hiddeen part)
   
## DISCIPLINE: Venomweaver — DoT stacks → burst 

  ### Human — flavor: annoying bleed and anti magic - slow and stay close taktics
   - Main 1 [DOT]: <name> — single-target apply bleed
   - Main 2 [DMG]: <name> — single-target remove bleed effect from target and does dmg on the stacks applyed  [Double]
   - Buff [self]: <name> — increases increases magic fail chance, crit rate,crit dmg
   - Passive [always-on]: <name> — chance to apply bleed on hit, when dual is equiped increases crit rate crit dmg , when light is equipped increases magi fail chance
  ### Elf — flavor:  annoying posion and anti phys - hit and run taktics
   - Main 1 [DOT]: <name> — single-target apply poison
   - Main 2 [DMG]: <name> — single-target remove poison effect from target and does dmg on the stacks applyed [Double]
   - Buff [self]: <name> — increases increases evasion, AS,MS
   - Passive [always-on]: <name> — chance to apply poison on hit, when dual is equiped increases atck speed,speed, when light is equipped increases evasion additionally
  ### Ork — flavor: comes trying to outhdmg/outlive the target
   - Main 1 [DOT]: <name> — single-target apply venom
   - Main 2 [DMG]: <name> — single-target remove venom effect from target and does dmg on the stacks applyed [Double]
   - Buff [self]: <name> — increases increases skill dmg, max hp,def
   - Passive [always-on]: <name> — chance to apply venom on hit, when dual is equiped increases skill dmg, when light is equipped increases skill dmg
   
   * apply stacks skills - debuff stays 30sec, skill cd 3 sec .. max 10 stacks- (hititng again refreshes duration)
   * burst dmg skill + 1xNumerOfStacks - x10 dmg at full stacks
   * Venomweavers must see stacks on enemy to know when to burst
   * bleed - DEX vs CON - does DOT - decreases target - MS
   * poison - ATK vs WIT - does DOT -  decreases target - AS/CAST
   * venom - DEX vs CON - does DOT - decreases target - Atk/def
   
   
# Archers

## DISCIPLINE: Sharpshooter — long range, high single-target damage

  ### Human — flavor: crit focus
   - Main 1 [DMG]: <name> — single-target dmg [Double]
   - Main 2 [DMG]: <name> — focused single target dmg + knock back (have 2s cast time, never misses, +20% chance to do double dmg) [Double]
   - Buff [self]: <name> — increases range, crit rate, crit dmg
   - Passive [always-on]: <name> — when bow is equiped basic attack chance to increase critical rate/magic crit rate to self + party, light armor - increas ms,eva
  ### Elf — flavor: kite/focus
   - Main 1 [DMG]: <name> — single-target dmg [Double]
   - Main 2 [DMG]: <name> — focused single target dmg + knock back (have 2s cast time, never misses, +20% chance to do double dmg) [Double]
   - Buff [self]: <name> — increases range, ms, as
   - Passive [always-on]: <name> — when bow is equiped basic attack chance to increase ms/as/cast to self + party, light armor - increas ms,eva
  ### Ork — flavor: dmg focus
   - Main 1 [DMG]: <name> — single-target dmg [Double]
   - Main 2 [DMG]: <name> — focused single target dmg + knock back (have 2s cast time, never misses, +20% chance to do double dmg) [Double]
   - Buff [self]: <name> — increases range, atk,skill dmg
   - Passive [always-on]: <name> — when bow is equiped basic attack chance to increase atk,skill dmg to self + party, light armor - increas ms,eva
   
## DISCIPLINE: Trapper — utility / traps / crowd control  

  ### Human — flavor: slow
   - Main 1 [DMG]: <name> — single-target dmg + knock back and decrese Atk [Double]
   - Main 2 [UTIL/AOE]: <name> — place a trap that when triggered stun targets for 5s and does dmg
   - Buff [self]: <name> — increases range, crit rate, crit dmg
   - Passive [always-on]: <name> — when bow is equiped basic have a chance to slow target by 50% for 2s, light armor - increas ms,eva
  ### Elf — flavor: root
   - Main 1 [DMG]: <name> — single-target dmg + knock back and decrease AS/Cast [Double]
   - Main 2 [UTIL/AOE]: <name> — place a trap that when triggered stun targets for 5s and does dmg
   - Buff [self]: <name> — increases range, ms, as
   - Passive [always-on]: <name> — when bow is equiped basic attack have chance to root target for 1s, light armor - increas ms,eva
  ### Ork — flavor: fear
   - Main 1 [DMG]: <name> — single-target dmg + knock back + decrease phys/magical defence [Double]
   - Main 2 [UTIL/AOE]: <name> — place a trap that when triggered stun targets for 5s and does dmg
   - Buff [self]: <name> — increases range, atk,skill dmg,skill cast speed
   - Passive [always-on]: <name> — when bow is equiped basic attack chance to fear target for 0.5s, light armor - increas ms,eva
   
   
   
# Nukers

## DISCIPLINE: Magus — single-target glass cannon

  ### Human — flavor: crit and mana shield
   - Main 1 [DMG]: <name> — big single-target nuke
   - Main 2 [DMG]: <name> — single-target armor-ignore nuke that slows target for 15s by 75% - cd 1min
   - Buff [self]: <name> — mana shield, 70% of dmg is converted to 0.5 mana per 1dmg - 30s cd and 30sec duration
   - Passive [always-on]: <name> — when robe is equiped +10% magic crit rate, mp reg, max mp
  ### Elf — flavor: cast like crazy
   - Main 1 [DMG]: <name> — big single-target nuke
   - Main 2 [DMG]: <name> — single-target armor-ignore nuke that root target for 10s - cd 1min
   - Buff [self]: <name> — increase max mp,cast,mp regen
   - Passive [always-on]: <name> — when robe is equiped +10% cast, mp reg, max mp
  ### Ork — flavor: dmg dmg dmg
   - Main 1 [DMG]: <name> — big single-target nuke
   - Main 2 [DMG]: <name> — single-target armor-ignore nuke that fears target for 10s - cd 1min
   - Buff [self]: <name> — normal hp shield, dur-15s, cd 30s , while active increases atk
   - Passive [always-on]: <name> — when robe is equiped +15% atak, mp reg, max mp
   
## DISCIPLINE: Tempest — AoE damage + control

  ### Human — flavor: aoe slow
   - Main 1 [DMG/AOE]: <name> — aoe multi-target skill that slows targets by 50% for 10s
   - Main 1 [DMG/AOE]: <name> — aoe multi-target skill that 50% more dmg to slowed targets - and decreases slows resist
   - Buff [self]: <name> — blink back 400 range and increases magic crit chance and slow chance - duration 5s cd 30s
   - Passive [always-on]: <name> — when robe is equiped mp reg, max mp,magic crit chance
  ### Elf — flavor: aoe root
   - Main 1 [DMG/AOE]: <name> — aoe multi-target skill that root targets  for 7s
   - Main 1 [DMG/AOE]: <name> — aoe multi-target skill that 50% more dmg to rooted targets - and decreases root resist
   - Buff [self]: <name> — blink back 400 range and increases cast and root chance - duration 5s cd 30s
   - Passive [always-on]: <name> — when robe is equiped mp reg, max mp,cast
  ### Ork — flavor: aoe fear
   - Main 1 [DMG/AOE]: <name> — aoe multi-target skill that fear targets for 5s
   - Main 1 [DMG/AOE]: <name> — aoe multi-target skill that 50% more dmg to rooted targets - and decreases root resist
   - Buff [self]: <name> — blink back 400 range and increases atk and fear chance - duration 5s cd 30s
   - Passive [always-on]: <name> — when robe is equiped mp reg, max mp,magic atk
   
   
   
# Healers

## DISCIPLINE: Lightbringer — AoE heals + shields [DONE]  
   
## DISCIPLINE:  Warchanter — buffer: stat buffs + heal-over-time

  ### Human — flavor: arcane/precision
   - Main 1 [DMG]: <name> — signle-target main maigc dmg skill
   - Buff [self + party]: <name> — increases magic def/cast/atk speed by 30%, phys def/atk by 15% move speed by 45 , max hp/mp by 35%, and mp/hp regen by 20%
   - Buff [self + party]: <name> — heal tagget for 10% of max hp and apply 10s buff that heals 2% per second 30s cd, 2s cast time
   - Passive [always-on]: <name> — when robe is equiped increces max mp/mp regen/magic crit rate, when light is equiped increaces atack,defence,max hp, crit rate crit dmg
  ### Elf — flavor: 
   - Main 1 [DMG]: <name> — signle target main dmg skill
   - Buff [self + party]: <name> — increases magic def/cast/atk speed by 30%, phys def/atk by 15% move speed by 45 , max hp/mp by 35%, and mp/hp regen by 20%
   - Buff [self + party]: <name> — heal tagget for 10% of max hp and apply 10s buff that heals 2% per second 30s cd, 2s cast time
   - Passive [always-on]: <name> — when robe is equiped increces max mp/mp regen/cast, when light is equiped increaces atack,defence,max hp, atk speed/speed
  ### Ork — flavor: 
   - Main 1 [DMG]: <name> — signle target main dmg skill
   - Buff [self + party]: <name> — increases magic def/cast/atk speed by 30%, phys def/atk by 15% move speed by 45 , max hp/mp by 35%, and mp/hp regen by 20%
   - Buff [self + party]: <name> — heal tagget for 10% of max hp and apply 10s buff that heals 2% per second 30s cd, 2s cast time
   - Passive [always-on]: <name> — when robe is equiped increces max mp/mp regen/atk, when light is equiped increaces atack(more),defence,max hp