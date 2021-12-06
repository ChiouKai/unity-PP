using UnityEngine;

public class PPPaniniProjection : PPVolumeComponent
{
    [SerializeField]
    public ClampedFloatParameter distance = new ClampedFloatParameter(0f, 0f, 1f);
    [SerializeField]
    public ClampedFloatParameter cropToFit = new ClampedFloatParameter(1f, 0f, 1f);

    public override bool isActive() => distance.value > 0f;

}
