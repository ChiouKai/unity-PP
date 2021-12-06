using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PPFog : PPVolumeComponent
{
    public BoolParameter EnableFog = new BoolParameter(false);
    public FogModeParameter Mode = new FogModeParameter(FogMode.Linear);
    public ClampedFloatParameter Density = new ClampedFloatParameter(0.0f, 0.0f, 5.0f);
    public FloatParameter Start = new FloatParameter(0.0f);
    public FloatParameter End = new FloatParameter(200.0f);
    public ColorParameter FogColor = new ColorParameter(Color.white);
    public ClampedFloatParameter IncludeSkybox = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
    

    public override bool isActive()
    {
        return EnableFog;
    }
}
[Serializable]
public class FogModeParameter : Parameter<FogMode>
{
    public FogModeParameter(FogMode value, bool overrideState = false)
    : base(value, overrideState) { }
}