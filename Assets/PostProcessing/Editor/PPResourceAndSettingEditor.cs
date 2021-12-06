using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PPResourceAndSetting), true)]
public class PPResourceAndSettingEditor : Editor
{
    SerializedProperty m_Shaders;
    SerializedProperty m_Textures;
    SerializedProperty m_Setting;
    SerializedProperty m_ColorGradingMode, m_ColorGradingLutSize;
    bool show = true;

    private void OnEnable()
    {
        m_Shaders = serializedObject.FindProperty("shaders");
        m_Textures = serializedObject.FindProperty("textures");
        m_Setting = serializedObject.FindProperty("setting");
        m_ColorGradingMode = m_Setting.FindPropertyRelative("m_ColorGradingMode");
        m_ColorGradingLutSize = m_Setting.FindPropertyRelative("m_ColorGradingLutSize");
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(m_Shaders);
        EditorGUILayout.PropertyField(m_Textures);
        //EditorGUILayout.PropertyField(m_Setting);
        show = EditorGUILayout.BeginFoldoutHeaderGroup(show, "setting");
        if (show)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(m_ColorGradingMode);
            int v = EditorGUILayout.IntField(new GUIContent("Color Grading LutSize"),m_ColorGradingLutSize.intValue);
            m_ColorGradingLutSize.intValue = Mathf.ClosestPowerOfTwo(Mathf.Clamp(v, 16, 64));
            --EditorGUI.indentLevel;
        }
        serializedObject.ApplyModifiedProperties();
    }
}
