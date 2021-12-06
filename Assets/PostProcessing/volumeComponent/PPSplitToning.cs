using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PPSplitToning : PPVolumeComponent
{
    [Tooltip("The color to use for shadows.")]
    public ColorParameter shadows = new ColorParameter(Color.grey, false, false, true);

    [Tooltip("The color to use for highlights.")]
    public ColorParameter highlights = new ColorParameter(Color.grey, false, false, true);

    [Tooltip("Balance between the colors in the highlights and shadows.")]
    public ClampedFloatParameter balance = new ClampedFloatParameter(0f, -100f, 100f);

    public override bool isLut => true;
    public override bool isActive() => shadows != Color.grey || highlights != Color.grey;

}
