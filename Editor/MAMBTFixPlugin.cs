using nadena.dev.ndmf;
using com.github.pandrabox.unlimitedcolor.editor;
using UnityEditor.Animations;
using com.github.pandrabox.unlimitedcolor.runtime;

[assembly: ExportsPlugin(typeof(MAMBTFixPass))]

namespace com.github.pandrabox.unlimitedcolor.editor
{
    public class MAMBTFixPass : Plugin<MAMBTFixPass>
    {
        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).AfterPlugin("nadena.dev.modular-avatar").Run("Pan Fx Sort", ctx => {

                if (ctx.AvatarRootTransform.GetComponentInChildren<MAMBTFix>(false) == null) return;

                var fxAnimatorController = ctx.AvatarDescriptor.baseAnimationLayers[4].animatorController as AnimatorController;
                if (fxAnimatorController == null) return;

                // "ModularAvatarMergeBlendTree"レイヤのインデックスを見つける
                int targetLayerIndex = -1;
                for (int i = 0; i < fxAnimatorController.layers.Length; i++)
                {
                    if (fxAnimatorController.layers[i].name == "ModularAvatar: Merge Blend Tree")
                    {
                        targetLayerIndex = i;
                        break;
                    }
                }

                // ターゲットレイヤが見つかった場合、そのレイヤを最後に移動
                if (targetLayerIndex != -1)
                {
                    var layers = fxAnimatorController.layers;
                    var layersCopy = new AnimatorControllerLayer[layers.Length];

                    // ターゲットレイヤを除くすべてのレイヤを新しい配列にコピー
                    int copyIndex = 0;
                    for (int i = 0; i < layers.Length; i++)
                    {
                        if (i != targetLayerIndex)
                        {
                            layersCopy[copyIndex] = layers[i];
                            copyIndex++;
                        }
                    }

                    // ターゲットレイヤを最後に追加
                    layersCopy[copyIndex] = layers[targetLayerIndex];

                    // 新しいレイヤ配列をAnimatorControllerに設定
                    fxAnimatorController.layers = layersCopy;
                }
            });
        }
    }
}