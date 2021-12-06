using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PPBloom : PPVolumeComponent
{
    public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0f);

    public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);

    public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

    public MinFloatParameter clamp = new MinFloatParameter(65472f, 0f);

    public ColorParameter tint = new ColorParameter(Color.white, false, false, true);

    public BoolParameter highQualityFiltering = new BoolParameter(false);

    public ClampedIntParameter skipIterations = new ClampedIntParameter(1, 0, 16);

    public TextureParameter dirtTexture = new TextureParameter(null);

    public MinFloatParameter dirtIntensity = new MinFloatParameter(0f, 0f);

    public override bool isActive() => intensity.value > 0f;

}
