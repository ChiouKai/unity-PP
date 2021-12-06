using UnityEditor;
using UnityEngine;


sealed class MinFloatParameterDrawer : ParameterDrawer
{
    public MinFloatParameterDrawer()
    {
        type = typeof(MinFloatParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Float)
            return false;

        var o = parameter.GetObjectRef<MinFloatParameter>();
        float v = EditorGUILayout.FloatField(title, value.floatValue);
        value.floatValue = Mathf.Max(v, o.min);
        return true;
    }
}

sealed class NoInterpMinFloatParameterDrawer : ParameterDrawer
{
    public NoInterpMinFloatParameterDrawer()
    {
        type = typeof(NoInterpMinFloatParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Float)
            return false;

        var o = parameter.GetObjectRef<NoInterpMinFloatParameter>();
        float v = EditorGUILayout.FloatField(title, value.floatValue);
        value.floatValue = Mathf.Max(v, o.min);
        return true;
    }
}

sealed class MaxFloatParameterDrawer : ParameterDrawer
{
    public MaxFloatParameterDrawer()
    {
        type = typeof(MaxFloatParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Float)
            return false;

        var o = parameter.GetObjectRef<MaxFloatParameter>();
        float v = EditorGUILayout.FloatField(title, value.floatValue);
        value.floatValue = Mathf.Min(v, o.max);
        return true;
    }
}

sealed class NoInterpMaxFloatParameterDrawer : ParameterDrawer
{
    public NoInterpMaxFloatParameterDrawer()
    {
        type = typeof(NoInterpMaxFloatParameter);
    }

    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Float)
            return false;

        var o = parameter.GetObjectRef<NoInterpMaxFloatParameter>();
        float v = EditorGUILayout.FloatField(title, value.floatValue);
        value.floatValue = Mathf.Min(v, o.max);
        return true;
    }
}
sealed class ClampedFloatParameterDrawer : ParameterDrawer
{
    public ClampedFloatParameterDrawer()
    {
        type = typeof(ClampedFloatParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Float)
            return false;

        var o = parameter.GetObjectRef<ClampedFloatParameter>();
        EditorGUILayout.Slider(value, o.min, o.max, title);
        value.floatValue = Mathf.Clamp(value.floatValue, o.min, o.max);
        return true;
    }
}

sealed class NoInterpClampedFloatParameterDrawer : ParameterDrawer
{
    public NoInterpClampedFloatParameterDrawer()
    {
        type = typeof(NoInterpClampedFloatParameter);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Float)
            return false;

        var o = parameter.GetObjectRef<NoInterpClampedFloatParameter>();
        EditorGUILayout.Slider(value, o.min, o.max, title);
        value.floatValue = Mathf.Clamp(value.floatValue, o.min, o.max);
        return true;
    }
}


