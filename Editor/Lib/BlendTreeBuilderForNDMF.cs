using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using static com.github.pandrabox.unlimitedcolor.runtime.config;

namespace com.github.pandrabox.unlimitedcolor.editor
{
    public partial class BlendTreeBuilderForNDMF : AvatarBuilder
    {
        List<BlendTree> BuildingTrees;
        public BlendTree RootTree;
        public int CurrentNum = 0;
        public string NextName;
        public bool IsAbsolute;
        public bool IsMMDSafe;
        /// <summary>
        /// BlendTreeを生成してMergeBlendTreeする。MBTなのでそのままでは使いにくい
        /// </summary>
        /// <param name="_ProjectFolder">作業ファイルの一時生成場所や各種命名に使われる基準パス</param>
        /// <param name="AvatarRootObject">TargetObject変数に代入される。特に内部処理はない</param>
        /// <param name="IsAbsolute">基本ON。MergeBlendTreeのパス解決方法</param>
        public BlendTreeBuilderForNDMF(string _ProjectFolder, GameObject AvatarRootObject, bool IsAbsolute=true) : base(_ProjectFolder, AvatarRootObject)
        {
            BuildingTrees = new List<BlendTree>() { null, new BlendTree() };
            RootTree = BuildingTrees[1];
            RootTree.blendType = BlendTreeType.Direct;
            CurrentNum = 1;
            this.IsAbsolute = IsAbsolute;
            if (DEBUGMODE)
            {
                Debug.LogWarning($@"BlendTreeBuilderはDebugモードで起動しています。このモードではProjectFolder(現在{ProjectFolder})へのファイル出力等が実行されます。配布時このモードはOFFになっているべきです。");
            }
        }
        public int MaxNum()
        {
            return BuildingTrees.Count - 1;
        }
        public virtual void Apply()
        {
            ModularAvatarMergeBlendTree MAMergeBlendTree = TargetObject.AddComponent<ModularAvatarMergeBlendTree>();
            MAMergeBlendTree.BlendTree = RootTree;
            if (IsAbsolute)
            {
                MAMergeBlendTree.PathMode = MergeAnimatorPathMode.Absolute;
            }
            if (DEBUGMODE)
            {
                AssetDatabase.CreateAsset(RootTree,$@"{DebugOutpFolder}UnlimitedColor.asset");
            }
        }
        public void setDirectBlendParameter(UnityEditor.Animations.BlendTree parentBlendTree, string parameterName, int setChildNum = -1)
        {
            var c = parentBlendTree.children;
            if (setChildNum == -1) setChildNum = c.Length - 1;
            c[setChildNum].directBlendParameter = parameterName;
            parentBlendTree.children = c;
        }
        public BlendTree getTree(int n)
        {
            return BuildingTrees[n];
        }
        public BlendTree currentTree()
        {
            return BuildingTrees[CurrentNum];
        }
        public BlendTreeBuilderForNDMF nName(string _NextName)
        {
            NextName = _NextName;
            return this;
        }
        public virtual AnimationClip AAP(params object[] args)
        {
            AnimationClip animationClip = new AnimationClip();
            for (int i = 0; i < args.Length; i += 2)
            {
                string paramName = NormalizedParameterName((string)args[i]);
                float val = Convert.ToSingle(args[i + 1]);
                EditorCurveBinding curveBinding = EditorCurveBinding.FloatCurve("", typeof(Animator), $"{paramName}");
                AnimationCurve curve = new AnimationCurve(new Keyframe(0, val));
                AnimationUtility.SetEditorCurve(animationClip, curveBinding, curve);
            }
            return animationClip;
        }
        public void rootDBT(Action act = null)
        {
            //When specified before RootDBT, it becomes MMDSafe DBT. MMDSafe safely stops the DBT before its layer weight becomes 0. An Environment prefab is required for MMDSafe DBT to operate
            if (act != null)
            {
                if (IsMMDSafe)
                {
                    Param("Env/DBTEnable").AddD(()=>act());
                    IsMMDSafe = false;
                }
                else
                {
                    act();
                }
            }
            Apply();
        }

        public BlendTreeBuilderForNDMF MMDSafe()
        {
            //When specified before RootDBT, it becomes MMDSafe DBT. MMDSafe safely stops the DBT before its layer weight becomes 0. An Environment prefab is required for MMDSafe DBT to operate
            IsMMDSafe = true;
            return this;
        }

