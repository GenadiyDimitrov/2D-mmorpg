using System.Globalization;
using Game.Server.Persistence;
using Game.Shared;

namespace Game.Server.Simulation;

// =====================================================================================================
//  `BL-339` — THE MENTOR SYSTEM. Design + his three answer passes: docs/design/Mentoring.md; numbers:
//  Game.Shared/Mentoring.cs.
//
//  🔑 THE WHOLE ROSTER LIVES IN MEMORY. `/mentor` names people who are usually OFFLINE (the mentee who has
//  not logged in for 29 days is the reason `remove` exists), so every answer — their level, their last
//  login, their penalty, their activity — has to be there without a database round trip. One
//  MentorProfileRecord per character (created on their first entry), every bond and invitation, loaded
//  once at startup and written through an ORDERED chain (MentorWrite) so an upsert and a later delete of
//  the same bond can never land in the wrong order.
//
//  The single writer owns all of it, exactly like everything else here: no locks.
// =====================================================================================================
public partial class GameLoopService
{
    private const int BondActive = 0, BondMentorInvited = 1, BondMenteeAsked = 2;

    private readonly Dictionary<int, MentorProfileRecord> _mentorProfiles = new();
    private readonly List<MentorBondRecord> _mentorBonds = new();
    /// <summary>When each character was last seen ACTIVE (combat or exp), runtime only — what lets a mentee
    /// who just logged out keep counting through the grace window without being re-read as AFK.</summary>
    private readonly Dictionary<int, DateTime> _mentorLastActive = new();
    private Task _mentorWrites = Task.CompletedTask;

    private async Task LoadMentoringAsync()
    {
        var (profiles, bonds) = await _db.LoadMentoringAsync();
        foreach (var p in profiles) _mentorProfiles[p.CharacterId] = p;
        _mentorBonds.AddRange(bonds);
        _log.LogInformation("Mentoring: {Profiles} profiles, {Bonds} bonds/invitations loaded.",
            profiles.Count, bonds.Count);
    }

    // ----- persistence: an ORDERED chain of copies taken on the tick thread -----

    private void MentorWrite(Func<Task> op) =>
        _mentorWrites = _mentorWrites.ContinueWith(async _ =>
        {
            try { await op(); }
            catch (Exception ex) { _log.LogError(ex, "Mentoring save failed"); }
        }).Unwrap();

    private void SaveMentorProfile(MentorProfileRecord p)
    {
        var copy = new MentorProfileRecord
        {
            CharacterId = p.CharacterId, AccountId = p.AccountId, Name = p.Name, MainLevel = p.MainLevel,
            MentorEligible = p.MentorEligible, LastOnlineUtc = p.LastOnlineUtc, LoginDaysCsv = p.LoginDaysCsv,
            PenaltyUntilUtc = p.PenaltyUntilUtc, MilestonesPaid = p.MilestonesPaid,
            OwedBondCerts = p.OwedBondCerts, OwedGraduationCerts = p.OwedGraduationCerts,
        };
        MentorWrite(() => _db.UpsertMentorProfileAsync(copy));
    }

    private void SaveMentorBond(MentorBondRecord b)
    {
        var copy = new MentorBondRecord
        {
            Id = b.Id, MentorCharacterId = b.MentorCharacterId, MenteeCharacterId = b.MenteeCharacterId,
            State = b.State, CreatedUtc = b.CreatedUtc, HeldAt20 = b.HeldAt20, HeldAt40 = b.HeldAt40,
        };
        MentorWrite(() => _db.UpsertMentorBondAsync(copy));
    }

    private void DeleteMentorBond(MentorBondRecord b)
    {
        _mentorBonds.Remove(b);
        var id = b.Id;
        MentorWrite(() => _db.DeleteMentorBondAsync(id));
    }

    // ----- the roster -----

    private static Subclass? MainClassOf(Entity e) => e.Subclasses.FirstOrDefault(s => s.Slot == 0);

