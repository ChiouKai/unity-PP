using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PPLensDistortion : PPVolumeComponent
{
    [Tooltip("Total distortion amount.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, -1f, 1f);

    [Tooltip("Intensity multiplier on X axis. Set it to 0 to disable distortion on this axis.")]
    public ClampedFloatParameter xMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

    [Tooltip("Intensity multiplier on Y axis. Set it to 0 to disable distortion on this axis.")]
    public ClampedFloatParameter yMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

    [Tooltip("Distortion center point.")]
    public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

    [Tooltip("Global screen scaling.")]
    public ClampedFloatParameter scale = new ClampedFloatParameter(1f, 0.01f, 5f);

    public override bool isActive()
    {
        return Mathf.Abs(intensity.value) > 0
            && (xMultiplier.value > 0f || yMultiplier.value > 0f);
    }
}
