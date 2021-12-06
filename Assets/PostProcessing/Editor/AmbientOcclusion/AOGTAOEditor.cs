using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[AOComponentEditor(typeof(AOGTAO))]
public class AOGTAOEditor : AOComponentEditor
{
    SerializedDataParameter m_DirSampler;
    SerializedDataParameter m_SliceSampler;
    SerializedDataParameter m_Intensity;
    SerializedDataParameter m_Radius;
    SerializedDataParameter m_MultiBounce;

    SerializedDataParameter m_SpatialBilateralAggressiveness;
    SerializedDataParameter m_GhostingReduction;

    public override void FindSerializedDataParameter()
    {
        m_DirSampler = Unpack(serializedObject.FindProperty("DirSampler"));
        m_SliceSampler = Unpack(serializedObject.FindProperty("SliceSampler"));
        m_Intensity = Unpack(serializedObject.FindProperty("Intensity"));
        m_Radius = Unpack(serializedObject.FindProperty("Radius"));
        m_MultiBounce = Unpack(serializedObject.FindProperty("MultiBounce"));

        m_SpatialBilateralAggressiveness = Unpack(serializedObject.FindProperty("SpatialBilateralAggressiveness"));
        m_GhostingReduction = Unpack(serializedObject.FindProperty("GhostingReduction"));
    }
    public override bool OnInspectorGUI()
    {
        serializedObject.Update();
        bool change = false;
        EditorGUI.BeginChangeCheck();
        PropertyField(m_DirSampler);
        PropertyField(m_SliceSampler);
        PropertyField(m_Intensity);
        PropertyField(m_Radius);
        PropertyField(m_MultiBounce);
        PropertyField(m_SpatialBilateralAggressiveness);
        PropertyField(m_GhostingReduction);
        change |= EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        return change;
    }

}
