using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[VolumeComponentEditor(typeof(PPDepthOfField))]
sealed class PPDepthOfFieldEditor : PPVolumeComponentEditor
{
    SerializedDataParameter m_Mode;

    SerializedDataParameter m_GaussianFarStart;
    SerializedDataParameter m_GaussianFarEnd;
    SerializedDataParameter m_GaussianNearStart;
    SerializedDataParameter m_GaussianNearEnd;
    SerializedDataParameter m_GaussianMaxRadius;
    SerializedDataParameter m_HighQualitySampling;

    SerializedDataParameter m_FocusDistance;
    SerializedDataParameter m_FocalLength;
    SerializedDataParameter m_Aperture;
    SerializedDataParameter m_BladeCount;
    SerializedDataParameter m_BladeCurvature;
    SerializedDataParameter m_BladeRotation;

    public override void FindSerializedDataParameter()
    {
        
        m_Mode = Unpack(serializedObject.FindProperty("mode"));
        m_GaussianFarEnd = Unpack(serializedObject.FindProperty("gaussianFarEnd"));
        m_GaussianFarStart = Unpack(serializedObject.FindProperty("gaussianFarStart"));
        m_GaussianNearStart = Unpack(serializedObject.FindProperty("gaussianNearStart"));
        m_GaussianNearEnd = Unpack(serializedObject.FindProperty("gaussianNearEnd"));
        m_GaussianMaxRadius = Unpack(serializedObject.FindProperty("gaussianMaxRadius"));
        m_HighQualitySampling = Unpack(serializedObject.FindProperty("highQualitySampling"));

        m_FocusDistance = Unpack(serializedObject.FindProperty("focusDistance"));
        m_FocalLength = Unpack(serializedObject.FindProperty("focalLength"));
        m_Aperture = Unpack(serializedObject.FindProperty("aperture"));
        m_BladeCount = Unpack(serializedObject.FindProperty("bladeCount"));
        m_BladeCurvature = Unpack(serializedObject.FindProperty("bladeCurvature"));
        m_BladeRotation = Unpack(serializedObject.FindProperty("bladeRotation"));
    }

    public override void OnInspectorGUI()
    {
        PropertyField(m_Mode);

        if (m_Mode.value.intValue == (int)DepthOfFieldMode.Gaussian)
        {
            PropertyField(m_GaussianFarEnd, EditorGUIUtility.TrTextContent("FarEnd"));
            PropertyField(m_GaussianFarStart, EditorGUIUtility.TrTextContent("FarStart"));
            m_GaussianFarEnd.value.floatValue = Mathf.Max(m_GaussianFarStart.value.floatValue, m_GaussianFarEnd.value.floatValue);
            PropertyField(m_GaussianNearStart, EditorGUIUtility.TrTextContent("NearStart"));
            PropertyField(m_GaussianNearEnd, EditorGUIUtility.TrTextContent("NearEnd"));
            m_GaussianNearEnd.value.floatValue = Mathf.Min(m_GaussianNearStart.value.floatValue, m_GaussianNearEnd.value.floatValue);
            PropertyField(m_GaussianMaxRadius, EditorGUIUtility.TrTextContent("Max Radius"));
            PropertyField(m_HighQualitySampling);
        }
        else if (m_Mode.value.intValue == (int)DepthOfFieldMode.Bokeh)
        {
            PropertyField(m_FocusDistance);
            PropertyField(m_FocalLength);
            PropertyField(m_Aperture);
            PropertyField(m_BladeCount);
            PropertyField(m_BladeCurvature);
            PropertyField(m_BladeRotation);
        }
    }
}