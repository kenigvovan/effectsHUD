using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace effectshud.src.DefaultEffects
{
    public class TemporalChargeEffect: Effect
    {
        public float statChangeValue = 0.15f;
        public TemporalChargeEffect()
        {
            effectTypeId = "cantemporalcharge";
        }
        public TemporalChargeEffect(int minutes = 1, float statChangeValue = 0.1f, int tier = 1, bool infinite = false) : base(tier, infinite)
        {
            this.statChangeValue = statChangeValue;
            SetExpiryInRealMinutes(minutes);
            effectTypeId = "cantemporalcharge";
        }
        public override void OnStart()
        {
            entity.Stats.Set("cantemporalcharge", "effectshudtemporalcharge", statChangeValue * tier);
        }

        public override void OnExpire()
        {
            entity.Stats.Set("cantemporalcharge", "effectshudtemporalcharge", 0);
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
            entity.Stats.Set("cantemporalcharge", "effectshudtemporalcharge", statChangeValue * tier);
            this.ExpireTick = otherEffect.ExpireTick;
            this.TickCounter = otherEffect.TickCounter;
        }
    }
}
