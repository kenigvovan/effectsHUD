using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;

namespace effectshud.src
{
    [HarmonyPatch]
    public class harmPatch
    {
        public static void Postfix_Map_OnGuiClosed(Vintagestory.GameContent.GuiDialogWorldMap __instance)
        {
            updateOffset();
        }
        public static void Postfix_Map_OnGuiOpened(Vintagestory.GameContent.GuiDialogWorldMap __instance)
        {
            updateOffset();
        }
        public static void Postfix_CoordsHUD_OnGuiClosed(Vintagestory.Client.NoObf.HudElementCoordinates __instance)
        {
            updateOffset();
        }
        public static void Postfix_CoordsHUD_OnGuiOpened(Vintagestory.Client.NoObf.HudElementCoordinates __instance)
        {
                effectshud.capi.Event.RegisterCallback((dt =>
                {
                    updateOffset();                  
                }), 1 * 1000);       
        }
        public static void updateOffset()
        {
            double startPointMap = -1;
            double startPointCoords = -1;
            GuiDialog effHud = null;
            lock (effectshud.capi.OpenedGuis)
            {
                foreach (var it in effectshud.capi.OpenedGuis)
                {
                    if ((it as GuiDialog).DebugName.Equals("GuiDialogWorldMap"))
                    {
                        if ((it as GuiDialog).SingleComposer.Bounds.Alignment == EnumDialogArea.RightTop)
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
                }

                if (startPointCoords != -1 && startPointMap != -1)
                {
                    HUDEffects.glOffset = (int)(startPointCoords + startPointMap) + 32;
                    // effHud.Composers[0].Bounds.fixedOffsetY = 600;
                }
                else if (startPointCoords != -1)
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
        }
    }
}
