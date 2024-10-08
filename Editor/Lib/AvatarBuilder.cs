using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.IO;

namespace com.github.pandrabox.unlimitedcolor.editor
{
    public class AvatarBuilder
    {
        const string ONEPARAM = "__ModularAvatarInternal/One";
        public string ProjectFolder;
        public GameObject TargetObject;
        public bool IsNDMF;
        public string ParamNormalizePreposition;
        public AvatarBuilder(string _ProjectFolder = "", GameObject _TargetObject = null)
        {
            TargetObject = _TargetObject;
            setProjectFolder(_ProjectFolder);
        }
        public void SetParamNormalizePreposition(string ParamNormalizePreposition)
        {
            ParamNormalizePreposition = ParamNormalizePreposition.EndsWith("/") ? ParamNormalizePreposition.Substring(0, ParamNormalizePreposition.Length - 1) : ParamNormalizePreposition;
            ParamNormalizePreposition = ParamNormalizePreposition.StartsWith("$") ? ParamNormalizePreposition.Substring(1) : ParamNormalizePreposition;
            this.ParamNormalizePreposition=ParamNormalizePreposition; 
        }
        private void setProjectFolder(string _ProjectFolder)
        {
            if (_ProjectFolder.Length == 0)
            {
                IsNDMF = true;
                _ProjectFolder = "Assets/Pan/Tmp";
            }
            ProjectFolder = $@"{_ProjectFolder.Replace("\\", "/")}/";
            ProjectFolder = ProjectFolder.Replace("//", "/").Replace("/Gen/", "/").Replace("/Editor/", "/").Replace("/Res/", "/").Replace("/ReSource/", "/");
            ProjectFolder = getAssetsPath(ProjectFolder);
        }
        public string ProjectNameSimple()
        {
            string[] parts = ProjectFolder.Split('/');
            if (parts.Length > 0)
            {
                return parts[parts.Length - 2];
            }
            return "";
        }
        public string projectName()
        {
            string t = ProjectFolder.Replace("Assets/", "");
            t = t.Substring(0, t.Length - 1);
            return t;
        }
        public string NormalizedParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new NotImplementedException("ParameterName is Null");
            }
            string res;
            if (parameterName == "1" || parameterName == "ONEf" || parameterName == "PBTB_CONST_1" || parameterName == ONEPARAM)
            {
                res = ONEPARAM;
            }
            else if (parameterName == "GestureRight" || parameterName == "GestureLeft" || parameterName == "GestureRightWeight" || parameterName == "GestureLeftWeight" || parameterName == "IsLocal"|| parameterName == "InStation" || parameterName == "Seated" || parameterName == "VRMode" || parameterName.StartsWith("Pan/") || parameterName.StartsWith("Env/"))
            {
                res = parameterName;
            }
            else if (parameterName == "Time") res = "Env/Time";
            else if (parameterName == "ExLoaded") res = "Env/ExLoaded";
            else if (parameterName == "IsMMD") res = "Env/IsMMD";
            else if (parameterName == "IsNotMMD") res = "Env/IsNotMMD";
            else if (parameterName == "FrameTime") res = "Env/FrameTime";
            else if (parameterName.Length > 0 && parameterName[0] == '$')
            {
                res = parameterName.Substring(1);
            }
            else
            {
                if (ParamNormalizePreposition != null && ParamNormalizePreposition.Length > 0)
                {
                    res = $@"{ParamNormalizePreposition}/{parameterName}";
                }
                else
                {
                    res = $@"{projectName()}/{parameterName}";
                }
            }
            if (res.Contains("PBTB_CONST"))
            {
                throw new Exception("directive has compatibility issues with MA and cannot be used.");
            }
            return res;
        }
        private string NormalizedMotionPath(string motionPath)
        {
            motionPath = motionPath.Trim().Replace("\\", "/");
            if (!motionPath.Contains("/"))
            {
                motionPath = $@"{ProjectFolder}Res/{motionPath}";
            }
            motionPath = $@"{motionPath.Replace(".anim", "")}.anim";
            if (!File.Exists(motionPath))
            {
                motionPath = motionPath.Replace("Res", "Gen");
            }
            return getAssetsPath(motionPath);
        }
        public string getAssetsPath(string path)
        {
            path = $@"Assets/{path}".Replace("Assets/Assets/", "Assets/");
            int assetsIndex = path.IndexOf("Assets");
            path = path.Substring(assetsIndex);
            return path;
        }
        public AnimationClip LoadMotion(string path)
        {
            path = NormalizedMotionPath(path);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Animation clip path is empty.");
                return null;
            }
            if (!File.Exists(path))
            {
                Debug.LogError("Animation clip does not exist: " + path);
                return null;
            }
            if (!path.EndsWith(".anim"))
            {
                Debug.LogError("Animation clip file format is invalid: " + path);
                return null;
            }
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                Debug.LogError("Failed to load animation clip: " + path);
                return null;
            }
            return clip;
        }
        public void createFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
        public void createGenFolder()
        {
            createFolder(genFolder());
        }
        public void createAAPFolder()
        {
            createFolder(AAPFolder());
        }
        public string genFolder()
        {
            return $@"{ProjectFolder}Gen/";
        }
        public string AAPFolder()
        {
            return $@"{genFolder()}AAP/";
        }

    }




    public static class BlendTreeExtensions
    {
        public static void AddDirectChild(this BlendTree blendTree, Motion childTree, string parameterName = "1")
        {
            if (blendTree == null)
            {
                throw new ArgumentNullException("blendTree not found");
            }
            if (childTree == null)
            {
                throw new ArgumentNullException("childTree not found");
            }
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentNullException("parameterName not found");
            }
            blendTree.AddChild(childTree);
            var c = blendTree.children;
            c[c.Length - 1].directBlendParameter = parameterName;
            blendTree.children = c;
        }
    }
}