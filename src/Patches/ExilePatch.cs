using System.Linq;
using AmongUs.Data;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Roles;
using VentLib.Logging;

namespace TownOfHost.Patches;

static class ExileControllerWrapUpPatch
{
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    class BaseExileControllerPatch
    {
        public static void Postfix(ExileController __instance)
        {
            try
            {
                WrapUpPostfix(__instance.exiled);
            }
            finally
            {
                WrapUpFinalizer();
            }
        }
    }

    [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
    class AirshipExileControllerPatch
    {
        public static void Postfix(AirshipExileController __instance)
        {
            try
            {
                WrapUpPostfix(__instance.exiled);
            }
            finally
            {
                WrapUpFinalizer();
            }
        }
    }
    static void WrapUpPostfix(GameData.PlayerInfo exiled)
    {
        if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
        if (exiled != null)
        {
            //霊界用暗転バグ対処
            if (!AntiBlackout.OverrideExiledPlayer && TOHPlugin.ResetCamPlayerList.Contains(exiled.PlayerId))
                exiled.Object?.ResetPlayerCam(1f);


            ActionHandle selfExiledHandle = ActionHandle.NoInit();
            ActionHandle otherExiledHandle = ActionHandle.NoInit();
            exiled.Object.Trigger(RoleActionType.SelfExiled, ref selfExiledHandle);
            Game.TriggerForAll(RoleActionType.OtherExiled, ref otherExiledHandle, exiled);
        }
        FallFromLadder.Reset();
    }

    static void WrapUpFinalizer()
    {
        if (AmongUsClient.Instance.AmHost) DTask.Schedule(() => {
            GameData.PlayerInfo? exiled = AntiBlackout.ExiledPlayer;
            AntiBlackout.LoadCosmetics();
            AntiBlackout.RestoreIsDead(doSend: true);
            if (AntiBlackout.OverrideExiledPlayer && exiled?.Object != null)
                exiled.Object.RpcExileV2();
        }, GameStats.DeriveDelay(1.2f));

        GameStates.AlreadyDied |= GameData.Instance.AllPlayers.ToArray().Any(x => x.IsDead);
        RemoveDisableDevicesPatch.UpdateDisableDevices();
        SoundManager.Instance.ChangeMusicVolume(DataManager.Settings.Audio.MusicVolume);
        VentLogger.Old("タスクフェイズ開始", "Phase");

        AntiBlackout.LoadCosmetics();
        AntiBlackout.FakeExiled = null;
    }
}

[HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
class PolusExileHatFixPatch
{
    public static void Prefix(PbExileController __instance)
    {
        __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
    }
}