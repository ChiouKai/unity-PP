using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[VolumeComponentEditor(typeof(PPChannelMixer))]
sealed class PPChannelMixerEditor : PPVolumeComponentEditor
{
    SerializedDataParameter m_RedOutRedIn;
    SerializedDataParameter m_RedOutGreenIn;
    SerializedDataParameter m_RedOutBlueIn;
    SerializedDataParameter m_GreenOutRedIn;
    SerializedDataParameter m_GreenOutGreenIn;
    SerializedDataParameter m_GreenOutBlueIn;
    SerializedDataParameter m_BlueOutRedIn;
    SerializedDataParameter m_BlueOutGreenIn;
    SerializedDataParameter m_BlueOutBlueIn;

    SavedInt m_SelectedChannel;


    public override void FindSerializedDataParameter()
    {
        m_RedOutRedIn = Unpack(serializedObject.FindProperty("redOutRedIn"));
        m_RedOutGreenIn = Unpack(serializedObject.FindProperty("redOutGreenIn"));
        m_RedOutBlueIn = Unpack(serializedObject.FindProperty("redOutBlueIn"));
        m_GreenOutRedIn = Unpack(serializedObject.FindProperty("greenOutRedIn"));
        m_GreenOutGreenIn = Unpack(serializedObject.FindProperty("greenOutGreenIn"));
        m_GreenOutBlueIn = Unpack(serializedObject.FindProperty("greenOutBlueIn"));
        m_BlueOutRedIn = Unpack(serializedObject.FindProperty("blueOutRedIn"));
        m_BlueOutGreenIn = Unpack(serializedObject.FindProperty("blueOutGreenIn"));
        m_BlueOutBlueIn = Unpack(serializedObject.FindProperty("blueOutBlueIn"));

        m_SelectedChannel = new SavedInt($"{target.GetType()}.SelectedChannel", 0);
    }

    public override void OnInspectorGUI()
    {
        int currentChannel = m_SelectedChannel.value;

        EditorGUI.BeginChangeCheck();
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(currentChannel == 0, EditorGUIUtility.TrTextContent("Red", "Red output channel."), EditorStyles.miniButtonLeft)) currentChannel = 0;
                if (GUILayout.Toggle(currentChannel == 1, EditorGUIUtility.TrTextContent("Green", "Green output channel."), EditorStyles.miniButtonMid)) currentChannel = 1;
                if (GUILayout.Toggle(currentChannel == 2, EditorGUIUtility.TrTextContent("Blue", "Blue output channel."), EditorStyles.miniButtonRight)) currentChannel = 2;
            }
        }
        if (EditorGUI.EndChangeCheck())
            GUI.FocusControl(null);

        m_SelectedChannel.value = currentChannel;

        if (currentChannel == 0)
        {
            PropertyField(m_RedOutRedIn, EditorGUIUtility.TrTextContent("Red"));
            PropertyField(m_RedOutGreenIn, EditorGUIUtility.TrTextContent("Green"));
            PropertyField(m_RedOutBlueIn, EditorGUIUtility.TrTextContent("Blue"));
        }
        else if (currentChannel == 1)
        {
            PropertyField(m_GreenOutRedIn, EditorGUIUtility.TrTextContent("Red"));
            PropertyField(m_GreenOutGreenIn, EditorGUIUtility.TrTextContent("Green"));
            PropertyField(m_GreenOutBlueIn, EditorGUIUtility.TrTextContent("Blue"));
        }
        else
        {
            PropertyField(m_BlueOutRedIn, EditorGUIUtility.TrTextContent("Red"));
            PropertyField(m_BlueOutGreenIn, EditorGUIUtility.TrTextContent("Green"));
            PropertyField(m_BlueOutBlueIn, EditorGUIUtility.TrTextContent("Blue"));
        }
    }
}
