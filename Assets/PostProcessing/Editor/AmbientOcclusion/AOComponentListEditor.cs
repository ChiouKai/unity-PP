using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AOComponentListEditor
{
    Editor m_BaseEditor;
    SerializedObject m_SerializedObject;
    SerializedProperty m_AOProperty , m_AmbientOcclusionType;
    public AmbientOcclusion asset;
    AOComponentEditor MainEditor;

    Dictionary<Type, Type> m_EditorTypes = new Dictionary<Type, Type>();

    public AOComponentListEditor(Editor editor)
    {
        m_BaseEditor = editor;
    }
    public void Init(AmbientOcclusion asset, SerializedObject serializedObject)
    {
        //Assert.IsNotNull(asset);
        //Assert.IsNotNull(serializedObject);

        this.asset = asset;
        m_SerializedObject = serializedObject;
        m_AOProperty = serializedObject.FindProperty("AO");
        m_AmbientOcclusionType = serializedObject.FindProperty("aoType");
        //Assert.IsNotNull(m_ComponentsProperty);

        var editorTypes = Utils.GetAllTypesDerivedFrom<AOComponentEditor>()
        .Where(t => t.IsDefined(typeof(AOComponentEditorAttribute), false)
                && !t.IsAbstract);

        foreach (var editorType in editorTypes)
        {
            var attribute = (AOComponentEditorAttribute)editorType.GetCustomAttributes(typeof(AOComponentEditorAttribute), false)[0];
            m_EditorTypes.Add(attribute.componentType, editorType);
        }

        CreateEditor(asset.AO, m_AOProperty);

        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }
    void OnUndoRedoPerformed()
    {
        asset.isDirty = true;

        // Dumb hack to make sure the serialized object is up to date on undo (else there'll be
        // a state mismatch when this class is used in a GameObject inspector).
        if (m_SerializedObject != null
             && !m_SerializedObject.Equals(null)
             && m_SerializedObject.targetObject != null
             && !m_SerializedObject.targetObject.Equals(null))
        {
            m_SerializedObject.Update();
            m_SerializedObject.ApplyModifiedProperties();
        }

        // Seems like there's an issue with the inspector not repainting after some undo events
        // This will take care of that
        m_BaseEditor.Repaint();
    }
    private void CreateEditor(AOComponent component, SerializedProperty property)
    {
        if (component == null)
        {
            MainEditor = null;
            return;
        }
        var componentType = component.GetType();
        Type editorType;

        m_EditorTypes.TryGetValue(componentType, out editorType);
            
        MainEditor = (AOComponentEditor)Activator.CreateInstance(editorType);
        MainEditor.Property = property.Copy();
        MainEditor.Init(component, m_BaseEditor);
    }
    public bool OnGUI()
    {
        if (asset == null)
            return false;
        m_SerializedObject.Update();
        bool change = false;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_AmbientOcclusionType, AOType);
        if (EditorGUI.EndChangeCheck())
        {
            m_SerializedObject.ApplyModifiedProperties();
            asset.ChangeType();
            m_AOProperty = m_SerializedObject.FindProperty("AO");
            CreateEditor(asset.AO, m_AOProperty);
        }
        if (MainEditor != null)
        {
            string title = ObjectNames.NicifyVariableName(MainEditor.target.GetType().Name);
            bool displayContent = EditorUtils.DrawHeader(title, MainEditor.Property);
            if (displayContent)
                change |= MainEditor.OnInspectorGUI();
        }
        m_SerializedObject.ApplyModifiedProperties();
        return change;
    }
    public void Clear()
    {
        MainEditor = null;
        asset = null;
        m_EditorTypes.Clear();
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
    }

    static GUIContent AOType = new GUIContent("AmbientOcclusion Type");
}


