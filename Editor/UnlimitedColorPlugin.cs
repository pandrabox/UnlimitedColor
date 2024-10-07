using UnityEngine;
using nadena.dev.ndmf;
using System.Linq;
using com.github.pandrabox.unlimitedcolor.editor;
using com.github.pandrabox.unlimitedcolor.runtime;

[assembly: ExportsPlugin(typeof(UnlimitedColorPass))]

namespace com.github.pandrabox.unlimitedcolor.editor
{
    public class UnlimitedColorPass : Plugin<UnlimitedColorPass>
    {
        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).BeforePlugin("nadena.dev.modular-avatar").Run("UnlimitedColor", ctx =>
            {
                //Debug.LogWarning("aaaa");
                UnlimitedColor tgt = ctx.AvatarRootTransform.GetComponentInChildren<UnlimitedColor>(false);
                if (tgt == null) return;
                new ColorChangerMain(tgt);
            });
        }
    }
}