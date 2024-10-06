using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.unlimitedcolor.runtime.Generic_Dev;

namespace com.github.pandrabox.unlimitedcolor.runtime
{
    public static partial class Generic_Dev
    {
#if UNITY_EDITOR
        static public GameObject CreateObject(GameObject Parent, string Name, Action<GameObject> InitialAction = null)
        {
            GameObject res = new GameObject(Name);
            res.transform.SetParent(Parent.transform);
            if (InitialAction != null)
            {
                InitialAction(res);
            }
            return res;
        }
        static public GameObject ReCreateObject(GameObject Parent, string Name, Action<GameObject> InitialAction = null)
        {
            GameObject res = Parent.transform.Find(Name)?.gameObject;
            if (res != null)
            {
                GameObject.DestroyImmediate(res);
            }
            return CreateObject(Parent, Name, InitialAction);
        }

        static public GameObject GetOrCreateObject(GameObject Parent, string Name, Action<GameObject> InitialAction = null)
        {

            GameObject res = Parent.transform.Find(Name)?.gameObject;
            if (res != null)
            {
                return res;
            }
            return CreateObject(Parent, Name, InitialAction);
        }



        public static string GetHierarchyPath(GameObject targetObj)
        {
            List<GameObject> objPath = new List<GameObject>();
            objPath.Add(targetObj);
            for (int i = 0; objPath[i].transform.parent != null; i++)
                objPath.Add(objPath[i].transform.parent.gameObject);
            string path = objPath[objPath.Count - 1].gameObject.name;
            for (int i = objPath.Count - 2; i >= 0; i--)
                path += "/" + objPath[i].gameObject.name;

            return path;
        }

        public static GameObject FindGameObjectIgnoreCase(string name)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            foreach (GameObject obj in objects)
            {
                if (string.Equals(obj.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return obj;
                }
            }
            return null;
        }

        public static int NextPowerOfTwoExponent(int paramNum)
        {
            if (paramNum <= 0)
            {
                return 0;
            }
            paramNum--;
            int exponent = 0;
            while (paramNum > 0)
            {
                paramNum >>= 1;
                exponent++;
            }
            return exponent;
        }

        public static void InstantiateEnvironment(GameObject targetObject)
        {
            if (targetObject != null && targetObject.transform.Find("Environment") == null)
            {
                string prefabPath = "Assets/Pan/Lib/Environment/Environment.prefab";
                GameObject environmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (environmentPrefab != null)
                {
                    GameObject environmentInstance = GameObject.Instantiate(environmentPrefab);
                    environmentInstance.name = "Environment";
                    if (environmentInstance != null)
                    {
                        environmentInstance.transform.SetParent(targetObject.transform);
                    }
                    else
                    {
                        Debug.LogError("Failed to instantiate environment prefab.");
                    }
                }
                else
                {
                    Debug.LogError("Environment prefab not found in resources.");
                }
            }
        }
#endif
    }
}