using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System;

[CustomEditor(typeof(PPCamera))]
public class PPCameraEditor : Editor
{
    SerializedProperty m_isPPEnabled;
    SerializedProperty m_trigger;
    SerializedProperty m_postlayer;
    SerializedProperty m_isStopNaNEnabled;
    SerializedProperty m_antialiasingMode;
    SerializedProperty m_antialiasingQuality;
    SerializedProperty m_changed;
    SerializedProperty m_ambientOcclusion;
    SerializedProperty m_dithering;

    AOComponentListEditor AOEditor;

    PPCamera Cam;
    private void OnEnable()
    {
        m_isPPEnabled = serializedObject.FindProperty("EnabledPostProcessing");
        m_trigger = serializedObject.FindProperty("Trigger");
        m_postlayer = serializedObject.FindProperty("PostLayer");
        m_isStopNaNEnabled = serializedObject.FindProperty("isStopNaNEnabled");
        m_antialiasingMode = serializedObject.FindProperty("antialiasingMode");
        m_antialiasingQuality = serializedObject.FindProperty("antialiasingQuality");
        m_changed = serializedObject.FindProperty("changed");
        m_ambientOcclusion = serializedObject.FindProperty("ambientOcclusion");
        m_dithering= serializedObject.FindProperty("dithering");
        Cam = target as PPCamera;
        AOEditor = new AOComponentListEditor(this);
        RefreshEditor(Cam.ambientOcclusion);
    }
    void RefreshEditor(AmbientOcclusion asset)
    {
        AOEditor.Clear();

        if (asset != null)
            AOEditor.Init(asset, new SerializedObject(asset));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_isPPEnabled);
        if(m_isPPEnabled.boolValue == true)
        {
            if(SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
            {
                EditorGUILayout.HelpBox("PP Not Support OpenGLES2.", MessageType.Warning);
            }
        }
        EditorGUILayout.PropertyField(m_trigger, volumeTrigger);

        EditorGUILayout.PropertyField(m_postlayer, volumeLayerMask);
        EditorGUILayout.PropertyField(m_isStopNaNEnabled, StopNaN);
        EditorGUILayout.PropertyField(m_dithering);
        EditorGUILayout.PropertyField(m_antialiasingMode);
        EditorGUILayout.PropertyField(m_antialiasingQuality);

        EditorGUILayout.PropertyField(m_ambientOcclusion);

        if (Cam.ambientOcclusion != AOEditor.asset)
        {
            serializedObject.ApplyModifiedProperties();
            RefreshEditor(Cam.ambientOcclusion);
        }
        m_changed.boolValue |= AOEditor.OnGUI();

        serializedObject.ApplyModifiedProperties();
    }



    public static GUIContent volumeTrigger = EditorGUIUtility.TrTextContent("Volume Trigger", "A transform that will act as a trigger for volume blending. If none is set, the camera itself will act as a trigger.");
    public static GUIContent volumeLayerMask = EditorGUIUtility.TrTextContent("PostLayer", "This camera will only be affected by volumes in the selected scene-layers.");
    public static GUIContent StopNaN = EditorGUIUtility.TrTextContent("Stop NaN");
}
