using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace effectshud.src.DefaultEffects
{
    public class FireDamageImmuneEffect: Effect
    {
        public FireDamageImmuneEffect()
        {
            this.effectTypeId = "firedamageimmune";
        }
        public override void OnShouldEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            if(damageSource.Type == EnumDamageType.Fire)
            {
                damage = 0;
                return;
            }
        }
    }
}
