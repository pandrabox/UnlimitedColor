﻿using UnityEngine;
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
            InPhase(BuildPhase.Resolving).BeforePlugin("nadena.dev.modular-avatar").Run("UnlimitedColor", ctx =>
            {
                UnlimitedColor tgt = ctx.AvatarRootTransform.GetComponentInChildren<UnlimitedColor>(false);
                if (tgt == null) return;
                new ColorChangerMain(tgt);
            });
        }
    }
}