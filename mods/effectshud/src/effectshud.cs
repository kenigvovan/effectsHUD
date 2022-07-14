using effectshud.src.DefaultEffects;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace effectshud.src
{
    public class effectshud: ModSystem
    {
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        public static Harmony harmonyInstance;
        public const string harmonyID = "effectshud.Patches";
        public static List<TrackedEffect> trackedEffects = new List<TrackedEffect>();
        public static Dictionary<string, Type> effects = new Dictionary<string, Type>();
        public static bool showHUD = true;
        internal static IClientNetworkChannel clientChannel;
        public static Dictionary<string, EffectClientData> clientsActiveEffects = new Dictionary<string, EffectClientData>();
        HUDEffects effectsHUD;
        public static Dictionary<string, AssetLocation[]> effectsPictures = new Dictionary<string, AssetLocation[]>();
        internal static IServerNetworkChannel serverChannel;
        public static bool redrawEffectPictures = true;
        public static HashSet<string> invisiblePlayers = new HashSet<string>();
        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            base.StartClientSide(api);
            api.Gui.RegisterDialog((GuiDialog)new HUDEffects((ICoreClientAPI)api));
            harmonyInstance = new Harmony(harmonyID);
            api.Input.RegisterHotKey("effectsghud", "Show effects hud", GlKeys.L, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("effectsghud", new ActionConsumable<KeyCombination>(this.OnHotKeySkillDialog));
           
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.GuiDialogWorldMap).GetMethod("OnGuiClosed"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Postfix_Map_OnGuiClosed")));
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.GuiDialogWorldMap).GetMethod("OnGuiOpened"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Postfix_Map_OnGuiOpened")));
            
            harmonyInstance.Patch(typeof(Vintagestory.Client.NoObf.HudElementCoordinates).GetMethod("OnGuiClosed"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Postfix_CoordsHUD_OnGuiClosed")));
            harmonyInstance.Patch(typeof(Vintagestory.Client.NoObf.HudElementCoordinates).GetMethod("OnGuiOpened"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Postfix_CoordsHUD_OnGuiOpened")));

            harmonyInstance.Patch(typeof(Vintagestory.GameContent.EntityShapeRenderer).GetMethod("BeforeRender"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_BeforeRender")));
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.EntityShapeRenderer).GetMethod("DoRender3DOpaque"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_DoRender3DOpaque")));
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.EntityShapeRenderer).GetMethod("DoRender3DOpaqueBatched"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_DoRender3DOpaqueBatched")));
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.EntityShapeRenderer).GetMethod("DoRender2D"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_DoRender2D")));
            harmonyInstance.Patch(typeof(Vintagestory.Server.ServerPackets).GetMethod("GetFullEntityPacket"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_GetFullEntityPacket")));
            //harmonyInstance.Patch(typeof(Vintagestory.GameContent.EntitySkinnableShapeRenderer).GetMethod("TesselateShape"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_TesselateShape")));
            api.RegisterEntityBehaviorClass("affectedByEffects", typeof(EBEffectsAffected));
            clientChannel = api.Network.RegisterChannel("effectshud");
            clientChannel.RegisterMessageType(typeof(EffectsSyncPacket));
            clientChannel.SetMessageHandler<EffectsSyncPacket>((packet) =>
            {
                var player = capi.World.PlayerByUid(packet.playerUID);
                if(player?.Entity != null)
                {
                    var ebef = player.Entity.GetBehavior<EBEffectsAffected>();
                    if(ebef != null)
                    {
                        if (packet.currentEffectsData != null)
                        {
                            
                            foreach (var it in JsonConvert.DeserializeObject<List<EffectClientData>>(packet.currentEffectsData))
                            {
                                if(it.typeId.Equals("invisibility"))
                                {
                                    invisiblePlayers.Add(packet.playerUID);
                                }
                                if (ebef.onlyClientsActiveEffects.TryGetValue(it.typeId, out EffectClientData ecd))
                                {
                                    ecd = it;
                                }
                                else
                                {
                                    ebef.onlyClientsActiveEffects[it.typeId] = it;
                                }
                            }
                        }
                        if (packet.playerUID.Equals(capi.World.Player.PlayerUID))
                        {
                            redrawEffectPictures = true;
                        }
                        if (packet.typeIdsToRemove != null)
                        {
                            if(packet.typeIdsToRemove.Contains("invisibility"))
                            {
                                 invisiblePlayers.Remove(packet.playerUID);                             
                            }
                            foreach (var effToRemove in packet.typeIdsToRemove.ToArray())
                            {
                                if (ebef.onlyClientsActiveEffects.TryGetValue(effToRemove, out EffectClientData ecd))
                                {
                                    ebef.onlyClientsActiveEffects.Remove(effToRemove);
                                }
                            }
                        }
                    }
                }
                if(!effectshud.capi.World.Player.PlayerUID.Equals(packet.playerUID))
                {
                    var c = 2;
                }
               
                if (showHUD && effectsHUD != null)
                {
                    effectsHUD.ComposeGuis();
                }
            });
            RegisterClientEffectData("regeneration", new string[] { "effectshud:effects/regeneration1", "effectshud:effects/regeneration2", "effectshud:effects/regeneration3" });
            RegisterClientEffectData("miningslow", new string[] { "effectshud:effects/slowmining1", "effectshud:effects/slowmining2", "effectshud:effects/slowmining3" });
            RegisterClientEffectData("miningspeed", new string[] { "effectshud:effects/miningspeed1", "effectshud:effects/miningspeed2", "effectshud:effects/miningspeed3" });
            RegisterClientEffectData("walkslow", new string[] { "effectshud:effects/walkspeed1m", "effectshud:effects/walkspeed2m", "effectshud:effects/walkspeed3m" });
            RegisterClientEffectData("walkspeed", new string[] { "effectshud:effects/walkspeed1p", "effectshud:effects/walkspeed2p", "effectshud:effects/walkspeed3p" });
            RegisterClientEffectData("weakmelee", new string[] { "effectshud:effects/weakmelee1", "effectshud:effects/weakmelee2", "effectshud:effects/weakmelee3" });
            RegisterClientEffectData("strengthmelee", new string[] { "effectshud:effects/strengthmelee1", "effectshud:effects/strengthmelee2", "effectshud:effects/strengthmelee3" });
            RegisterClientEffectData("bleeding", new string[] { "effectshud:effects/bleeding1", "effectshud:effects/bleeding2", "effectshud:effects/bleeding3" });
            RegisterClientEffectData("thorns", new string[] { });
            RegisterClientEffectData("safefall", new string[] { });
            RegisterClientEffectData("firedamageimmune", new string[] { });
            RegisterClientEffectData("forgetting", new string[] { });
            RegisterClientEffectData("invisibility", new string[] { });
            //RegisterClientEffectData("vampirism", new string[] { });
        }
        public static bool RegisterClientEffectData(string typeId, string[] domainAndPath)
        {
            AssetLocation tmpAL;
            AssetLocation[] tmpArr = new AssetLocation[domainAndPath.Length];
            for (int i = 0; i < domainAndPath.Length; i++)
            {
                try
                {
                    tmpAL = new AssetLocation(domainAndPath[i] + ".png");
                    tmpArr[i] = tmpAL;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            effectsPictures.Add(typeId, tmpArr);
            return true;
        }
        public void addDefaultEffect(IPlayer player, int groupId, CmdArgs args)
        {
            if(player.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                return;
            }
            //effectname minutes tier targetname
            if(args.Length < 4)
            {
                return;
            }
            effects.TryGetValue(args[0], out Type effectType);
            if(effectType == null)
            {
                return;
            }
            int durationMin = 0;
            try
            {
                durationMin = int.Parse(args[1]);
            }
            catch(FormatException e)
            {
                return;
            }
            int tier = 1;
            try
            {
                tier = int.Parse(args[2]);
            }
            catch (FormatException e)
            {
                return;
            }

            foreach(var it in sapi.World.AllOnlinePlayers)
            {
                if(it.PlayerName.Equals(args[3]))
                {
                    Effect ef = (Effect)Activator.CreateInstance(effectType);
                    ef.SetExpiryInRealMinutes(durationMin);
                    ef.tier = tier;
                    ApplyEffectOnEntity(it.Entity, ef);
                    break;
                }
            }
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.EntityAgent).GetMethod("ReceiveDamage"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_On_ReceiveDamage")));
            base.StartServerSide(api);
            api.RegisterCommand("ef", "", "", addDefaultEffect);
            api.RegisterEntityBehaviorClass("affectedByEffects", typeof(EBEffectsAffected));
            RegisterEntityEffect("regeneration", typeof(RegenerationEffect));
            RegisterEntityEffect("miningslow", typeof(MiningSlowEffect));
            RegisterEntityEffect("miningspeed", typeof(MiningSpeedEffect));
            RegisterEntityEffect("walkslow", typeof(WalkSlowEffect));
            RegisterEntityEffect("walkspeed", typeof(WalkSpeedEffect));
            RegisterEntityEffect("weakmelee", typeof(WeakMeleeEffect));
            RegisterEntityEffect("strengthmelee", typeof(StrengthMeleeEffect));
            RegisterEntityEffect("bleeding", typeof(BleedingEffect));
            RegisterEntityEffect("thorns", typeof(ThornsEffect));
            RegisterEntityEffect("safefall", typeof(SafeFallEffect));
            RegisterEntityEffect("firedamageimmune", typeof(FireDamageImmuneEffect));
            RegisterEntityEffect("forgetting", typeof(ForgettingEffect));
            RegisterEntityEffect("invisibility", typeof(InvisibilityEffect));
            //RegisterEntityEffect("vampirism", typeof(VampirismEffect));
            serverChannel = sapi.Network.RegisterChannel("effectshud");
            serverChannel.RegisterMessageType(typeof(EffectsSyncPacket));
            api.Event.PlayerDeath += onPlayerDead;

            //api.Event.PlayerDisconnect += onPlayerLeft;
            sapi.Event.PlayerNowPlaying += (serverPlayer) =>
            {
                EBEffectsAffected ebea = serverPlayer.Entity.GetBehavior<EBEffectsAffected>();
                if (ebea == null)
                {
                    return;
                }
                ebea.SendActiveEffectsToClient(null);
            };
        }
        public void onPlayerDead(IServerPlayer byPlayer, DamageSource damageSource)
        {
          
        }
        public void onPlayerLeft(IServerPlayer byPlayer)
        {
            EBEffectsAffected ebea = byPlayer.Entity.GetBehavior<EBEffectsAffected>();
            if(ebea == null)
            {
                return;
            }
            ebea.serialize();
        }
        public static bool RegisterEntityEffect(string typeId, Type effectType)
        {
            effects.Add(typeId, effectType);
            return true;
        }
        public static bool ApplyEffectOnEntity(Entity entity, Effect effect)
        {
            EBEffectsAffected ebea = entity.GetBehavior<EBEffectsAffected>();
            if(ebea == null)
            {
                return false;
            }
            return ebea.AddEffect(effect);
        }
        private bool OnHotKeySkillDialog(KeyCombination comb)
        {
            showHUD = !showHUD;
            double startPointMap = -1;
            double startPointCoords = -1;
            effectsHUD = null;
            lock (capi.OpenedGuis) {
                foreach (var it in capi.OpenedGuis)
                {
                    if((it as GuiDialog).DebugName.Equals("GuiDialogWorldMap"))
                    {
                        if((it as GuiDialog).SingleComposer.Bounds.Alignment == EnumDialogArea.RightTop)
                        {
                            startPointMap = (it as GuiDialog).SingleComposer.Bounds.absInnerHeight;
                            continue;
                        }
                    }
                    if ((it as GuiDialog).DebugName.Equals("HudElementCoordinates"))
                    {
                        if ((it as GuiDialog).SingleComposer.Bounds.Alignment == EnumDialogArea.RightTop)
                        {
                            startPointCoords = (it as GuiDialog).SingleComposer.Bounds.absInnerHeight;
                            continue;
                        }
                    }
                    if (it is HUDEffects)
                    {
                        if (!showHUD)
                        {
                            (it as HUDEffects).TryClose();
                            break;
                      }
                        
                    }
                }
                if(showHUD)
                {
                    //effectsHUD = new HUDEffects(capi);
                    //effectsHUD.ComposeGuis();
                    // effectsHUD.TryOpen();
                }
                
                if (startPointCoords != -1 && startPointMap != -1)
                {
                    HUDEffects.glOffset = (int)(startPointCoords + startPointMap) + 32;
                    // effHud.Composers[0].Bounds.fixedOffsetY = 600;
                }
                else if(startPointCoords != -1)
                {
                    HUDEffects.glOffset = (int)(startPointCoords) + 32;
                }
                else if (startPointMap != -1)
                {
                    HUDEffects.glOffset = (int)(startPointMap) + 32;
                }
                else
                {
                    HUDEffects.glOffset = 64;
                }

            }
            
            return true;
        }
        public static bool RegisterEffect(string watchedBranch, string effectWatchedName, bool showTime, string effectDurationWatchedName, string [] domainAndPath, Vintagestory.API.Common.Func<int> needToShow)
        {
            AssetLocation tmpAL;
            AssetLocation [] tmpArr = new AssetLocation [domainAndPath.Length];
            for (int i = 0; i < domainAndPath.Length; i++)
            {
                try
                {
                    tmpAL = new AssetLocation(domainAndPath[i] + ".png");
                    tmpArr[i] = tmpAL;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            trackedEffects.Add(new TrackedEffect(tmpArr, showTime, watchedBranch, effectWatchedName, effectDurationWatchedName, needToShow));
            return true;
        }
        public override void Dispose()
        {
            base.Dispose();
            trackedEffects.Clear();
            harmonyInstance.UnpatchAll();
            effects.Clear();
            effectsPictures.Clear();
        }
        public static double Now { get { return sapi.World.Calendar.TotalDays; } }
    }
}
