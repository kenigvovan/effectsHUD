using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace effectshud.src
{
    public class effectshud: ModSystem
    {
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        public static Harmony harmonyInstance;
        public const string harmonyID = "effectshud.Patches";
        public static List<TrackedEffect> trackedEffects = new List<TrackedEffect>();
        public static bool showHUD = true;
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

        }

        private bool OnHotKeySkillDialog(KeyCombination comb)
        {
            showHUD = !showHUD;
            double startPointMap = -1;
            double startPointCoords = -1;
            GuiDialog effHud = null;
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
                        effHud = it as GuiDialog;
                    }
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
        }
        public int walkSpeedMul()
        {
            var t2 = capi.World.Player.Entity.Stats["walkspeed"];
            var t = t2.GetBlended();
            if (t < 0.35)
            {
                return 2;
            }
            else if (t < 0.5)
            {
                return 1;
            }
            else if (t < 0.85)
            {
                return 0;
            }
            return -1;
        }
        public int checkminigSpeedMul()
        {
            var t = capi.World.Player.Entity.Stats["miningSpeedMul"];
            if (t.GetBlended() < 1)
            {
                return 0;
            }
            return -1;
        }
        //public int checkWeight()
        //{
        //    var t = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("weightmod");
        //    if (t.GetFloat("currentweight") >= t.GetFloat("maxweight"))
        //    {
        //        return 0;
        //    }
        //    return -1;
        //}
        //public int checkTemp()
        //{
        //    var t = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("bodyTemp").GetFloat("bodytemp");
        //    if (t < 1000)
        //    {
        //        return 0;
        //    }
        //    return -1;
        //}
        //public int checkSaturation()
        //{
        //    var t = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger").GetFloat("currentsaturation");
        //    if(t < 1000)
        //    {
        //        return 0;
        //    }
        //    return -1;
        //}
    }
}
