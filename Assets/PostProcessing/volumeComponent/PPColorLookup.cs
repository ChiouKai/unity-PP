using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[Serializable]
public class PPColorLookup : PPVolumeComponent
{
    [Tooltip("A custom 2D texture lookup table to apply.")]
    public TextureParameter texture = new TextureParameter(null);

    [Tooltip("How much of the lookup texture will contribute to the color grading effect.")]
    public ClampedFloatParameter contribution = new ClampedFloatParameter(1f, 0f, 1f);

    public override bool isActive() => contribution.value > 0f && ValidateLUT();

    public bool ValidateLUT()
    {
        if (texture.value == null)
            return false;
        var layer = PPCamera.layer;
        if (layer != null)
        {
            int lutSize = layer.resource.setting.colorGradingLutSize;
            if (texture.value.height != lutSize || texture.value.width != lutSize * lutSize)
                return false;
        }
        bool valid = false;

        switch (texture.value)
        {
            case Texture2D t:
                valid |=!GraphicsFormatUtility.IsSRGBFormat(t.graphicsFormat);
                break;
            case RenderTexture rt:
                valid |= rt.dimension == TextureDimension.Tex2D
                      && !rt.sRGB;
                break;
        }

        return valid;
    }
}
