using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[AOComponentEditor(typeof(AOHBAO))]
public class AOHBAOEditor : AOComponentEditor
{
    SerializedDataParameter m_pipelineStage;
    SerializedDataParameter m_quality;
    SerializedDataParameter m_deinterleaving;
    SerializedDataParameter m_resolution;
    SerializedDataParameter m_noiseType;
    SerializedDataParameter m_debugMode;

    SerializedDataParameter m_radius;
    SerializedDataParameter m_maxRadiusPixels;
    SerializedDataParameter m_bias;
    SerializedDataParameter m_intensity;
    SerializedDataParameter m_useMultiBounce;
    SerializedDataParameter m_multiBounceInfluence;
    SerializedDataParameter m_offscreenSamplesContribution;
    SerializedDataParameter m_maxDistance;
    SerializedDataParameter m_distanceFalloff;
    SerializedDataParameter m_perPixelNormals;
    SerializedDataParameter m_baseColor;

    SerializedDataParameter m_TFenabled;
    SerializedDataParameter m_varianceClipping;

    SerializedDataParameter m_blurType;
    SerializedDataParameter m_sharpness;

    SerializedDataParameter m_colorBleedingEnabled;
    SerializedDataParameter m_saturation;
    SerializedDataParameter m_albedoMultiplier;
    SerializedDataParameter m_brightnessMask;
    SerializedDataParameter m_brightnessMaskRange;

    public override void FindSerializedDataParameter()
    {
        var generalSetting = serializedObject.FindProperty("generalSetting");
        m_pipelineStage = Unpack(generalSetting.FindPropertyRelative("pipelineStage"));
        m_quality = Unpack(generalSetting.FindPropertyRelative("quality"));
        m_deinterleaving = Unpack(generalSetting.FindPropertyRelative("deinterleaving"));
        m_resolution = Unpack(generalSetting.FindPropertyRelative("resolution"));
        m_noiseType = Unpack(generalSetting.FindPropertyRelative("noiseType"));
        m_debugMode = Unpack(generalSetting.FindPropertyRelative("debugMode"));

        var aoSetting = serializedObject.FindProperty("aoSetting");
        m_radius = Unpack(aoSetting.FindPropertyRelative("radius"));
        m_maxRadiusPixels = Unpack(aoSetting.FindPropertyRelative("maxRadiusPixels"));
        m_intensity = Unpack(aoSetting.FindPropertyRelative("intensity"));
        m_bias = Unpack(aoSetting.FindPropertyRelative("bias"));
        m_useMultiBounce = Unpack(aoSetting.FindPropertyRelative("useMultiBounce"));
        m_multiBounceInfluence = Unpack(aoSetting.FindPropertyRelative("multiBounceInfluence"));
        m_offscreenSamplesContribution = Unpack(aoSetting.FindPropertyRelative("offscreenSamplesContribution"));
        m_maxDistance = Unpack(aoSetting.FindPropertyRelative("maxDistance"));
        m_distanceFalloff = Unpack(aoSetting.FindPropertyRelative("distanceFalloff"));
        m_perPixelNormals = Unpack(aoSetting.FindPropertyRelative("perPixelNormals"));
        m_baseColor = Unpack(aoSetting.FindPropertyRelative("baseColor"));

        var temporalFilterSettings = serializedObject.FindProperty("temporalFilterSettings");
        m_TFenabled = Unpack(temporalFilterSettings.FindPropertyRelative("TFenabled"));
        m_varianceClipping = Unpack(temporalFilterSettings.FindPropertyRelative("varianceClipping"));

        var blurSetting = serializedObject.FindProperty("blurSetting");
        m_blurType = Unpack(blurSetting.FindPropertyRelative("blurType"));
        m_sharpness = Unpack(blurSetting.FindPropertyRelative("sharpness"));

        var colorBleedingSetting = serializedObject.FindProperty("colorBleedingSetting");
        
        m_colorBleedingEnabled = Unpack(colorBleedingSetting.FindPropertyRelative("colorBleedingEnabled"));
        m_saturation = Unpack(colorBleedingSetting.FindPropertyRelative("saturation"));
        m_albedoMultiplier = Unpack(colorBleedingSetting.FindPropertyRelative("albedoMultiplier"));
        m_brightnessMask = Unpack(colorBleedingSetting.FindPropertyRelative("brightnessMask"));
        m_brightnessMaskRange = Unpack(colorBleedingSetting.FindPropertyRelative("brightnessMaskRange"));

        GeneralFoldout = new SavedBool($"{target.GetType()}.GeneralFoldout", true);
        AOFoldout = new SavedBool($"{target.GetType()}.AOFoldout", true);
        TemporalFilterFoldout = new SavedBool($"{target.GetType()}.TemporalFilterFoldout", true);
        BlurFoldout = new SavedBool($"{target.GetType()}.BlurFoldout", true);
        ColorBleedingFoldout = new SavedBool($"{target.GetType()}.ColorBleedingFoldout", true);
    }
    SavedBool GeneralFoldout;
    SavedBool AOFoldout;
    SavedBool TemporalFilterFoldout;
    SavedBool BlurFoldout;
    SavedBool ColorBleedingFoldout;

