using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using System.IO;

namespace com.github.pandrabox.unlimitedcolor.runtime
{
    public static class config
    {
        public const bool DEBUGMODE = false;
        private const string DEBUGOUTPFOLDER = "Assets/pandrabox/Debug/";

        public static string DebugOutpFolder
        {
            get
            {
                if (DEBUGMODE)
                {
                    if (!Directory.Exists(DEBUGOUTPFOLDER)) Directory.CreateDirectory(DEBUGOUTPFOLDER);
                    return DEBUGOUTPFOLDER;
                }
                else
                {
                    Debug.LogWarning("PandraBox.DebugOutpFolder : この機能はDEBUG専用ですが、非DEBUGMODEで実行されました。開発者に連絡して下さい。");
                    return null;
                }
            }
        }
    }
}