    /// <summary>This online character's profile, created on first sight and refreshed from the live entity.
    /// Null only for an entity with no database row yet. Returns whether anything persisted changed.</summary>
    private MentorProfileRecord? MentorProfile(Entity e, out bool changed)
    {
        changed = false;
        if (e.PersistentId is not int id) return null;
        var main = MainClassOf(e);
        int mainLevel = main?.Level ?? e.Level;
        bool eligible = main is not null && main.FourthClass > 0 && main.Level >= Mentoring.MentorLevel;

        if (!_mentorProfiles.TryGetValue(id, out var p))
        {
            p = new MentorProfileRecord { CharacterId = id, Name = e.Name, LastOnlineUtc = DateTime.UtcNow };
            // Milestones already behind a character who predates mentoring are PASSED, not owed: the bond
            // pays the levels reached while it holds, never ones reached before it existed.
            for (int i = 0; i < Mentoring.MilestoneLevels.Length; i++)
                if (mainLevel >= Mentoring.MilestoneLevels[i]) p.MilestonesPaid |= 1 << i;
            _mentorProfiles[id] = p;
            changed = true;
        }
        if (p.Name != e.Name || p.AccountId != e.AccountId || p.MainLevel != mainLevel || p.MentorEligible != eligible)
        {
            p.Name = e.Name; p.AccountId = e.AccountId; p.MainLevel = mainLevel; p.MentorEligible = eligible;
            changed = true;
        }
        return p;
    }

    private MentorProfileRecord? MentorProfileByName(string name) =>
        _mentorProfiles.Values.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    private Entity? OnlineCharacter(int characterId) =>
        _world.Entities.Values.FirstOrDefault(e => e.Kind == EntityKind.Player && e.PersistentId == characterId);

    private MentorBondRecord? MenteeBond(int menteeId) =>
        _mentorBonds.FirstOrDefault(b => b.State == BondActive && b.MenteeCharacterId == menteeId);

    private List<MentorBondRecord> MentorBonds(int mentorId) =>
        _mentorBonds.Where(b => b.State == BondActive && b.MentorCharacterId == mentorId).ToList();

    private MentorBondRecord? BondBetween(int a, int b) =>
        _mentorBonds.FirstOrDefault(x => (x.MentorCharacterId == a && x.MenteeCharacterId == b)
                                      || (x.MentorCharacterId == b && x.MenteeCharacterId == a));

    private string NameOf(int characterId) =>
        _mentorProfiles.TryGetValue(characterId, out var p) ? p.Name : "?";

    /// <summary>"0m" when online, else the time since they left — the list's column and the penalty's key.</summary>
    private TimeSpan SinceOnline(MentorProfileRecord p) =>
        OnlineCharacter(p.CharacterId) is not null ? TimeSpan.Zero : DateTime.UtcNow - p.LastOnlineUtc;

