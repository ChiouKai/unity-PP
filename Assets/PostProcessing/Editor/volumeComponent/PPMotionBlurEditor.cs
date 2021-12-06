using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[VolumeComponentEditor(typeof(PPMotionBlur))]
sealed class PPMotionBlurEditor : PPVolumeComponentEditor
{
    //SerializedDataParameter m_Mode;
    SerializedDataParameter m_Quality;
    SerializedDataParameter m_Intensity;
    SerializedDataParameter m_Clamp;

    public override void FindSerializedDataParameter()
    {
        //m_Mode = Unpack(o.Find(x => x.mode));
        m_Quality = Unpack(serializedObject.FindProperty("quality"));
        m_Intensity = Unpack(serializedObject.FindProperty("intensity"));
        m_Clamp = Unpack(serializedObject.FindProperty("clamp"));
    }

    public override void OnInspectorGUI()
    {
        //PropertyField(m_Mode);

        //if (m_Mode.value.intValue == (int)MotionBlurMode.CameraOnly)
        //{
        PropertyField(m_Quality);
        PropertyField(m_Intensity);
        PropertyField(m_Clamp);
        //}
        //else
        //{
        //    EditorGUILayout.HelpBox("Object motion blur is not supported on the Universal Render Pipeline yet.", MessageType.Info);
        //}
    }
}