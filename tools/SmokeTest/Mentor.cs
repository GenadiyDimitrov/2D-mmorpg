using Game.Shared;
using Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// `BL-339` — the mentor system, headless: `dotnet run --project tools/SmokeTest -- mentor` (seconds, not the
/// full run's minutes). Everything it checks is invisible on screen until the day it pays out wrong: who is
/// paid what at 20 / 40 / 76, the graduation count, the aura's rung, the penalty that refuses the next bond.
///
/// Cast: the seeded Admin (a 90, given a 4th class by the debug toggle for the run and put back after) is the
/// mentor; a FRESH test2 character is the mentee; a FRESH test3 character tests remove + penalty. Fresh every
/// run, so the run never depends on what the last one left — and the penalty lands on the throwaway mentee,
/// never on Admin, whose next run it would otherwise block for a day.
/// </summary>
static class MentorTest
{
    const string Url = "http://localhost:5238/game";
    static int _failures;

    static void Check(string what, bool ok, string? detail = null)
    {
        Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write(ok ? "  PASS  " : "  FAIL  ");
        Console.ResetColor();
        Console.WriteLine(detail is null ? what : $"{what}  ({detail})");
        if (!ok) _failures++;
    }

    static async Task<Session> Login(string user)
    {
        var s = new Session();
        await s.OpenAsync(Url);
        var auth = await s.Hub.InvokeAsync<AuthResponse>("Login",
            new AuthRequest(user, user == "admin" ? "admin" : "test", GameConstants.ProtocolVersion), GameConstants.GameVersion);
        if (!auth.Success) throw new Exception($"login {user} failed: {auth.Error}");
        return s;
    }

