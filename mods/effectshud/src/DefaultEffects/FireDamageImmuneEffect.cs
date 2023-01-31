using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace effectshud.src.DefaultEffects
{
    public class FireDamageImmuneEffect: Effect
    {
        public FireDamageImmuneEffect()
        {
            this.effectTypeId = "firedamageimmune";
        }
        public override void OnShouldEntityReceiveDamage(ref float damage, DamageSource dmgSource)
        {
            if(dmgSource.Type == EnumDamageType.Fire)
            {
                damage = 0;
            }
        }
    }
}
