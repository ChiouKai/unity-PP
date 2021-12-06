using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[VolumeComponentEditor(typeof(PPFog))]
public class PPFogEditor : PPVolumeComponentEditor
{
    SerializedDataParameter m_EnableFog;
    SerializedDataParameter m_Mode;
    SerializedDataParameter m_Density;
    SerializedDataParameter m_Start;

    SerializedDataParameter m_End;
    SerializedDataParameter m_FogColor;
    SerializedDataParameter m_IncludeSkybox;

    public override void FindSerializedDataParameter()
    {
        m_EnableFog = Unpack(serializedObject.FindProperty("EnableFog")); 
        m_Mode = Unpack(serializedObject.FindProperty("Mode"));
        m_Density = Unpack(serializedObject.FindProperty("Density"));
        m_Start = Unpack(serializedObject.FindProperty("Start"));

        m_End = Unpack(serializedObject.FindProperty("End"));
        m_FogColor = Unpack(serializedObject.FindProperty("FogColor"));
        m_IncludeSkybox = Unpack(serializedObject.FindProperty("IncludeSkybox"));
    }
    public override void OnInspectorGUI()
    {
        PropertyField(m_EnableFog);
        PropertyField(m_Mode);
        if (m_Mode.value.intValue == (int)FogMode.Linear)
        {
            PropertyField(m_Start);
            PropertyField(m_End);
            m_End.value.floatValue = Mathf.Max(m_End.value.floatValue, m_Start.value.floatValue);
        }
        else
        {
            PropertyField(m_Density);
        }
        PropertyField(m_FogColor);
        PropertyField(m_IncludeSkybox);
    }
}
