using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace effectshud.src.DefaultEffects
{
    public class VampirismEffect: Effect
    {
        public float percentHealPerDamage = 0.10f;
        public VampirismEffect()
        {
            this.effectTypeId = "vampirism";
        }
        public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
        {
            var c = 2;
            //ource.
            //if(targetEntity != null)
        }
    }
}
