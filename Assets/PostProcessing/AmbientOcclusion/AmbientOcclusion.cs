using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
[Serializable]
public class AmbientOcclusion : ScriptableObject
{
    public AOComponent AO;
    public AmbientOcclusionType aoType;
    public List<int> AODicKey = new List<int>();
    public List<AOComponent> AODicValue = new List<AOComponent>();

    public bool isDirty;

 
    public AOComponent FindAO(int type)
    {
        if (AODicKey.Contains(type))
            return AODicValue[AODicKey.IndexOf(type)];
        return null;

    }

    public void ChangeType()
    {
        AO = FindAO((int)aoType);
    }

}
public enum AmbientOcclusionType
{
    None,
    HBAO,
    GTAO
}
public class AmbientOcclusionAttribute : Attribute
{
    public readonly AmbientOcclusionType type;

    public AmbientOcclusionAttribute(AmbientOcclusionType type)
    {
        this.type = type;
    }
}

