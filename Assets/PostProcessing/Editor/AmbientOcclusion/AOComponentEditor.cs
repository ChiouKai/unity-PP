using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class AOComponentEditor
{
    public SerializedObject serializedObject;
    public AOComponent target;
    public SerializedProperty Property;
    Editor m_Inspector;


    public void Init(AOComponent target, Editor inspector)
    {
        this.target = target;
        m_Inspector = inspector;
        serializedObject = new SerializedObject(target);
        FindSerializedDataParameter();
    }
    public virtual void FindSerializedDataParameter()
    {

    }
    public virtual bool OnInspectorGUI()
    {
        return false;
    }

    protected SerializedDataParameter Unpack(SerializedProperty property)
    {
        Assert.IsNotNull(property);
        return new SerializedDataParameter(property);
    }

    protected void PropertyField(SerializedDataParameter property)
    {
        PPVolumeComponentEditor.s_ParameterDrawers.TryGetValue(property.referenceType, out var drawer);
        var title = EditorGUIUtility.TrTextContent(property.displayName, property.GetAttribute<TooltipAttribute>()?.tooltip);
        if (drawer != null)
        {
            if (drawer.OnGUI(property, title))
                return;
        }
        EditorGUILayout.PropertyField(property.value, title);
    }
}
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class AOComponentEditorAttribute : Attribute
{

    public readonly Type componentType;

    public AOComponentEditorAttribute(Type componentType)
    {
        this.componentType = componentType;
    }
}
