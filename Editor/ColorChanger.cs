using UnityEngine;
using nadena.dev.ndmf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using nadena.dev.modular_avatar.core;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using static com.github.pandrabox.unlimitedcolor.runtime.Generic;
using static com.github.pandrabox.unlimitedcolor.runtime.Generic_Dev;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.unlimitedcolor.runtime.config;
using System.IO;
using com.github.pandrabox.unlimitedcolor.runtime;
using com.github.pandrabox.unlimitedcolor.editor;

namespace com.github.pandrabox.unlimitedcolor.editor
{

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

    public class ColorChangerMain
    {
        public string ProjectFolder = "Assets/Pan/ClothManager/ColorChanger";
        public GameObject AvatarRoot;
        public ColorParam[] ColorParams;
        public UnlimitedColor Target;
        public List<Renderer> SoloRenderers;
        private readonly string IcoFolder = "Packages/com.github.pandrabox.unlimitedcolor/Assets/Ico/";
        public ColorChangerMain(UnlimitedColor tgt)
        {
            Target = tgt;
            AvatarRoot = FindComponentFromParent<VRCAvatarDescriptor>(Target.transform).gameObject;
            SoloRenderers = new List<Renderer>(AvatarRoot.GetComponentsInChildren<Renderer>(true));
            ColorParams = new ColorParam[]
            {
                new ColorParam("Hue", "色相"),
                new ColorParam("Saturation", "彩度"),
                new ColorParam("Value", "明度"),
                new ColorParam("Gamma", "ガンマ")
            };
            Run();

            new PanMergeBlendTreePass().run(AvatarRoot);
        }
        public void Run()
        {
            int n = 0;
            foreach(RendererGroup rGroup in Target.RendererGroups)
            {
                //何等かのグループに属していたら個別からは削除
                foreach (Renderer renderer in rGroup.Renderers)
                {
                    if (SoloRenderers.Contains(renderer))
                    {
                        SoloRenderers.Remove(renderer);
                    }
                }
                if (rGroup.GroupName == "OutOfTarget") continue;
                var safeName = SanitizeFileName(rGroup.GroupName);
                if (safeName == null || safeName == "" || safeName == "Untitled")
                {
                    safeName = $@"Untitled{n:D3}";
                }
                if(DEBUGMODE)
                {
                    Debug.LogWarning(safeName);
                }
                //グループ定義したもののチェンジャー
                MakeUnitColorChanger(safeName, "", RenderersToPaths(rGroup.Renderers));
                n++;
            }
            if (!Target.Explicit)
            {
                foreach (Renderer renderer in SoloRenderers)
                {
                    //未定義の個別チェンジャー
                    MakeUnitColorChanger(renderer.name, "", RenderersToPaths(new Renderer[1] { renderer }));
                }
            }
        }


