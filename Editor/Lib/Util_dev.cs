using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.pandrabox.unlimitedcolor.editor
{
    public class PanUtil : MonoBehaviour
    {
        public BuildContext ctx;
        public PanUtil(BuildContext ctx)
        {
            this.ctx = ctx;
        }

        public AnimatorController FxController()
        {
            return ctx.AvatarDescriptor.baseAnimationLayers[4].animatorController as AnimatorController;
        }
        public GameObject FindObject(string FindObjectName)
        {
            if (ctx != null && ctx.AvatarRootTransform != null)
            {
                Transform rttObject = ctx.AvatarRootTransform.Find(FindObjectName);
                return rttObject != null ? rttObject.gameObject : null;
            }
            return null;
        }

        public void InstantiateAsset(string ResPath, Transform Parent=null)
        {
            if (CheckExistGameObject(ResPath)) return;
            GameObject assetObject = (GameObject)Resources.Load(ResPath);
            if (assetObject == null)
            {
                Debug.LogError($@"Pan.Util.PanUtil InstantiateAsset NotFound{ResPath}");
                return;
            }
            if (Parent == null) Parent = ctx.AvatarRootTransform;
            Instantiate(assetObject).transform.parent = Parent;
        }

        public bool CheckExistGameObject(string Name)
        {
            if (Name.Contains("/"))
            {
                Name = Name.Substring(Name.LastIndexOf("/") + 1);
            }
            var children = ctx.AvatarRootObject.GetComponentsInChildren<Transform>();
            foreach(Transform child in children)
            {
                if (child.name == Name) return true;
            }
            return false;
        }
    }

    public static class Constant
    {
    }
}