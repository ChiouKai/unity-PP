using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PPVolumeProfile))]
public class PPVolumeProfileEditor : Editor
{
    PPVolumeComponentListEditor componentList;

    private void OnEnable()
    {
        componentList = new PPVolumeComponentListEditor(this);
        componentList.Init(target as PPVolumeProfile, serializedObject);
    }
    void OnDisable()
    {
        if (componentList != null)
            componentList.Clear();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        componentList.OnGUI();
        serializedObject.ApplyModifiedProperties();
    }
}
