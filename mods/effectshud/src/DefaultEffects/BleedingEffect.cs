using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace effectshud.src.DefaultEffects
{
    public class BleedingEffect : Effect
    {
        public float hpPerTick;
        public BleedingEffect()
        {
            effectTypeId = "bleeding";
        }
        public BleedingEffect(int ticks = 20, float hpPerTick = 0.08f, int tier = 1, bool infinite = false) : base(tier, infinite)
        {
            SetExpiryInTicks(ticks);
            this.hpPerTick = hpPerTick * tier;
            effectTypeId = "bleeding";
        }
        public override void OnTick()
        {
            entity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.PiercingAttack
            }, hpPerTick);
        }
        public override void OnStack(Effect otherEffect)
        {
            base.OnStack(otherEffect);
        }
    }
}
