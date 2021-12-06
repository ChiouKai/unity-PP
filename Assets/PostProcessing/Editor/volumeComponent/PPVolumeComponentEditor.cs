using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System;
using UnityEngine.Assertions;

public class PPVolumeComponentEditor
{
    public SerializedObject serializedObject;
    public PPVolumeComponent target;
    public SerializedProperty baseProperty, activeProperty, PreActiveProperty;
    Editor m_Inspector;
    List<SerializedDataParameter> m_Parameters;

    public void Init(PPVolumeComponent target, Editor inspector)
    {
        this.target = target;
        m_Inspector = inspector;
        serializedObject = new SerializedObject(target);
        activeProperty = serializedObject.FindProperty("active");
        PreActiveProperty = serializedObject.FindProperty("PreActive");
        FindSerializedDataParameter();
    }

    public virtual void FindSerializedDataParameter()
    {
        var fields = new List<SerializedProperty>();
        GetFields(target, fields);
        m_Parameters = fields.Select(t =>
        {
            var parameter = new SerializedDataParameter(t);
            return parameter;
        }).ToList();
    }

    void GetFields(object o, List<SerializedProperty> infos, SerializedProperty prop = null)
    {
        if (o == null)
            return;

        var fields = o.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType.IsSubclassOf(typeof(Parameter)))
            {
                if ((field.GetCustomAttributes(typeof(HideInInspector), false).Length == 0) &&
                    ((field.GetCustomAttributes(typeof(SerializeField), false).Length > 0) ||
                     (field.IsPublic && field.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0)))
                    infos.Add((prop == null ?
                        serializedObject.FindProperty(field.Name) : prop.FindPropertyRelative(field.Name)));
            }
            else if (!field.FieldType.IsArray && field.FieldType.IsClass)
                GetFields(field.GetValue(o), infos, prop == null ?
                    serializedObject.FindProperty(field.Name) : prop.FindPropertyRelative(field.Name));
        }
    }
    public static Dictionary<Type, ParameterDrawer> s_ParameterDrawers;

    static PPVolumeComponentEditor()
    {
        s_ParameterDrawers = new Dictionary<Type, ParameterDrawer>();
        ReloadDecoratorTypes();
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnEditorReload()
    {
        ReloadDecoratorTypes();
    }

    static void ReloadDecoratorTypes()
    {
        s_ParameterDrawers.Clear();

        // Look for all the valid parameter drawers
        var types = Utils.GetAllTypesDerivedFrom<ParameterDrawer>()
            .Where(t => !t.IsAbstract);

        // Store them
        foreach (var type in types)
        {
            var decorator = (ParameterDrawer)Activator.CreateInstance(type);
            s_ParameterDrawers.Add(decorator.type, decorator);
        }
    }

    public void Repaint()
    {
        m_Inspector.Repaint();
    }

    public virtual void OnDisable()
    { 
    }

    internal void SetAllOverridesTo(bool state)
    {
        Undo.RecordObject(target, "Toggle All");
        target.SetAllOverridesTo(state);
        serializedObject.Update();
    }
    internal bool OnInternalInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        TopRowFields();
        OnInspectorGUI();//
        EditorGUILayout.Space();
        bool change = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        return change;
    }
    public virtual void OnInspectorGUI()
    {
        // Display every field as-is
        foreach (var parameter in m_Parameters)
        {
            PropertyField(parameter);
        }
    }

    protected void PropertyField(SerializedDataParameter property)
    {
        var title = EditorGUIUtility.TrTextContent(property.displayName, property.GetAttribute<TooltipAttribute>()?.tooltip);
        PropertyField(property, title);
    }
    protected void PropertyField(SerializedDataParameter property, GUIContent title)
    {
        HandleDecorators(property, title);

        // Custom parameter drawer
        s_ParameterDrawers.TryGetValue(property.referenceType, out var drawer);

        bool invalidProp = false;

        if (drawer != null && !drawer.IsAutoProperty())
        {
            if (drawer.OnGUI(property, title))
                return;

            invalidProp = true;
        }

        // ObjectParameter<T> is a special case
        if (Parameter.IsObjectParameter(property.referenceType))
        {
            bool expanded = property.value.isExpanded;
            expanded = EditorGUILayout.Foldout(expanded, title, true);

            if (expanded)
            {
                EditorGUI.indentLevel++;

                // Not the fastest way to do it but that'll do just fine for now
                var it = property.value.Copy();
                var end = it.GetEndProperty();
                bool first = true;

                while (it.Next(first) && !SerializedProperty.EqualContents(it, end))
                {
                    PropertyField(Unpack(it));
                    first = false;
                }

                EditorGUI.indentLevel--;
            }

            property.value.isExpanded = expanded;
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            // Override checkbox
            DrawOverrideCheckbox(property);

            // Property
            using (new EditorGUI.DisabledScope(!property.overrideState.boolValue))
            {
                if (drawer != null && !invalidProp)
                {
                    if (drawer.OnGUI(property, title))
                        return;
                }

                // Default unity field
                EditorGUILayout.PropertyField(property.value, title);
            }
        }
    }

    protected SerializedDataParameter Unpack(SerializedProperty property)
    {
        Assert.IsNotNull(property);
        return new SerializedDataParameter(property);
    }
    void HandleDecorators(SerializedDataParameter property, GUIContent title)
    {
        foreach (var attr in property.attributes)
        {
            if (!(attr is PropertyAttribute))
                continue;

            switch (attr)
            {
                case SpaceAttribute spaceAttribute:
                    EditorGUILayout.GetControlRect(false, spaceAttribute.height);
                    break;
                case HeaderAttribute headerAttribute:
                    {
                        var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
                        EditorGUI.LabelField(rect, headerAttribute.header, EditorStyles.miniLabel);
                        break;
                    }
                case TooltipAttribute tooltipAttribute:
                    {
                        if (string.IsNullOrEmpty(title.tooltip))
                            title.tooltip = tooltipAttribute.tooltip;
                        break;
                    }
                case InspectorNameAttribute inspectorNameAttribute:
                    title.text = inspectorNameAttribute.displayName;
                    break;
            }
        }
    }
    void TopRowFields()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("All", "Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need."), CoreEditorStyles.miniLabelButton, GUILayout.Width(17f), GUILayout.ExpandWidth(false)))
                SetAllOverridesTo(true);

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("None", "Toggle all overrides off."), CoreEditorStyles.miniLabelButton, GUILayout.Width(32f), GUILayout.ExpandWidth(false)))
                SetAllOverridesTo(false);
        }
    }
    protected void DrawOverrideCheckbox(SerializedDataParameter property)
    {
        var overrideRect = GUILayoutUtility.GetRect(17f, 17f, GUILayout.ExpandWidth(false));
        overrideRect.yMin += 4f;
        property.overrideState.boolValue = GUI.Toggle(overrideRect, property.overrideState.boolValue, EditorGUIUtility.TrTextContent("", "Override this setting for this volume."), CoreEditorStyles.smallTickbox);
    }


}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class VolumeComponentEditorAttribute : Attribute
{
    /// <summary>
    /// A type derived from <see cref="VolumeComponent"/>.
    /// </summary>
    public readonly Type componentType;

    /// <summary>
    /// Creates a new <see cref="VolumeComponentEditorAttribute"/> instance.
    /// </summary>
    /// <param name="componentType">A type derived from <see cref="VolumeComponent"/></param>
    public VolumeComponentEditorAttribute(Type componentType)
    {
        this.componentType = componentType;
    }
}
