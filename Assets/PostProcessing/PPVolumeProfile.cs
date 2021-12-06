using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PPVolumeProfile : ScriptableObject
{
    public List<PPVolumeComponent> components = new List<PPVolumeComponent>();

    internal int lutCount;
    public bool isDirty =true;
    private void OnEnable()
    {
        lutCount = 0;
        components.RemoveAll(x => x == null);
        foreach (var component in components)
        {
            if(component.isLut)
                ++lutCount;
        }
    }

    public bool Has(Type type)
    {
        foreach (var component in components)
        {
            if (component.GetType() == type)
                return true;
        }
        return false;
    }


    public void ChangeComponent()
    {
        OnEnable();
        PPManager.instance.ChangeComponent(this, lutCount > 0);
    }

    public int GetComponentListHashCode()
    {
        unchecked
        {
            int hash = 17;

            for (int i = 0; i < components.Count; i++)
                hash = hash * 23 + components[i].GetType().GetHashCode();

            return hash;
        }
    }
    public void Reset()
    {
        isDirty = true;
    }
}
