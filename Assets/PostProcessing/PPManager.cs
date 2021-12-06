using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class PPManager
{
    static readonly Lazy<PPManager> s_Instance = new Lazy<PPManager>(() => new PPManager());
    public static PPManager instance => s_Instance.Value;

    readonly List<PPVolumeComponent> DefaultState;
    public Type[] baseComponentTypeArray { get; private set; }

    Dictionary<PPCamera, int> DicPPCamera;

    readonly Dictionary<int, List<PPVolume>> SortedVolumes;
    readonly List<PPVolume> m_Volumes;
    readonly List<Collider> m_TempColliders;

    IEnumerable<Type> m_AssemblyTypes;
    public PPManager()
    {
        DefaultState = new List<PPVolumeComponent>();
        SortedVolumes = new Dictionary<int, List<PPVolume>>();
        m_Volumes = new List<PPVolume>();
        m_TempColliders = new List<Collider>(8);
        DicPPCamera = new Dictionary<PPCamera, int>();
        ReloadBaseType();
    }

    void ReloadBaseType()
    {
        DefaultState.Clear();
        baseComponentTypeArray = Utils.GetAllTypesDerivedFrom<PPVolumeComponent>().Where(t => !t.IsAbstract).ToArray();
        foreach (var type in baseComponentTypeArray)
        {
            var inst = (PPVolumeComponent)ScriptableObject.CreateInstance(type);
            DefaultState.Add(inst);
        }
    }
    public PPStack CreateStack()
    {
        var stack = new PPStack();
        stack.Reload(baseComponentTypeArray);
        return stack;
    }
    public void CheckStack(PPStack stack)
    {
        var components = stack.components;
        if (components == null)
        {
            stack.Reload(baseComponentTypeArray);
            return;
        }
        foreach(var component in components)
        {
            if (component.Key == null || component.Value == null)
            {
                stack.Reload(baseComponentTypeArray);
                return;
            }
        }
    }

    void ReplaceData(PPStack stack, List<PPVolumeComponent> components)
    {
        foreach (var component in components)
        {
            var target = stack.GetComponent(component.GetType());
            int count = component.parameters.Count;

            for (int i = 0; i < count; i++)
            {
                if (target.parameters[i] != null)
                {
                    target.parameters[i].overrideState = false;
                    target.parameters[i].SetValue(component.parameters[i]);
                }
            }
        }
    }
    //ppcamera Register need unregister
    public void CameraRegister(PPCamera PPCam/*, Transform trigger*/, LayerMask layermask)
    {
        var stack = PPCam.stack;
        Dictionary<PPVolume, float> volumeWeight = new Dictionary<PPVolume, float>();

        if (DefaultState == null || DefaultState.Count > 0 && DefaultState[0] == null)
            ReloadBaseType();

        CheckStack(stack);
        ReplaceData(stack, DefaultState);

        DicPPCamera[PPCam] = layermask;

        //bool onlyGlobal = trigger == null;

        //var volumes = GrabVolume(layermask);
        //if (volumes != null)
        //{
        //    foreach (var volume in volumes)
        //    {
        //        if (!volume.enabled || volume.profile == null || volume.weight <= 0f)
        //        {
        //            volumeWeight[volume] = 0f;
        //            continue;
        //        }
        //        if (volume.isGlobal)
        //        {
        //            volumeWeight[volume] = volume.weight;
        //            OverrideData(stack, volume.profile.components, volume.weight);
        //            continue;
        //        }
        //        if (onlyGlobal)
        //        {
        //            volumeWeight[volume] = 0f;
        //            continue;
        //        }
        //        float interpFactor = CalBlendDistance(volume, trigger);

        //        volumeWeight[volume] = interpFactor;
        //        if (interpFactor != 0f)
        //            OverrideData(stack, volume.profile.components, interpFactor);
        //    }
        //}
        PPCam.volumeWeight = volumeWeight;
    }

    bool ChenkVolumeWeight(Dictionary<PPVolume, float> volumeWeight , PPVolume volume, float weight, out bool lutChange)
    {
        lutChange = false;
        if (volumeWeight.TryGetValue(volume, out float tmp))
        {
            if (tmp == weight)
            {
                return false;
            }
        }
        volumeWeight[volume] = weight;
        if (volume.profile != null && volume.profile.lutCount > 0)
        {
            lutChange = true;
        }
        return true;
    }


    public bool CalVolumeWeight(LayerMask layermask, Transform trigger, Dictionary<PPVolume, float> volumeWeight,out bool lutChange)
    {      
        bool change = false, lutTmpChange;
        lutChange = false;
        var volumes = GrabVolume(layermask);
        if (volumes != null)
        {
            bool onlyGlobal = trigger == null;
            foreach (var volume in volumes)
            {
                if (!volume.enabled || volume.profile == null || volume.weight <= 0f)
                {
                    change |= ChenkVolumeWeight(volumeWeight, volume, 0f, out lutTmpChange);
                    lutChange |= lutTmpChange;
                    continue;
                }

                if (volume.isGlobal)
                {
                    change |= ChenkVolumeWeight(volumeWeight, volume, volume.weight, out lutTmpChange);
                    lutChange |= lutTmpChange;
                    continue;
                }
                if (onlyGlobal)
                {
                    change |= ChenkVolumeWeight(volumeWeight, volume, 0f, out lutTmpChange);
                    lutChange |= lutTmpChange;
                    continue;
                }

                change |= ChenkVolumeWeight(volumeWeight, volume, CalBlendDistance(volume, trigger), out lutTmpChange);
                lutChange |= lutTmpChange;
            }
        }
        else
            volumeWeight.Clear();
        return change;
    }

    float CalBlendDistance(PPVolume volume, Transform trigger)
    {
        var colliders = m_TempColliders;
        volume.GetComponents(colliders);
        if (colliders.Count == 0)
        {
            return 0f;
        }

        float closestDistanceSqr = float.PositiveInfinity;
        var triggerPos = trigger.position;
        foreach (var collider in colliders)
        {
            if (!collider.enabled)
                continue;
            var closestPoint = collider.ClosestPoint(triggerPos);
            var distance = (closestPoint - triggerPos).sqrMagnitude;
            if (distance < closestDistanceSqr)
                closestDistanceSqr = distance;
        }
        colliders.Clear();

        float blendDisSqr = volume.blendDistance * volume.blendDistance;
        if (closestDistanceSqr > blendDisSqr)
        {
            return 0f;
        }

        float interpFactor = (blendDisSqr > 0f ? 1f - (closestDistanceSqr / blendDisSqr) : 1f) * volume.weight;

        return interpFactor;
    }

    public void UpdateStack(PPCamera Cam ,Dictionary<PPVolume, float> volumeWeight, LayerMask layer)
    {
        if (DefaultState == null || DefaultState.Count > 0 && DefaultState[0] == null)
            ReloadBaseType();
        var stack = Cam.stack;
        CheckStack(stack);
        ReplaceData(stack, DefaultState);
        if (DicPPCamera[Cam] != layer)
        {
            DicPPCamera[Cam] = layer;
        }
        
        var volumes = GrabVolume(layer);
        if (volumes != null)
        {
            foreach (var volume in volumes)
            {
                if (volumeWeight[volume] != 0)
                    OverrideData(stack, volume.profile.components, volumeWeight[volume]);
            }
        }
    }


    public void CameraUnregister(PPCamera PPCam)
    {
        DicPPCamera.Remove(PPCam);
        if (PPCam.volumeWeight != null)
            PPCam.volumeWeight.Clear();
    }


    List<PPVolume> GrabVolume(LayerMask mask)
    {
        if (!SortedVolumes.TryGetValue(mask.value, out List<PPVolume> list))
        {
            foreach (var sorVol in SortedVolumes)
            {
                if (Mathf.NextPowerOfTwo(sorVol.Key) == sorVol.Key && (sorVol.Key & mask) != 0)
                {
                    if(list == null)
                        list = new List<PPVolume>();
                    list = SortTwoList(list, sorVol.Value);
                }
            }
            if (list != null)
                SortedVolumes[mask] = list;
        }
        return list;
    }

    List<PPVolume> SortTwoList(List<PPVolume> A, List<PPVolume> B)
    {
        if (A.Count == 0)
        {
            A.AddRange(B);
        }
        else
        {
            int tmp = 0;
            for (int i = 0; i < B.Count; ++i)
            {
                InsertVolume(tmp, A, B[i]);
            }
        }
        return A;
    }

    void OverrideData(PPStack stack, List<PPVolumeComponent> components, float interpFactor)
    {
        foreach (var component in components)
        {
            if (!component.active)
                continue;

            var state = stack.GetComponent(component.GetType());
            component.Override(state, interpFactor);
        }
    }

    public void Register(PPVolume volume, int layer)
    {
        m_Volumes.Add(volume);

        foreach(var sorVol in SortedVolumes)
        {
            if((sorVol.Key & layer) != 0)
            {
                InsertVolume(0, sorVol.Value, volume);
            }
        }
        if (!SortedVolumes.TryGetValue(layer, out List<PPVolume> list))
        {
            list = new List<PPVolume>();
            list.Add(volume);
            SortedVolumes.Add(layer, list);
        }
        else if (!list.Contains(volume))
        {
            InsertVolume(0, list, volume);
        }
        VolumeChange(volume, layer, volume.profile != null && volume.profile.lutCount > 0);
    }

    int InsertVolume(int tmp, List<PPVolume> list, PPVolume volume)
    {
        if (list.Contains(volume))
            return tmp;
        while (tmp < list.Count && list[tmp].priority < volume.priority)
        {
            ++tmp;
        }
        list.Insert(tmp, volume);

        ++tmp;
        return tmp;
    }


    public void Unregister(PPVolume volume, int layer)
    {
        m_Volumes.Remove(volume);

        SortedVolumes.TryGetValue(layer, out var list);
        list.Remove(volume);
        if (list.Count == 0)
            SortedVolumes.Remove(layer);
        foreach (var dic in DicPPCamera)
        {
            if ((dic.Value & layer) != 0)
                dic.Key.volumeWeight.Remove(volume);
        }
        VolumeChange(volume, layer, volume.profile != null && volume.profile.lutCount > 0);
    }

    internal void ResortByPriority(PPVolume volume, int layer)
    {
        foreach(var sorVol in SortedVolumes)
        {
            if((sorVol.Key & layer) != 0)
            {
                sorVol.Value.Remove(volume);
                InsertVolume(0, sorVol.Value, volume);
            }
        }
        VolumeChange(volume, layer, volume.profile != null && volume.profile.lutCount > 0);
    }

    internal void UpdateVolumeLayer(PPVolume volume, int prevLayer, int newLayer)
    {
        Unregister(volume, prevLayer);
        Register(volume, newLayer);
    }

    public void VolumeChange(PPVolume volume,int layer, bool hasLut)
    {

        foreach (var dic in DicPPCamera)
        {
            if ((dic.Value & layer) != 0)
            {
                dic.Key.changed = true;
                if (hasLut)
                    dic.Key.lutPassChange = true;
            }
        }
    }
    public void ChangeComponent(PPVolumeProfile profile, bool islut)
    {
        for(int i = 0; i < m_Volumes.Count; ++i) 
        { 
            if (m_Volumes[i].profile == profile)
            {
                foreach (var dic in DicPPCamera)
                {
                    if ((dic.Value & m_Volumes[i].m_PreviousLayer) != 0)
                    {
                        dic.Key.changed = true;
                        if (islut)
                            dic.Key.lutPassChange = true;
                    }
                }
            }
        }
    }
}

