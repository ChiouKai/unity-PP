using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
[AmbientOcclusion(AmbientOcclusionType.None)]
public class AOPass
{
    protected Material material;
    protected PPResourceAndSetting resource;
    protected RenderData data;
    protected Camera m_Camera;

    public bool enabled = false;
    public virtual void Initialize(Camera Cam, PPResourceAndSetting Resource, RenderData Data, AOComponent Parameter)
    {

    }
    public virtual void Execute(CommandBuffer cmd)
    {

    }
    public virtual void UpdateAfterSet(CommandBuffer cmd)
    {

    }
    public virtual void CleanUp(CommandBuffer cmd)
    {

    }

    public static Dictionary<AmbientOcclusionType, AOPass> GetAllAOpass()
    {
        var dic = new Dictionary<AmbientOcclusionType, AOPass>();

        var editorTypes = Utils.GetAllTypesDerivedFrom<AOPass>().Where(t => t.IsDefined(typeof(AmbientOcclusionAttribute), false));
        dic[AmbientOcclusionType.None] = new AOPass();
        foreach (var editorType in editorTypes)
        {
            var attribute = (AmbientOcclusionAttribute)editorType.GetCustomAttributes(typeof(AmbientOcclusionAttribute), false)[0];
            dic[attribute.type] = (AOPass)Activator.CreateInstance(editorType);
        }
        return dic;
    }
}
