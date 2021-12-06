using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PPDepthOfField : PPVolumeComponent
{
    [SerializeField]
    public DepthOfFieldModeParameter mode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off);

    public NoInterpMinFloatParameter gaussianFarEnd = new NoInterpMinFloatParameter(30f, 0f);

    public NoInterpMinFloatParameter gaussianFarStart = new NoInterpMinFloatParameter(10f, 0f);
    
    public NoInterpMinFloatParameter gaussianNearStart = new NoInterpMinFloatParameter(5f, 0f);

    public NoInterpMinFloatParameter gaussianNearEnd = new NoInterpMinFloatParameter(0f, -100f);

    public ClampedFloatParameter gaussianMaxRadius = new ClampedFloatParameter(1f, 0.5f, 2f);

    public BoolParameter highQualitySampling = new BoolParameter(false);

    public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);

    public ClampedFloatParameter aperture = new ClampedFloatParameter(5.6f, 1f, 32f);

    public ClampedFloatParameter focalLength = new ClampedFloatParameter(50f, 1f, 300f);

    public ClampedIntParameter bladeCount = new ClampedIntParameter(5, 3, 9);

    public ClampedFloatParameter bladeCurvature = new ClampedFloatParameter(1f, 0f, 1f);

    public ClampedFloatParameter bladeRotation = new ClampedFloatParameter(0f, -180f, 180f);

    public override bool isActive()
    {
        if (mode.value == DepthOfFieldMode.Off || SystemInfo.graphicsShaderLevel < 35)
            return false;

        return mode.value != DepthOfFieldMode.Gaussian || SystemInfo.supportedRenderTargetCount > 1;
    }

}
[Serializable]
public sealed class DepthOfFieldModeParameter : Parameter<DepthOfFieldMode>
{
    public DepthOfFieldModeParameter(DepthOfFieldMode value, bool overrideState = false)
        : base(value, overrideState) { }

}
[Serializable]
public enum DepthOfFieldMode
{
    Off,
    Gaussian, // Non physical, fast, small radius, far blur only
    Bokeh
}
