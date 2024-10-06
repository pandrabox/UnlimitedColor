using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using static com.github.pandrabox.unlimitedcolor.runtime.config;

namespace com.github.pandrabox.unlimitedcolor.editor
{
    public class AvatarBuilderSampleClass
    {
        private void SampleMethod()
        {
            var ac = new AnimationClipsBuilder();
            AnimationClip Test1 = ac.Name("Test1").Curve("FlyingCloudObj/Collider", typeof(Transform), "m_LocalPosition.z").Keys(0, 1, 1, 0).Outp();
            ac.Name("Test2").Curve("FlyingCloudObj/Collider", typeof(Transform), "m_LocalPosition.z").Keys(0, 1, 1, 0);
            AnimationClip Test2 = ac.Clip("Test2");
            ac.Save();
        }
    }

    //A management class that creates, loads, and saves multiple animation Builders.
    public class AnimationClipsBuilder : AvatarBuilder
    {
        public Dictionary<string, AnimationClipBuilder> AnimationClips;
        public AnimationClipsBuilder(string ProjectFolder = "") : base(ProjectFolder)
        {
            AnimationClips=new Dictionary<string, AnimationClipBuilder>();
        }
        //Create or load a builder
        public AnimationClipBuilder Name(string ClipName)
        {
            if (!AnimationClips.ContainsKey(ClipName))
            {
                AnimationClips[ClipName] = new AnimationClipBuilder(ClipName);
            }
            return AnimationClips[ClipName];
        }
        /// <summary>
        /// Load result Animation Clip
        /// </summary>
        /// <param name="ClipName">The name of the clip to load</param>
        /// <returns>Loaded Animation Clips</returns>
        public AnimationClip Clip(string ClipName)
        {
            return Name(ClipName).Outp();
        }
        //Save Animation Clip Assets file (Editor Only)
        public void Save()
        {
#if UNITY_EDITOR
            if (ProjectFolder.Length == 0)
            {
                Debug.LogWarning("Save() requires the ProjectFolder to be specified.");
                return;
            }
            createGenFolder();
            foreach (var AnimationClip in AnimationClips)
            {
                string ClipPath = Path.Combine(genFolder(), $"{AnimationClip.Key}.anim");
                if (!File.Exists(ClipPath))
                {
                    AssetDatabase.CreateAsset(AnimationClip.Value.Outp(), ClipPath);
                }
            }
#endif
        }
    }

    //A class for building a single animation clip made up of multiple curves/keys.
    public class AnimationClipBuilder 
    {
        AnimationClip AnimationClip;
        EditorCurveBinding CurrentCurveBinding;
        bool CurrentLiner;
        List<Keyframe> CurrentKeys;
        public AnimationClipBuilder(string ClipName) {
            AnimationClip = new AnimationClip();
            AnimationClip.name = ClipName;
            CurveInitialize();
        }
        //Initialization before creating a new curve
        public void CurveInitialize()
        {
            CurrentCurveBinding = new EditorCurveBinding();
            CurrentLiner = false;
            CurrentKeys = new List<Keyframe>();
        }
        //Curve variable definition
        public AnimationClipBuilder Curve(string inPath, Type inType, string inPropertyName, bool IsLiner= false)
        {
            CurrentCurveBinding = EditorCurveBinding.FloatCurve(inPath, inType, inPropertyName);
            CurrentLiner = IsLiner;
            return this;
        }
        //Define Keyframe (and register curve to Clip. Curve definition is required beforehand)
        public AnimationClipBuilder Keys(params float[] KeyA)
        {
            for (int i = 0; i < KeyA.Length - 1; i += 2)
            {
                Keyframe k = new Keyframe(KeyA[i], KeyA[i + 1]);
                CurrentKeys.Add(k);
            }
            PushCurve();
            CurveInitialize();
            return this;
        }
        //Register curve to Clip
        public void PushCurve()
        {
            AnimationCurve Curve;
            if (CurrentLiner && CurrentKeys.Count == 2)
            {
                Curve = AnimationCurve.Linear(CurrentKeys[0].time, CurrentKeys[0].value, CurrentKeys[1].time, CurrentKeys[1].value);
            }
            else
            {
                Curve = new AnimationCurve(CurrentKeys.ToArray());
            }
            AnimationUtility.SetEditorCurve(AnimationClip, CurrentCurveBinding, Curve);
        }
        //Output Built Clips
        public AnimationClip Outp()
        {
            return AnimationClip;
        }
    }

    public class AnimationClipBuilder_old : AvatarBuilder
    {
        public AnimationClipsBuilder ACB;
        string CurrentName;
        public AnimationClipBuilder_old(string ProjectFolder) : base(ProjectFolder, null)
        {
            ACB = new AnimationClipsBuilder(ProjectFolder);
        }
        public AnimationClipBuilder_old clip(string name)
        {
            ACB.Name(name);
            CurrentName = name;
            return this;
        }
        public AnimationClipBuilder_old addCurve(string inPath, Type inType, string inPropertyName, bool IsLiner, params float[] keys)
        {
            ACB.Name(CurrentName).Curve(inPath,inType,inPropertyName, IsLiner).Keys(keys);
            return this;
        }
        public AnimationClipBuilder_old save()
        {
            ACB.Save();
            return this;
        }
    }
}


