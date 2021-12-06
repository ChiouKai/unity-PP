using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PPChromaticAberration : PPVolumeComponent
{
    [Tooltip("Amount of tangential distortion.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

    public override bool isActive() => intensity.value > 0f;

}
