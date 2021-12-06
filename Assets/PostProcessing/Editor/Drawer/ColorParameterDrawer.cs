using UnityEditor;
using UnityEngine;

sealed class ColorParameterDrawer : ParameterDrawer
{
    public ColorParameterDrawer()
    {
        type = typeof(ColorParameterDrawer);
    }
    public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    {
        var value = parameter.value;

        if (value.propertyType != SerializedPropertyType.Color)
            return false;

        var o = parameter.GetObjectRef<ColorParameter>();
        value.colorValue = EditorGUILayout.ColorField(title, value.colorValue, o.showEyeDropper, o.showAlpha, o.hdr);
        return true;
    }
}
