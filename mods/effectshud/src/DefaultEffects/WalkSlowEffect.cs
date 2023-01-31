using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace effectshud.src.DefaultEffects
{
    public class WalkSlowEffect : Effect
    {
        public float statChangeValue = -0.25f;
        public WalkSlowEffect()
        {
            effectTypeId = "walkslow";
        }
        public WalkSlowEffect(int tier = 1, float statChangeValue = -0.25f, bool infinite = false) : base(tier, infinite)
        {
            SetExpiryInRealMinutes(1 * tier);

            effectTypeId = "walkslow";
        }
        public override void OnStart()
        {
            entity.Stats.Set("walkspeed", "effectshudwalkslow", statChangeValue * tier);
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
            entity.Stats.Set("walkspeed", "effectshudwalkslow", statChangeValue * tier);
            this.ExpireTick = otherEffect.ExpireTick;
            this.TickCounter = otherEffect.TickCounter;
        }
        public override void OnExpire()
        {
            entity.Stats.Set("walkspeed", "effectshudwalkslow", 0);
        }
        public override void OnDeath()
        {
            entity.Stats.Set("walkspeed", "effectshudwalkslow", 0);
            EBEffectsAffected ebea = entity.GetBehavior<EBEffectsAffected>();
            if (ebea == null)
            {
                return;
            }
            ebea.activeEffects.Remove(this.effectTypeId);
        }
    }
}
