using effectshud.src.DefaultEffects;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static List<EffectClientData> clientsActiveEffects = new List<EffectClientData>();
        HUDEffects effectsHUD;
        public static Dictionary<string, AssetLocation[]> effectsPictures = new Dictionary<string, AssetLocation[]>();
        internal static IServerNetworkChannel serverChannel;
        public static bool redrawEffectPictures = true;
        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            base.StartClientSide(api);
            api.Gui.RegisterDialog((GuiDialog)new HUDEffects((ICoreClientAPI)api));
            harmonyInstance = new Harmony(harmonyID);
            api.Input.RegisterHotKey("effectsghud", "Show effects hud", GlKeys.L, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("effectsghud", new ActionConsumable<KeyCombination>(this.OnHotKeySkillDialog));
           // RegisterEffect("hunger", "currentsaturation", false, "", new string[] { "effectshud:effects/hunger1" }, checkSaturation);
            //RegisterEffect("weightmod", "currentweight", false, "", new string[] { "effectshud:effects/weight1" }, checkWeight);
           //RegisterEffect("stats", "miningSpeedMul", false, "", new string[] { "effectshud:effects/slowmining1" }, checkminigSpeedMul); 
            //RegisterEffect("stats", "walkspeed", false, "", new string[] { "effectshud:effects/slow1", "effectshud:effects/slow2", "effectshud:effects/slow3" }, walkSpeedMul);
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.GuiDialogWorldMap).GetMethod("OnGuiClosed"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Postfix_Map_OnGuiClosed")));
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.GuiDialogWorldMap).GetMethod("OnGuiOpened"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Postfix_Map_OnGuiOpened")));
            
            harmonyInstance.Patch(typeof(Vintagestory.Client.NoObf.HudElementCoordinates).GetMethod("OnGuiClosed"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Postfix_CoordsHUD_OnGuiClosed")));
            harmonyInstance.Patch(typeof(Vintagestory.Client.NoObf.HudElementCoordinates).GetMethod("OnGuiOpened"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Postfix_CoordsHUD_OnGuiOpened")));
            api.RegisterEntityBehaviorClass("affectedByEffects", typeof(EBEffectsAffected));
            clientChannel = api.Network.RegisterChannel("effectshud");
            clientChannel.RegisterMessageType(typeof(EffectsSyncPacket));
            clientChannel.SetMessageHandler<EffectsSyncPacket>((packet) =>
            { 
                clientsActiveEffects = JsonConvert.DeserializeObject<List<EffectClientData>>(packet.currentEffectsData);
                redrawEffectPictures = true;
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
                ebea.SendActiveEffectsToClient();
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
