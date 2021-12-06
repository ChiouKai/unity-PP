using UnityEngine;
using UnityEditorInternal;
using UnityEditor;

sealed class MinIntParameterDrawer : ParameterDrawer
{
    public MinIntParameterDrawer()
    {
        type = typeof(MinIntParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Integer)
            return false;

        var o = parameter.GetObjectRef<MinIntParameter>();
        int v = EditorGUILayout.IntField(title, value.intValue);
        value.intValue = Mathf.Max(v, o.min);
        return true;
    }
}

sealed class NoInterpMinIntParameterDrawer : ParameterDrawer
{
    public NoInterpMinIntParameterDrawer()
    {
        type = typeof(NoInterpMinIntParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Integer)
            return false;

        var o = parameter.GetObjectRef<NoInterpMinIntParameter>();
        int v = EditorGUILayout.IntField(title, value.intValue);
        value.intValue = Mathf.Max(v, o.min);
        return true;
    }
}


sealed class MaxIntParameterDrawer : ParameterDrawer
{
    public MaxIntParameterDrawer()
    {
        type = typeof(MaxIntParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Integer)
            return false;

        var o = parameter.GetObjectRef<MaxIntParameter>();
        int v = EditorGUILayout.IntField(title, value.intValue);
        value.intValue = Mathf.Min(v, o.max);
        return true;
    }
}

sealed class NoInterpMaxIntParameterDrawer : ParameterDrawer
{
    public NoInterpMaxIntParameterDrawer()
    {
        type = typeof(NoInterpMaxIntParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Integer)
            return false;

        var o = parameter.GetObjectRef<NoInterpMaxIntParameter>();
        int v = EditorGUILayout.IntField(title, value.intValue);
        value.intValue = Mathf.Min(v, o.max);
        return true;
    }
}


sealed class ClampedIntParameterDrawer : ParameterDrawer
{
    public ClampedIntParameterDrawer()
    {
        type = typeof(ClampedIntParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Integer)
            return false;

        var o = parameter.GetObjectRef<ClampedIntParameter>();
        EditorGUILayout.IntSlider(value, o.min, o.max, title);
        value.intValue = Mathf.Clamp(value.intValue, o.min, o.max);
        return true;
    }
}

sealed class NoInterpClampedIntParameterDrawer : ParameterDrawer
{
    public NoInterpClampedIntParameterDrawer()
    {
        type = typeof(NoInterpClampedIntParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Integer)
            return false;

        var o = parameter.GetObjectRef<NoInterpClampedIntParameter>();
        EditorGUILayout.IntSlider(value, o.min, o.max, title);
        value.intValue = Mathf.Clamp(value.intValue, o.min, o.max);
        return true;
    }
}

sealed class LayerMaskParameterDrawer : ParameterDrawer
{
    public LayerMaskParameterDrawer()
    {
        type = typeof(LayerMaskParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.LayerMask)
            return false;

        value.intValue = EditorGUILayout.MaskField(title, value.intValue, InternalEditorUtility.layers);
        return true;
    }
}

