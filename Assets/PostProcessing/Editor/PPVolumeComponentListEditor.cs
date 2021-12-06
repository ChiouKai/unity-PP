using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class PPVolumeComponentListEditor
{
    public PPVolumeProfile asset { get; private set; }

    Editor m_BaseEditor;

    SerializedObject m_SerializedObject;
    SerializedProperty m_ComponentsProperty;

    Dictionary<Type, Type> m_EditorTypes; // Component type => Editor type
    List<PPVolumeComponentEditor> m_Editors;

    //static Dictionary<Type, string> m_EditorDocumentationURLs;



    public PPVolumeComponentListEditor(Editor editor)
    {
        //Assert.IsNotNull(editor);
        m_BaseEditor = editor;
    }

    public void Init(PPVolumeProfile asset, SerializedObject serializedObject)
    {
        //Assert.IsNotNull(asset);
        //Assert.IsNotNull(serializedObject);

        this.asset = asset;
        m_SerializedObject = serializedObject;
        m_ComponentsProperty = serializedObject.FindProperty("components");
        //Assert.IsNotNull(m_ComponentsProperty);
        m_EditorTypes = new Dictionary<Type, Type>();
        m_Editors = new List<PPVolumeComponentEditor>();

        var editorTypes = Utils.GetAllTypesDerivedFrom<PPVolumeComponentEditor>()
        .Where(
                t => t.IsDefined(typeof(VolumeComponentEditorAttribute), false)
                && !t.IsAbstract);

        foreach (var editorType in editorTypes)
        {
            var attribute = (VolumeComponentEditorAttribute)editorType.GetCustomAttributes(typeof(VolumeComponentEditorAttribute), false)[0];
            m_EditorTypes.Add(attribute.componentType, editorType);
        }

        var com = asset.components;
        for(int i = 0; i < com.Count; ++i)
        {
            CreateEditor(com[i], m_ComponentsProperty.GetArrayElementAtIndex(i));
        }

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
        asset.ChangeComponent();
        // Seems like there's an issue with the inspector not repainting after some undo events
        // This will take care of that
        m_BaseEditor.Repaint();
    }
    private void CreateEditor(PPVolumeComponent component, SerializedProperty property, int i = -1, bool forceOpen = false)
    {
        var componentType = component.GetType();
        Type editorType;

        if (!m_EditorTypes.TryGetValue(componentType, out editorType))
            editorType = typeof(PPVolumeComponentEditor);

        var editor = (PPVolumeComponentEditor)Activator.CreateInstance(editorType);
        editor.baseProperty = property.Copy();
        editor.Init(component, m_BaseEditor);

        if (forceOpen)
            editor.baseProperty.isExpanded = true;

        if (i < 0)
            m_Editors.Add(editor);
        else
            m_Editors[i] = editor;
    }

    void RefreshEditors()
    {
        // Disable all editors first
        foreach (var editor in m_Editors)
            editor.OnDisable();

        // Remove them
        m_Editors.Clear();

        // Refresh the ref to the serialized components in case the asset got swapped or another
        // script is editing it while it's active in the inspector
        m_SerializedObject.Update();
        m_ComponentsProperty = m_SerializedObject.FindProperty("components");
        Assert.IsNotNull(m_ComponentsProperty);

        // Recreate editors for existing settings, if any
        var components = asset.components;
        for (int i = 0; i < components.Count; i++)
            CreateEditor(components[i], m_ComponentsProperty.GetArrayElementAtIndex(i));
    }

    public void Clear()
    {
        if (m_Editors == null)
            return; // Hasn't been initialized yet

        foreach (var editor in m_Editors)
            editor.OnDisable();

        m_Editors.Clear();
        m_EditorTypes.Clear();

        // ReSharper disable once DelegateSubtraction
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
    }

    int m_CurrentHashCode;
    public void OnGUI()
    {
        if (asset == null)
            return;
        int tmp = asset.GetComponentListHashCode();
        if (asset.isDirty || tmp != m_CurrentHashCode)
        {
            RefreshEditors();
            m_CurrentHashCode = tmp;
            asset.isDirty = false;
        }
        bool isEditable = !UnityEditor.VersionControl.Provider.isActive
               || AssetDatabase.IsOpenForEdit(asset, StatusQueryOptions.UseCachedIfPossible);
        using (new EditorGUI.DisabledScope(!isEditable))
        {
            // Component list
            for (int i = 0; i < m_Editors.Count; i++)
            {
                var editor = m_Editors[i];
                string title = ObjectNames.NicifyVariableName(editor.target.GetType().Name);
                int id = i; // Needed for closure capture below

                bool change;
                EditorUtils.DrawSplitter();
                bool displayContent = EditorUtils.DrawHeaderToggle(title, editor.baseProperty, editor.activeProperty,
                        pos => OnContextClick(pos, editor.target, id), null, out change);

                if (displayContent)
                {
                    using (new EditorGUI.DisabledScope(!editor.activeProperty.boolValue))
                        if (editor.OnInternalInspectorGUI())
                        {
                            change = true;
                        }
                }
                if (change)
                {
                    asset.ChangeComponent();
                }
            }

            if (m_Editors.Count > 0)
                EditorUtils.DrawSplitter();
            else
                EditorGUILayout.HelpBox("This Volume Profile contains no overrides.", MessageType.Info);
            EditorGUILayout.Space();

            using (var hscope = new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Add Override"), EditorStyles.miniButton))
                {
                    var r = hscope.rect;
                    var pos = new Vector2(r.x + r.width / 2f, r.yMax + 18f);
                    FilterWindow.Show(pos, new VolumeComponentProvider(asset, this));
                }
            }
        }
    }

    void OnContextClick(Vector2 position, PPVolumeComponent targetComponent, int id)
    {
        var menu = new GenericMenu();

        if (id == 0)
        {
            menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Up"));
            menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move to Top"));
        }
        else
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Move to Top"), false, () => MoveComponent(id, -id));
            menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveComponent(id, -1));
        }

        if (id == m_Editors.Count - 1)
        {
            menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move to Bottom"));
            menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"));
        }
        else
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Move to Bottom"), false, () => MoveComponent(id, (m_Editors.Count - 1) - id));
            menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(id, 1));
        }

        menu.AddSeparator(string.Empty);
        menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All"), false, () => CollapseComponents());
        menu.AddItem(EditorGUIUtility.TrTextContent("Expand All"), false, () => ExpandComponents());
        menu.AddSeparator(string.Empty);
        menu.AddItem(EditorGUIUtility.TrTextContent("Reset"), false, () => ResetComponent(targetComponent.GetType(), id));
        menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));
        menu.AddSeparator(string.Empty);
        menu.AddItem(EditorGUIUtility.TrTextContent("Copy Settings"), false, () => CopySettings(targetComponent));

        if (CanPaste(targetComponent))
            menu.AddItem(EditorGUIUtility.TrTextContent("Paste Settings"), false, () => PasteSettings(targetComponent));
        else
            menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Paste Settings"));

        menu.AddSeparator(string.Empty);
        menu.AddItem(EditorGUIUtility.TrTextContent("Toggle All"), false, () => m_Editors[id].SetAllOverridesTo(true));
        menu.AddItem(EditorGUIUtility.TrTextContent("Toggle None"), false, () => m_Editors[id].SetAllOverridesTo(false));

        menu.DropDown(new Rect(position, Vector2.zero));
    }

    internal void ResetComponent(Type type, int id)
    {
        // Remove from the cached editors list
        m_Editors[id].OnDisable();
        m_Editors[id] = null;

        m_SerializedObject.Update();

        var property = m_ComponentsProperty.GetArrayElementAtIndex(id);
        var prevComponent = property.objectReferenceValue;

        // Unassign it but down remove it from the array to keep the index available
        property.objectReferenceValue = null;

        // Create a new object
        var newComponent = CreateNewComponent(type);
        Undo.RegisterCreatedObjectUndo(newComponent, "Reset Volume Overrides");

        // Store this new effect as a subasset so we can reference it safely afterwards
        AssetDatabase.AddObjectToAsset(newComponent, asset);

        // Put it in the reserved space
        property.objectReferenceValue = newComponent;
        asset.ChangeComponent();
        // Create & store the internal editor object for this effect

        CreateEditor(newComponent, property, id);

        m_SerializedObject.ApplyModifiedProperties();

        // Same as RemoveComponent, destroy at the end so it's recreated first on Undo to make
        // sure the GUID exists before undoing the list state
        Undo.DestroyObjectImmediate(prevComponent);

        // Force save / refresh
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
    PPVolumeComponent CreateNewComponent(Type type)
    {
        var effect = (PPVolumeComponent)ScriptableObject.CreateInstance(type);
        effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        effect.name = type.Name;
        return effect;
    }

    internal void AddComponent(Type type)
    {
        m_SerializedObject.Update();

        var component = CreateNewComponent(type);
        Undo.RegisterCreatedObjectUndo(component, "Add Volume Override");

        // Store this new effect as a subasset so we can reference it safely afterwards
        // Only when we're not dealing with an instantiated asset
        if (EditorUtility.IsPersistent(asset))
            AssetDatabase.AddObjectToAsset(component, asset);

        // Grow the list first, then add - that's how serialized lists work in Unity
        m_ComponentsProperty.arraySize++;
        var componentProp = m_ComponentsProperty.GetArrayElementAtIndex(m_ComponentsProperty.arraySize - 1);
        componentProp.objectReferenceValue = component;
        asset.ChangeComponent();
        // Create & store the internal editor object for this effect
        CreateEditor(component, componentProp, forceOpen: true);

        m_SerializedObject.ApplyModifiedProperties();

        // Force save / refresh
        if (EditorUtility.IsPersistent(asset))
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }


    internal void RemoveComponent(int id)
    {
        // Huh. Hack to keep foldout state on the next element...
        bool nextFoldoutState = false;
        if (id < m_Editors.Count - 1)
            nextFoldoutState = m_Editors[id + 1].baseProperty.isExpanded;

        // Remove from the cached editors list
        m_Editors[id].OnDisable();
        m_Editors.RemoveAt(id);

        m_SerializedObject.Update();

        var property = m_ComponentsProperty.GetArrayElementAtIndex(id);
        var component = property.objectReferenceValue;

        // Unassign it (should be null already but serialization does funky things
        property.objectReferenceValue = null;

        // ...and remove the array index itself from the list
        m_ComponentsProperty.DeleteArrayElementAtIndex(id);

        // Finally refresh editor reference to the serialized settings list
        for (int i = 0; i < m_Editors.Count; i++)
            m_Editors[i].baseProperty = m_ComponentsProperty.GetArrayElementAtIndex(i).Copy();

        // Set the proper foldout state if needed
        if (id < m_Editors.Count)
            m_Editors[id].baseProperty.isExpanded = nextFoldoutState;

        asset.ChangeComponent();
        m_SerializedObject.ApplyModifiedProperties();

        // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
        // actions will be in the wrong order and the reference to the setting object in the
        // list will be lost.
        Undo.DestroyObjectImmediate(component);

        // Force save / refresh
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    internal void MoveComponent(int id, int offset)
    {
        // Move components
        m_SerializedObject.Update();
        m_ComponentsProperty.MoveArrayElement(id, id + offset);
        m_SerializedObject.ApplyModifiedProperties();

        // We need to keep track of what was expanded before to set it afterwards.
        bool targetExpanded = m_Editors[id + offset].baseProperty.isExpanded;
        bool sourceExpanded = m_Editors[id].baseProperty.isExpanded;

        // Move editors
        var prev = m_Editors[id + offset];
        m_Editors[id + offset] = m_Editors[id];
        m_Editors[id] = prev;

        // Set the expansion values
        m_Editors[id + offset].baseProperty.isExpanded = targetExpanded;
        m_Editors[id].baseProperty.isExpanded = sourceExpanded;
    }

    internal void CollapseComponents()
    {
        // Move components
        m_SerializedObject.Update();
        int numEditors = m_Editors.Count;
        for (int i = 0; i < numEditors; ++i)
        {
            m_Editors[i].baseProperty.isExpanded = false;
        }
        m_SerializedObject.ApplyModifiedProperties();
    }

    internal void ExpandComponents()
    {
        // Move components
        m_SerializedObject.Update();
        int numEditors = m_Editors.Count;
        for (int i = 0; i < numEditors; ++i)
        {
            m_Editors[i].baseProperty.isExpanded = true;
        }
        m_SerializedObject.ApplyModifiedProperties();
    }

    static bool CanPaste(PPVolumeComponent targetComponent)
    {
        if (string.IsNullOrWhiteSpace(EditorGUIUtility.systemCopyBuffer))
            return false;

        string clipboard = EditorGUIUtility.systemCopyBuffer;
        int separator = clipboard.IndexOf('|');

        if (separator < 0)
            return false;

        return targetComponent.GetType().AssemblyQualifiedName == clipboard.Substring(0, separator);
    }

    static void CopySettings(PPVolumeComponent targetComponent)
    {
        string typeName = targetComponent.GetType().AssemblyQualifiedName;
        string typeData = JsonUtility.ToJson(targetComponent);
        EditorGUIUtility.systemCopyBuffer = $"{typeName}|{typeData}";
    }

    static void PasteSettings(PPVolumeComponent targetComponent)
    {
        string clipboard = EditorGUIUtility.systemCopyBuffer;
        string typeData = clipboard.Substring(clipboard.IndexOf('|') + 1);
        Undo.RecordObject(targetComponent, "Paste Settings");
        JsonUtility.FromJsonOverwrite(typeData, targetComponent);
    }
}
