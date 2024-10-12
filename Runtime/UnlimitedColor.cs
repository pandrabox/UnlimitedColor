#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.runtime.Util;
using com.github.pandrabox.pandravase.runtime;
using System.Linq;

namespace com.github.pandrabox.unlimitedcolor.runtime
{
    [AddComponentMenu("Pan/UnlimitedColor")]
    public class UnlimitedColor : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public bool Explicit;
        public float SaturationMax, ValueMax, GammaMax;

        public RendererGroup[] RendererGroups;
        public UnlimitedColor()
        {
            Explicit = false;
            SaturationMax = 2;
            ValueMax = 2;
            GammaMax = 2;
            RendererGroups = new RendererGroup[1];
        }

    }


    [CustomEditor(typeof(UnlimitedColor))]
    public class UnlimitedColorEditor : PandraEditor
    {
        UnlimitedColorEditor() : base(true, "UnlimitedColor", ProjectTypes.VPM) { }

        private SerializedProperty _pSaturationMax, _pValueMax, _pGammaMax, _pRendererGroups, _pExplicit;

        protected sealed override void DefineSerial()
        {
            _pSaturationMax = serializedObject.FindProperty(nameof(UnlimitedColor.SaturationMax));
            _pValueMax = serializedObject.FindProperty(nameof(UnlimitedColor.ValueMax));
            _pGammaMax = serializedObject.FindProperty(nameof(UnlimitedColor.GammaMax));
            _pRendererGroups = serializedObject.FindProperty(nameof(UnlimitedColor.RendererGroups));
            _pExplicit = serializedObject.FindProperty(nameof(UnlimitedColor.Explicit));
        }

        /// <summary>
        /// パラメータを消費するグループ数のカウント
        /// </summary>
        /// <returns></returns>
        private int EnableGroupCount()
        {
            RendererGroup[] groups = ((UnlimitedColor)target).RendererGroups;
            int c = 0;
            foreach (RendererGroup group in groups)
            {
                if (group.GroupName == "OutOfTarget") continue;
                if (group.Renderers.Any(IsLil)) c++;
            }
            return c;
        }

        public sealed override void OnInnerInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("UnlimitedColorはlilToonを使ったRendererの色をVRC上で変更可能にするツールです。\n多くのパラメータを使うため、衣装や小物が多いアバターではアップロードできないことがあります。このプレハブを消せば元に戻るので、困ったら消して下さい。\n\n詳細な使い方・アップロードできないときの処置は同梱のHowToUseを御覧下さい。", EditorStyles.wordWrappedLabel);

            Title("使用Bit数");
            var undefinedRenderers = UndefinedRenderers((UnlimitedColor)target);
            int paramnum = _pExplicit.boolValue ? 0 : undefinedRenderers.Length * 4 * 8;
            paramnum += EnableGroupCount() * 4 * 8;
            if (paramnum > 256) EditorGUILayout.HelpBox("VRCの上限を超えるパラメータを使っています。アップロードのためにはVRCFury Parameter Comressorが必要です", MessageType.Error);
            else if (paramnum > 128) EditorGUILayout.HelpBox("多くのパラメータを使っています。他のギミックとの相性次第でアップロードできないかもしれません", MessageType.Warning);
            EditorGUILayout.IntField(paramnum);

            Title("色調最大値設定");
            EditorGUILayout.HelpBox("彩度(S)、明度(V)、ガンマ(G)の最大値は標準で2ですが、大きくすることで色幅を増やすことができます。ただしノイズが乗ることがあります。", MessageType.Info);
            EditorGUILayout.PropertyField(_pSaturationMax);
            EditorGUILayout.PropertyField(_pValueMax);
            EditorGUILayout.PropertyField(_pGammaMax);

            Title("初期値設定");
            EditorGUILayout.PropertyField(_pExplicit);
            if (_pExplicit.boolValue)
            {
                EditorGUILayout.HelpBox("現在の設定では、色変更したいRendererをグループ定義する必要があります。\n定義していないものは、変更できません", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("すべてのRendererは色変更できます。不要なものは、OutOfTargetグループに登録して下さい\n※注意：現在、パラメータを多く使用する設定になっています（上のチェックで切り替え）", MessageType.Info);
                SerializedProperty groupProperty = _pRendererGroups.GetArrayElementAtIndex(0);
                EditorGUILayout.PropertyField(groupProperty, new GUIContent($"Group {1}"));
            }

            Title("グルーピング");
            if (_pExplicit.boolValue)
            {
                EditorGUILayout.HelpBox("同時に色を変えたいグループを作り、Rendererを登録して下さい。名前は重複しないよう注意してください。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("同時に色を変えたいグループを作り、Rendererを登録して下さい。名前は重複しないよう注意してください。\nグループ化すると消費パラメータが軽減できます。", MessageType.Info);
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                _pRendererGroups.arraySize++;
                SerializedProperty pNewGroup = _pRendererGroups.GetArrayElementAtIndex(_pRendererGroups.arraySize - 1);
                pNewGroup.FindPropertyRelative(nameof(RendererGroup.GroupName)).stringValue = "";
                pNewGroup.FindPropertyRelative(nameof(RendererGroup.Renderers)).arraySize = 0;
            }
            if (GUILayout.Button("-"))
            {
                if (_pRendererGroups.arraySize > 1)
                {
                    _pRendererGroups.arraySize--;
                }
            }
            if (_pRendererGroups.arraySize <= 1) _pRendererGroups.arraySize = 1;
            _pRendererGroups.GetArrayElementAtIndex(0).FindPropertyRelative(nameof(RendererGroup.GroupName)).stringValue = "OutOfTarget";
            EditorGUILayout.EndHorizontal();
            for (int i = 1; i < _pRendererGroups.arraySize; i++)
            {
                SerializedProperty groupProperty = _pRendererGroups.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(groupProperty, new GUIContent($"Group {i + 1}"));
            }

            Title("未定義のlilToon Renderer");
            if (undefinedRenderers == null || undefinedRenderers.Length == 0)
            {
                EditorGUILayout.HelpBox("未定義のRendererはありません。", MessageType.Info);
            }
            else if (_pExplicit.boolValue)
            {
                EditorGUILayout.HelpBox("次のRendererは未定義です。Explicit=ONのため、色変更できません", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("次のRendererは未定義です。Explicit=OFFのため、個別色変更可能です", MessageType.Info);
            }
            EditorGUI.indentLevel++;
            foreach (var ur in undefinedRenderers)
            {
                EditorGUILayout.ObjectField(ur, typeof(Renderer), allowSceneObjects: true);
            }
            EditorGUI.indentLevel--;


            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 未定義Rendererの取得
        /// </summary>
        /// <param name="ConfigComponent"></param>
        /// <returns></returns>
        private Renderer[] UndefinedRenderers(UnlimitedColor ConfigComponent)
        {
            // avatarRoot を取得
            Transform avatarRoot = GetAvatarRootTransform(ConfigComponent.transform);
            // avatarRoot 以下全ての Renderer を取得
            List<Renderer> allRenderers = new List<Renderer>(avatarRoot.GetComponentsInChildren<Renderer>(true));
            // すべての Renderer に対して IsLil を実行し、false の場合はリストから除外
            allRenderers.RemoveAll(renderer => !IsLil(renderer));
            // 定義されている RendererGroups から、既に登録されている Renderer を allRenderers から除外
            RendererGroup[] definedRendererGroups = ConfigComponent.RendererGroups;
            foreach (var rendererGroup in definedRendererGroups)
            {
                foreach (Renderer renderer in rendererGroup.Renderers)
                {
                    allRenderers.Remove(renderer);
                }
            }
            // 残った Renderer を配列に変換して返す
            return allRenderers.ToArray();
        }
    }


    [System.Serializable]
    public class RendererGroup
    {
        public string GroupName;
        public Renderer[] Renderers;
    }

    [CustomPropertyDrawer(typeof(RendererGroup))]
    public class RendererGroupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Don't display the property name label
            EditorGUI.BeginProperty(position, label, property);

            // Get properties
            SerializedProperty GroupName = property.FindPropertyRelative(nameof(RendererGroup.GroupName));
            SerializedProperty Renderers = property.FindPropertyRelative(nameof(RendererGroup.Renderers));

            // Calculate widths
            float buttonWidth = 30;
            {
                Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                Rect button1 = new Rect(line.width - buttonWidth * 2, line.y, buttonWidth, EditorGUIUtility.singleLineHeight);
                Rect button2 = new Rect(line.width - buttonWidth, line.y, buttonWidth, EditorGUIUtility.singleLineHeight);
                Rect name = new Rect(line.x, line.y, line.width - buttonWidth * 3, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(name, GroupName, GUIContent.none);
                if (GUI.Button(button1, "+"))
                {
                    Renderers.arraySize++; // Increase array size
                    SerializedProperty newRenderer = Renderers.GetArrayElementAtIndex(Renderers.arraySize - 1);
                    newRenderer.objectReferenceValue = null; // Reset to null or an initial value
                }
                if (GUI.Button(button2, "-"))
                {
                    if (Renderers.arraySize > 0)
                    {
                        Renderers.arraySize--; // Decrease array size if it's greater than 0
                    }
                }
            }
            EditorGUI.indentLevel++;
            position.y += EditorGUIUtility.singleLineHeight;
            for (int i = 0; i < Renderers.arraySize; i++)
            {
                Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(line, Renderers.GetArrayElementAtIndex(i), GUIContent.none);
                position.y += EditorGUIUtility.singleLineHeight;
            }
            EditorGUI.indentLevel--;
        }


        // Adjust height dynamically based on the number of elements
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty Renderers = property.FindPropertyRelative(nameof(RendererGroup.Renderers));
            float lineHeight = EditorGUIUtility.singleLineHeight;
            return lineHeight * (Renderers.arraySize + 1) + .1f;
        }
    }
}

#endif