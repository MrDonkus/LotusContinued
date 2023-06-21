﻿using System.Linq;
using Lotus.API;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.NeutralKilling;
using UnityEngine;

namespace Lotus.Roles.Subroles.Romantics;

public class RuthlessRomantic: NeutralKillingBase
{
    [UIComponent(UI.Cooldown)]
    private Cooldown cooldown;
    private bool usesPetToKill;

    protected override void PostSetup()
    {
        usesPetToKill = !MyPlayer.GetVanillaRole().IsImpostor();
        cooldown.SetDuration(GetOverride(Override.KillCooldown)?.GetValue() as float? ?? AUSettings.KillCooldown());
    }

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(RoleActionType.OnPet)]
    public void EnablePetKill()
    {
        if (!usesPetToKill || cooldown.NotReady()) return;
        PlayerControl? target = MyPlayer.GetPlayersInAbilityRangeSorted().FirstOrDefault();
        if (target == null) return;
        cooldown.Start();
        MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(new FatalIntent(), this));
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleFlags(RoleFlag.VariationRole)
            .RoleColor(new Color(0.23f, 0f, 0.24f, 0.98f));
}