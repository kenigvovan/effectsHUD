using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace effectshud.src
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public abstract class Effect
    {
        public int TickCounter = 0;
        public double ExpireTimestampInDays = 0;
        public int ExpireTick = 0;
        public string effectTypeId;
        internal Entity entity;
        public bool infinite = false;
        public int tier = 1;
        public Effect(int tier = 1, bool infinite = false)
        {
            this.tier = tier;
            this.infinite = infinite;
        }
        public virtual void OnStart() { }
        
        public virtual void OnStack(Effect otherEffect) 
        {
            if(this.tier > otherEffect.tier)
            {
                return;
            }
            if(this.tier == otherEffect.tier)
            {
                this.ExpireTick = otherEffect.ExpireTick;
                this.TickCounter = otherEffect.TickCounter;
                return;
            }
            this.tier = otherEffect.tier;
            this.ExpireTick = otherEffect.ExpireTick;
            this.TickCounter = otherEffect.TickCounter;          
        }
       
        public virtual void OnExpire() { }
    
        
        public virtual void OnTick() { }
       
        public virtual void OnLeave() { }
        
        public virtual void OnJoin() { }
      
        public void SetExpiryInGameDays(double deltaDays)
        {
            ExpireTimestampInDays = effectshud.Now + deltaDays;
            ExpireTick = Int32.MaxValue;
        }

        public void SetExpiryInGameHours(double deltaHours)
        {
            ExpireTimestampInDays = effectshud.Now + deltaHours / 24.0;
            ExpireTick = Int32.MaxValue;
        }

        public void SetExpiryInGameMinutes(double deltaMinutes)
        {
            ExpireTimestampInDays = effectshud.Now + deltaMinutes / 24.0 / 60.0;
            ExpireTick = Int32.MaxValue;
        }

        public void SetExpiryInTicks(int deltaTicks)
        {
            ExpireTick = TickCounter + deltaTicks;
            ExpireTimestampInDays = double.PositiveInfinity;
        }

        public void SetExpiryInRealSeconds(int deltaSeconds)
        {
            SetExpiryInTicks((int)Math.Ceiling(deltaSeconds / Config.Current.TICK_EVERY_SECONDS.Val));
        }

        public void SetExpiryInRealMinutes(int deltaMinutes)
        {
            SetExpiryInRealSeconds(deltaMinutes * 60);
        }

        /*public void SetExpiryNever()
        {
            ExpireTimestampInDays = double.PositiveInfinity;
            ExpireTick = Int32.MaxValue;
        }*/

        public void SetExpiryImmediately()
        {
            ExpireTimestampInDays = 0;
        }
        
        public void Apply(Entity entity)
        {
            if(entity == null)
            {
                throw new Exception("Target entity for effect is null");
            }
            EBEffectsAffected ebea = entity.GetBehavior<EBEffectsAffected>();
            if(ebea == null)
            {
                return;
            }
        }
     
        public void Remove()
        {
            
           // BuffManager.RemoveBuff(entity, this);
        }
        public virtual void OnDeath()
        {
            EBEffectsAffected ebea = entity.GetBehavior<EBEffectsAffected>();
            if (ebea == null)
            {
                return;
            }
            ebea.activeEffects.Remove(this.effectTypeId);
        }
    }
}
