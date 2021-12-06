using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

[CustomEditor(typeof(AmbientOcclusion))]
public class AmbientOcclusionEditor : Editor
{
    AOComponentListEditor editor;

    private void OnEnable()
    {
        editor = new AOComponentListEditor(this);
        editor.Init(target as AmbientOcclusion, serializedObject);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        editor.OnGUI();
        serializedObject.ApplyModifiedProperties();
    }
    void OnDisable()
    {
        if (editor != null)
        editor.Clear();
    }
}
