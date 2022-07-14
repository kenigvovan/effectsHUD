﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace effectshud.src.DefaultEffects
{
    public class WalkSpeedEffect: Effect
    {
        public float statChangeValue = 0.25f;
        public WalkSpeedEffect()
        {
            effectTypeId = "walkspeed";
        }
        public WalkSpeedEffect(int tier = 1, float statChangeValue = 0.25f, bool infinite = false) : base(tier, infinite)
        {
            SetExpiryInRealMinutes(1 * tier);
            this.statChangeValue = statChangeValue;
            effectTypeId = "walkspeed";
        }
        public override void OnStart()
        {
            entity.Stats.Set("walkspeed", "effectshudwalkspeed", statChangeValue * tier);
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
            entity.Stats.Set("walkspeed", "effectshudwalkspeed", statChangeValue * tier);
            
            this.ExpireTick = otherEffect.ExpireTick;
            this.TickCounter = otherEffect.TickCounter;
        }
        public override void OnExpire()
        {
            entity.Stats.Set("walkspeed", "effectshudwalkspeed", 0f);
        }
        public override void OnDeath()
        {
            entity.Stats.Set("walkspeed", "effectshudwalkspeed", 0f);
            EBEffectsAffected ebea = entity.GetBehavior<EBEffectsAffected>();
            if (ebea == null)
            {
                return;
            }
            ebea.activeEffects.Remove(this.effectTypeId);
        }
    }
}
