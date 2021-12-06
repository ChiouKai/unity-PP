using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PPVignette : PPVolumeComponent
{
    [Tooltip("Vignette color.")]
    public ColorParameter color = new ColorParameter(Color.black, false, false, true);

    [Tooltip("Sets the vignette center point (screen center is [0.5,0.5]).")]
    public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

    [Tooltip("Amount of vignetting on screen.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Smoothness of the vignette borders.")]
    public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);

    [Tooltip("Should the vignette be perfectly round or be dependent on the current aspect ratio?")]
    public BoolParameter rounded = new BoolParameter(false);

    public override bool isActive() => intensity.value > 0f;

}
