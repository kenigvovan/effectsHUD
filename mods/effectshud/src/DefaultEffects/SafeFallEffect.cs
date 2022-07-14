using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace effectshud.src.DefaultEffects
{
    public class SafeFallEffect: Effect
    {

        public SafeFallEffect()
        {
            effectTypeId = "safefall";
        }
        public override void OnShouldEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            if(damageSource.Type == EnumDamageType.Gravity)
            {
                damage = 0;
            }
        }
    }
}
