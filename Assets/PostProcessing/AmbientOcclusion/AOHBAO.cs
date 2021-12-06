using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[AmbientOcclusion(AmbientOcclusionType.HBAO), Serializable]
public class AOHBAO : AOComponent
{
    //general setting
    [Serializable]
    public class GeneralSetting
    {
        public HBAOPipelineStageParameter pipelineStage = new HBAOPipelineStageParameter(HBAOPipelineStage.BeforeImageEffectsOpaque);
        public HBAOQualityParameter quality = new HBAOQualityParameter(HBAOQuality.Medium);
        public BoolParameter deinterleaving = new BoolParameter(false);
        public ResolutionParameter resolution = new ResolutionParameter(Resolution.Full);
        public NoiseTypeParameter noiseType = new NoiseTypeParameter(NoiseType.Dither);
        public DebugModeParameter debugMode = new DebugModeParameter(DebugMode.Disabled);
    }
    //AO setting
    [Serializable]
    public class AOSetting
    {
        public ClampedFloatParameter radius = new ClampedFloatParameter(1f, 0.25f, 5f);
        public ClampedFloatParameter maxRadiusPixels = new ClampedFloatParameter(32f, 16f, 256f);
        public ClampedFloatParameter bias = new ClampedFloatParameter(0f, 0f, 0.5f);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);
        public BoolParameter useMultiBounce = new BoolParameter(false);
        public ClampedFloatParameter multiBounceInfluence = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter offscreenSamplesContribution = new ClampedFloatParameter(0f, 0f, 1f);
        public MinFloatParameter maxDistance = new MinFloatParameter(150f, 0f);
        public MinFloatParameter distanceFalloff = new MinFloatParameter(50f, 0f);
        public PerPixelNormalsParameter perPixelNormals = new PerPixelNormalsParameter(PerPixelNormals.Camera);
        public ColorParameter baseColor = new ColorParameter(Color.black);
    }
    //TemporalFilterSettings
    [Serializable]
    public class TemporalFilterSettings
    {
        public BoolParameter TFenabled = new BoolParameter(false);
        public VarianceClippingParameter varianceClipping = new VarianceClippingParameter(VarianceClipping._4Tap);
    }
    //blur setting
    [Serializable]
    public class BlurSetting
    {
        public BlurTypeParameter blurType = new BlurTypeParameter(BlurType.Medium);
        public ClampedFloatParameter sharpness = new ClampedFloatParameter(8f, 0f, 16f);
    }
    //ColorBleedingSettings
    [Serializable]
    public class ColorBleedingSetting
    {
        public BoolParameter colorBleedingEnabled = new BoolParameter(false);
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1f, 0f, 4f);
        public ClampedFloatParameter albedoMultiplier = new ClampedFloatParameter(4f, 0f, 32f);
        public ClampedFloatParameter brightnessMask = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter brightnessMaskRange = new ClampedFloatParameter(0.5f, 0.5f, 2.0f);
    }
    public GeneralSetting generalSetting = new GeneralSetting();
    public AOSetting aoSetting = new AOSetting();
    public TemporalFilterSettings temporalFilterSettings = new TemporalFilterSettings();
    public BlurSetting blurSetting = new BlurSetting();
    public ColorBleedingSetting colorBleedingSetting = new ColorBleedingSetting();

    public override bool IsActive()
    {
        return aoSetting.intensity > 0f;
    }
}
public enum HBAOPipelineStage
{
    BeforeImageEffectsOpaque,
    AfterLighting,
    BeforeReflections
}
[Serializable]
public class HBAOPipelineStageParameter : Parameter<HBAOPipelineStage>
{
    public HBAOPipelineStageParameter(HBAOPipelineStage value, bool overrideState = false)
    : base(value, overrideState) { }
}

public enum HBAOQuality
{
    Lowest,
    Low,
    Medium,
    High,
    Highest
}
[Serializable]
public class HBAOQualityParameter : Parameter<HBAOQuality>
{
    public HBAOQualityParameter(HBAOQuality value, bool overrideState = false)
    : base(value, overrideState) { }
}
public enum Resolution
{
    Full,
    Half
}
[Serializable]
public class ResolutionParameter : Parameter<Resolution>
{
    public ResolutionParameter(Resolution value, bool overrideState = false)
    : base(value, overrideState) { }
}
public enum NoiseType
{
    Dither,
    InterleavedGradientNoise,
    SpatialDistribution
}
[Serializable]
public class NoiseTypeParameter : Parameter<NoiseType>
{
    public NoiseTypeParameter(NoiseType value, bool overrideState = false)
    : base(value, overrideState) { }
}
//public enum Deinterleaving
//{
//    Disabled,
//    x4
//}
public enum BlurType
{
    None,
    Narrow,
    Medium,
    Wide,
    ExtraWide
}
[Serializable]
public class BlurTypeParameter : Parameter<BlurType>
{
    public BlurTypeParameter(BlurType value, bool overrideState = false)
    : base(value, overrideState) { }
}
public enum PerPixelNormals
{
    GBuffer,
    Camera,
    Reconstruct
}
[Serializable]
public class PerPixelNormalsParameter : Parameter<PerPixelNormals>
{
    public PerPixelNormalsParameter(PerPixelNormals value, bool overrideState = false)
    : base(value, overrideState) { }
}
public enum VarianceClipping
{
    Disabled,
    _4Tap,
    _8Tap
}
[Serializable]
public class VarianceClippingParameter : Parameter<VarianceClipping>
{
    public VarianceClippingParameter(VarianceClipping value, bool overrideState = false)
    : base(value, overrideState) { }
}
public enum DebugMode
{
    Disabled,
    AOOnly,
    ColorBleedingOnly,
    SplitWithoutAOAndWithAO,
    SplitWithAOAndAOOnly,
    SplitWithoutAOAndAOOnly,
    ViewNormals
}
[Serializable]
public class DebugModeParameter : Parameter<DebugMode>
{
    public DebugModeParameter(DebugMode value, bool overrideState = false)
    : base(value, overrideState) { }
}
