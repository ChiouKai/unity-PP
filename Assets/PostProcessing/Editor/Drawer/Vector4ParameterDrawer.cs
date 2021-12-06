using UnityEditor;
using UnityEngine;


sealed class Vector4ParametrDrawer : ParameterDrawer
{
    public Vector4ParametrDrawer()
    {
        type = typeof(Vector4Parameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Vector4)
            return false;

        value.vector4Value = EditorGUILayout.Vector4Field(title, value.vector4Value);
        return true;
    }
}