    private static string TodayKey() => DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    /// <summary>Days logged in of the last 7, today included.</summary>
    private static int ActiveDays(MentorProfileRecord p)
    {
        var floor = DateTime.UtcNow.Date.AddDays(-(Mentoring.ActivityDays - 1));
        int n = 0;
        foreach (var s in p.LoginDaysCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal
                                       | DateTimeStyles.AdjustToUniversal, out var d) && d.Date >= floor)
                n++;
        return n;
    }

    private static bool StampLoginDay(MentorProfileRecord p)
    {
        string today = TodayKey();
        var days = p.LoginDaysCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (days.Count > 0 && days[0] == today) return false;
        days.Insert(0, today);
        p.LoginDaysCsv = string.Join(',', days.Take(Mentoring.ActivityDays));
        return true;
    }

    private static TimeSpan PenaltyLeft(MentorProfileRecord p) =>
        p.PenaltyUntilUtc is DateTime u && u > DateTime.UtcNow ? u - DateTime.UtcNow : TimeSpan.Zero;

    // ----- hooks -----

    /// <summary>Entering the world: the profile, today's login, anything owed, the auras.</summary>
    private void MentorOnEnter(Entity e)
    {
        var p = MentorProfile(e, out _);
        if (p is null) return;
        StampLoginDay(p);
        p.LastOnlineUtc = DateTime.UtcNow;
        PayOwedCertificates(e, p);
        SaveMentorProfile(p);

        int waiting = _mentorBonds.Count(b =>
            (b.State == BondMentorInvited && b.MenteeCharacterId == p.CharacterId)
            || (b.State == BondMenteeAsked && b.MentorCharacterId == p.CharacterId));
        if (waiting > 0)
            SendSystemToEntity(e, $"You have {waiting} mentoring invitation(s) waiting — type /mentor list.");
        RefreshMentorAuras();
    }

    /// <summary>Leaving: stamp the time. The auras drop at the next re-check, after the grace window.</summary>
    private void MentorOnLeave(Entity e)
    {
        if (MentorProfile(e, out _) is not MentorProfileRecord p) return;
        p.LastOnlineUtc = DateTime.UtcNow;
        NoteMentorActivity(e);
        SaveMentorProfile(p);
    }

    private void NoteMentorActivity(Entity e)
    {
        if (e.PersistentId is not int id) return;
        var now = DateTime.UtcNow;
        var last = e.LastExpGainUtc;
        if (e.LastCombatTick > 0)
        {
            var combat = now - TimeSpan.FromSeconds((_tick - e.LastCombatTick) * GameConstants.TickSeconds);
            if (combat > last) last = combat;
        }
        if (!_mentorLastActive.TryGetValue(id, out var known) || last > known) _mentorLastActive[id] = last;
    }

    /// <summary>A level-up: pay any milestone the MAIN class just crossed, and graduate at 76.</summary>
    private void MentorOnLevelUp(Entity e)
    {
        if (e.ActiveSubclass.Slot != 0) return;   // a subclass level is never a mentee milestone
        var p = MentorProfile(e, out bool changed);
        if (p is null) return;

        for (int i = 0; i < Mentoring.MilestoneLevels.Length; i++)
        {
            int bit = 1 << i;
            if (p.MainLevel < Mentoring.MilestoneLevels[i] || (p.MilestonesPaid & bit) != 0) continue;
            // PASSED either way: a milestone reached with no mentor is gone, not owed to a later one.
            p.MilestonesPaid |= bit;
            changed = true;
            if (MenteeBond(p.CharacterId) is not MentorBondRecord bond) continue;
            if (!_mentorProfiles.TryGetValue(bond.MentorCharacterId, out var mentor)) continue;

            int lvl = Mentoring.MilestoneLevels[i];
            bool graduation = lvl >= Mentoring.GraduationLevel;
            int grad = graduation ? Mentoring.GraduationCertificates(bond.HeldAt20, bond.HeldAt40) : 0;
            if (i == 0) bond.HeldAt20 = true;
            if (i == 1) bond.HeldAt40 = true;

            mentor.OwedBondCerts += Mentoring.MentorBondPay[i];
            mentor.OwedGraduationCerts += grad;
            p.OwedBondCerts += Mentoring.MenteeBondPay[i];

            var mentorOnline = OnlineCharacter(mentor.CharacterId);
            SendSystemToEntity(e, graduation
                ? $"You graduated! Your mentor {mentor.Name} pays you {Mentoring.MenteeBondPay[i]} Bond Certificates, and the bond ends here."
                : $"Level {lvl} with your mentor {mentor.Name}: {Mentoring.MenteeBondPay[i]} Bond Certificates.");
            if (mentorOnline is not null)
                SendSystemToEntity(mentorOnline, graduation
                    ? $"Your mentee {p.Name} graduated at {lvl}: {Mentoring.MentorBondPay[i]} Bond and {grad} Graduation Certificates."
                    : $"Your mentee {p.Name} reached level {lvl}: {Mentoring.MentorBondPay[i]} Bond Certificates.");

            if (graduation) DeleteMentorBond(bond);
            else SaveMentorBond(bond);
            if (mentorOnline is not null) PayOwedCertificates(mentorOnline, mentor);
            SaveMentorProfile(mentor);
        }

        // At 76 a character can be nobody's mentee: every invitation naming them as one goes (his rule).
        if (p.MainLevel >= Mentoring.GraduationLevel)
            foreach (var b in _mentorBonds.Where(b => b.MenteeCharacterId == p.CharacterId).ToList())
                DeleteMentorBond(b);

        PayOwedCertificates(e, p);
        if (changed) SaveMentorProfile(p);
        RefreshMentorAuras();
    }

    /// <summary>Move what is owed into the bag. A full bag keeps it owed — told, and paid at the next login.</summary>
    private void PayOwedCertificates(Entity e, MentorProfileRecord p)
    {
        bool paid = false;
        if (p.OwedBondCerts > 0 && AddItem(e, ItemCatalog.BondCertificate, p.OwedBondCerts))
        { p.OwedBondCerts = 0; paid = true; }
        if (p.OwedGraduationCerts > 0 && AddItem(e, ItemCatalog.GraduationCertificate, p.OwedGraduationCerts))
        { p.OwedGraduationCerts = 0; paid = true; }
        if (p.OwedBondCerts > 0 || p.OwedGraduationCerts > 0)
            SendSystemToEntity(e, "Mentoring certificates are waiting but your bag is full — make room and relog.");
        if (paid) { SendInventory(e); SaveMentorProfile(p); }
    }

    // ----- the auras -----

    private bool OnlineOrInGrace(MentorProfileRecord p) =>
        OnlineCharacter(p.CharacterId) is not null
        || (DateTime.UtcNow - p.LastOnlineUtc).TotalSeconds < Mentoring.GraceSeconds;

    private bool RecentlyActive(int characterId) =>
        _mentorLastActive.TryGetValue(characterId, out var t)
        && (DateTime.UtcNow - t).TotalSeconds < Mentoring.ActiveWindowSeconds;

    /// <summary>The 2-minute re-check (and every bond change): refresh the online characters' profiles,
    /// then set each online mentor's rung and each online mentee's guidance. Only a CHANGE recomputes
    /// stats and re-sends the bar.</summary>
    private void RefreshMentorAuras()
    {
        foreach (var e in _world.Entities.Values)
        {
            if (e.Kind != EntityKind.Player) continue;
            NoteMentorActivity(e);
            if (MentorProfile(e, out bool changed) is MentorProfileRecord p)
            {
                changed |= StampLoginDay(p);   // a session running across midnight is a login that day too
                if (changed) SaveMentorProfile(p);
            }
        }

        foreach (var e in _world.Entities.Values)
        {
            if (e.Kind != EntityKind.Player || e.PersistentId is not int id) continue;
            int rung = 0;
            bool guidance = false;
            if (_mentorProfiles.TryGetValue(id, out var me))
            {
                if (me.MentorEligible)
                {
                    float weight = 0f;
                    foreach (var b in MentorBonds(id))
                        if (_mentorProfiles.TryGetValue(b.MenteeCharacterId, out var mentee)
                            && OnlineOrInGrace(mentee) && RecentlyActive(mentee.CharacterId))
                            weight += Mentoring.MenteeWeight(mentee.MainLevel);
                    rung = Mentoring.AuraRung(weight);
                }
                if (MenteeBond(id) is MentorBondRecord mb
                    && _mentorProfiles.TryGetValue(mb.MentorCharacterId, out var mentor))
                    guidance = OnlineOrInGrace(mentor);
            }
            if (rung == e.MentorAuraRung && guidance == e.MentorGuidance) continue;
            e.MentorAuraRung = rung;
            e.MentorGuidance = guidance;
            e.RecomputeDerived();
            PushBuffs(e);
        }
    }

    private void TickMentoring()
    {
        if (_tick % (Mentoring.RecheckSeconds * GameConstants.TickRate) == 0) RefreshMentorAuras();
    }

    /// <summary>The two synthetic buff-bar rows. No timer (-1), like the paving: they last as long as the
    /// condition does, and the player is meant to read them as "while online".</summary>
    private static IEnumerable<BuffDto> MentorRows(Entity p)
    {
        if (p.MentorAuraRung > 0)
        {
            int r = p.MentorAuraRung;
            yield return new BuffDto("Mentor Aura",
                $"Your mentees are out there levelling: +{r * Mentoring.AuraExpSpPerRung * 100:0}% experience and SP, "
                + $"+{r * Mentoring.AuraDropGoldPerRung * 100:0}% drop chance and gold. It grows with how many active "
                + $"mentees are online and how high they are (max level {Mentoring.MaxAuraRung}).",
                -1f, false, "mentor_aura", 1, BuffRow.Buff, "", Level: r, IconSkillId: "mentor_aura");
        }
        if (p.MentorGuidance)
            yield return new BuffDto("Mentor's Guidance",
                $"Your mentor is online and watching over you: +{Mentoring.MenteeExpSpBonus * 100:0}% experience and SP.",
                -1f, false, "mentor_guidance", 1, BuffRow.Buff, "", IconSkillId: "mentor_guidance");
    }

    // ----- /mentor -----

    private void HandleMentorCommand(Entity p, string argument)
    {
        var parts = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string verb = parts.Length > 0 ? parts[0].ToLowerInvariant() : "list";
        string name = parts.Length > 1 ? parts[1].Trim() : "";
        var me = MentorProfile(p, out bool changed);
        if (me is null) return;
        if (changed) SaveMentorProfile(me);

        switch (verb)
        {
            case "invite":  MentorInvite(p, me, name); break;
            case "accept":  MentorAnswer(p, me, name, accept: true); break;
            case "decline": MentorAnswer(p, me, name, accept: false); break;
            case "remove":  MentorRemove(p, me, name); break;
            case "list":    MentorList(p, me); break;
            default:
                SendSystemToEntity(p, "Usage: /mentor invite <name> | accept <name> | decline <name> | remove [name] | list");
                break;
        }
    }

    /// <summary>Who is the mentor and who the mentee between these two, from the inviter's side — or a refusal.</summary>
    private string? MentorRoles(MentorProfileRecord me, MentorProfileRecord other,
                                out MentorProfileRecord mentor, out MentorProfileRecord mentee)
    {
        mentor = me; mentee = other;
        if (me.MentorEligible) return null;
        if (me.MainLevel >= Mentoring.MentorLevel)
            return "You need your 4th class to become a mentor.";
        mentor = other; mentee = me;
        if (!other.MentorEligible)
            return $"{other.Name} is not a mentor (a mentor's main class is level {Mentoring.MentorLevel} with a 4th class).";
        return null;
    }

    /// <summary>Every rule a NEW bond must pass — checked when it is offered and again when it is accepted,
    /// since anything may have changed in between (his: an invite sent before a penalty fails on accept).</summary>
    private string? BondRefusal(Entity actor, MentorProfileRecord mentor, MentorProfileRecord mentee)
    {
        bool actorIsMentor = actor.PersistentId == mentor.CharacterId;
        if (mentee.MainLevel >= Mentoring.GraduationLevel)
            return $"{mentee.Name} is level {Mentoring.GraduationLevel} or above and cannot be a mentee.";
        if (mentor.AccountId == mentee.AccountId)
            return actorIsMentor ? "You cannot mentor a character of your own account."
                                 : "Your mentor cannot be a character of your own account.";
        if (MenteeBond(mentee.CharacterId) is not null)
            return actorIsMentor ? $"Mentee {mentee.Name} already has a mentor."
                                 : "You already have a mentor. Use '/mentor remove' first.";
        var bonds = MentorBonds(mentor.CharacterId);
        if (bonds.Count >= Mentoring.MaxMentees)
            return actorIsMentor ? "You cannot accept any more mentees. Use '/mentor remove <name>' or wait for graduation."
                                 : $"Mentor {mentor.Name} cannot accept any more mentees.";
        // His anti-abuse rule: ten alts on one account are worth ONE mentee.
        if (bonds.Any(b => _mentorProfiles.TryGetValue(b.MenteeCharacterId, out var m) && m.AccountId == mentee.AccountId))
            return actorIsMentor ? $"You already mentor a character of {mentee.Name}'s account."
                                 : $"Mentor {mentor.Name} already mentors a character of your account.";
        foreach (var (who, label) in new[] { (mentor, "Mentor"), (mentee, "Mentee") })
        {
            var left = PenaltyLeft(who);
            if (left <= TimeSpan.Zero) continue;
            return actor.PersistentId == who.CharacterId
                ? $"You are under a bond penalty for {Mentoring.Remaining(left)}. Bond cannot be made."
                : $"{label} is under a bond penalty for {Mentoring.Remaining(left)}. Bond cannot be made.";
        }
        return null;
    }

    private void MentorInvite(Entity p, MentorProfileRecord me, string name)
    {
        if (name.Length == 0) { SendSystemToEntity(p, "Usage: /mentor invite <name>"); return; }
        var other = MentorProfileByName(name);
        if (other is null) { SendSystemToEntity(p, $"No character '{name}'."); return; }
        if (other.CharacterId == me.CharacterId) { SendSystemToEntity(p, "You cannot mentor yourself."); return; }

        if (MentorRoles(me, other, out var mentor, out var mentee) is string role) { SendSystemToEntity(p, role); return; }

        if (BondBetween(me.CharacterId, other.CharacterId) is MentorBondRecord existing)
        {
            bool mine = existing.State == (me == mentor ? BondMentorInvited : BondMenteeAsked);
            SendSystemToEntity(p, existing.State == BondActive ? $"{other.Name} is already bonded with you."
                : mine ? $"You have already invited {other.Name}."
                : $"{other.Name} has already invited you — type /mentor accept {other.Name}.");
            return;
        }
        if (BondRefusal(p, mentor, mentee) is string refusal) { SendSystemToEntity(p, refusal); return; }

        var bond = new MentorBondRecord
        {
            Id = Guid.NewGuid(), MentorCharacterId = mentor.CharacterId, MenteeCharacterId = mentee.CharacterId,
            State = me == mentor ? BondMentorInvited : BondMenteeAsked, CreatedUtc = DateTime.UtcNow,
        };
        _mentorBonds.Add(bond);
        SaveMentorBond(bond);
        SendSystemToEntity(p, me == mentor ? $"You invited {other.Name} to be your mentee."
                                           : $"You asked {other.Name} to be your mentor.");
        if (OnlineCharacter(other.CharacterId) is Entity o)
            SendSystemToEntity(o, (me == mentor ? $"{me.Name} invites you to be their mentee."
                                                : $"{me.Name} asks you to be their mentor.")
                                  + $" Type /mentor accept {me.Name} or /mentor decline {me.Name}.");
    }

    private void MentorAnswer(Entity p, MentorProfileRecord me, string name, bool accept)
    {
        if (name.Length == 0) { SendSystemToEntity(p, $"Usage: /mentor {(accept ? "accept" : "decline")} <name>"); return; }
        var other = MentorProfileByName(name);
        var bond = other is null ? null : BondBetween(me.CharacterId, other.CharacterId);
        // Only an invitation the OTHER side sent can be answered.
        bool theirs = bond is not null
            && ((bond.State == BondMentorInvited && bond.MenteeCharacterId == me.CharacterId)
                || (bond.State == BondMenteeAsked && bond.MentorCharacterId == me.CharacterId));
        if (!theirs || other is null || bond is null) { SendSystemToEntity(p, $"No mentoring invitation from {name}."); return; }

        var otherOnline = OnlineCharacter(other.CharacterId);
        if (!accept)
        {
            DeleteMentorBond(bond);
            SendSystemToEntity(p, $"You declined {other.Name}.");
            if (otherOnline is not null) SendSystemToEntity(otherOnline, $"{me.Name} declined your mentoring invitation.");
            return;
        }

        var mentor = _mentorProfiles[bond.MentorCharacterId];
        var mentee = _mentorProfiles[bond.MenteeCharacterId];
        if (!mentor.MentorEligible) { SendSystemToEntity(p, $"{mentor.Name} is no longer a mentor."); return; }
        if (BondRefusal(p, mentor, mentee) is string refusal) { SendSystemToEntity(p, refusal); return; }

        bond.State = BondActive;
        bond.CreatedUtc = DateTime.UtcNow;
        SaveMentorBond(bond);
        // His rule: once a mentee has a mentor, every other invitation naming them leaves every list.
        foreach (var b in _mentorBonds.Where(b => b != bond && b.MenteeCharacterId == mentee.CharacterId).ToList())
            DeleteMentorBond(b);

        string line = $"{mentor.Name} is now the mentor of {mentee.Name}.";
        SendSystemToEntity(p, line);
        if (otherOnline is not null) SendSystemToEntity(otherOnline, line);
        RefreshMentorAuras();
    }

    private void MentorRemove(Entity p, MentorProfileRecord me, string name)
    {
        MentorBondRecord? bond;
        if (name.Length == 0)
        {
            bond = MenteeBond(me.CharacterId);
            if (bond is null) { SendSystemToEntity(p, "Usage: /mentor remove <name>"); return; }
        }
        else
        {
            var other = MentorProfileByName(name);
            bond = other is null ? null : BondBetween(me.CharacterId, other.CharacterId);
            if (bond is null) { SendSystemToEntity(p, $"{name} is not on your mentor list."); return; }
        }

        int otherId = bond.MentorCharacterId == me.CharacterId ? bond.MenteeCharacterId : bond.MentorCharacterId;
        var them = _mentorProfiles[otherId];
        var themOnline = OnlineCharacter(otherId);
        DeleteMentorBond(bond);

        if (bond.State != BondActive)
        {
            SendSystemToEntity(p, $"The invitation between you and {them.Name} is withdrawn.");
            return;
        }

        // His table, keyed on the OTHER side's last login: removing someone who still plays costs you.
        var penalty = Mentoring.RemovalPenalty(SinceOnline(them));
        string tail = "";
        if (penalty > TimeSpan.Zero)
        {
            var until = DateTime.UtcNow + penalty;
            if (me.PenaltyUntilUtc is not DateTime cur || cur < until) me.PenaltyUntilUtc = until;
            SaveMentorProfile(me);
            tail = $" You cannot make a new bond for {Mentoring.Remaining(penalty)}.";
        }
        bool iWasMentor = bond.MentorCharacterId == me.CharacterId;
        SendSystemToEntity(p, (iWasMentor ? $"{them.Name} is no longer your mentee." : $"{them.Name} is no longer your mentor.") + tail);
        if (themOnline is not null)
            SendSystemToEntity(themOnline, iWasMentor ? $"{me.Name} is no longer your mentor." : $"{me.Name} is no longer your mentee.");
        RefreshMentorAuras();
    }

    private void MentorList(Entity p, MentorProfileRecord me)
    {
        var lines = new List<string>();
        var penalty = PenaltyLeft(me);

        if (me.MentorEligible || _mentorBonds.Any(b => b.MentorCharacterId == me.CharacterId))
        {
            var rows = _mentorBonds.Where(b => b.MentorCharacterId == me.CharacterId).ToList();
            lines.Add($"Mentees ({rows.Count(b => b.State == BondActive)}/{Mentoring.MaxMentees})"
                      + (p.MentorAuraRung > 0 ? $" — Mentor Aura Lv.{p.MentorAuraRung}:" : ":"));
            foreach (var b in rows.OrderBy(b => b.State).ThenBy(b => NameOf(b.MenteeCharacterId)))
            {
                var m = _mentorProfiles[b.MenteeCharacterId];
                lines.Add(b.State switch
                {
                    BondActive => $"  {m.Name} {(OnlineCharacter(m.CharacterId) is null ? "Offline" : "Online")} ({m.MainLevel}) {Mentoring.Ago(SinceOnline(m))}",
                    BondMentorInvited => $"  {m.Name} Invited",
                    _ => $"  {m.Name} Pending",
                });
            }
            if (rows.Count == 0) lines.Add("  none yet — /mentor invite <name> (a character below 76)");
        }
        else if (MenteeBond(me.CharacterId) is MentorBondRecord mb)
        {
            var m = _mentorProfiles[mb.MentorCharacterId];
            lines.Add($"Mentor: {m.Name} {(OnlineCharacter(m.CharacterId) is null ? "Offline" : "Online")} {Mentoring.Ago(SinceOnline(m))}");
        }
        else
        {
            var rows = _mentorBonds.Where(b => b.MenteeCharacterId == me.CharacterId).ToList();
            if (rows.Count == 0)
                lines.Add($"You have no mentor. A mentor is a level {Mentoring.MentorLevel}+ character with a 4th class: "
                          + "find one in World chat, then /mentor invite <name>.");
            else lines.Add("Mentor invitations:");
            foreach (var b in rows.OrderBy(b => NameOf(b.MentorCharacterId)))
            {
                var m = _mentorProfiles[b.MentorCharacterId];
                int days = ActiveDays(m);
                lines.Add($"  {m.Name} {(b.State == BondMenteeAsked ? "Invited" : "Pending")} "
                          + $"{(OnlineCharacter(m.CharacterId) is null ? "Offline" : "Online")} "
                          + $"{days}/{Mentoring.ActivityDays}d ({Math.Round(days * 100.0 / Mentoring.ActivityDays):0}%)");
            }
        }
        if (penalty > TimeSpan.Zero) lines.Add($"Bond penalty: {Mentoring.Remaining(penalty)} left.");
        foreach (var l in lines) SendSystemToEntity(p, l);
    }
}
