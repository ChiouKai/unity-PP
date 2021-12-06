using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[VolumeComponentEditor(typeof(PPBloom))]
sealed class PPBloomEditor : PPVolumeComponentEditor
{
    SerializedDataParameter m_Threshold;
    SerializedDataParameter m_Intensity;
    SerializedDataParameter m_Scatter;
    SerializedDataParameter m_Clamp;
    SerializedDataParameter m_Tint;
    SerializedDataParameter m_HighQualityFiltering;
    SerializedDataParameter m_SkipIterations;
    SerializedDataParameter m_DirtTexture;
    SerializedDataParameter m_DirtIntensity;
    public override void FindSerializedDataParameter()
    {
   

        m_Threshold = Unpack(serializedObject.FindProperty("threshold"));
        m_Intensity = Unpack(serializedObject.FindProperty("intensity"));
        m_Scatter = Unpack(serializedObject.FindProperty("scatter"));
        m_Clamp = Unpack(serializedObject.FindProperty("clamp"));
        m_Tint = Unpack(serializedObject.FindProperty("tint"));
        m_HighQualityFiltering = Unpack(serializedObject.FindProperty("highQualityFiltering"));
        m_SkipIterations = Unpack(serializedObject.FindProperty("skipIterations"));
        m_DirtTexture = Unpack(serializedObject.FindProperty("dirtTexture"));
        m_DirtIntensity = Unpack(serializedObject.FindProperty("dirtIntensity"));
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Bloom", EditorStyles.miniLabel);

        PropertyField(m_Threshold);
        PropertyField(m_Intensity);
        PropertyField(m_Scatter);
        PropertyField(m_Tint);
        PropertyField(m_Clamp);
        PropertyField(m_HighQualityFiltering);

        if (m_HighQualityFiltering.overrideState.boolValue && m_HighQualityFiltering.value.boolValue && EditorUtils.buildTargets.Contains(GraphicsDeviceType.OpenGLES2))
            EditorGUILayout.HelpBox("High Quality Bloom isn't supported on GLES2 platforms.", MessageType.Warning);

        PropertyField(m_SkipIterations);

        EditorGUILayout.LabelField("Lens Dirt", EditorStyles.miniLabel);
        PropertyField(m_DirtTexture);
        PropertyField(m_DirtIntensity);
    }
}
