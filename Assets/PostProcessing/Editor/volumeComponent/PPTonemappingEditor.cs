using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[VolumeComponentEditor(typeof(PPTonemapping))]
sealed class PPTonemappingEditor : PPVolumeComponentEditor
{
    SerializedDataParameter m_Mode;

    public override void FindSerializedDataParameter()
    {
        m_Mode = Unpack(serializedObject.FindProperty("mode"));
    }

    public override void OnInspectorGUI()
    {
        PropertyField(m_Mode);

        // Display a warning if the user is trying to use a tonemap while rendering in LDR
        var layer = PPCamera.layer;
        if (layer != null && !Utils.CheckSupportHDR())
        {
            EditorGUILayout.HelpBox("Tonemapping should only be used when working in HDR.", MessageType.Warning);
            return;
        }
    }
}