    static async Task<(Session S, string Name)> FreshCharacter(string user, string prefix)
    {
        var s = await Login(user);
        string name = prefix + DateTime.UtcNow.ToString("HHmmssff");
        var err = await s.Hub.InvokeAsync<string?>("CreateCharacter", new CreateCharacterRequest(name, Race.Elf, BaseClass.Mage));
        if (err is not null) throw new Exception($"create {name}: {err}");
        var list = await s.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var r = await s.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(list.Characters.First(c => c.Name == name).Id));
        if (!r.Success) throw new Exception($"enter {name}: {r.Error}");
        s.MyId = r.EntityId;
        await s.Settle();
        return (s, name);
    }

    static int Count(Session s, string defId) => s.Inv?.Items.Where(i => i.DefId == defId).Sum(i => i.Quantity) ?? 0;

    static Task Cmd(Session s, string arg) => s.Hub.SendAsync("AdminCommand", "mentor", arg);

    static bool Said(Session s, string fragment) => s.SystemChat.Any(t => t.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    static int Rung(Session s) => s.Buffs?.Buffs.FirstOrDefault(b => b.Key == "mentor_aura")?.Level ?? 0;
    static bool Guided(Session s) => s.Buffs?.Buffs.Any(b => b.Key == "mentor_guidance") == true;
    static int Knowledge(Session s) => s.Buffs?.Buffs.FirstOrDefault(b => b.Key == "mentor_knowledge")?.Level ?? 0;
    static int Blessings(Session s) => s.Learned?.Skills.Count(k => SkillCatalog.IsMentorBlessing(k.Id)) ?? 0;
    static BuffDto? BuffOn(Session s, string key) => s.Buffs?.Buffs.FirstOrDefault(b => b.Key == key);

    public static async Task<int> RunAsync()
    {
        Console.WriteLine("\n=== BL-339 mentor smoke test ===\n");

        // ---- the mentor: Admin, ascended for the run ----
        var a = await Login("admin");
        var al = await a.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var ar = await a.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(al.Characters.First(c => c.Name == "Admin").Id));
        Check("Admin entered", ar.Success, ar.Error);
        await a.Settle();
        bool ascended = false;
        if (a.Subclasses?.Classes.FirstOrDefault(c => c.Slot == 0)?.FourthClass == 0)
        {
            await a.Hub.SendAsync("DebugFourthClass");
            ascended = await a.WaitFor(() => a.Subclasses?.Classes.FirstOrDefault(c => c.Slot == 0)?.FourthClass > 0);
            Check("Admin took a 4th class for the run", ascended);
        }
        int aBond0 = Count(a, ItemCatalog.BondCertificate), aGrad0 = Count(a, ItemCatalog.GraduationCertificate);

        // ---- the mentee asks, the mentor accepts ----
        var (m, mName) = await FreshCharacter("test2", "Mte");
        await Cmd(m, "invite Admin");
        Check("the mentee's request is sent", await m.WaitFor(() => Said(m, "You asked Admin to be your mentor")));
        Check("the mentor hears of it", await a.WaitFor(() => Said(a, $"{mName} asks you to be their mentor")));
        await Cmd(a, "list");
        Check("the mentor's list shows the request as Pending", await a.WaitFor(() => Said(a, $"{mName} Pending")));
        await Cmd(a, $"accept {mName}");
        Check("the bond is made", await m.WaitFor(() => Said(m, $"Admin is now the mentor of {mName}")));
        Check("the mentee has Mentor's Guidance at once (mentor online)", await m.WaitFor(() => Guided(m)));
        Check("a level-1 mentee gives the mentor no aura (1²/1800 rounds down)", Rung(a) == 0, $"rung {Rung(a)}");
        Check("the mentee now knows the eleven Mentor Blessings",
              await m.WaitFor(() => Blessings(m) == SkillCatalog.MentorBlessingIds.Length), $"{Blessings(m)}");

        // ---- milestones ----
        await a.Hub.SendAsync("AdminCommand", "lvl", $"{mName} 20");
        Check("level 20 pays the mentee 150 Bond Certificates",
              await m.WaitFor(() => Count(m, ItemCatalog.BondCertificate) == 150), $"{Count(m, ItemCatalog.BondCertificate)}");
        Check("…and the mentor 15", await a.WaitFor(() => Count(a, ItemCatalog.BondCertificate) == aBond0 + 15),
              $"{Count(a, ItemCatalog.BondCertificate) - aBond0}");

        // `/lvl` banks no exp, so the mentee is ONLINE BUT AFK here — his fourth pass: that still feeds the aura.
        await a.Hub.SendAsync("AdminCommand", "lvl", $"{mName} 60");
        Check("level 40 on the way to 60 pays the mentee 300 more",
              await m.WaitFor(() => Count(m, ItemCatalog.BondCertificate) == 450), $"{Count(m, ItemCatalog.BondCertificate)}");
        Check("…and the mentor 30 more", await a.WaitFor(() => Count(a, ItemCatalog.BondCertificate) == aBond0 + 45),
              $"{Count(a, ItemCatalog.BondCertificate) - aBond0}");
        Check("an AFK level-60 mentee still gives Mentor Aura Lv.2 (60²/1800 = 2.0)", await a.WaitFor(() => Rung(a) == 2), $"rung {Rung(a)}");
        Check("…but no Mentor Knowledge (that needs an ACTIVE mentee)", Knowledge(a) == 0, $"knowledge {Knowledge(a)}");
        await Cmd(a, "list");
        Check("the mentor's list row reads 'Name Online (60) 0m'", await a.WaitFor(() => Said(a, $"{mName} Online (60) 0m")));

        await a.Hub.SendAsync("AdminCommand", "exp", $"{mName} 1");   // exp gained = an ACTIVE mentee
        await a.Settle();
        await a.Hub.SendAsync("AdminCommand", "lvl", $"{mName} 61");  // a level-up re-checks at once
        Check("one ACTIVE mentee = Mentor Knowledge Lv.1, whatever his level", await a.WaitFor(() => Knowledge(a) == 1),
              $"knowledge {Knowledge(a)}");
        Check("…and the aura is unchanged (61²/1800 = 2.07)", Rung(a) == 2, $"rung {Rung(a)}");

        // ---- the blessings: self-only, an hour, in the original's family ----
        await m.Hub.SendAsync("UseSkill", "mentor_feral_precision", m.MyId);
        Check("Mentor Blessing: Feral Precision lands in Feral Precision's family, for an hour",
              await m.WaitFor(() => BuffOn(m, SkillCatalog.WcFeralPrecision)?.SecondsLeft > 3000),
              $"{BuffOn(m, SkillCatalog.WcFeralPrecision)?.SecondsLeft:0}s");
        await m.Hub.SendAsync("UseSkill", "mentor_harmony_warrior", m.MyId);
        Check("Mentor Blessing: Harmony of the Warrior lands as harmony_warrior, for an hour",
              await m.WaitFor(() => BuffOn(m, "harmony_warrior")?.SecondsLeft > 3000),
              $"{BuffOn(m, "harmony_warrior")?.SecondsLeft:0}s");
        await m.Hub.SendAsync("UseSkill", "mentor_war_frenzy", m.MyId);
        Check("Mentor Blessing: War Frenzy lands as its Frenzy rung (one-child wrapper), for an hour",
              await m.WaitFor(() => BuffOn(m, "frenzy")?.SecondsLeft > 3000),
              string.Join(" | ", m.Buffs?.Buffs.Select(b => $"{b.Key}/{b.SourceSkillId}/{b.SecondsLeft:0}") ?? Array.Empty<string>()));

        // ---- graduation ----
        await a.Hub.SendAsync("AdminCommand", "lvl", $"{mName} 76");
        Check("graduation pays the mentee 550 (1000 in all)",
              await m.WaitFor(() => Count(m, ItemCatalog.BondCertificate) == 1000), $"{Count(m, ItemCatalog.BondCertificate)}");
        Check("…the mentor 55 Bond (100 in all)", await a.WaitFor(() => Count(a, ItemCatalog.BondCertificate) == aBond0 + 100),
              $"{Count(a, ItemCatalog.BondCertificate) - aBond0}");
        Check("…and 10 Graduation Certificates (held at 20 and 40)",
              await a.WaitFor(() => Count(a, ItemCatalog.GraduationCertificate) == aGrad0 + 10),
              $"{Count(a, ItemCatalog.GraduationCertificate) - aGrad0}");
        Check("the bond ends: the mentee loses Mentor's Guidance", await m.WaitFor(() => !Guided(m)));
        Check("…and the mentor's aura and Knowledge go with it", await a.WaitFor(() => Rung(a) == 0 && Knowledge(a) == 0),
              $"rung {Rung(a)}, knowledge {Knowledge(a)}");
        Check("…and the Mentor Blessings leave the graduate's skill list", await m.WaitFor(() => Blessings(m) == 0),
              $"{Blessings(m)}");
        Check("the certificates cannot be traded", ItemCatalog.Get(ItemCatalog.BondCertificate)?.Tradable == false);
        await m.LeaveWorldAsync();
        await m.DisposeAsync();

        // ---- remove + penalty, on a throwaway mentee ----
        var (r, rName) = await FreshCharacter("test3", "Rmv");
        await Cmd(a, $"invite {rName}");
        Check("the mentor's invitation reaches the player", await r.WaitFor(() => Said(r, "Admin invites you to be their mentee")));
        await Cmd(r, "list");
        Check("the invitee's list shows Admin as Pending with an activity figure",
              await r.WaitFor(() => Said(r, "Admin Pending Online") && Said(r, "/7d (")));
        await Cmd(r, "accept Admin");
        Check("second bond made", await r.WaitFor(() => Said(r, $"Admin is now the mentor of {rName}")));
        await Cmd(r, "remove");
        Check("removing an ONLINE mentor costs the mentee 24h",
              await r.WaitFor(() => Said(r, "is no longer your mentor") && Said(r, "for 24h")));
        Check("…and takes the Mentor Blessings with the bond", await r.WaitFor(() => Blessings(r) == 0), $"{Blessings(r)}");
        await Cmd(r, "invite Admin");
        Check("the penalty refuses the next bond",
              await r.WaitFor(() => Said(r, "You are under a bond penalty for 24h. Bond cannot be made.")));
        await r.LeaveWorldAsync();
        await r.DisposeAsync();

        if (ascended) await a.Hub.SendAsync("DebugFourthClass");   // put Admin back as the seed made him
        await a.Settle();
        await a.LeaveWorldAsync();
        await a.DisposeAsync();

        Console.WriteLine(_failures == 0 ? "\nALL PASS\n" : $"\n{_failures} FAILURE(S)\n");
        return _failures == 0 ? 0 : 1;
    }
}
