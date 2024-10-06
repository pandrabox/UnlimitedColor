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
            InPhase(BuildPhase.Resolving).BeforePlugin("nadena.dev.modular-avatar").Run("UnlimitedColor", ctx =>
            {
                if (!ctx.AvatarRootTransform.GetComponentsInChildren<UnlimitedColor>(true).Any()) return;
                new ColorChangerMain(ctx.AvatarRootObject);
            });
        }
    }

    public class UnlimitedColorMain
    {
        public UnlimitedColorMain(BuildContext ctx)
        {
            GameObject FlatPlusObj = ctx.AvatarRootTransform?.Find("FlatPlus")?.gameObject;
            if (FlatPlusObj == null) return;
            GameObject TailRootObj = FlatPlusObj?.transform?.Find("SippoEx/Tail Root")?.gameObject;
            if (TailRootObj == null) return;

            var targetTransform = TailRootObj.transform;

            float targetPosX = 0f;
            float targetPosY = -0.1264026f;
            float targetPosZ = -0.1477546f;
            float targetScaleX = 61.43048f;
            float targetScaleY = 61.43048f;
            float targetScaleZ = 61.43048f;

            float tolerance = 0.001f;

            if (Mathf.Abs(targetTransform.localPosition.x - targetPosX) < tolerance &&
                Mathf.Abs(targetTransform.localPosition.y - targetPosY) < tolerance &&
                Mathf.Abs(targetTransform.localPosition.z - targetPosZ) < tolerance &&
                Mathf.Abs(targetTransform.localScale.x - targetScaleX) < tolerance &&
                Mathf.Abs(targetTransform.localScale.y - targetScaleY) < tolerance &&
                Mathf.Abs(targetTransform.localScale.z - targetScaleZ) < tolerance)
            {
                targetTransform.localPosition = new Vector3(0, -0.1834847f, -0.2144791f);
                targetTransform.eulerAngles = new Vector3(-108.156f, -3.148987f, 2.991989f);
                targetTransform.localScale = new Vector3(74.01262f, 74.0126f, 74.0126f);
            }
        }
    }
}