        public string[] RenderersToPaths(Renderer[] renderers)
        {
            List<string> pathsList = new List<string>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    string path = RendererToPath(renderer);
                    if (!string.IsNullOrEmpty(path))
                    {
                        pathsList.Add(path);
                    }
                }
            }
            return pathsList.ToArray();
        }

        public string RendererToPath(Renderer tgt)
        {
            return FindPathRecursive(AvatarRoot.transform, tgt.transform);
        }

        /// <summary>
        /// fileName用の文字列から危険な文字を除いていい感じの名前を返す
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string SanitizeFileName(string fileName)
        {
            // 空文字列やnullの場合はデフォルト名を返す
            if (string.IsNullOrEmpty(fileName))
                return "Untitled";

            // 無効な文字を空文字に置き換える
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), string.Empty);
            }

            // スペースやその他の余分な文字をトリム
            fileName = fileName.Trim();

            // 空のファイル名の場合はデフォルト名を返す
            return string.IsNullOrEmpty(fileName) ? "Untitled" : fileName;
        }

        public void MakeUnitColorChanger(string safeName, string ColorType, string[] TargetObjNames)
        {
            safeName = SanitizeFileName(safeName);
            //ルートオブジェクトNDMFColorChangerの作成（ただの枠）
            var ColorChangerRoot = GetOrCreateObject(AvatarRoot, "NDMFColorChanger");
            //FlatsClothオブジェクトを直下に作成（後でマージするため着せ替えと同じ名称ツリーにする）
            var DummyFCM = GetOrCreateObject(ColorChangerRoot, "UnlimitedColor", (GameObject x) =>
            {
                x.AddComponent<ModularAvatarMenuInstaller>();
                var CCRM = x.AddComponent<ModularAvatarMenuItem>();
                CCRM.Control.name = "UnlimitedColor";
                CCRM.Control.type = ControlType.SubMenu;
                CCRM.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{IcoFolder}Hue.png");
                CCRM.MenuSource = SubmenuSource.Children;
            });

            //実動作を仕舞う入れ物の作成（スラッシュ区切りで階層と名称を指定、後で中に「色合い(Hue,Gamma)」と「明るさ(Value,Saturation)」を入れる
            GameObject CurrentObj = null, UnitColorChangerObj = null;
            var NameHierarchy = safeName.Split('/');
            for (var n = 0; n < NameHierarchy.Length; n++)
            {
                CurrentObj = GetOrCreateObject(n == 0 ? DummyFCM : CurrentObj, NameHierarchy[n]);
                var CurrentMAMI = CurrentObj.AddComponent<ModularAvatarMenuItem>();
                CurrentMAMI.Control.name = safeName;
                CurrentMAMI.Control.type = ControlType.SubMenu;
                CurrentMAMI.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{IcoFolder}Hue.png");
                CurrentMAMI.MenuSource = SubmenuSource.Children;
                UnitColorChangerObj = CurrentObj;
            }


            //実動作を呼ぶスイッチ1
            var suffix = $@"Pan/UnlimitedColor/{safeName}";
            foreach(var colorParam in ColorParams)
            {
                var obj = GetOrCreateObject(UnitColorChangerObj, colorParam.jp);
                var menu = obj.AddComponent<ModularAvatarMenuItem>();
                menu.Control.type = ControlType.RadialPuppet;
                menu.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{IcoFolder}{colorParam.eng}.png");
                var CurrentParameter = new Parameter[1];
                CurrentParameter[0] = new Parameter() { name = $@"{suffix}/{colorParam.eng}" };
                menu.Control.subParameters = CurrentParameter;
            }

            //MAパラメータの定義
            ModularAvatarParameters MAP = UnitColorChangerObj.GetComponent<ModularAvatarParameters>();
            if (MAP == null)
            {
                MAP = UnitColorChangerObj.AddComponent<ModularAvatarParameters>();
                MAP.parameters = new List<ParameterConfig>();
            }
            foreach (var colorParam in ColorParams)
            {
                MAP.parameters.Add(new ParameterConfig() { nameOrPrefix = $@"{suffix}/{colorParam.eng}", syncType = ParameterSyncType.Float, saved = true, localOnly = false, defaultValue = 0.5f });
            }



            //アニメとDBTの定義
            var ac = new AnimationClipsBuilder(ProjectFolder);
            foreach (var obj in TargetObjNames)
            {
                ac.Name($@"{safeName}Hue-1").Curve(obj, typeof(Renderer), "material._MainTexHSVG.x").Keys(0, -.5f);
                ac.Name($@"{safeName}Hue0").Curve(obj, typeof(Renderer), "material._MainTexHSVG.x").Keys(0, 0);
                ac.Name($@"{safeName}Hue1").Curve(obj, typeof(Renderer), "material._MainTexHSVG.x").Keys(0, .5f);
                ac.Name($@"{safeName}Saturation-1").Curve(obj, typeof(Renderer), "material._MainTexHSVG.y").Keys(0, 0);
                ac.Name($@"{safeName}Saturation0").Curve(obj, typeof(Renderer), "material._MainTexHSVG.y").Keys(0, 1);
                ac.Name($@"{safeName}Saturation1").Curve(obj, typeof(Renderer), "material._MainTexHSVG.y").Keys(0, Target.SaturationMax);
                ac.Name($@"{safeName}Value-1").Curve(obj, typeof(Renderer), "material._MainTexHSVG.z").Keys(0, 0);
                ac.Name($@"{safeName}Value0").Curve(obj, typeof(Renderer), "material._MainTexHSVG.z").Keys(0, 1);
                ac.Name($@"{safeName}Value1").Curve(obj, typeof(Renderer), "material._MainTexHSVG.z").Keys(0, Target.ValueMax);
                ac.Name($@"{safeName}Gamma-1").Curve(obj, typeof(Renderer), "material._MainTexHSVG.w").Keys(0, 0.01f);
                ac.Name($@"{safeName}Gamma0").Curve(obj, typeof(Renderer), "material._MainTexHSVG.w").Keys(0, 1);
                ac.Name($@"{safeName}Gamma1").Curve(obj, typeof(Renderer), "material._MainTexHSVG.w").Keys(0, Target.GammaMax);
            }
            if (DEBUGMODE)
            {
                foreach (var unitClip in ac.AnimationClips)
                {
                    string ClipPath = Path.Combine(DebugOutpFolder, $"{unitClip.Key}.anim");
                    if (!File.Exists(ClipPath))
                    {
                        AssetDatabase.CreateAsset(unitClip.Value.Outp(), ClipPath);
                    }
                }
            }
            var bb = new BlendTreeBuilderForNDMF(ProjectFolder, ColorChangerRoot, false, AvatarRoot, safeName);
            bb.rootDBT(() =>
            {
                foreach (var colorParam in ColorParams)
                {
                    bb.Param("1").Add1D($@"Pan/UnlimitedColor/{safeName}/{colorParam.eng}", () =>
                    {
                        for (int n = -1; n <= 1; n++)
                        {
                            bb.Param(((float)n + 1) / 2).CM(ac.Clip($@"{safeName}{colorParam.eng}{n}"));
                        }
                    });
                }
            });
        }
    }
}