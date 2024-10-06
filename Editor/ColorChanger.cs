using UnityEngine;
using nadena.dev.ndmf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using nadena.dev.modular_avatar.core;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using static VRC.SDKBase.Networking;
using static com.github.pandrabox.unlimitedcolor.runtime.Generic;
using static com.github.pandrabox.unlimitedcolor.runtime.Generic_Dev;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.unlimitedcolor.runtime.config;
using System.IO;

//[assembly: ExportsPlugin(typeof(ColorChangerPass))]

namespace com.github.pandrabox.unlimitedcolor.editor
{
    /// <summary>
    /// To call from Unity menu
    /// </summary>
    public class GenColorChangerClass : MonoBehaviour
    {
        [MenuItem("PBTB/Gen_ColorChanger")]
        static void GenColorChanger()
        {
            var Target = GameObject.Find("ColorChanger");
            if (Target != null)
            {
                new ColorChangerMain(Target);
            }
        }
    }

    public class ColorParam
    {
        public string eng;
        public string jp;

        public ColorParam(string eng, string jp)
        {
            this.eng = eng;
            this.jp = jp;
        }
    }
    /// <summary>
    /// Actual operation
    /// </summary>
    public class ColorChangerMain
    {
        public string ProjectFolder = "Assets/Pan/ClothManager/ColorChanger";
        public GameObject AvatarRoot;
        public List<string> ResolvedColorType;
        public ColorParam[] ColorParams;
        public ColorChangerMain(GameObject Target)
        {
            AvatarRoot = FindComponentFromParent<VRCAvatarDescriptor>(Target).gameObject;
            ColorParams = new ColorParam[]
            {
                new ColorParam("Hue", "色相"),
                new ColorParam("Saturation", "彩度"),
                new ColorParam("Value", "明度"),
                new ColorParam("Gamma", "ガンマ")
            };
            Run();
        }
        public void Run()
        {
            ResolvedColorType = new List<string>();
            string[] Items = new string[] { "Body", "hair", "hone" };
            MakeUnitColorChanger($@"C", "T", Items);
        }

        public void MakeUnitColorChanger(string TypeName, string ColorType, string[] TargetObjNames)
        {
            //ルートオブジェクトNDMFColorChangerの作成（ただの枠）
            var ColorChangerRoot = ReCreateObject(AvatarRoot, "NDMFColorChanger");
            //FlatsClothオブジェクトを直下に作成（後でマージするため着せ替えと同じ名称ツリーにする）
            var DummyFCM = GetOrCreateObject(ColorChangerRoot, "UnlimitedColor", (GameObject x) =>
            {
                x.AddComponent<ModularAvatarMenuInstaller>();
                var CCRM = x.AddComponent<ModularAvatarMenuItem>();
                CCRM.Control.name = "UnlimitedColor";
                CCRM.Control.type = ControlType.SubMenu;
                //CCRM.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{ProjectFolder}/FlatsClothManager/ClothManager.png");
                CCRM.MenuSource = SubmenuSource.Children;
            });

            //実動作を仕舞う入れ物の作成（スラッシュ区切りで階層と名称を指定、後で中に「色合い(Hue,Gamma)」と「明るさ(Value,Saturation)」を入れる
            GameObject CurrentObj = null, UnitColorChangerObj = null;
            var NameHierarchy = TypeName.Split('/');
            for (var n = 0; n < NameHierarchy.Length; n++)
            {
                CurrentObj = GetOrCreateObject(n == 0 ? DummyFCM : CurrentObj, NameHierarchy[n]);
                var CurrentMAMI = CurrentObj.AddComponent<ModularAvatarMenuItem>();
                CurrentMAMI.Control.name = TypeName;
                CurrentMAMI.Control.type = ControlType.SubMenu;
                //CurrentMAMI.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{ProjectFolder}/Ico/Color1.png");
                CurrentMAMI.MenuSource = SubmenuSource.Children;
                UnitColorChangerObj = CurrentObj;
            }


            //実動作を呼ぶスイッチ1
            var suffix = $@"Pan/UnlimitedColor/{ColorType}";
            foreach(var colorParam in ColorParams)
            {
                var obj = GetOrCreateObject(UnitColorChangerObj, colorParam.jp);
                var menu = obj.AddComponent<ModularAvatarMenuItem>();
                menu.Control.type = ControlType.RadialPuppet;
                // HueGammaMenu.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{ProjectFolder}/Ico/Color1.png");
                var CurrentParameter = new Parameter[1];
                CurrentParameter[0] = new Parameter() { name = $@"{suffix}/{colorParam.eng}" };
                menu.Control.subParameters = CurrentParameter;
            }

            if (!ResolvedColorType.Contains(ColorType))
            {
                ResolvedColorType.Add(ColorType);
                //MAパラメータの定義
                var MAP = UnitColorChangerObj.AddComponent<ModularAvatarParameters>();
                MAP.parameters = new List<ParameterConfig>();
                foreach (var colorParam in ColorParams)
                {
                    MAP.parameters.Add(new ParameterConfig() { nameOrPrefix = $@"{suffix}/{colorParam.eng}", syncType = ParameterSyncType.Float, saved = true ,localOnly=true, defaultValue=0.5f });
                }

                //アニメとDBTの定義
                var ac = new AnimationClipsBuilder(ProjectFolder);
                foreach (var obj in TargetObjNames)
                {
                    ac.Name($@"{ColorType}Hue-1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.x").Keys(0, -.5f);
                    ac.Name($@"{ColorType}Hue0").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.x").Keys(0, 0);
                    ac.Name($@"{ColorType}Hue1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.x").Keys(0, .5f);
                    ac.Name($@"{ColorType}Saturation-1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.y").Keys(0, 0);
                    ac.Name($@"{ColorType}Saturation0").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.y").Keys(0, 1);
                    ac.Name($@"{ColorType}Saturation1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.y").Keys(0, 5);
                    ac.Name($@"{ColorType}Value-1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.z").Keys(0, 0);
                    ac.Name($@"{ColorType}Value0").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.z").Keys(0, 1);
                    ac.Name($@"{ColorType}Value1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.z").Keys(0, 2);
                    ac.Name($@"{ColorType}Gamma-1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.w").Keys(0, 0.01f);
                    ac.Name($@"{ColorType}Gamma0").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.w").Keys(0, 1);
                    ac.Name($@"{ColorType}Gamma1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.w").Keys(0, 7);
                }
                if (DEBUGMODE)
                {
                    foreach(var unitClip in ac.AnimationClips)
                    {
                        string ClipPath = Path.Combine(DebugOutpFolder, $"{unitClip.Key}.anim");
                        if (!File.Exists(ClipPath))
                        {
                            AssetDatabase.CreateAsset(unitClip.Value.Outp(), ClipPath);
                        }
                    }
                }
                var bb = new BlendTreeBuilderForNDMF(ProjectFolder, UnitColorChangerObj);
                bb.rootDBT(() =>
                {
                    foreach (var colorParam in ColorParams)
                    {
                        bb.Param("1").Add1D($@"Pan/UnlimitedColor/{ColorType}/{colorParam.eng}", () =>
                        {
                            for (int n = -1; n <= 1; n++)
                            {
                                bb.Param(((float)n+1)/2).CM(ac.Clip($@"{ColorType}{colorParam.eng}{n}"));
                            }
                        });
                    }
                });
            }
        }
    }
}