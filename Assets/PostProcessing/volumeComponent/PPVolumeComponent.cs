using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PPVolumeComponentMenu : Attribute
{
    public readonly string menu;

    public PPVolumeComponentMenu(string menu)
    {
        this.menu = menu;
    }
}
[Serializable]
public class PPVolumeComponent : ScriptableObject
{
    public bool active = true;
    public bool PreActive;
    //[SerializeField]
    //bool m_AdvancedMode = false; // Editor-only
    public ReadOnlyCollection<Parameter> parameters { get; private set; }

    private void OnEnable()
    {
        parameters = this.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(t => t.FieldType.IsSubclassOf(typeof(Parameter)))
            .OrderBy(t => t.MetadataToken) // Guaranteed order
            .Select(t => (Parameter)t.GetValue(this))
            .ToList()
            .AsReadOnly();
        PreActive = active;
    }
    public virtual bool isActive()
    {
        return false;
    }
    public virtual bool isLut => false;

    public virtual void Override(PPVolumeComponent state, float interpFactor)
    {
        int count = parameters.Count;

        for (int i = 0; i < count; i++)
        {
            var stateParam = state.parameters[i];
            var toParam = parameters[i];

            if (toParam.overrideState)
            {
                // Keep track of the override state for debugging purpose
                stateParam.overrideState = toParam.overrideState;
                stateParam.Interp(stateParam, toParam, interpFactor);
            }
        }
    }

    public override int GetHashCode()
    {
        unchecked
        {
            //return parameters.Aggregate(17, (i, p) => i * 23 + p.GetHash());

            int hash = 17;

            for (int i = 0; i < parameters.Count; i++)
                hash = hash * 23 + parameters[i].GetHashCode();

            return hash;
        }
    }

    protected virtual void OnDestroy() => Release();

    /// <summary>
    /// Releases all the allocated resources.
    /// </summary>
    public void Release()
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i] != null)
                parameters[i].Release();
        }
    }
    public void SetAllOverridesTo(bool state)
    {
        SetAllOverridesTo(parameters, state);
    }
    void SetAllOverridesTo(IEnumerable<Parameter> enumerable, bool state)
    {
        foreach (var prop in enumerable)
        {
            prop.overrideState = state;
            var t = prop.GetType();

            if (Parameter.IsObjectParameter(t))
            {
                // This method won't be called a lot but this is sub-optimal, fix me
                var innerParams = (ReadOnlyCollection<Parameter>)
                    t.GetProperty("parameters", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(prop, null);

                if (innerParams != null)
                    SetAllOverridesTo(innerParams, state);
            }
        }
    }
}
