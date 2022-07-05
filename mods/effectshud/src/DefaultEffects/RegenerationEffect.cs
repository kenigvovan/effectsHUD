using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace effectshud.src.DefaultEffects
{
    public class RegenerationEffect: Effect
    {
        public float hpPerTick = 0.1f;
        public RegenerationEffect()
        {
            effectTypeId = "regeneration";
        }
        public RegenerationEffect(int ticks = 20, float hpPerTick = 0.08f, int tier = 1, bool infinite = false) : base(tier, infinite)
        {        
            SetExpiryInTicks(ticks);
            this.hpPerTick = hpPerTick * tier;
            effectTypeId = "regeneration";
        }
        public override void OnTick()
        {           
            entity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Heal
            }, hpPerTick * tier);
        }
        public override void OnStack(Effect otherEffect)
        {
            base.OnStack(otherEffect);
        }
       
    }
}
