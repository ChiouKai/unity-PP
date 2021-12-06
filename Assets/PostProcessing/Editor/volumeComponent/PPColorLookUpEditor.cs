using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[VolumeComponentEditor(typeof(PPColorLookup))]
sealed class ColorLookupEditor : PPVolumeComponentEditor
{
    SerializedDataParameter m_Texture;
    SerializedDataParameter m_Contribution;

    public override void FindSerializedDataParameter()
    {
        m_Texture = Unpack(serializedObject.FindProperty("texture"));
        m_Contribution = Unpack(serializedObject.FindProperty("contribution"));
    }

    public override void OnInspectorGUI()
    {
        PropertyField(m_Texture, EditorGUIUtility.TrTextContent("Lookup Texture"));

        var lut = m_Texture.value.objectReferenceValue;
        if (lut != null && !((PPColorLookup)target).ValidateLUT())
            EditorGUILayout.HelpBox("Invalid lookup texture. It must be a non-sRGB 2D texture or render texture with the same size as set in the settings.", MessageType.Warning);

        PropertyField(m_Contribution, EditorGUIUtility.TrTextContent("Contribution"));

        var layer = PPCamera.layer;
        if (layer != null)
        {
            if (Utils.CheckSupportHDR()&& layer.gradingMode == ColorGradingMode.HighDynamicRange)
                EditorGUILayout.HelpBox("Color Grading Mode in the Settings is set to HDR. As a result, this LUT will be applied after the internal color grading and tonemapping have been applied.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("Color Grading Mode in the Settings is set to LDR. As a result, this LUT will be applied after tonemapping and before the internal color grading has been applied.", MessageType.Info);
        }
    }
}