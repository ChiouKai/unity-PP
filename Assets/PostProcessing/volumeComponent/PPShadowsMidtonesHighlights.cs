using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PPShadowsMidtonesHighlights : PPVolumeComponent
{
    [Tooltip("Controls the darkest portions of the render.")]
    public Vector4Parameter shadows = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

    [Tooltip("Power function that controls mid-range tones.")]
    public Vector4Parameter midtones = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

    [Tooltip("Controls the lightest portions of the render.")]
    public Vector4Parameter highlights = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

    [Tooltip("Start point of the transition between shadows and midtones.")]
    public MinFloatParameter shadowsStart = new MinFloatParameter(0f, 0f);

    [Tooltip("End point of the transition between shadows and midtones.")]
    public MinFloatParameter shadowsEnd = new MinFloatParameter(0.3f, 0f);

    [Tooltip("Start point of the transition between midtones and highlights.")]
    public MinFloatParameter highlightsStart = new MinFloatParameter(0.55f, 0f);

    [Tooltip("End point of the transition between midtones and highlights.")]
    public MinFloatParameter highlightsEnd = new MinFloatParameter(1f, 0f);

    public override bool isLut => true;
    public override bool isActive()
    {
        var defaultState = new Vector4(1f, 1f, 1f, 0f);
        return shadows != defaultState
            || midtones != defaultState
            || highlights != defaultState;
    }
}
