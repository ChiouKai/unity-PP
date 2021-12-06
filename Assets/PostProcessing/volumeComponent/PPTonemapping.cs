using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PPTonemapping : PPVolumeComponent
{
    [Tooltip("Select a tonemapping algorithm to use for the color grading process.")]
    public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None);

    public override bool isLut => true;
    public override bool isActive() => mode.value != TonemappingMode.None;

}
public enum TonemappingMode
{
    None,
    Neutral, // Neutral tonemapper
    ACES,    // ACES Filmic reference tonemapper (custom approximation)
}
[Serializable]
public sealed class TonemappingModeParameter : Parameter<TonemappingMode> { public TonemappingModeParameter(TonemappingMode value, bool overrideState = false) : base(value, overrideState) { } }