    public override bool OnInspectorGUI()
    {
        serializedObject.Update();
        bool change = false;
        GeneralFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(GeneralFoldout.value, "General settings", EditorStyles.foldout);
        if (GeneralFoldout.value)
        {
            EditorGUI.BeginChangeCheck();
            PropertyField(m_pipelineStage);

            if(m_pipelineStage.value.enumValueIndex != (int)HBAOPipelineStage.BeforeImageEffectsOpaque&& Camera.main != null 
                && Camera.main.actualRenderingPath != RenderingPath.DeferredShading)
            {
                EditorGUILayout.HelpBox("Only work with deferred path.", MessageType.Warning);
            }

            PropertyField(m_quality);
            PropertyField(m_deinterleaving);
            if (m_deinterleaving.value.boolValue == false)
            {
                PropertyField(m_resolution);
                PropertyField(m_noiseType);
            }
            else if(SystemInfo.supportedRenderTargetCount < 4)
            {
                EditorGUILayout.HelpBox("Not work, because SystemInfo.supportedRenderTargetCount < 4.", MessageType.Error);
            }
            PropertyField(m_debugMode);

            change |= EditorGUI.EndChangeCheck();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        AOFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(AOFoldout.value, "AO settings", EditorStyles.foldout);
        if (AOFoldout.value)
        {
            EditorGUI.BeginChangeCheck();
            PropertyField(m_radius);
            PropertyField(m_maxRadiusPixels);
            PropertyField(m_bias);
            PropertyField(m_intensity);
            PropertyField(m_useMultiBounce);
            if(m_useMultiBounce.value.boolValue)
                PropertyField(m_multiBounceInfluence);
            PropertyField(m_offscreenSamplesContribution);
            PropertyField(m_maxDistance);
            PropertyField(m_distanceFalloff);
            if (m_distanceFalloff.value.floatValue > m_maxDistance.value.floatValue)
                m_distanceFalloff.value.floatValue = m_maxDistance.value.floatValue;
            PropertyField(m_perPixelNormals);

            
            if (m_perPixelNormals.value.enumValueIndex == (int)PerPixelNormals.GBuffer&& Camera.main!=null
                && Camera.main.actualRenderingPath != RenderingPath.DeferredShading)
            {
                EditorGUILayout.HelpBox("Only work with deferred path.", MessageType.Warning);
            }
            else if(m_perPixelNormals.value.enumValueIndex == (int)PerPixelNormals.Camera && m_pipelineStage.value.enumValueIndex != (int)HBAOPipelineStage.BeforeImageEffectsOpaque)
            {
                EditorGUILayout.HelpBox("only work on CameraEvent.BeforeImageEffectsOpaque.", MessageType.Warning);
            }

            PropertyField(m_baseColor);
            change |= EditorGUI.EndChangeCheck();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        TemporalFilterFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(TemporalFilterFoldout.value, "Temporal Filter settings", EditorStyles.foldout);
        if (TemporalFilterFoldout.value)
        {
            EditorGUI.BeginChangeCheck();
            PropertyField(m_TFenabled);
            if (m_TFenabled.value.boolValue)
            {
                if(!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf))
                    EditorGUILayout.HelpBox("System not support.", MessageType.Error);
                if(m_colorBleedingEnabled.value.boolValue && SystemInfo.supportedRenderTargetCount < 2)
                    EditorGUILayout.HelpBox("System not support with colorBleeding.", MessageType.Error);
            }

            PropertyField(m_varianceClipping);
            change |= EditorGUI.EndChangeCheck();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        BlurFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(BlurFoldout.value, "Blur settings", EditorStyles.foldout);
        //EditorGUILayout.LabelField("Blur settings", EditorStyles.miniLabel);
        if (BlurFoldout.value)
        {
            EditorGUI.BeginChangeCheck();
            PropertyField(m_blurType);
            PropertyField(m_sharpness);
            change |= EditorGUI.EndChangeCheck();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        ColorBleedingFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(ColorBleedingFoldout.value, "Color Bleeding settings", EditorStyles.foldout);
        if (ColorBleedingFoldout.value)
        {
            EditorGUI.BeginChangeCheck();
            PropertyField(m_colorBleedingEnabled);
            PropertyField(m_saturation);
            if (m_pipelineStage.value.enumValueIndex != (int)HBAOPipelineStage.BeforeImageEffectsOpaque)
                PropertyField(m_albedoMultiplier);
            else
                m_albedoMultiplier.overrideState.boolValue = false;
            PropertyField(m_brightnessMask);
            PropertyField(m_brightnessMaskRange);
            change |= EditorGUI.EndChangeCheck();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
        return change;

    }

    
}
