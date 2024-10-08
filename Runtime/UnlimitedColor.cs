﻿
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.unlimitedcolor.runtime.Generic;

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
    public class UnlimitedColorEditor : Editor
    {
        private bool showExMenu = false;
        private Texture2D _emotePrefabIcoAndLogo;
        public override void OnInspectorGUI()
        {
            if (_emotePrefabIcoAndLogo == null) _emotePrefabIcoAndLogo = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.github.pandrabox.unlimitedcolor/Assets/Ico/minilogo.png");
            if (_emotePrefabIcoAndLogo != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(_emotePrefabIcoAndLogo);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            serializedObject.Update();

            EditorGUILayout.LabelField("UnlimitedColorはlilToonを使ったRendererの色をVRC上で変更可能にするツールです。\n・非常に多くのパラメータを使うため、多数の衣装等を使っているアバターではアップロードできないことがあります(このプレハブを消せば元に戻ります)。\n\n詳細な使い方・アップロードできないときの処置は同梱のHowToUseを御覧下さい。", EditorStyles.wordWrappedLabel);

            Title("色調最大値設定");
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UnlimitedColor.SaturationMax)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UnlimitedColor.ValueMax)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UnlimitedColor.GammaMax)));
            EditorGUILayout.HelpBox("彩度(S)、明度(V)、ガンマ(G)の最大値は標準で2ですが、大きくすることで色幅を増やすことができます。ただしノイズが乗ることがあります。", MessageType.Info);





            Title("初期値設定");

            var rendererGroupsProperty = serializedObject.FindProperty(nameof(UnlimitedColor.RendererGroups));

            var prop_Explicit = serializedObject.FindProperty(nameof(UnlimitedColor.Explicit));
            EditorGUILayout.PropertyField(prop_Explicit);
            if (prop_Explicit.boolValue)
            {
                EditorGUILayout.HelpBox("現在の設定では、色変更したいRendererをグループ定義する必要があります。\n定義していないものは、変更できません", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("すべてのRendererは色変更できます。不要なものは、OutOfTargetグループに登録して下さい\n※注意：現在、パラメータを多く使用する設定になっています（上のチェックで切り替え）", MessageType.Info);
                SerializedProperty groupProperty = rendererGroupsProperty.GetArrayElementAtIndex(0);
                EditorGUILayout.PropertyField(groupProperty, new GUIContent($"Group {1}"));
            }


            Title("グルーピング");

            //カスタムグループの描画
            if (prop_Explicit.boolValue)
            {
                EditorGUILayout.HelpBox("同時に色を変えたいグループを作り、Rendererを登録して下さい。名前は重複しないよう注意してください。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("同時に色を変えたいグループを作り、Rendererを登録して下さい。名前は重複しないよう注意してください。\nグループ化すると消費パラメータが軽減できます。", MessageType.Info);
            }

            // Buttons for adding and removing RendererGroups
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                rendererGroupsProperty.arraySize++;
                SerializedProperty newGroup = rendererGroupsProperty.GetArrayElementAtIndex(rendererGroupsProperty.arraySize - 1);

                // Initialize the GroupName and Renderers array in the new RendererGroup
                SerializedProperty groupName = newGroup.FindPropertyRelative(nameof(RendererGroup.GroupName));
                SerializedProperty renderers = newGroup.FindPropertyRelative(nameof(RendererGroup.Renderers));

                groupName.stringValue = "";  // Set default group name
                renderers.arraySize = 0;
            }

            if (GUILayout.Button("-"))
            {
                if (rendererGroupsProperty.arraySize > 1)
                {
                    rendererGroupsProperty.arraySize--;
                }
            }
            if (rendererGroupsProperty.arraySize <= 1)
            {
                rendererGroupsProperty.arraySize = 1;
            }
            if (rendererGroupsProperty.arraySize > 0)
            {
                var firstGroupProperty = rendererGroupsProperty.GetArrayElementAtIndex(0);
                firstGroupProperty.FindPropertyRelative(nameof(RendererGroup.GroupName)).stringValue = "OutOfTarget";
            }

            EditorGUILayout.EndHorizontal();



            for (int i = 1; i < rendererGroupsProperty.arraySize; i++)
            {
                SerializedProperty groupProperty = rendererGroupsProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(groupProperty, new GUIContent($"Group {i + 1}"));
            }


            Title("未定義のlilToon Renderer");
            if (prop_Explicit.boolValue)
            {
                EditorGUILayout.HelpBox("次のRendererは未定義です。Explicit=ONのため、色変更できません", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("次のRendererは未定義です。Explicit=OFFのため、個別色変更可能です", MessageType.Info);
            }
            var undefinedRenderers = UndefinedRenderers((UnlimitedColor)target);

            EditorGUI.indentLevel++;
            foreach(var ur in undefinedRenderers)
            {
                EditorGUILayout.ObjectField(ur, typeof(Renderer), allowSceneObjects: true);
            }
            EditorGUI.indentLevel--;


            serializedObject.ApplyModifiedProperties();
        }
        private Renderer[] UndefinedRenderers(UnlimitedColor ConfigComponent)
        {
            // avatarRoot を取得
            Transform avatarRoot = GetAvatarRootTransform(ConfigComponent.transform);
            // avatarRoot 以下全ての Renderer を取得
            List<Renderer> allRenderers = new List<Renderer>(avatarRoot.GetComponentsInChildren<Renderer>());
            // すべての Renderer に対して isLil を実行し、false の場合はリストから除外
            allRenderers.RemoveAll(renderer => !isLil(renderer));
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

        private static void Title(string t)
        {
            GUILayout.BeginHorizontal();

            var lineRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            int leftBorderSize = 5;
            var leftRect = new Rect(lineRect.x, lineRect.y, leftBorderSize, lineRect.height);
            var rightRect = new Rect(lineRect.x + leftBorderSize, lineRect.y, lineRect.width - leftBorderSize, lineRect.height);
            Color leftColor = new Color32(0xF4, 0xAD, 0x39, 0xFF);
            Color rightColor = new Color32(0x39, 0xA7, 0xF4, 0xFF);

            EditorGUI.DrawRect(leftRect, leftColor);
            EditorGUI.DrawRect(rightRect, rightColor);

            var textStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 0, 0, 0),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black },
            };

            GUI.Label(rightRect, t, textStyle);

            GUILayout.EndHorizontal();
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
            return lineHeight * (Renderers.arraySize + 1) +.1f;
        }
    }
}

#endif