using UnityEngine;
using UnityEditor;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

[Serializable]
public abstract class Parameter
{
    [SerializeField]
    public virtual bool overrideState
    {
        get => m_overrideState;
        set => m_overrideState = value;
    }
    [SerializeField]
    protected bool m_overrideState;
    internal abstract void Interp(Parameter from, Parameter to, float p);
    public T GetValue<T>()
    {
        return ((Parameter<T>)this).value;
    }

    public static bool IsObjectParameter(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObjectParameter<>))
            return true;

        return type.BaseType != null
            && IsObjectParameter(type.BaseType);
    }

    public abstract void SetValue(Parameter parameter);

    public virtual void Release() { }
}

[Serializable]
public class Parameter<T> : Parameter
{
    [SerializeField]
    protected T m_value;
    public virtual T value { get => m_value; set => m_value = value; }



    public Parameter() : this(default, false)
    {
    }
    protected Parameter(T value,bool overrideState)
    {
        m_value = value;
        this.overrideState = overrideState;
    }

    public override void SetValue(Parameter parameter)
    {
        m_value = parameter.GetValue<T>();
    }
    internal override void Interp(Parameter from, Parameter to, float p)
    {
        Interp(from.GetValue<T>(), to.GetValue<T>(), p);
    }
    public virtual void Interp(T from, T to, float p)
    {
        m_value = p > 0f ? to : from; ;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + overrideState.GetHashCode();

            if (!EqualityComparer<T>.Default.Equals(value, default)) // Catches null for references with boxing of value types
                hash = hash * 23 + value.GetHashCode();

            return hash;
        }
    }

    public static implicit operator T(Parameter<T> prop) => prop.m_value;

}

[Serializable]
public class BoolParameter : Parameter<bool>
{
    public BoolParameter(bool value, bool overrideState =false)
        : base(value, overrideState) { }
}

[Serializable]
public class LayerMaskParameter : Parameter<LayerMask>
{
    public LayerMaskParameter(LayerMask value, bool overrideState = false)
        : base(value, overrideState) { }

}
[Serializable]
public class ColorParameter : Parameter<Color>
{

    public bool hdr = false;


    public bool showAlpha = true;


    public bool showEyeDropper = true;

    public ColorParameter(Color value, bool overrideState = false)
        : base(value, overrideState) { }

    public ColorParameter(Color value, bool hdr, bool showAlpha, bool showEyeDropper, bool overrideState = false)
        : base(value, overrideState)
    {
        this.hdr = hdr;
        this.showAlpha = showAlpha;
        this.showEyeDropper = showEyeDropper;
        this.overrideState = overrideState;
    }

    public override void Interp(Color from, Color to, float p)
    {
        m_value.r = from.r + (to.r - from.r) * p;
        m_value.g = from.g + (to.g - from.g) * p;
        m_value.b = from.b + (to.b - from.b) * p;
        m_value.a = from.a + (to.a - from.a) * p;
    }
}

#region Int parameter

[Serializable]
public class IntParameter : Parameter<int>
{
    public IntParameter(int value, bool overrideState = false)
        : base(value, overrideState) { }

    public sealed override void Interp(int from, int to, float t)
    {
        m_value = (int)(from + (to - from) * t);
    }
}
[Serializable]
public class NoInterpIntParameter : Parameter<int>
{
    public NoInterpIntParameter(int value, bool overrideState = false)
        : base(value, overrideState) { }
}


[Serializable]
public class MinIntParameter : IntParameter
{

    public int min;

    public override int value
    {
        get => m_value;
        set => m_value = Mathf.Max(value, min);
    }

    public MinIntParameter(int value, int min, bool overrideState = false)
        : base(value, overrideState)
    {
        this.min = min;
    }
}

[Serializable]
public class NoInterpMinIntParameter : Parameter<int>
{

    public int min;

    public override int value
    {
        get => m_value;
        set => m_value = Mathf.Max(value, min);
    }

    public NoInterpMinIntParameter(int value, int min, bool overrideState = false)
        : base(value, overrideState)
    {
        this.min = min;
    }
}
[Serializable]
public class MaxIntParameter : IntParameter
{
    public int max;

    public override int value
    {
        get => m_value;
        set => m_value = Mathf.Min(value, max);
    }

    public MaxIntParameter(int value, int max, bool overrideState = false)
        : base(value, overrideState)
    {
        this.max = max;
    }
}

[Serializable]
public class NoInterpMaxIntParameter : Parameter<int>
{
    public int max;

    public override int value
    {
        get => m_value;
        set => m_value = Mathf.Min(value, max);
    }

    public NoInterpMaxIntParameter(int value, int max, bool overrideState = false)
        : base(value, overrideState)
    {
        this.max = max;
    }
}



    [Serializable]
public class ClampedIntParameter : IntParameter
{
    public int min;
    public int max;

    public override int value
    {
        get => m_value;
        set => m_value = Mathf.Clamp(value, min, max);
    }

    public ClampedIntParameter(int value, int min, int max, bool overrideState = false)
        : base(value, overrideState)
    {
        this.min = min;
        this.max = max;
    }
}

[Serializable]
public class NoInterpClampedIntParameter : Parameter<int>
{
    public int min;

    public int max;
    public override int value
    {
        get => m_value;
        set => m_value = Mathf.Clamp(value, min, max);
    }

    public NoInterpClampedIntParameter(int value, int min, int max, bool overrideState = false)
        : base(value, overrideState)
    {
        this.min = min;
        this.max = max;
    }
}

#endregion

#region Float Parameter

