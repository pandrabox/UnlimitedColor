
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.github.pandrabox.unlimitedcolor.runtime
{
    [AddComponentMenu("Pan/UnlimitedColor")]
    public class UnlimitedColor : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public RendererGroup[] RendererGroups;
        public UnlimitedColor()
        {
            RendererGroups = new RendererGroup[1];
        }
    }


    [CustomEditor(typeof(UnlimitedColor))]
    public class UnlimitedColorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
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