using Hazel;
using Lotus.Extensions;
using Lotus.Patches.Actions;
using Lotus.Server;
using VentLib.Networking.RPC;
using VentLib.Utilities;

namespace Lotus.API;

public class ProtectedRpc
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ProtectedRpc));

    public static void CheckMurder(PlayerControl killer, PlayerControl target)
    {
        ProjectLotus.ServerPatchManager.Amalgamate.Execute(PatchedCode.CheckMurder, killer, target);
    }

    internal static class Unpatched
    {
        private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Unpatched));

        internal static void CheckMurder(PlayerControl killer, PlayerControl target)
        {
            log.Trace("Protected Check Murder", "ProtectedRpc::CheckMurder");
            if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost) return;
            if (target == null) return;
            GameData.PlayerInfo data = target.Data;
            if (data == null) return;
            if (!MurderPatches.Lock(killer.PlayerId)) return;

            if (MeetingHud.Instance != null)
            {
                killer.RpcVaporize(target);
                RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer).Write(target).Send(target.GetClientId());
                return;
            }

            if (AmongUsClient.Instance.AmHost) killer.MurderPlayer(target);
            RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer).Write(target).Send();
            target.Data.IsDead = true;
        }
    }

    internal static class ProtectionPatch
    {
        internal static void CheckMurder(PlayerControl killer, PlayerControl target)
        {
            log.Trace("Protected Check Murder", "ProtectedRpc::CheckMurder");
            if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost) return;
            if (target == null) return;
            GameData.PlayerInfo data = target.Data;
            if (data == null) return;

            if (MeetingHud.Instance != null)
            {
                killer.RpcVaporize(target);
                RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer, SendOption.None).Write(target).Send(target.GetClientId());
                return;
            }

            if (target.protectedByGuardian)
            {
                target.RemoveProtection();
                target.protectedByGuardian = false;
                RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer, SendOption.None).Write(target).Send();
            }

            RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer, SendOption.None).Write(target).Send();
            if (AmongUsClient.Instance.AmHost) killer.MurderPlayer(target);

            target.Data.IsDead = true;
        }
    }
}