[Serializable]
public class FloatParameter : Parameter<float>
{
    public FloatParameter(float value, bool overrideState = false)
        : base(value, overrideState) { }

    
    public sealed override void Interp(float from, float to, float t)
    {
        m_value = from + (to - from) * t;
    }

}
[Serializable]
public class MinFloatParameter : FloatParameter
{

    public float min;

    public override float value
    {
        get => m_value;
        set => m_value = Mathf.Max(value, min);
    }

    public MinFloatParameter(float value, float min, bool overrideState = false)
        : base(value, overrideState)
    {
        this.min = min;
    }
}
[Serializable]
public class MaxFloatParameter : FloatParameter
{

    public float max;

    public override float value
    {
        get => m_value;
        set => m_value = Mathf.Min(value, max);
    }

    public MaxFloatParameter(float value, float max, bool overrideState = false)
        : base(value, overrideState)
    {
        this.max = max;
    }
}
[Serializable]
public class NoInterpMinFloatParameter : Parameter<float>
{

    public float min;

    public override float value
    {
        get => m_value;
        set => m_value = Mathf.Max(value, min);
    }

    public NoInterpMinFloatParameter(float value, float min, bool overrideState = false)
        : base(value, overrideState)
    {
        this.min = min;
    }
}
[Serializable]
public class NoInterpMaxFloatParameter : Parameter<float>
{
    public float max;

    public override float value
    {
        get => m_value;
        set => m_value = Mathf.Min(value, max);
    }

    public NoInterpMaxFloatParameter(float value, float max, bool overrideState = false)
        : base(value, overrideState)
    {
        this.max = max;
    }
}


[Serializable]
public class ClampedFloatParameter : FloatParameter
{ 
    public float min;

    public float max;

    public override float value
    {
        get => m_value;
        set => m_value = Mathf.Clamp(value, min, max);
    }

    public ClampedFloatParameter(float value, float min, float max, bool overrideState = false)
        : base(value, overrideState)
    {
        this.min = min;
        this.max = max;
    }
}

[Serializable]
public class NoInterpClampedFloatParameter : Parameter<float>
{
    public float min;

    public float max;

    public override float value
    {
        get => m_value;
        set => m_value = Mathf.Clamp(value, min, max);
    }

    public NoInterpClampedFloatParameter(float value, float min, float max, bool overrideState = false)
        : base(value, overrideState)
    {
        this.min = min;
        this.max = max;
    }
}

#endregion 

[Serializable]
public class Vector2Parameter : Parameter<Vector2>
{
    public Vector2Parameter(Vector2 value, bool overrideState = false)
        : base(value, overrideState) { }

    public override void Interp(Vector2 from, Vector2 to, float t)
    {
        m_value.x = from.x + (to.x - from.x) * t;
        m_value.y = from.y + (to.y - from.y) * t;
    }
}
[Serializable]
public class Vector4Parameter : Parameter<Vector4>
{

    public Vector4Parameter(Vector4 value, bool overrideState = false)
        : base(value, overrideState) { }

    public override void Interp(Vector4 from, Vector4 to, float t)
    {
        m_value.x = from.x + (to.x - from.x) * t;
        m_value.y = from.y + (to.y - from.y) * t;
        m_value.z = from.z + (to.z - from.z) * t;
        m_value.w = from.w + (to.w - from.w) * t;
    }
}



[Serializable]
public class TextureParameter : Parameter<Texture>
{
    public TextureParameter(Texture value, bool overrideState = false)
        : base(value, overrideState) { }
}
[Serializable]
public class NoInterpTextureParameter : Parameter<Texture>//different?
{
    /// <summary>
    /// Creates a new <seealso cref="NoInterpTextureParameter"/> instance.
    /// </summary>
    /// <param name="value">The initial value to store in the parameter.</param>
    /// <param name="overrideState">The initial override state for the parameter.</param>
    public NoInterpTextureParameter(Texture value, bool overrideState = false)
        : base(value, overrideState) { }
}
[Serializable]
public class ObjectParameter<T> : Parameter<T>
{
    internal ReadOnlyCollection<Parameter> parameters { get; private set; }

    /// <summary>
    /// The current override state for this parameter. Note that this is always forced enabled
    /// on <see cref="ObjectParameter{T}"/>.
    /// </summary>
    public sealed override bool overrideState
    {
        get => true;
        set => m_overrideState = true;
    }

    /// <summary>
    /// The value stored by this parameter.
    /// </summary>
    public sealed override T value
    {
        get => m_value;
        set
        {
            m_value = value;

            if (m_value == null)
            {
                parameters = null;
                return;
            }

            // Automatically grab all fields of type VolumeParameter contained in this instance
            parameters = m_value.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(t => t.FieldType.IsSubclassOf(typeof(Parameter)))
                .OrderBy(t => t.MetadataToken) // Guaranteed order
                .Select(t => (Parameter)t.GetValue(m_value))
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Creates a new <seealso cref="ObjectParameter{T}"/> instance.
    /// </summary>
    /// <param name="value">The initial value to store in the parameter.</param>
    public ObjectParameter(T value)
    {
        m_overrideState = true;
        this.value = value;
    }

    internal override void Interp(Parameter from, Parameter to, float t)
    {
        if (m_value == null)
            return;

        var paramOrigin = parameters;
        var paramFrom = ((ObjectParameter<T>)from).parameters;
        var paramTo = ((ObjectParameter<T>)to).parameters;

        for (int i = 0; i < paramFrom.Count; i++)
        {
            // Keep track of the override state for debugging purpose
            paramOrigin[i].overrideState = paramTo[i].overrideState;

            if (paramTo[i].overrideState)
                paramOrigin[i].Interp(paramFrom[i], paramTo[i], t);
        }
    }
}