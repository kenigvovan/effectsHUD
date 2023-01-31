using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace effectshud.src
{
    /*class ProductConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {

            return (objectType == typeof(Effect));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
           return serializer.Deserialize(reader, typeof(HealEffect));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value, typeof(HealEffect));
        }
    }*/
    public class EBEffectsAffected : EntityBehavior
    {
        public Dictionary<string, Effect> activeEffects = new Dictionary<string, Effect>();
        public Dictionary<string, EffectClientData> onlyClientsActiveEffects = new Dictionary<string, EffectClientData>();
        List<string> effectsToRemove = new List<string>();
        ITreeAttribute effectsTree;
        JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        public bool needUpdate { get; set; } = false;
        float accum = 0;
        public void serialize()
        {
            List<SerializedEffect> sel = new List<SerializedEffect>();
            foreach(var it in activeEffects)
            {
                sel.Add(new SerializedEffect { typeId = it.Key, data = JsonConvert.SerializeObject(it.Value) });
            }
            effectsTree.SetString("activeEffectsData", JsonConvert.SerializeObject(sel));
            entity.WatchedAttributes.MarkPathDirty("activeEffects");
        }
        public void deserialize()
        {
            if (effectsTree.HasAttribute("activeEffectsData"))
            {
                var tmp = JsonConvert.DeserializeObject<List<SerializedEffect>>(effectsTree.GetString("activeEffectsData"));
                foreach(var it in tmp)
                {
                    effectshud.effects.TryGetValue(it.typeId, out Type ourType);
                    if(activeEffects.TryGetValue(it.typeId, out _))
                    {
                       var tmpE = JsonConvert.DeserializeObject(it.data, ourType) as Effect;
                        if (tmpE == null)
                            continue;
                        else
                            activeEffects[it.typeId] = tmpE;
                    }
                    else
                    {
                        var tmpE = JsonConvert.DeserializeObject(it.data, ourType) as Effect;
                        if (tmpE == null)
                            continue;
                        activeEffects.Add(it.typeId, tmpE);
                    }
                    
                }
                
            }
            foreach (var it in activeEffects.Values)
            {
                it.entity = entity;
            }
        }
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            if (entity.Api.Side == EnumAppSide.Client)
            {
                return;
            }
           
            base.Initialize(properties, attributes);
            effectsTree = entity.WatchedAttributes.GetTreeAttribute("activeEffects");
            
            if (effectsTree == null)
            {
                entity.WatchedAttributes.SetAttribute("activeEffects", effectsTree = new TreeAttribute());
                serialize();
            }
            else
            {
                deserialize();
            }
            entity.GetBehavior<EntityBehaviorHealth>().onDamaged += OnShouldEntityReceiveDamage;
            //SendActiveEffectsToClient(null);

        }
        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            base.OnEntityDespawn(despawn);
            if (entity.Api.Side == EnumAppSide.Client)
            {
                return;
            }
            serialize();
        }
        public EBEffectsAffected(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "affectedByEffects";
        }
        internal double Now { get { return entity.Api.World.Calendar.TotalDays; } }
        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);
            
             if (entity.Api.Side == EnumAppSide.Server) {
                double now = Now;
                accum += deltaTime;
                if (accum > Config.Current.TICK_EVERY_SECONDS.Val)
                {
                    accum = 0;
                    
                    foreach (var effect in activeEffects)
                    {
                        if (effect.Value.ExpireTimestampInDays < now || effect.Value.ExpireTick <= effect.Value.TickCounter)
                        {
                            effectsToRemove.Add(effect.Key);
                            effect.Value.OnExpire();
                        }
                        else
                        {
                            if (!effect.Value.infinite)
                            {
                                effect.Value.TickCounter++;
                            }                     
                            effect.Value.OnTick();
                        }
                    }
                    if (effectsToRemove.Count > 0)
                    {
                        foreach (var it in effectsToRemove)
                        {
                            activeEffects.Remove(it);
                        }
                        SendActiveEffectsToClient(effectsToRemove);
                        effectsToRemove.Clear();
                        //SendActiveEffectsToClient();
                    }

                    

                }
            }
        }
        
        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            
            foreach (var it in activeEffects.Values.ToArray())
            {
                it.OnDeath();
            }

            if (needUpdate)
            {
                SendActiveEffectsToClient(null);
            }
            needUpdate = false;
            //base.OnEntityDeath(damageSourceForDeath);
            //remove effects which not stay after death
        }

        public void SendActiveEffectsToClient(List<string> effectsTypeIdsToRemove, Effect ef = null)
        {
            List<EffectClientData> effectData = new List<EffectClientData>();
            if (ef != null)
            {
                effectData.Add(new EffectClientData { typeId = ef.effectTypeId, duration = ef.ExpireTimestampInDays == double.PositiveInfinity ? (ef.ExpireTick - ef.TickCounter) : (int)(ef.ExpireTimestampInDays * 24 * 60 * 60), tier = ef.tier, infinite = ef.infinite });
            }
            else
            {
                foreach (var it in activeEffects.Values)
                {
                    effectData.Add(new EffectClientData { typeId = it.effectTypeId, duration = it.ExpireTimestampInDays == double.PositiveInfinity ? (it.ExpireTick - it.TickCounter) : (int)(it.ExpireTimestampInDays * 24 * 60 * 60), tier = it.tier, infinite = it.infinite });
                }
            }
            var packetToSend = new EffectsSyncPacket()
            {
                playerUID = (entity as EntityPlayer).PlayerUID,
                currentEffectsData = JsonConvert.SerializeObject(effectData),
                typeIdsToRemove = effectsTypeIdsToRemove == null ? null : new HashSet<string>(effectsTypeIdsToRemove)
            };
            var f = effectshud.sapi.World.GetPlayersAround(entity.ServerPos.XYZ, 15000, 15000);
            foreach (var it in f)
            {
                effectshud.serverChannel.SendPacket(packetToSend, it as IServerPlayer);
            }
            if (!f.Contains((entity as EntityPlayer).Player)){
                effectshud.serverChannel.SendPacket(packetToSend, (entity as EntityPlayer).Player as IServerPlayer);
            }
        }
        public bool AddEffect(Effect ef)
        {
            if (activeEffects.TryGetValue(ef.effectTypeId, out Effect oldEffect))
            {
                oldEffect.OnStack(ef);
            }
            else
            {
                ef.entity = entity;
                activeEffects.Add(ef.effectTypeId, ef);
                ef.OnStart();
            }
            //no need to send info about an instant effect to client
            if (ef.ExpireTick != 0)
            {
                SendActiveEffectsToClient(null, ef);
            }
            //effectshud.sapi.Network.bro
            return true;
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
        {
            var c = 2;
        }

        public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
        {
            foreach (var it in activeEffects.Values)
            {
                it.DidAttack(source, targetEntity, ref handled);
            }
            if (needUpdate)
            {
                SendActiveEffectsToClient(null);
            }
            needUpdate = false;
        }

        public float OnShouldEntityReceiveDamage(float damage, DamageSource dmgSource)
        {
            foreach (var it in activeEffects.Values)
            {
                it.OnShouldEntityReceiveDamage(ref damage, dmgSource);
            }
            if (needUpdate)
            {
                SendActiveEffectsToClient(null);
            }
            needUpdate = false;
            return damage;
        }

        public override void OnEntityRevive()
        {
            foreach (var it in activeEffects.Values)
            {
                it.OnRevive();
            }
            if (needUpdate)
            {
                SendActiveEffectsToClient(null);
            }
            needUpdate = false;
        }
    }
}
