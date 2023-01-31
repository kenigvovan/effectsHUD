using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace effectshud.src.DefaultEffects
{
    public class MiningSlowEffect : Effect
    {
        public float statChangeValue = -0.25f;
        public MiningSlowEffect()
        {
            effectTypeId = "miningslow";
        }
        public MiningSlowEffect(int tier = 1, float statChangeValue = -0.25f, bool infinite = false):base(tier, infinite)
        {
            SetExpiryInRealMinutes(1);
            this.statChangeValue = statChangeValue;
            effectTypeId = "miningslow";
        }
        public override void OnStart()
        {
            entity.Stats.Set("miningSpeedMul", "effectshudminingslow", statChangeValue * tier, true);
        }
        public override void OnStack(Effect otherEffect)
        {
            if (this.tier > otherEffect.tier)
            {
                return;
            }
            if (this.tier == otherEffect.tier)
            {
                this.ExpireTick = otherEffect.ExpireTick;
                this.TickCounter = otherEffect.TickCounter;
                return;
            }
            this.tier = otherEffect.tier;
            entity.Stats.Set("miningSpeedMul", "effectshudminingslow", statChangeValue * tier, true);
            this.ExpireTick = otherEffect.ExpireTick;
            this.TickCounter = otherEffect.TickCounter;
        }
        public override void OnExpire()
        {
            entity.Stats.Set("miningSpeedMul", "effectshudminingslow", 0);
        }
        public override void OnDeath()
        {
            entity.Stats.Set("miningSpeedMul", "effectshudminingslow", 0);
            EBEffectsAffected ebea = entity.GetBehavior<EBEffectsAffected>();
            if (ebea == null)
            {
                return;
            }
            ebea.activeEffects.Remove(this.effectTypeId);
        }
    }
}
