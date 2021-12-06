using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AmbientOcclusion(AmbientOcclusionType.GTAO), System.Serializable]
public class AOGTAO : AOComponent
{
    public ClampedIntParameter DirSampler = new ClampedIntParameter(2, 1, 8);

    public ClampedIntParameter SliceSampler = new ClampedIntParameter(4, 2, 16);

    public ClampedFloatParameter Intensity = new ClampedFloatParameter(0f, 0f, 2f);//

    public ClampedFloatParameter Radius = new ClampedFloatParameter(2.5f, 0.25f, 5f);//

    public BoolParameter MultiBounce = new BoolParameter(true);


    //public BoolParameter FullResolution = new BoolParameter(false);


    public ClampedFloatParameter SpatialBilateralAggressiveness = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);

    public ClampedFloatParameter GhostingReduction = new ClampedFloatParameter(0.5f, 0f, 1f);



    public override bool IsActive() => Intensity > 0f;

}
