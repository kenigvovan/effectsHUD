using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace effectshud.src
{
    [HarmonyPatch]
    public class harmPatch
    {
        /*public static bool Prefix_TesselateShape(Vintagestory.GameContent.EntitySkinnableShapeRenderer __instance)
        {
            return true;
        }*/
        public static bool Prefix_GetFullEntityPacket(Vintagestory.Client.NoObf.ClientSystemEntities __instance, Entity entity)
        {
            EBEffectsAffected ebef = entity.GetBehavior<EBEffectsAffected>();
            if(ebef != null)
            {
                ebef.SendActiveEffectsToClient(null);
            }
            return true;
        }
        public static bool Prefix_BeforeRender(EntityShapeRenderer __instance)
        {
            return true;
        }
        
        public static bool Prefix_DoRender3DOpaque(EntityShapeRenderer __instance, float dt)
        {        
            return true;
        }
        public static bool Prefix_DoRender3DOpaqueBatched(EntityShapeRenderer __instance)
        {
            if (effectshud.invisiblePlayers.Contains((__instance.entity as EntityPlayer)?.PlayerUID))
            {
                return false;
            }
            return true;
        }
        public static bool Prefix_DoRender2D(EntityShapeRenderer __instance)
        {
            if (effectshud.invisiblePlayers.Contains((__instance.entity as EntityPlayer)?.PlayerUID))
            {
                return false;
            }
            return true;
        }
        /*public static bool Prefix_On_ReceiveDamage(Vintagestory.API.Common.Entities.Entity __instance, DamageSource damageSource, ref float damage)
        {
            EBEffectsAffected ebea = __instance.GetBehavior<EBEffectsAffected>();
            if (ebea == null)
            {
                return true;
            }
            ebea.OnShouldEntityReceiveDamage(damageSource, ref damage);
            return true;
        }*/
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
        public static double addNameAndProcessMadeTool(EntityBehaviorTemporalStabilityAffected ebtsa, double gain)
        {
            var tmpVal = ebtsa.entity.Stats.GetBlended("cantemporalcharge");
            if (tmpVal == 1)
            {
                return gain;
            }
            if(gain >= 0)
            {
                gain *= tmpVal;
            }
            else
            {
                gain /= tmpVal;
            }
            return gain;
        }
        public static IEnumerable<CodeInstruction> Prefix_EntityBehaviorTemporalStabilityAffected(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            var codes = new List<CodeInstruction>(instructions);
            var decMethod = AccessTools.GetDeclaredMethods(typeof(IWorldAccessor))
            .Where(m => m.Name == "SpawnItemEntity" && m.GetParameters().Types().Contains(typeof(ItemStack)) && m.GetParameters().Types().Contains(typeof(Vec3d)) && m.GetParameters().Types().Contains(typeof(Vec3d)))
            .Single();
            var proxyMethod = AccessTools.Method(typeof(harmPatch), "addNameAndProcessMadeTool");
            for (int i = 0; i < codes.Count; i++)
            {
                if (!found &&
                    codes[i].opcode == OpCodes.Ldloc_2 && codes[i + 1].opcode == OpCodes.Add && codes[i + 2].opcode == OpCodes.Ldc_R8 && codes[i - 1].opcode == OpCodes.Call)
                {
                    //yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return codes[i];
                    yield return new CodeInstruction(OpCodes.Call, proxyMethod);
                    found = true;
                    continue;
                }
                yield return codes[i];
            }
        }
    }
}
