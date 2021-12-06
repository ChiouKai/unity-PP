using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class PPColorGradingLut
{
    Material m_LutBuilderLdr;
    Material m_LutBuilderHdr;
    GraphicsFormat m_HdrLutFormat;
    GraphicsFormat m_LdrLutFormat;
    RenderData data;
    RenderTexture LutTexture;
    PPResourceAndSetting resource;
    PPStack stack;
    public PPColorGradingLut(PPResourceAndSetting resource, RenderData data, PPStack stack)
    {
        this.resource = resource;
        this.data = data;
        this.stack = stack;

        m_LutBuilderLdr = Utils.CreateMaterial(resource.shaders.lutBuilderLdr);
        m_LutBuilderHdr = Utils.CreateMaterial(resource.shaders.lutBuilderHdr);

        const FormatUsage kFlags = FormatUsage.Linear | FormatUsage.Render;
        if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, kFlags))
            m_HdrLutFormat = GraphicsFormat.R16G16B16A16_SFloat;
        else if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, kFlags))
            m_HdrLutFormat = GraphicsFormat.B10G11R11_UFloatPack32;
        else
            // Obviously using this for log lut encoding is a very bad idea for precision but we
            // need it for compatibility reasons and avoid black screens on platforms that don't
            // support floating point formats. Expect banding and posterization artifact if this
            // ends up being used.
            m_HdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;

        m_LdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;
    }

    public RenderTexture Execute(CommandBuffer CBuffer)
    {
        var channelMixer = stack.GetComponent<PPChannelMixer>();
        var colorAdjustments = stack.GetComponent<PPColorAdjustments>();
        var curves = stack.GetComponent<PPColorCurves>();
        var liftGammaGain = stack.GetComponent<PPLiftGammaGain>();
        var shadowMidtonesHightlight = stack.GetComponent<PPShadowsMidtonesHighlights>();
        var splitToning = stack.GetComponent<PPSplitToning>();
        var tonemapping = stack.GetComponent<PPTonemapping>();
        var whiteBalance = stack.GetComponent<PPWhiteBalance>();

        bool HDR = data.allowHDR && data.supportHDR && resource.setting.m_ColorGradingMode == ColorGradingMode.HighDynamicRange;
        int lutHeight = resource.setting.colorGradingLutSize;
        int lutWidth = lutHeight * lutHeight;

        var format = HDR ? m_HdrLutFormat : m_LdrLutFormat;
        var material = HDR ? m_LutBuilderHdr : m_LutBuilderLdr;
        var desc = new RenderTextureDescriptor(lutWidth, lutHeight, format, 0)
        {
            vrUsage = VRTextureUsage.None
        };

        if (LutTexture != null)
            LutTexture.Release();
        LutTexture = new RenderTexture(desc);
        var lmsColorBalance = ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint);
        var hueSatCon = new Vector4(colorAdjustments.hueShift / 360f, colorAdjustments.saturation / 100f + 1f
            , colorAdjustments.contrast / 100f + 1f, 0f);

        var channelMixerR = new Vector4(channelMixer.redOutRedIn / 100f, channelMixer.redOutGreenIn / 100f, channelMixer.redOutBlueIn / 100f, 0f);
        var channelMixerG = new Vector4(channelMixer.greenOutRedIn / 100f, channelMixer.greenOutGreenIn / 100f, channelMixer.greenOutBlueIn / 100f, 0f);
        var channelMixerB = new Vector4(channelMixer.blueOutRedIn / 100f, channelMixer.blueOutGreenIn / 100f, channelMixer.blueOutBlueIn / 100f, 0f);

        var shadowHighlightsLimits = new Vector4(shadowMidtonesHightlight.shadowsStart, shadowMidtonesHightlight.shadowsEnd
            , shadowMidtonesHightlight.highlightsStart, shadowMidtonesHightlight.highlightsEnd);

        var (shadows, midetone, highlights) = PrepareShadowsMidtonesHighlights(
            shadowMidtonesHightlight.shadows, shadowMidtonesHightlight.midtones, shadowMidtonesHightlight.highlights);

        var (lift, gamma, gain) = PrepareLiftGammaGain(liftGammaGain.lift, liftGammaGain.gamma, liftGammaGain.gain);

        var (splitShadows, splitHighlights) = PrepareSplitToning(splitToning.shadows.value, splitToning.highlights.value, splitToning.balance.value);

        var lutParameters = new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f));

        material.SetVector(ShaderConstant._Lut_Params, lutParameters);
        material.SetVector(ShaderConstant._ColorBalance, lmsColorBalance);
        material.SetVector(ShaderConstant._ColorFilter, colorAdjustments.colorFilter.value.linear);
        material.SetVector(ShaderConstant._ChannelMixerRed, channelMixerR);
        material.SetVector(ShaderConstant._ChannelMixerGreen, channelMixerG);
        material.SetVector(ShaderConstant._ChannelMixerBlue, channelMixerB);
        material.SetVector(ShaderConstant._HueSatCon, hueSatCon);
        material.SetVector(ShaderConstant._Lift, lift);
        material.SetVector(ShaderConstant._Gamma, gamma);
        material.SetVector(ShaderConstant._Gain, gain);
        material.SetVector(ShaderConstant._Shadows, shadows);
        material.SetVector(ShaderConstant._Midtones, midetone);
        material.SetVector(ShaderConstant._Highlights, highlights);
        material.SetVector(ShaderConstant._ShaHiLimits, shadowHighlightsLimits);
        material.SetVector(ShaderConstant._SplitShadows, splitShadows);
        material.SetVector(ShaderConstant._SplitHighlights, splitHighlights);

        //YRGB curves
        material.SetTexture(ShaderConstant._CurveMaster, curves.master.value.GetTexture());
        material.SetTexture(ShaderConstant._CurveRed, curves.red.value.GetTexture());
        material.SetTexture(ShaderConstant._CurveGreen, curves.green.value.GetTexture());
        material.SetTexture(ShaderConstant._CurveBlue, curves.blue.value.GetTexture());

        // Secondary curves
        material.SetTexture(ShaderConstant._CurveHueVsHue, curves.hueVsHue.value.GetTexture());
        material.SetTexture(ShaderConstant._CurveHueVsSat, curves.hueVsSat.value.GetTexture());
        material.SetTexture(ShaderConstant._CurveLumVsSat, curves.lumVsSat.value.GetTexture());
        material.SetTexture(ShaderConstant._CurveSatVsSat, curves.satVsSat.value.GetTexture());

        // Tonemapping (baked into the lut for HDR)
        if (HDR)
        {
            material.shaderKeywords = null;

            switch (tonemapping.mode.value)
            {
                case TonemappingMode.Neutral: material.EnableKeyword(ShaderConstant.TonemapNeutral); break;
                case TonemappingMode.ACES: material.EnableKeyword(ShaderConstant.TonemapACES); break;
                default: break; // None
            }
        }

        CBuffer.Blit(LutTexture, LutTexture, material);
        Graphics.ExecuteCommandBuffer(CBuffer);
        CBuffer.Clear();
        return LutTexture;
    }

    public void CleanUp()
    {
        if (LutTexture != null)
            LutTexture.Release();
        Utils.Destroy(m_LutBuilderLdr);
        Utils.Destroy(m_LutBuilderHdr);
    }



    /// <summary>
    /// Converts white balancing parameter to LMS coefficients.
    /// </summary>
    /// <param name="temperature">A temperature offset, in range [-100;100].</param>
    /// <param name="tint">A tint offset, in range [-100;100].</param>
    /// <returns>LMS coefficients.</returns>
    public Vector3 ColorBalanceToLMSCoeffs(float temperature, float tint)
    {
        // Range ~[-1.5;1.5] works best
        float t1 = temperature / 65f;
        float t2 = tint / 65f;

        // Get the CIE xy chromaticity of the reference white point.
        // Note: 0.31271 = x value on the D65 white point
        float x = 0.31271f - t1 * (t1 < 0f ? 0.1f : 0.05f);
        float y = StandardIlluminantY(x) + t2 * 0.05f;

        // Calculate the coefficients in the LMS space.
        var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
        var w2 = CIExyToLMS(x, y);
        return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
    }
    /// <summary>
    /// An analytical model of chromaticity of the standard illuminant, by Judd et al.
    /// http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
    /// Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public float StandardIlluminantY(float x) => 2.87f * x - 3f * x * x - 0.27509507f;

    /// <summary>
    /// CIE xy chromaticity to CAT02 LMS.
    /// http://en.wikipedia.org/wiki/LMS_color_space#CAT02
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public Vector3 CIExyToLMS(float x, float y)
    {
        float Y = 1f;
        float X = Y * x / y;
        float Z = Y * (1f - x - y) / y;

        float L = 0.7328f * X + 0.4296f * Y - 0.1624f * Z;
        float M = -0.7036f * X + 1.6975f * Y + 0.0061f * Z;
        float S = 0.0030f * X + 0.0136f * Y + 0.9834f * Z;

        return new Vector3(L, M, S);
    }

    /// <summary>
    /// Pre-filters shadows, midtones and highlights trackball values for shader use.
    /// </summary>
    /// <param name="inShadows">A color used for shadows.</param>
    /// <param name="inMidtones">A color used for midtones.</param>
    /// <param name="inHighlights">A color used for highlights.</param>
    /// <returns>The three input colors pre-filtered for shader use.</returns>
    public (Vector4, Vector4, Vector4) PrepareShadowsMidtonesHighlights(in Vector4 inShadows, in Vector4 inMidtones, in Vector4 inHighlights)
    {
        float weight;

        var shadows = inShadows;
        shadows.x = Mathf.GammaToLinearSpace(shadows.x);
        shadows.y = Mathf.GammaToLinearSpace(shadows.y);
        shadows.z = Mathf.GammaToLinearSpace(shadows.z);
        weight = shadows.w * (Mathf.Sign(shadows.w) < 0f ? 1f : 4f);
        shadows.x = Mathf.Max(shadows.x + weight, 0f);
        shadows.y = Mathf.Max(shadows.y + weight, 0f);
        shadows.z = Mathf.Max(shadows.z + weight, 0f);
        shadows.w = 0f;

        var midtones = inMidtones;
        midtones.x = Mathf.GammaToLinearSpace(midtones.x);
        midtones.y = Mathf.GammaToLinearSpace(midtones.y);
        midtones.z = Mathf.GammaToLinearSpace(midtones.z);
        weight = midtones.w * (Mathf.Sign(midtones.w) < 0f ? 1f : 4f);
        midtones.x = Mathf.Max(midtones.x + weight, 0f);
        midtones.y = Mathf.Max(midtones.y + weight, 0f);
        midtones.z = Mathf.Max(midtones.z + weight, 0f);
        midtones.w = 0f;

        var highlights = inHighlights;
        highlights.x = Mathf.GammaToLinearSpace(highlights.x);
        highlights.y = Mathf.GammaToLinearSpace(highlights.y);
        highlights.z = Mathf.GammaToLinearSpace(highlights.z);
        weight = highlights.w * (Mathf.Sign(highlights.w) < 0f ? 1f : 4f);
        highlights.x = Mathf.Max(highlights.x + weight, 0f);
        highlights.y = Mathf.Max(highlights.y + weight, 0f);
        highlights.z = Mathf.Max(highlights.z + weight, 0f);
        highlights.w = 0f;

        return (shadows, midtones, highlights);
    }

    /// <summary>
    /// Pre-filters lift, gamma and gain trackball values for shader use.
    /// </summary>
    /// <param name="inLift">A color used for lift.</param>
    /// <param name="inGamma">A color used for gamma.</param>
    /// <param name="inGain">A color used for gain.</param>
    /// <returns>The three input colors pre-filtered for shader use.</returns>
    public (Vector4, Vector4, Vector4) PrepareLiftGammaGain(in Vector4 inLift, in Vector4 inGamma, in Vector4 inGain)
    {
        var lift = inLift;
        lift.x = Mathf.GammaToLinearSpace(lift.x) * 0.15f;
        lift.y = Mathf.GammaToLinearSpace(lift.y) * 0.15f;
        lift.z = Mathf.GammaToLinearSpace(lift.z) * 0.15f;

        float lumLift = Luminance(lift);
        lift.x = lift.x - lumLift + lift.w;
        lift.y = lift.y - lumLift + lift.w;
        lift.z = lift.z - lumLift + lift.w;
        lift.w = 0f;

        var gamma = inGamma;
        gamma.x = Mathf.GammaToLinearSpace(gamma.x) * 0.8f;
        gamma.y = Mathf.GammaToLinearSpace(gamma.y) * 0.8f;
        gamma.z = Mathf.GammaToLinearSpace(gamma.z) * 0.8f;

        float lumGamma = Luminance(gamma);
        gamma.w += 1f;
        gamma.x = 1f / Mathf.Max(gamma.x - lumGamma + gamma.w, 1e-03f);
        gamma.y = 1f / Mathf.Max(gamma.y - lumGamma + gamma.w, 1e-03f);
        gamma.z = 1f / Mathf.Max(gamma.z - lumGamma + gamma.w, 1e-03f);
        gamma.w = 0f;

        var gain = inGain;
        gain.x = Mathf.GammaToLinearSpace(gain.x) * 0.8f;
        gain.y = Mathf.GammaToLinearSpace(gain.y) * 0.8f;
        gain.z = Mathf.GammaToLinearSpace(gain.z) * 0.8f;

        float lumGain = Luminance(gain);
        gain.w += 1f;
        gain.x = gain.x - lumGain + gain.w;
        gain.y = gain.y - lumGain + gain.w;
        gain.z = gain.z - lumGain + gain.w;
        gain.w = 0f;

        return (lift, gamma, gain);
    }
    public float Luminance(in Color color) => color.r * 0.2126729f + color.g * 0.7151522f + color.b * 0.072175f;

    /// <summary>
    /// Pre-filters colors used for the split toning effect.
    /// </summary>
    /// <param name="inShadows">A color used for shadows.</param>
    /// <param name="inHighlights">A color used for highlights.</param>
    /// <param name="balance">The balance between the shadow and highlight colors, in range [-100;100].</param>
    /// <returns>The two input colors pre-filtered for shader use.</returns>
    public (Vector4, Vector4) PrepareSplitToning(in Vector4 inShadows, in Vector4 inHighlights, float balance)
    {
        // As counter-intuitive as it is, to make split-toning work the same way it does in
        // Adobe products we have to do all the maths in sRGB... So do not convert these to
        // linear before sending them to the shader, this isn't a bug!
        var shadows = inShadows;
        var highlights = inHighlights;

        // Balance is stored in `shadows.w`
        shadows.w = balance / 100f;
        highlights.w = 0f;

        return (shadows, highlights);
    }
}
