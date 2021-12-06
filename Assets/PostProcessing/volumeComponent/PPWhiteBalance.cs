using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PPWhiteBalance : PPVolumeComponent
{
    [Tooltip("Sets the white balance to a custom?? color temperature.")]
    public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100, 100f);

    [Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
    public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100, 100f);

    public override bool isLut => true;
    public override bool isActive() => temperature.value != 0f || tint.value != 0f;
}
