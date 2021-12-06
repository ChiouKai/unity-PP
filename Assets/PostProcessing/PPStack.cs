using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PPStack 
{
    internal Dictionary<Type, PPVolumeComponent> components;
    internal void Reload(Type[] baseTypes)
    {
        if (components == null)
            components = new Dictionary<Type, PPVolumeComponent>();
        else
            components.Clear();

        foreach (var type in baseTypes)
        {
            var inst = (PPVolumeComponent)ScriptableObject.CreateInstance(type);
            components.Add(type, inst);
        }
    }
    public T GetComponent<T>()
    where T : PPVolumeComponent
    {
        var comp = GetComponent(typeof(T));
        return (T)comp;
    }
    public PPVolumeComponent GetComponent(Type type)
    {
        components.TryGetValue(type, out var comp);
        return comp;
    }

}