        public string ParentDirectParameterName;
        public float ParentThreshold, ParentThresholdY;
        public BlendTreeType ParentTreeType;
        public bool ChildWait;
        public BlendTreeBuilderForNDMF Param(string DirectParameterName)
        {
            return ParentTreeParameterSet(BlendTreeType.Direct,DirectParameterName,0,0);
        }
        public BlendTreeBuilderForNDMF Param(float Threshold)
        {
            return ParentTreeParameterSet(BlendTreeType.Simple1D, null, Threshold, 0);
        }
        public BlendTreeBuilderForNDMF Param(float Threshold, float ThresholdY)
        {
            return ParentTreeParameterSet(BlendTreeType.SimpleDirectional2D, null, Threshold, ThresholdY);
        }
        public BlendTreeBuilderForNDMF ParentTreeParameterSet(BlendTreeType TreeType, string DirectParameterName, float Threshold, float ThresholdY)
        {
            if (currentTree().blendType != TreeType)
            {
                Debug.LogError($@"[BlendTreeEditor] Type Mismatch Error:You specified ParentType={ParentTreeType}, but the current ParentType is {currentTree().blendType} ");
            }
            if (ChildWait)
            {
                Debug.LogWarning("[BlendTreeEditor] ChildWait Already Set! It is better to use the C method immediately after executing the P method.");
            }
            ParentTreeType = TreeType;
            ParentDirectParameterName = DirectParameterName;
            ParentThreshold = Threshold;
            ParentThresholdY = ThresholdY;
            ChildWait = true;
            return this;
        }
        public void AssignmentBy1D(string FromAAPName, float FromAAPMin, float FromAAPMax, string ToAAPName, float? ToAAPMin=null, float?ToAAPMax=null)
        {
            if(ToAAPMin==null) ToAAPMin= FromAAPMin;
            if (ToAAPMax == null) ToAAPMax = FromAAPMax;
            Add1D(FromAAPName, () =>
            {
                Param(FromAAPMin).AddAAP(ToAAPName, ToAAPMin);
                Param(FromAAPMax).AddAAP(ToAAPName, ToAAPMax);
            });
        }

        public Action ChildAct;
        public string ChildThresholdName, ChildThresholdNameY;
        public Motion ChildMotionClip;
        public bool ChildIsBlendTree;
        public BlendTreeType ChildTreeType;
        public void AddD(Action act = null)
        {
            ChildSet(true, BlendTreeType.Direct, null, null, null, act);
        }
        public void Add1D(string ThresholdName, Action act)
        {
            ChildSet(true, BlendTreeType.Simple1D, ThresholdName, null, null, act);
        }
        public void Add2D(string ThresholdName, string ThresholdNameY, Action act)
        {
            ChildSet(true, BlendTreeType.SimpleDirectional2D, ThresholdName, ThresholdNameY, null, act);
        }
        public void AddMotion(string MotionPath)
        {
            CM(LoadMotion(MotionPath));
        }
        public void AddAAP(params object[] args)
        {
            CM(AAP(args));
        }
        public void CM(Motion MotionClip)
        {
            ChildSet(false, 0, null, null, MotionClip, null);
        }
        public void ChildSet(bool IsBlendTree, BlendTreeType TreeType, string ThresholdName, string ThresholdNameY, Motion MotionClip, Action act)
        {
            if (currentTree().blendType != ParentTreeType)
            {
                Debug.LogError($@"[BlendTreeEditor] Type Mismatch Error:You specified ParentType={ParentTreeType}, but the current ParentType is {currentTree().blendType} ");
            }
            if (!ChildWait)
            {
                Debug.LogWarning("[BlendTreeEditor] If you don't intend for this situation, it will lead to terrible results.");
            }
            ChildWait = false;
            ChildTreeType = TreeType;
            ChildIsBlendTree = IsBlendTree;
            ChildThresholdName = ThresholdName;
            ChildThresholdNameY = ThresholdNameY;
            ChildMotionClip= MotionClip;
            ChildAct = act;
            MakeChild();
        }

        private void MakeChild()
        {
            int parentTreeNum = CurrentNum;
            BlendTree parentTree = getTree(parentTreeNum);
            if (parentTree.blendType == BlendTreeType.Simple1D)
            {
                foreach (var c in parentTree.children)
                {
                    if (c.threshold > ParentThreshold)
                    {
                        throw new Exception("[BlendTreeBuilder][1DTreeThresholdError] You tried to register a Threshold that is smaller than the one already registered! This creates terrible problems that are difficult to solve.");
                    }
                }
            }
            if (ChildIsBlendTree)
            {
                if (parentTree.blendType == BlendTreeType.SimpleDirectional2D)
                {
                    BuildingTrees.Add(parentTree.CreateBlendTreeChild(new Vector2(ParentThreshold, ParentThresholdY)));
                }
                else
                {
                    BuildingTrees.Add(parentTree.CreateBlendTreeChild(ParentThreshold));
                }
                CurrentNum = MaxNum();
                BlendTree MyTree = getTree(CurrentNum);
                MyTree.useAutomaticThresholds = false;
                if (NextName != null)
                {
                    MyTree.name = NextName;
                    NextName = null;
                }
                MyTree.blendType = ChildTreeType;
                if (ChildTreeType == BlendTreeType.Simple1D)
                {
                    MyTree.blendParameter = NormalizedParameterName(ChildThresholdName);
                }
                if (ChildTreeType == BlendTreeType.SimpleDirectional2D)
                {
                    MyTree.blendParameter = NormalizedParameterName(ChildThresholdName);
                    MyTree.blendParameterY = NormalizedParameterName(ChildThresholdNameY);
                }
            }
            else
            {
                if (currentTree().blendType == BlendTreeType.Simple1D)
                {
                    currentTree().AddChild(ChildMotionClip, ParentThreshold);
                }
                if (currentTree().blendType == BlendTreeType.SimpleDirectional2D)
                {
                    currentTree().AddChild(ChildMotionClip, new Vector2(ParentThreshold, ParentThresholdY));
                }
                if (currentTree().blendType == BlendTreeType.Direct)
                {
                    currentTree().AddDirectChild(ChildMotionClip, ParentDirectParameterName);
                }
            }

            if (parentTree.blendType == BlendTreeType.Direct)
            {
                setDirectBlendParameter(parentTree, NormalizedParameterName(ParentDirectParameterName));
            }
            if (ChildAct != null) ChildAct();
            CurrentNum = parentTreeNum;
        }








    }



}