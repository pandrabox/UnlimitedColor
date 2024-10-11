using UnityEngine;
using nadena.dev.ndmf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using nadena.dev.modular_avatar.core;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using VRC.SDK3.Avatars.Components;
using System.IO;
using com.github.pandrabox.unlimitedcolor.runtime;
using com.github.pandrabox.unlimitedcolor.editor;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using static com.github.pandrabox.pandravase.runtime.Util;

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
                new UnlimitedColorMain(tgt);

            });
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

    public class UnlimitedColorMain
    {
        public ColorParam[] ColorParams;
        public UnlimitedColor Target;
        public List<Renderer> SoloRenderers;
        public PandraProject Prj;
        public UnlimitedColorMain(UnlimitedColor tgt)
        {
            Prj = new PandraProject(tgt.gameObject, "UnlimitedColor", ProjectTypes.VPM);
            Target = tgt;
            SoloRenderers = new List<Renderer>(Prj.RootTransform.GetComponentsInChildren<Renderer>(true));
            ColorParams = new ColorParam[]
            {
                new ColorParam("Hue", "色相"),
                new ColorParam("Saturation", "彩度"),
                new ColorParam("Value", "明度"),
                new ColorParam("Gamma", "ガンマ")
            };
            Run();

            new PanMergeBlendTreePlugin(Prj.RootObject);
        }
        public void Run()
        {
            int n = 0;
            foreach (RendererGroup rGroup in Target.RendererGroups)
            {
                //何等かのグループに属していたら個別からは削除
                foreach (Renderer renderer in rGroup.Renderers)
                {
                    if (SoloRenderers.Contains(renderer))
                    {
                        SoloRenderers.Remove(renderer);
                    }
                }
                //除外グループなら処理を終了
                if (rGroup.GroupName == "OutOfTarget") continue;
                //安全なグループ名の算出
                var safeName = SanitizeFileName(rGroup.GroupName);
                if (safeName == null || safeName == "" || safeName == "Untitled")
                {
                    safeName = $@"Untitled{n:D3}";
                }
                // シェーダーチェック
                for (int i = 0; i < rGroup.Renderers.Length; i++)
                {
                    if (!IsLil(rGroup.Renderers[i]))
                    {
                        rGroup.Renderers[i] = null;
                    }
                }

                //グループ定義したもののチェンジャー
                CreateUnitColorChanger(safeName, RenderersToPaths(rGroup.Renderers));
                n++;
            }
            if (!Target.Explicit)
            {
                foreach (Renderer renderer in SoloRenderers)
                {
                    //未定義の個別チェンジャー
                    if (!IsLil(renderer)) continue;
                    CreateUnitColorChanger(renderer.name, RenderersToPaths(new Renderer[1] { renderer }));
                }
            }
        }

        /// <summary>
        /// Rendererの配列からRelativePathの配列を得る
        /// </summary>
        /// <param name="renderers"></param>
        /// <returns></returns>
        public string[] RenderersToPaths(Renderer[] renderers)
        {
            List<string> pathsList = new List<string>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    string path = GetRelativePath(Prj.RootTransform, renderer.transform);
                    if (!string.IsNullOrEmpty(path))
                    {
                        pathsList.Add(path);
                    }
                }
            }
            return pathsList.Count == 0 ? null : pathsList.ToArray();
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

        public void CreateUnitColorChanger(string safeName, string[] targetObjNames)
        {
            // 対象が空の場合（空グループ,lilToonでないものだけ）は何もしない
            if (targetObjNames == null) return;

            // メニューのルート定義または取得
            var menuRoot = Prj.GetOrCreateComponentObject<ModularAvatarMenuItem>("RootMenu", (x) =>
            {
                x.gameObject.AddComponent<ModularAvatarMenuInstaller>();
                x.Control.name = "UnlimitedColor";
                x.Control.type = ControlType.SubMenu;
                x.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{Prj.ImgFolder}Hue.png");
                x.MenuSource = SubmenuSource.Children;
            });
            // 対象1つ分のサブメニュー作成
            safeName = SanitizeStr(safeName);
            var colorMenu = ReCreateComponentObject<ModularAvatarMenuItem>(menuRoot.transform, safeName, (x) =>
            {
                x.Control.name = safeName;
                x.Control.type = ControlType.SubMenu;
                x.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{Prj.ImgFolder}Hue.png");
                x.MenuSource = SubmenuSource.Children;
            });
            // それぞれの色調整メニュー作成
            foreach (var colorParam in ColorParams)
            {
                var ColorPuppet = CreateComponentObject<ModularAvatarMenuItem>(colorMenu.transform, colorParam.jp, (x) =>
                {
                    x.Control.name = colorParam.jp;
                    x.Control.type = ControlType.RadialPuppet;
                    x.Control.icon = AssetDatabase.LoadAssetAtPath<Texture2D>($@"{Prj.ImgFolder}{colorParam.eng}.png");
                    x.Control.subParameters = new[] { new Parameter { name = Prj.GetParameterName($@"{safeName}/{colorParam.eng}") } };
                });
            }

            //MAパラメータの定義
            var MAP = Prj.GetOrCreateComponentObject<ModularAvatarParameters>("Parameter", (x) =>
            {
                x.parameters = new List<ParameterConfig>();
            });
            foreach (var colorParam in ColorParams)
            {
                MAP.parameters.Add(new ParameterConfig { nameOrPrefix = Prj.GetParameterName($@"{safeName}/{colorParam.eng}"), syncType = ParameterSyncType.Float, saved = true, localOnly = false, defaultValue = 0.5f });
            }

            //アニメの定義
            var ac = new AnimationClipsBuilder(Prj);
            foreach (var obj in targetObjNames)
            {
                ac.Clip($@"{safeName}Hue-1").Bind(obj, typeof(Renderer), "material._MainTexHSVG.x").Const2F(-.5f);
                ac.Clip($@"{safeName}Hue0").Bind(obj, typeof(Renderer), "material._MainTexHSVG.x").Const2F(0);
                ac.Clip($@"{safeName}Hue1").Bind(obj, typeof(Renderer), "material._MainTexHSVG.x").Const2F(.5f);
                ac.Clip($@"{safeName}Saturation-1").Bind(obj, typeof(Renderer), "material._MainTexHSVG.y").Const2F(0);
                ac.Clip($@"{safeName}Saturation0").Bind(obj, typeof(Renderer), "material._MainTexHSVG.y").Const2F(1);
                ac.Clip($@"{safeName}Saturation1").Bind(obj, typeof(Renderer), "material._MainTexHSVG.y").Const2F(Target.SaturationMax);
                ac.Clip($@"{safeName}Value-1").Bind(obj, typeof(Renderer), "material._MainTexHSVG.z").Const2F(0);
                ac.Clip($@"{safeName}Value0").Bind(obj, typeof(Renderer), "material._MainTexHSVG.z").Const2F(1);
                ac.Clip($@"{safeName}Value1").Bind(obj, typeof(Renderer), "material._MainTexHSVG.z").Const2F(Target.ValueMax);
                ac.Clip($@"{safeName}Gamma-1").Bind(obj, typeof(Renderer), "material._MainTexHSVG.w").Const2F(0.01f);
                ac.Clip($@"{safeName}Gamma0").Bind(obj, typeof(Renderer), "material._MainTexHSVG.w").Const2F(1);
                ac.Clip($@"{safeName}Gamma1").Bind(obj, typeof(Renderer), "material._MainTexHSVG.w").Const2F(Target.GammaMax);
            }
            ac.DebugSave();

            //DBTの定義
            var bb = new BlendTreeBuilder(Prj, false, safeName, Prj.RootObject);
            bb.RootDBT(() =>
            {
                foreach (var colorParam in ColorParams)
                {
                    bb.Param("1").Add1D($@"{safeName}/{colorParam.eng}", () =>
                    {
                        for (int n = -1; n <= 1; n++)
                        {
                            bb.Param((float)n / 2 + .5f).AddMotion(ac.Outp($@"{safeName}{colorParam.eng}{n}"));
                        }
                    });
                }
            });
        }
    }
}