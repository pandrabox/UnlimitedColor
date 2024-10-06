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
                new ColorChangerMain(Target).Run();
                Debug.LogWarning("a!");
            }
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
        public ColorChangerMain(GameObject Target)
        {
            AvatarRoot = FindComponentFromParent<VRCAvatarDescriptor>(Target).gameObject;
        }
        public void Run()
        {
            ResolvedColorType = new List<string>();
            string[] Items = new string[] { "body", "hair", "hone" };
            MakeUnitColorChanger($@"pandra/Color", "T", Items);
            //MakeUnitColorChanger("BodyColor", "Body", new string[] { "Body", "karada", "mohu" });
            //FCMCloth[] FCMCloths = AvatarRoot.GetComponentsInChildren<FCMCloth>(true);
            //var uniqueTypes = FCMCloths.Select(c => c.Type).Distinct().ToArray(); 
            //foreach (var type in uniqueTypes)
            //{
            //    string[] ColorTypes = FCMCloths.Where(c => c.Type == type).Select(c=>c.ColorType).Distinct().ToArray();
            //    string ColorType = ColorTypes[ColorTypes.Length - 1];
            //    string[] Items = FCMCloths.Where(c => c.ColorType == ColorType).Select(c => c.Path()).ToArray();
            //    MakeUnitColorChanger($@"{type}/Color", ColorType, Items);
            //}
        }

        public void MakeUnitColorChanger(string TypeName, string ColorType, string[] TargetObjNames)
        {
            //ルートオブジェクトNDMFColorChangerの作成（ただの枠）
            var ColorChangerRoot = ReCreateObject(AvatarRoot, "NDMFColorChanger");
            //FlatsClothオブジェクトを直下に作成（後でマージするため着せ替えと同じ名称ツリーにする）
            var DummyFCM = GetOrCreateObject(ColorChangerRoot, "FlatsCloth", (GameObject x) =>
            {
                x.AddComponent<ModularAvatarMenuInstaller>();
                var CCRM = x.AddComponent<ModularAvatarMenuItem>();
                CCRM.Control.name = "FlatsCloth";
                CCRM.Control.type = ControlType.SubMenu;
                //CCRM.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{ProjectFolder}/FlatsClothManager/ClothManager.png");
                CCRM.MenuSource = SubmenuSource.Children;
            });

            //実動作を仕舞う入れ物の作成（スラッシュ区切りで階層と名称を指定、後で中に「色合い(Hue,Gamma)」と「明るさ(BrightNess,Saturation)」を入れる
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
            //var suffix = $@"{ProjectFolder.Replace("Assets/", "")}/{ColorType}";
            var suffix = $@"Pan/UnlimitedColor/{ColorType}";
            var HueGammaObj = GetOrCreateObject(UnitColorChangerObj, "色合い");
            var HueGammaMenu = HueGammaObj.AddComponent<ModularAvatarMenuItem>();
            HueGammaMenu.Control.name = HueGammaObj.name;
            HueGammaMenu.Control.type = ControlType.TwoAxisPuppet;
            // HueGammaMenu.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{ProjectFolder}/Ico/Color1.png");
            var CurrentParameter = new Parameter[2];
            CurrentParameter[0] = new Parameter() { name = $@"{suffix}/Hue" };
            CurrentParameter[1] = new Parameter() { name = $@"{suffix}/Gamma" };
            HueGammaMenu.Control.subParameters = CurrentParameter;

            //実動作を呼ぶスイッチ2
            var BrightSaturationObj = GetOrCreateObject(UnitColorChangerObj, "明るさ");
            var BrightSaturationMenu = BrightSaturationObj.AddComponent<ModularAvatarMenuItem>();
            BrightSaturationMenu.Control.name = BrightSaturationObj.name;
            BrightSaturationMenu.Control.type = ControlType.TwoAxisPuppet;
            // BrightSaturationMenu.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{ProjectFolder}/Ico/Color2.png");
            var CurrentParameter2 = new Parameter[2];
            CurrentParameter2[0] = new Parameter() { name = $@"{suffix}/BrightNess" };
            CurrentParameter2[1] = new Parameter() { name = $@"{suffix}/Saturation" };
            BrightSaturationMenu.Control.subParameters = CurrentParameter2;

            if (!ResolvedColorType.Contains(ColorType))
            {
                ResolvedColorType.Add(ColorType);
                //MAパラメータの定義
                string[] ColorParams = { "Hue", "BrightNess", "Saturation", "Gamma" };
                var MAP = UnitColorChangerObj.AddComponent<ModularAvatarParameters>();
                MAP.parameters = new List<ParameterConfig>();
                //List<string> LateSyncParam = new List<string>();
                foreach (var c in ColorParams)
                {
                    MAP.parameters.Add(new ParameterConfig() { nameOrPrefix = $@"{suffix}/{c}", syncType = ParameterSyncType.Float, saved = true ,localOnly=true });
                    //LateSyncParam.Add($@"{suffix}/{c}");
                }
                //var LateSync = UnitColorChangerObj.AddComponent<ExLateSync>();
                //LateSync.SyncParamName = LateSyncParam.ToArray();

                //アニメとDBTの定義
                var ac = new AnimationClipsBuilder(ProjectFolder);
                foreach (var obj in TargetObjNames)
                {
                    ac.Name($@"{ColorType}Hue-1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.x").Keys(0, -.5f);
                    ac.Name($@"{ColorType}Hue0").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.x").Keys(0, 0);
                    ac.Name($@"{ColorType}Hue1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.x").Keys(0, .5f);
                    ac.Name($@"{ColorType}BrightNess-1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.y").Keys(0, 0);
                    ac.Name($@"{ColorType}BrightNess0").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.y").Keys(0, 1);
                    ac.Name($@"{ColorType}BrightNess1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.y").Keys(0, 2);
                    ac.Name($@"{ColorType}Saturation-1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.z").Keys(0, 0);
                    ac.Name($@"{ColorType}Saturation0").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.z").Keys(0, 1);
                    ac.Name($@"{ColorType}Saturation1").Curve(obj, typeof(SkinnedMeshRenderer), "material._MainTexHSVG.z").Keys(0, 5);
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
                    foreach (var param in ColorParams)
                    {
                        bb.Param("1").Add1D($@"Pan/UnlimitedColor/{ColorType}/{param}", () =>
                        {
                            for (int n = -1; n < 2; n++)
                            {
                                bb.Param(n).CM(ac.Clip($@"{ColorType}{param}{n}"));
                            }
                        });
                    }
                });
            }
        }
    }
}