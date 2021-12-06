using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[VolumeComponentEditor(typeof(PPLiftGammaGain))]
sealed class LiftGammaGainEditor : PPVolumeComponentEditor
{
    SerializedDataParameter m_Lift;
    SerializedDataParameter m_Gamma;
    SerializedDataParameter m_Gain;

    readonly TrackballUIDrawer m_TrackballUIDrawer = new TrackballUIDrawer();

    public override void FindSerializedDataParameter()
    {
        m_Lift = Unpack(serializedObject.FindProperty("lift"));
        m_Gamma = Unpack(serializedObject.FindProperty("gamma"));
        m_Gain = Unpack(serializedObject.FindProperty("gain"));
    }

    public override void OnInspectorGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            m_TrackballUIDrawer.OnGUI(m_Lift.value, m_Lift.overrideState, EditorGUIUtility.TrTextContent("Lift"), GetLiftValue);
            GUILayout.Space(4f);
            m_TrackballUIDrawer.OnGUI(m_Gamma.value, m_Gamma.overrideState, EditorGUIUtility.TrTextContent("Gamma"), GetLiftValue);
            GUILayout.Space(4f);
            m_TrackballUIDrawer.OnGUI(m_Gain.value, m_Gain.overrideState, EditorGUIUtility.TrTextContent("Gain"), GetLiftValue);
        }
    }

    static Vector3 GetLiftValue(Vector4 x) => new Vector3(x.x + x.w, x.y + x.w, x.z + x.w);
}
