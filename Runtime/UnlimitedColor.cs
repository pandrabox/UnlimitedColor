
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.github.pandrabox.unlimitedcolor.runtime
{
    [AddComponentMenu("Pan/UnlimitedColor")]
    public class UnlimitedColor : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public bool FixMAMBT;
        public float SaturationMax, ValueMax, GammaMax;

        public RendererGroup[] RendererGroups;
        public UnlimitedColor()
        {
            FixMAMBT = true;
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
        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            showExMenu = EditorGUILayout.Toggle("Show Advanced Menu", showExMenu);
            if (showExMenu)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("FixMAMBT"));
                EditorGUILayout.HelpBox("本アセットは内部的にModular Avatar Merge Blend Treeを使っています。これはFXの一番上にレイヤを作成するため、MMD等にずれが生じます。ここのチェックボックスをONすると、そのズレを抑制します。多くの場合ONでいいはずですが、既にMerge BlendTreeが最初であることを前提して作成しているアバターの場合異常動作の原因となりますので、チェックを外して下さい。", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SaturationMax"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ValueMax"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("GammaMax"));
                EditorGUILayout.HelpBox("彩度(S)、明度(V)、ガンマ(G)はLiltoonのGUI上において最大値が2で制限されており、本アセットも基本はその値を上限にしています。システム上はそれより大きい値の設定も可能です。大きくしすぎるとノイズ増加や操作操作感悪化につながるため、変更には注意して下さい。", MessageType.Info);
            }


            var rendererGroupsProperty = serializedObject.FindProperty("RendererGroups");

            // Buttons for adding and removing RendererGroups
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                rendererGroupsProperty.arraySize++;
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
                firstGroupProperty.FindPropertyRelative("GroupName").stringValue = "OutOfTarget";
            }

            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < rendererGroupsProperty.arraySize; i++)
            {
                SerializedProperty groupProperty = rendererGroupsProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(groupProperty, new GUIContent($"Group {i + 1}"));

            }
            EditorGUILayout.HelpBox("デフォルトでは全てのRendererを独立して色変更できるようになり、非常に多くのパラメータを消費します。色変更が不要なものはOutOfTargetグループに登録して下さい。同時に色を変えたいものはグループを作成して登録して下さい。グループ名・オブジェクト名は重複しないよう注意して下さい。", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
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
            SerializedProperty GroupName = property.FindPropertyRelative("GroupName");
            SerializedProperty Renderers = property.FindPropertyRelative("Renderers");

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
            SerializedProperty Renderers = property.FindPropertyRelative("Renderers");
            float lineHeight = EditorGUIUtility.singleLineHeight;
            return lineHeight * (Renderers.arraySize + 1) +.1f;
        }
    }
}

#endif