using Cairo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace effectshud.src
{
    public class HUDEffects : HudElement
    {
        public static int glOffset = 0;
        float HUDWidth = 128;
        float HUDHeight = 1000;
        float wChange = 64;
        float hChange = 64;
        float del = 20;
        float texSizeW = 64;
        float texSizeH = 64;
        public override double DrawOrder => 0.1;
        public HUDEffects(ICoreClientAPI capi) : base(capi)
        {
            capi.World.RegisterGameTickListener((float dt) => { 
                if(effectshud.showHUD) ComposeGuis(); }, 1000);
        }
        
        public override void OnOwnPlayerDataReceived()
        {
            this.ComposeGuis();
            /*foreach(var it in effectshud.trackedEffects)
            {

                capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener(it.watchedBranch, () => 
                { 
                    if(!effectshud.showHUD)
                    {
                        return;
                    }
                    if(it.getTextureNumber != null)
                    {
                        int resultTexture = it.getTextureNumber();
                        if(resultTexture == -1)
                        {
                            if(it.active)
                            {
                                it.active = false;
                                ComposeGuis();
                            }
                            return;
                        }
                        it.active = true;
                        it.activeTexture = resultTexture;
                        ComposeGuis();
                    }
                        
                    
                });
            }*/
        }
         
        public void ComposeGuis()
        {
           //capi.World.Player.Entity.WatchedAttributes.
            IRenderAPI render = this.capi.Render;
            //this.TryClose
            ElementBounds bounds1 = new ElementBounds()
            {
                Alignment = EnumDialogArea.RightFixed,
                BothSizing = ElementSizing.Fixed,
                fixedWidth = HUDWidth,
                fixedHeight = HUDHeight
            };

            var Compo = this.capi.Gui.CreateCompo("effectshud", bounds1);
            var wa = capi.World.Player.Entity.WatchedAttributes;
            int currentEffectCounter = 0;
            foreach (var it in effectshud.clientsActiveEffects.ToArray())
            {
                if(it.duration <= 0)
                {
                    effectshud.clientsActiveEffects.Remove(it);
                }
            }
            foreach (var it in effectshud.clientsActiveEffects)
            {
                //if (effectshud.redrawEffectPictures)
                {
                    if (effectshud.effectsPictures.TryGetValue(it.typeId, out AssetLocation[] al))
                    {
                        Compo.AddImage(ElementBounds.Fixed(0, (int)((texSizeH + del) * currentEffectCounter) + glOffset, 64, 64), al[it.tier - 1]);
                    }
                }
                if (it.infinite)
                {
                    Compo.AddStaticText("--:--",
                   CairoFont.WhiteSmallText().WithFontSize(12),
                   ElementBounds.Fixed(10, (int)(((texSizeH + del) * currentEffectCounter) + glOffset + ((del) * currentEffectCounter + 64))).WithFixedSize(32.0, 10.0));
                }
                else
                {
                    //Compo.AddStaticText("67", CairoFont.WhiteSmallText().WithFontSize(12), ElementBounds.Fixed(6, (int)(hChange + del) * currentEffectCounter + 32).WithFixedSize(32.0, 10.0));
                    Compo.AddStaticText((it.duration / 60).ToString() + ":" + ((it.duration % 60) < 10 ? "0" + (it.duration % 60) : (it.duration % 60).ToString()),
                        CairoFont.WhiteSmallText().WithFontSize(12),
                        ElementBounds.Fixed(10, (int)(((texSizeH + del) * currentEffectCounter) + glOffset + 64)).WithFixedSize(32.0, 10.0));
                }
                currentEffectCounter++;
            }
            foreach (var it in effectshud.clientsActiveEffects)
            {
                if (it.infinite)
                    continue;
                it.duration--;
            }
           effectshud.redrawEffectPictures = false;
            Compo.Compose();
            this.Composers["effectshud"] = Compo;
            this.TryOpen();

           /* foreach (var it in effectshud.trackedEffects)
            {
                if(!it.active)
                {
                    continue;
                }

                Compo.AddImage(ElementBounds.Fixed(0, (int)((texSizeH + del) * currentEffectCounter) + glOffset, 64, 64) , it.effectTexture[it.activeTexture]);
                //Compo.AddImage
                if (!it.showTime)
                {
                    Compo.AddStaticText("--:--", CairoFont.WhiteSmallText().WithFontSize(12), ElementBounds.Fixed(20, (int)((hChange) * (currentEffectCounter + 1) + currentEffectCounter * del) + glOffset).WithFixedSize(32.0, 10.0));
                }
                else
                {
                    ITreeAttribute ta = wa.GetTreeAttribute(it.watchedBranch);
                    if (it.watchedBranch.Equals("stats"))
                    {
                        //var t2 = capi.World.Player.Entity.Stats[trackedEffect.effectWatchedName];
                        //var tt = t[trackedEffect.statsAdditionalPath];
                    }
                    else
                    {
                        int secondsDur = ta.GetInt(it.effectDurationWatchedName);
                        Compo.AddStaticText((secondsDur / 60) + ":" + (secondsDur), CairoFont.WhiteSmallText().WithFontSize(12), ElementBounds.Fixed(6, (int)(hChange + del) * currentEffectCounter + 32).WithFixedSize(32.0, 10.0));
                    }
                }
                currentEffectCounter++;
            }         */  
        }
    }
}
