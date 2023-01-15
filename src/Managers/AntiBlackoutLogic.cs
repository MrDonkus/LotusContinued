using System;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Roles;
using VentLib.Logging;

namespace TownOfHost.Managers;

public static class AntiBlackoutLogic
{


    public static void PatchedDataLogic()
    {
        VentLogger.Debug("Patching GameData", "AntiBlackout");
        int aliveCrew = GameStats.CountRealCrew();
        int aliveImpostors = GameStats.CountRealImpostors();

        if (AntiBlackout.FakeExiled != null)
        {
            if (AntiBlackout.FakeExiled.GetCustomRole().RealRole.IsImpostor()) aliveImpostors--;
            else aliveCrew--;
        }

        VentLogger.Debug($"Real Crew: {aliveCrew} | Real Impostors: {aliveImpostors}", "AntiBlackout");
        //if (aliveCrew > aliveImpostors) AntiBlackoutManager.RestoreIsDead();
        GameData.PlayerInfo[] allPlayers = GameData.Instance.AllPlayers.ToArray();

        foreach (PlayerControl player in Game.GetAllPlayers())
        {
            int localImpostors = aliveImpostors;
            /*CustomRole playerRole = player.GetCustomRole();
            if (playerRole.IsDesyncRole() && playerRole.RealRole.IsImpostor())
                localImpostors = Math.Max(localImpostors, playerRole.Factions.GetAllies().Count);*/
            ReviveEveryone();
            VentLogger.Trace($"Patching for {player.GetRawName()}");
            foreach (var info in allPlayers.Where(p => AntiBlackout.FakeExiled != p))
            {
                if (localImpostors < aliveCrew) continue;
                if (player.PlayerId == info.PlayerId) continue;

                if (info.Object.GetCustomRole().IsCrewmate()) continue;
                if (info.Object.IsHost())
                {
                    VentLogger.Trace($"Set {info.Object.GetRawName()} => isDead = true");
                    info.IsDead = true;
                }
                else
                {
                    VentLogger.Trace($"Set {info.Object.GetRawName()} => Disconnected = true");
                    info.Disconnected = true;
                }
                VentLogger.Trace($"Local Impostors {localImpostors} => {localImpostors - 1}");
                localImpostors--;
            }
            AntiBlackout.SendGameData(player.GetClientId());
        }
    }

    private static void ReviveEveryone() {
        foreach (var info in GameData.Instance.AllPlayers)
        {
            info.IsDead = false;
            info.Disconnected = false;
        }
    }

    public static bool IsFakeable(GameData.PlayerInfo? checkedPlayer)
    {
        if (checkedPlayer == null || checkedPlayer.Object == null) return false;
        int aliveCrew = GameStats.CountRealCrew();
        int aliveImpostors = GameStats.CountRealImpostors();

        if (checkedPlayer.Object.GetCustomRole().RealRole.IsImpostor()) aliveImpostors -= 1;
        else aliveCrew -= 1;

        return aliveCrew > aliveImpostors && aliveImpostors != 0;
    }
}