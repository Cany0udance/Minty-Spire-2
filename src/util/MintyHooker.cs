using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace MintySpire2.util;

public class MintyHooker : AbstractModel
{
    public override bool ShouldReceiveCombatHooks => true;

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        RestHPRender.CatchHPChange(creature);
        return Task.CompletedTask;
    }
}