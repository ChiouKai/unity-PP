using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
[AmbientOcclusion(AmbientOcclusionType.HBAO)]
public class HBAOpass : AOPass
{
    RenderTextureDescriptor m_sourceDescriptor;
    bool motionVectorsSupported;
    AOHBAO parameter;
    Shader shader;
    public override void Initialize(Camera Cam, PPResourceAndSetting Resource, RenderData Data, AOComponent Parameter)
    {
        if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
        {
            Debug.LogWarning("HBAO shader is not supported on this platform.");

            return;
        }
        data = Data;
        shader = Resource.shaders.HBAO;
        if (shader == null)
        {
            Debug.LogError("HBAO shader was not found...");
            return;
        }
        if (!shader.isSupported)
        {
            Debug.LogWarning("HBAO shader is not supported on this platform.");
            return;
        }
        if (material != null)
            Utils.Destroy(material);
        material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;

        parameter = (AOHBAO)Parameter;
        m_sourceDescriptor = new RenderTextureDescriptor(0, 0);

        m_Camera = Cam;
        m_Camera.forceIntoRenderTexture = true;

        // For platforms not supporting motion vectors texture
        // https://docs.unity3d.com/ScriptReference/DepthTextureMode.MotionVectors.html
        motionVectorsSupported = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf);

        colorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default;
        depthFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf;
        normalsFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010) ? RenderTextureFormat.ARGB2101010 : RenderTextureFormat.Default;

        enabled = true;
    }

    public override void Execute(CommandBuffer cmd)
    {
        if (enabled)
        {
            FetchRenderParameters();
            CheckParameters();
            UpdateMaterialProperties();
            UpdateShaderKeywords();


            ClearCommandBuffer(cmd);
            BuildCommandBuffer(cmd, m_PipelineStage);

            m_Camera.AddCommandBuffer(m_PipelineStage, cmd);

            ++frameCount;
        }
    }

    public override void UpdateAfterSet(CommandBuffer cmd)
    {
        if (enabled)
        {
#if UNITY_EDITOR //scene camera
            m_Camera.depthTextureMode |= DepthTextureMode.Depth;
            if (parameter.aoSetting.perPixelNormals == PerPixelNormals.Camera)
                m_Camera.depthTextureMode |= DepthTextureMode.DepthNormals;
            if (parameter.temporalFilterSettings.TFenabled)
                m_Camera.depthTextureMode |= DepthTextureMode.MotionVectors;
#endif
            material.SetMatrix(ShaderConstant.worldToCameraMatrix, m_Camera.worldToCameraMatrix);///
            material.SetVector(ShaderConstant.temporalParams, parameter.temporalFilterSettings.TFenabled && !renderingInSceneView ? new Vector2(s_temporalRotations[frameCount % 6] / 360.0f, s_temporalOffsets[frameCount % 4]) : Vector2.zero);
            ++frameCount;
        }
    }


    private static bool isLinearColorSpace { get { return QualitySettings.activeColorSpace == ColorSpace.Linear; } }
    private bool renderingInSceneView { get { return m_Camera.cameraType == CameraType.SceneView; } }

    private static RenderTextureFormat defaultHDRRenderTextureFormat
    {
        get
        {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH || UNITY_EDITOR
            RenderTextureFormat format = RenderTextureFormat.RGB111110Float;
#if UNITY_EDITOR
            var target = EditorUserBuildSettings.activeBuildTarget;
            if (target != BuildTarget.Android && target != BuildTarget.iOS && target != BuildTarget.tvOS && target != BuildTarget.Switch)
                return RenderTextureFormat.DefaultHDR;
#endif // UNITY_EDITOR
            if (SystemInfo.SupportsRenderTextureFormat(format))
                return format;
#endif // UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH || UNITY_EDITOR
            return RenderTextureFormat.DefaultHDR;
        }
    }
    private RenderTextureFormat sourceFormat { get { return data.allowHDR ? defaultHDRRenderTextureFormat : RenderTextureFormat.Default; } }
    private static RenderTextureFormat colorFormat;
    private static RenderTextureFormat depthFormat;
    private static RenderTextureFormat normalsFormat;

    int width;
    int height;
    int screenWidth;
    int screenHeight;
    bool stereoActive;


    int aoWidth;
    int aoHeight;
    int reinterleavedAoWidth;
    int reinterleavedAoHeight;
    int deinterleavedAoWidth;
    int deinterleavedAoHeight;
    uint frameCount;

    void FetchRenderParameters()
    {
        width = data.pixelWidth;
        height = data.pixelHeight;
        m_sourceDescriptor.width = width;
        m_sourceDescriptor.height = height;
        screenWidth = width;
        screenHeight = height;
        stereoActive = false;


        var downsamplingFactor = parameter.generalSetting.resolution == Resolution.Full ? 1 : parameter.generalSetting.deinterleaving == false ? 2 : 1;
        if (downsamplingFactor > 1)
        {
            aoWidth = (width + width % 2) / downsamplingFactor;
            aoHeight = (height + height % 2) / downsamplingFactor;
        }
        else
        {
            aoWidth = width;
            aoHeight = height;
        }

        reinterleavedAoWidth = width + (width % 4 == 0 ? 0 : 4 - (width % 4));
        reinterleavedAoHeight = height + (height % 4 == 0 ? 0 : 4 - (height % 4));
        deinterleavedAoWidth = reinterleavedAoWidth / 4;
        deinterleavedAoHeight = reinterleavedAoHeight / 4;
    }

    NoiseType? m_PreviousNoiseType;
    private void CheckParameters()
    {

        // Settings to force
        if (m_Camera.actualRenderingPath != RenderingPath.DeferredShading && parameter.aoSetting.perPixelNormals == PerPixelNormals.GBuffer)
            parameter.aoSetting.perPixelNormals.value = PerPixelNormals.Camera;

        if (parameter.generalSetting.deinterleaving != false && SystemInfo.supportedRenderTargetCount < 4)
            parameter.generalSetting.deinterleaving.value = false;

        if (parameter.generalSetting.pipelineStage != HBAOPipelineStage.BeforeImageEffectsOpaque && m_Camera.actualRenderingPath != RenderingPath.DeferredShading)
            parameter.generalSetting.pipelineStage.value = HBAOPipelineStage.BeforeImageEffectsOpaque;

        if (parameter.generalSetting.pipelineStage != HBAOPipelineStage.BeforeImageEffectsOpaque && parameter.aoSetting.perPixelNormals == PerPixelNormals.Camera)
            parameter.aoSetting.perPixelNormals.value = PerPixelNormals.GBuffer;

        if (parameter.temporalFilterSettings.TFenabled && !motionVectorsSupported)
            parameter.temporalFilterSettings.TFenabled.value = false;

        if (parameter.temporalFilterSettings.TFenabled && parameter.colorBleedingSetting.colorBleedingEnabled && SystemInfo.supportedRenderTargetCount < 2)
            parameter.temporalFilterSettings.TFenabled.value = false;

        // Camera textures
        m_Camera.depthTextureMode |= DepthTextureMode.Depth;
        if (parameter.aoSetting.perPixelNormals == PerPixelNormals.Camera)
            m_Camera.depthTextureMode |= DepthTextureMode.DepthNormals;
        if (parameter.temporalFilterSettings.TFenabled)
            m_Camera.depthTextureMode |= DepthTextureMode.MotionVectors;

        // Noise texture
        if (noiseTex == null || m_PreviousNoiseType != parameter.generalSetting.noiseType)
        {
            Utils.Destroy(noiseTex);

            CreateNoiseTexture();

            m_PreviousNoiseType = parameter.generalSetting.noiseType;
        }
    }

    private void CreateNoiseTexture()
    {
        noiseTex = new Texture2D(4, 4, SystemInfo.SupportsTextureFormat(TextureFormat.RGHalf) ? TextureFormat.RGHalf : TextureFormat.RGB24, false, true);
        noiseTex.filterMode = FilterMode.Point;
        noiseTex.wrapMode = TextureWrapMode.Repeat;
        int z = 0;
        for (int x = 0; x < 4; ++x)
        {
            for (int y = 0; y < 4; ++y)
            {
                float r1 = parameter.generalSetting.noiseType != NoiseType.Dither ? 0.25f * (0.0625f * ((x + y & 3) << 2) + (x & 3)) : mersenneTwister.Numbers[z++];
                float r2 = parameter.generalSetting.noiseType != NoiseType.Dither ? 0.25f * ((y - x) & 3) : mersenneTwister.Numbers[z++];
                Color color = new Color(r1, r2, 0);
                noiseTex.SetPixel(x, y, color);
            }
        }
        noiseTex.Apply();

        for (int i = 0, j = 0; i < s_jitter.Length; ++i)
        {
            float r1 = mersenneTwister.Numbers[j++];
            float r2 = mersenneTwister.Numbers[j++];
            s_jitter[i] = new Vector2(r1, r2);
        }
    }

    private void UpdateMaterialProperties()
    {
        float tanHalfFovY = Mathf.Tan(0.5f * data.fieldOfView * Mathf.Deg2Rad);
        float invFocalLenX = 1.0f / (1.0f / tanHalfFovY * (screenHeight / (float)screenWidth));
        float invFocalLenY = 1.0f / (1.0f / tanHalfFovY);
        float maxRadInPixels = Mathf.Max(16, parameter.aoSetting.maxRadiusPixels * Mathf.Sqrt((screenWidth * screenHeight) / (1080.0f * 1920.0f)));
        maxRadInPixels /= (parameter.generalSetting.deinterleaving == true ? 4 : 1);

        var targetScale = parameter.generalSetting.deinterleaving == true ?
                              new Vector4(reinterleavedAoWidth / (float)width, reinterleavedAoHeight / (float)height, 1.0f / (reinterleavedAoWidth / (float)width), 1.0f / (reinterleavedAoHeight / (float)height)) :
                              parameter.generalSetting.resolution == Resolution.Half /*&& parameter.aoSettings.perPixelNormals == PerPixelNormals.Reconstruct*/ ?
                                  new Vector4((width + 0.5f) / width, (height + 0.5f) / height, 1f, 1f) :
                                  Vector4.one;
        if (material == null)
            material = new Material(shader);

        material.SetTexture(ShaderConstant.noiseTex, noiseTex);
        material.SetVector(ShaderConstant.inputTexelSize, new Vector4(1f / width, 1f / height, width, height));
        material.SetVector(ShaderConstant.aoTexelSize, new Vector4(1f / aoWidth, 1f / aoHeight, aoWidth, aoHeight));
        material.SetVector(ShaderConstant.deinterleavedAOTexelSize, new Vector4(1.0f / deinterleavedAoWidth, 1.0f / deinterleavedAoHeight, deinterleavedAoWidth, deinterleavedAoHeight));
        material.SetVector(ShaderConstant.reinterleavedAOTexelSize, new Vector4(1f / reinterleavedAoWidth, 1f / reinterleavedAoHeight, reinterleavedAoWidth, reinterleavedAoHeight));
        material.SetVector(ShaderConstant.targetScale, targetScale);
        material.SetVector(ShaderConstant.uvToView, new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));

        material.SetMatrix(ShaderConstant.worldToCameraMatrix, m_Camera.worldToCameraMatrix);///

        material.SetFloat(ShaderConstant.radius, parameter.aoSetting.radius * 0.5f * ((screenHeight / (parameter.generalSetting.deinterleaving == true ? 4 : 1)) / (tanHalfFovY * 2.0f)));
        material.SetFloat(ShaderConstant.maxRadiusPixels, maxRadInPixels);
        material.SetFloat(ShaderConstant.negInvRadius2, -1.0f / (parameter.aoSetting.radius * parameter.aoSetting.radius));
        material.SetFloat(ShaderConstant.angleBias, parameter.aoSetting.bias);
        material.SetFloat(ShaderConstant.aoMultiplier, 2.0f * (1.0f / (1.0f - parameter.aoSetting.bias)));
        material.SetFloat(ShaderConstant.intensity, isLinearColorSpace ? parameter.aoSetting.intensity : parameter.aoSetting.intensity * 0.454545454545455f);
        material.SetColor(ShaderConstant.baseColor, parameter.aoSetting.baseColor);
        material.SetFloat(ShaderConstant.multiBounceInfluence, parameter.aoSetting.multiBounceInfluence);
        material.SetFloat(ShaderConstant.offscreenSamplesContrib, parameter.aoSetting.offscreenSamplesContribution);
        material.SetFloat(ShaderConstant.maxDistance, parameter.aoSetting.maxDistance);
        material.SetFloat(ShaderConstant.distanceFalloff, parameter.aoSetting.distanceFalloff);
        material.SetFloat(ShaderConstant.blurSharpness, parameter.blurSetting.sharpness);
        material.SetFloat(ShaderConstant.colorBleedSaturation, parameter.colorBleedingSetting.saturation);
        material.SetFloat(ShaderConstant.albedoMultiplier, parameter.colorBleedingSetting.albedoMultiplier);
        material.SetFloat(ShaderConstant.colorBleedBrightnessMask, parameter.colorBleedingSetting.brightnessMask);
        material.SetVector(ShaderConstant.colorBleedBrightnessMaskRange, Utils.AdjustBrightnessMaskToGammaSpace(new Vector2(Mathf.Pow(parameter.colorBleedingSetting.brightnessMaskRange - 0.5f, 3), Mathf.Pow(parameter.colorBleedingSetting.brightnessMaskRange, 3))));
        material.SetVector(ShaderConstant.temporalParams, parameter.temporalFilterSettings.TFenabled && !renderingInSceneView ? new Vector2(s_temporalRotations[frameCount % 6] / 360.0f, s_temporalOffsets[frameCount % 4]) : Vector2.zero);
    }

    private string[] m_ShaderKeywords;
    private void UpdateShaderKeywords()
    {

        if (m_ShaderKeywords == null || m_ShaderKeywords.Length != 13) m_ShaderKeywords = new string[13];

        m_ShaderKeywords[0] = GetOrthographicOrDeferredKeyword(data.orthographic, parameter);
        m_ShaderKeywords[1] = GetDirectionsKeyword(parameter);
        m_ShaderKeywords[2] = GetStepsKeyword(parameter);
        m_ShaderKeywords[3] = GetNoiseKeyword(parameter);
        m_ShaderKeywords[4] = GetDeinterleavingKeyword(parameter);
        m_ShaderKeywords[5] = GetDebugKeyword(parameter); //"__"; //ShaderConstant.GetDebugKeyword(parameter.generalSettings);
        m_ShaderKeywords[6] = GetMultibounceKeyword(parameter);
        m_ShaderKeywords[7] = GetOffscreenSamplesContributionKeyword(parameter);
        m_ShaderKeywords[8] = GetPerPixelNormalsKeyword(parameter);
        m_ShaderKeywords[9] = GetBlurRadiusKeyword(parameter);
        m_ShaderKeywords[10] = GetVarianceClippingKeyword(parameter);
        m_ShaderKeywords[11] = GetColorBleedingKeyword(parameter);
        m_ShaderKeywords[12] = GetLightingLogEncodedKeyword(data.allowHDR);

        material.shaderKeywords = m_ShaderKeywords;
    }

    public override void CleanUp(CommandBuffer cmd)
    {
        ClearCommandBuffer(cmd);

        ReleaseHistoryBuffers();

        Utils.Destroy(material);
        Utils.Destroy(noiseTex);
        Utils.Destroy(Utils.fullscreenTriangle);
    }
    void ClearCommandBuffer(CommandBuffer cmd)
    {
        if (cmd != null)
        {
            if (m_Camera != null)
            {
                cmd.Clear();
                m_Camera.RemoveCommandBuffer(m_PipelineStage, cmd);
            }
        }
        if (m_PipelineStage != cameraEvent)
        {
            m_PipelineStage = cameraEvent;
        }
    }
    void BuildCommandBuffer(CommandBuffer cmd, CameraEvent camEvent)
    {
        // AO
        cmd.BeginSample("HBAO");
        if (parameter.generalSetting.deinterleaving == false)
        {
            GetScreenSpaceTemporaryRT(cmd, ShaderConstant.hbaoTex, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
            AO(cmd);
        }
        else
        {
            GetScreenSpaceTemporaryRT(cmd, ShaderConstant.hbaoTex, widthOverride: reinterleavedAoWidth, heightOverride: reinterleavedAoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
            DeinterleavedAO(cmd);
        }

        // Blur
        Blur(cmd);

        // Temporal Filter
        TemporalFilter(cmd);

        // Composite
        Composite(cmd, camEvent);

        cmd.ReleaseTemporaryRT(ShaderConstant.hbaoTex);
        cmd.EndSample("HBAO");
        //Debug.Log("CommandBuffer has been rebuilt");
    }
    private void AO(CommandBuffer cmd)
    {
        Utils.BlitFullscreenTriangleWithClear(cmd, BuiltinRenderTextureType.CameraTarget, ShaderConstant.hbaoTex, material, new Color(0, 0, 0, 1), Pass.AO);
    }

    private void DeinterleavedAO(CommandBuffer cmd)
    {
        // Deinterleave depth & normals (4x4)
        for (int i = 0; i < 4; i++)
        {
            var rtsDepth = new RenderTargetIdentifier[] {
                ShaderConstant.depthSliceTex[(i << 2) + 0],
                ShaderConstant.depthSliceTex[(i << 2) + 1],
                ShaderConstant.depthSliceTex[(i << 2) + 2],
                ShaderConstant.depthSliceTex[(i << 2) + 3]
            };
            var rtsNormals = new RenderTargetIdentifier[] {
                ShaderConstant.normalsSliceTex[(i << 2) + 0],
                ShaderConstant.normalsSliceTex[(i << 2) + 1],
                ShaderConstant.normalsSliceTex[(i << 2) + 2],
                ShaderConstant.normalsSliceTex[(i << 2) + 3]
            };

            int offsetX = (i & 1) << 1; int offsetY = (i >> 1) << 1;
            cmd.SetGlobalVector(ShaderConstant.deinterleaveOffset[0], new Vector2(offsetX + 0, offsetY + 0));
            cmd.SetGlobalVector(ShaderConstant.deinterleaveOffset[1], new Vector2(offsetX + 1, offsetY + 0));
            cmd.SetGlobalVector(ShaderConstant.deinterleaveOffset[2], new Vector2(offsetX + 0, offsetY + 1));
            cmd.SetGlobalVector(ShaderConstant.deinterleaveOffset[3], new Vector2(offsetX + 1, offsetY + 1));
            for (int j = 0; j < 4; j++)
            {
                GetScreenSpaceTemporaryRT(cmd, ShaderConstant.depthSliceTex[j + 4 * i], widthOverride: deinterleavedAoWidth, heightOverride: deinterleavedAoHeight, colorFormat: depthFormat, readWrite: RenderTextureReadWrite.Linear, filter: FilterMode.Point);
                GetScreenSpaceTemporaryRT(cmd, ShaderConstant.normalsSliceTex[j + 4 * i], widthOverride: deinterleavedAoWidth, heightOverride: deinterleavedAoHeight, colorFormat: normalsFormat, readWrite: RenderTextureReadWrite.Linear, filter: FilterMode.Point);
            }
            Utils.BlitFullscreenTriangle(cmd, BuiltinRenderTextureType.CameraTarget, rtsDepth, material, Pass.Deinterleave_Depth); // outputs 4 render textures
            Utils.BlitFullscreenTriangle(cmd, BuiltinRenderTextureType.CameraTarget, rtsNormals, material, Pass.Deinterleave_Normals); // outputs 4 render textures
        }

        // AO on each layer
        for (int i = 0; i < 4 * 4; i++)
        {
            cmd.SetGlobalTexture(ShaderConstant.depthTex, ShaderConstant.depthSliceTex[i]);
            cmd.SetGlobalTexture(ShaderConstant.normalsTex, ShaderConstant.normalsSliceTex[i]);
            cmd.SetGlobalVector(ShaderConstant.jitter, s_jitter[i]);
            GetScreenSpaceTemporaryRT(cmd, ShaderConstant.aoSliceTex[i], widthOverride: deinterleavedAoWidth, heightOverride: deinterleavedAoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear, filter: FilterMode.Point);
            Utils.BlitFullscreenTriangleWithClear(cmd, BuiltinRenderTextureType.CameraTarget, ShaderConstant.aoSliceTex[i], material, new Color(0, 0, 0, 1), Pass.AO_Deinterleaved); // ao
            cmd.ReleaseTemporaryRT(ShaderConstant.depthSliceTex[i]);
            cmd.ReleaseTemporaryRT(ShaderConstant.normalsSliceTex[i]);
        }

        // Atlas Deinterleaved AO, 4x4
        GetScreenSpaceTemporaryRT(cmd, ShaderConstant.tempTex, widthOverride: reinterleavedAoWidth, heightOverride: reinterleavedAoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
        for (int i = 0; i < 4 * 4; i++)
        {
            cmd.SetGlobalVector(ShaderConstant.atlasOffset, new Vector2(((i & 1) + (((i & 7) >> 2) << 1)) * deinterleavedAoWidth, (((i & 3) >> 1) + ((i >> 3) << 1)) * deinterleavedAoHeight));
            Utils.BlitFullscreenTriangle(cmd, ShaderConstant.aoSliceTex[i], ShaderConstant.tempTex, material, Pass.Atlas_AO_Deinterleaved); // atlassing
            cmd.ReleaseTemporaryRT(ShaderConstant.aoSliceTex[i]);
        }

        // Reinterleave AO
        ApplyFlip(cmd);
        Utils.BlitFullscreenTriangle(cmd, ShaderConstant.tempTex, ShaderConstant.hbaoTex, material, Pass.Reinterleave_AO); // reinterleave
        cmd.ReleaseTemporaryRT(ShaderConstant.tempTex);
    }

    private void Blur(CommandBuffer cmd)
    {
        if (parameter.blurSetting.blurType != BlurType.None)
        {
            GetScreenSpaceTemporaryRT(cmd, ShaderConstant.tempTex, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
            cmd.SetGlobalVector(ShaderConstant.blurDeltaUV, new Vector2(1f / width, 0));
            Utils.BlitFullscreenTriangle(cmd, ShaderConstant.hbaoTex, ShaderConstant.tempTex, material, Pass.Blur); // blur X
            cmd.SetGlobalVector(ShaderConstant.blurDeltaUV, new Vector2(0, 1f / height));
            Utils.BlitFullscreenTriangle(cmd, ShaderConstant.tempTex, ShaderConstant.hbaoTex, material, Pass.Blur); // blur Y
            cmd.ReleaseTemporaryRT(ShaderConstant.tempTex);
        }
    }
    private void TemporalFilter(CommandBuffer cmd)
    {
        if (parameter.temporalFilterSettings.TFenabled && !renderingInSceneView)
        {
            AllocateHistoryBuffers();

            if (parameter.colorBleedingSetting.colorBleedingEnabled)
            {
                // For Color Bleeding we have 2 history buffers to fill so there are 2 render targets.
                // AO is still contained in Color Bleeding history buffer (alpha channel) so that we
                // can use it as a render texture for the composite pass.
                var rts = new RenderTargetIdentifier[] {
                    aoHistoryBuffer,
                    colorBleedingHistoryBuffer
                };
                GetScreenSpaceTemporaryRT(cmd, ShaderConstant.tempTex, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                GetScreenSpaceTemporaryRT(cmd, ShaderConstant.tempTex2, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                Utils.BlitFullscreenTriangle(cmd, aoHistoryBuffer, ShaderConstant.tempTex2, material, Pass.Copy);
                Utils.BlitFullscreenTriangle(cmd, colorBleedingHistoryBuffer, ShaderConstant.tempTex, material, Pass.Copy);
                Utils.BlitFullscreenTriangle(cmd, ShaderConstant.tempTex2, rts, material, Pass.Temporal_Filter);
                cmd.ReleaseTemporaryRT(ShaderConstant.tempTex);
                cmd.ReleaseTemporaryRT(ShaderConstant.tempTex2);
                cmd.SetGlobalTexture(ShaderConstant.hbaoTex, colorBleedingHistoryBuffer);
            }
            else
            {
                // AO history buffer contains ao in aplha channel so we can just use history as
                // a render texture for the composite pass.
                GetScreenSpaceTemporaryRT(cmd, ShaderConstant.tempTex, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                Utils.BlitFullscreenTriangle(cmd, aoHistoryBuffer, ShaderConstant.tempTex, material, Pass.Copy);
                Utils.BlitFullscreenTriangle(cmd, ShaderConstant.tempTex, aoHistoryBuffer, material, Pass.Temporal_Filter);
                cmd.ReleaseTemporaryRT(ShaderConstant.tempTex);
                cmd.SetGlobalTexture(ShaderConstant.hbaoTex, aoHistoryBuffer);
            }
        }
    }

    private void Composite(CommandBuffer cmd, CameraEvent cameraEvent)
    {

        if (cameraEvent == CameraEvent.BeforeReflections)
            CompositeBeforeReflections(cmd);
        else if (cameraEvent == CameraEvent.AfterLighting)
            CompositeAfterLighting(cmd);
        else // if (BeforeImageEffectsOpaque)
            CompositeBeforeImageEffectsOpaque(cmd);
    }
    private void CompositeBeforeReflections(CommandBuffer cmd)
    {
        var hdr = data.allowHDR;
        var rts = new RenderTargetIdentifier[] {
            BuiltinRenderTextureType.GBuffer0, // Albedo, Occ
            hdr ? BuiltinRenderTextureType.CameraTarget : BuiltinRenderTextureType.GBuffer3 // Ambient
        };
        GetScreenSpaceTemporaryRT(cmd, ShaderConstant.tempTex, colorFormat: RenderTextureFormat.ARGB32);
        GetScreenSpaceTemporaryRT(cmd, ShaderConstant.tempTex2, colorFormat: hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB2101010);
        Utils.BlitFullscreenTriangle(cmd, rts[0], ShaderConstant.tempTex, material, Pass.Copy);
        Utils.BlitFullscreenTriangle(cmd, rts[1], ShaderConstant.tempTex2, material, Pass.Copy);
        Utils.BlitFullscreenTriangle(cmd, ShaderConstant.tempTex2, rts, material, Pass.Composite_BeforeReflections);
        cmd.ReleaseTemporaryRT(ShaderConstant.tempTex);
        cmd.ReleaseTemporaryRT(ShaderConstant.tempTex2);
    }

    private void CompositeAfterLighting(CommandBuffer cmd)
    {
        var hdr = data.allowHDR;
        var rt3 = hdr ? BuiltinRenderTextureType.CameraTarget : BuiltinRenderTextureType.GBuffer3;
        GetScreenSpaceTemporaryRT(cmd, ShaderConstant.tempTex, colorFormat: hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB2101010);
        Utils.BlitFullscreenTriangle(cmd, rt3, ShaderConstant.tempTex, material, Pass.Copy);
        Utils.BlitFullscreenTriangle(cmd, ShaderConstant.tempTex, rt3, material, Pass.Composite_AfterLighting);
        cmd.ReleaseTemporaryRT(ShaderConstant.tempTex);
    }

    private void CompositeBeforeImageEffectsOpaque(CommandBuffer cmd, int finalPassId = Pass.Composite)
    {
        // This pass can be used to display all debug mode as well
        GetScreenSpaceTemporaryRT(cmd, ShaderConstant.tempTex, colorFormat: sourceFormat);
        Utils.BlitFullscreenTriangle(cmd, BuiltinRenderTextureType.CameraTarget, ShaderConstant.tempTex, material, Pass.Copy);
        ApplyFlip(cmd, SystemInfo.graphicsUVStartsAtTop);
        Utils.BlitFullscreenTriangle(cmd, ShaderConstant.tempTex, BuiltinRenderTextureType.CameraTarget, material, finalPassId);
        cmd.ReleaseTemporaryRT(ShaderConstant.tempTex);
    }

    private void AllocateHistoryBuffers()
    {
        ReleaseHistoryBuffers();

        aoHistoryBuffer = GetScreenSpaceRT(widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
        if (parameter.colorBleedingSetting.colorBleedingEnabled)
            colorBleedingHistoryBuffer = GetScreenSpaceRT(widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);

        // Clear history buffers to default
        var lastActive = RenderTexture.active;
        RenderTexture.active = aoHistoryBuffer;
        GL.Clear(false, true, Color.white);
        if (parameter.colorBleedingSetting.colorBleedingEnabled)
        {
            RenderTexture.active = colorBleedingHistoryBuffer;
            GL.Clear(false, true, new Color(0, 0, 0, 1));
        }
        RenderTexture.active = lastActive;

        frameCount = 0;
    }
    private void ReleaseHistoryBuffers()
    {
        if (aoHistoryBuffer != null)
            aoHistoryBuffer.Release();

        if (colorBleedingHistoryBuffer != null)
            colorBleedingHistoryBuffer.Release();
    }


    private RenderTexture aoHistoryBuffer;
    private RenderTexture colorBleedingHistoryBuffer;
    private Texture2D noiseTex;

    CameraEvent m_PipelineStage;
    private CameraEvent cameraEvent
    {
        get
        {
            switch (parameter.generalSetting.pipelineStage.value)
            {
                case HBAOPipelineStage.BeforeReflections:
                    return CameraEvent.BeforeReflections;
                case HBAOPipelineStage.AfterLighting:
                    return CameraEvent.AfterLighting;
                case HBAOPipelineStage.BeforeImageEffectsOpaque:
                default:
                    return CameraEvent.BeforeImageEffectsOpaque;
            }
        }
    }
    
    private static class mersenneTwister
    {
        // Mersenne-Twister random numbers in [0,1).
        public static float[] Numbers = new float[]
        {
            //0.463937f,0.340042f,0.223035f,0.468465f,0.322224f,0.979269f,0.031798f,0.973392f,0.778313f,0.456168f,0.258593f,0.330083f,0.387332f,0.380117f,0.179842f,0.910755f,
            //0.511623f,0.092933f,0.180794f,0.620153f,0.101348f,0.556342f,0.642479f,0.442008f,0.215115f,0.475218f,0.157357f,0.568868f,0.501241f,0.629229f,0.699218f,0.707733f
            0.556725f,0.005520f,0.708315f,0.583199f,0.236644f,0.992380f,0.981091f,0.119804f,0.510866f,0.560499f,0.961497f,0.557862f,0.539955f,0.332871f,0.417807f,0.920779f,
            0.730747f,0.076690f,0.008562f,0.660104f,0.428921f,0.511342f,0.587871f,0.906406f,0.437980f,0.620309f,0.062196f,0.119485f,0.235646f,0.795892f,0.044437f,0.617311f
        };
    }
    private static readonly Vector2[] s_jitter = new Vector2[4 * 4];
    private static readonly float[] s_temporalRotations = { 60.0f, 300.0f, 180.0f, 240.0f, 120.0f, 0.0f };
    private static readonly float[] s_temporalOffsets = { 0.0f, 0.5f, 0.25f, 0.75f };

    private static class Pass
    {
        public const int AO = 0;
        public const int AO_Deinterleaved = 1;

        public const int Deinterleave_Depth = 2;
        public const int Deinterleave_Normals = 3;
        public const int Atlas_AO_Deinterleaved = 4;
        public const int Reinterleave_AO = 5;

        public const int Blur = 6;

        public const int Temporal_Filter = 7;

        public const int Copy = 8;

        public const int Composite = 9;
        public const int Composite_AfterLighting = 10;
        public const int Composite_BeforeReflections = 11;

        public const int Debug_ViewNormals = 12;
    }

    public static string GetOrthographicOrDeferredKeyword(bool orthographic, AOHBAO settings)
    {
        // need to check that integrationStage is not BeforeImageEffectOpaque as Gbuffer0 is not available in this case
        return orthographic ? "ORTHOGRAPHIC_PROJECTION" : settings.generalSetting.pipelineStage != HBAOPipelineStage.BeforeImageEffectsOpaque ? "DEFERRED_SHADING" : "__";
    }

    public static string GetDirectionsKeyword(AOHBAO settings)
    {
        switch (settings.generalSetting.quality.value)
        {
            case HBAOQuality.Lowest:
                return "DIRECTIONS_3";
            case HBAOQuality.Low:
                return "DIRECTIONS_4";
            case HBAOQuality.Medium:
                return "DIRECTIONS_6";
            case HBAOQuality.High:
                return "DIRECTIONS_8";
            case HBAOQuality.Highest:
                return "DIRECTIONS_8";
            default:
                return "DIRECTIONS_6";
        }
    }

    public static string GetStepsKeyword(AOHBAO settings)
    {
        switch (settings.generalSetting.quality.value)
        {
            case HBAOQuality.Lowest:
                return "STEPS_2";
            case HBAOQuality.Low:
                return "STEPS_3";
            case HBAOQuality.Medium:
                return "STEPS_4";
            case HBAOQuality.High:
                return "STEPS_4";
            case HBAOQuality.Highest:
                return "STEPS_6";
            default:
                return "STEPS_4";
        }
    }

    public static string GetNoiseKeyword(AOHBAO settings)
    {
        switch (settings.generalSetting.noiseType.value)
        {
            case NoiseType.InterleavedGradientNoise:
                return "INTERLEAVED_GRADIENT_NOISE";
            case NoiseType.Dither:
            case NoiseType.SpatialDistribution:
            default:
                return "__";
        }
    }

    public static string GetDeinterleavingKeyword(AOHBAO settings)
    {
        return settings.generalSetting.deinterleaving ? "DEINTERLEAVED" : "__";
    }

    public static string GetDebugKeyword(AOHBAO settings)
    {
        switch (settings.generalSetting.debugMode.value)
        {
            case DebugMode.AOOnly:
                return "DEBUG_AO";
            case DebugMode.ColorBleedingOnly:
                return "DEBUG_COLORBLEEDING";
            case DebugMode.SplitWithoutAOAndWithAO:
                return "DEBUG_NOAO_AO";
            case DebugMode.SplitWithAOAndAOOnly:
                return "DEBUG_AO_AOONLY";
            case DebugMode.SplitWithoutAOAndAOOnly:
                return "DEBUG_NOAO_AOONLY";
            case DebugMode.Disabled:
            default:
                return "__";
        }
    }

    public static string GetMultibounceKeyword(AOHBAO settings)
    {
        return settings.aoSetting.useMultiBounce ? "MULTIBOUNCE" : "__";
    }

    public static string GetOffscreenSamplesContributionKeyword(AOHBAO settings)
    {
        return settings.aoSetting.offscreenSamplesContribution > 0 ? "OFFSCREEN_SAMPLES_CONTRIBUTION" : "__";
    }

    public static string GetPerPixelNormalsKeyword(AOHBAO settings)
    {
        switch (settings.aoSetting.perPixelNormals.value)
        {
            case PerPixelNormals.Camera:
                return "NORMALS_CAMERA";
            case PerPixelNormals.Reconstruct:
                return "NORMALS_RECONSTRUCT";
            case PerPixelNormals.GBuffer:
            default:
                return "__";
        }
    }

    public static string GetBlurRadiusKeyword(AOHBAO settings)
    {
        switch (settings.blurSetting.blurType.value)
        {
            case BlurType.Narrow:
                return "BLUR_RADIUS_2";
            case BlurType.Medium:
                return "BLUR_RADIUS_3";
            case BlurType.Wide:
                return "BLUR_RADIUS_4";
            case BlurType.ExtraWide:
                return "BLUR_RADIUS_5";
            case BlurType.None:
            default:
                return "BLUR_RADIUS_3";
        }
    }

    public static string GetVarianceClippingKeyword(AOHBAO settings)
    {
        switch (settings.temporalFilterSettings.varianceClipping.value)
        {
            case VarianceClipping._4Tap:
                return "VARIANCE_CLIPPING_4TAP";
            case VarianceClipping._8Tap:
                return "VARIANCE_CLIPPING_8TAP";
            case VarianceClipping.Disabled:
            default:
                return "__";
        }
    }

    public static string GetColorBleedingKeyword(AOHBAO settings)
    {
        return settings.colorBleedingSetting.colorBleedingEnabled ? "COLOR_BLEEDING" : "__";
    }

    public static string GetLightingLogEncodedKeyword(bool hdr)
    {
        return hdr ? "__" : "LIGHTING_LOG_ENCODED";
    }


    private RenderTextureDescriptor GetDefaultDescriptor(int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default)
    {
        var modifiedDesc = new RenderTextureDescriptor(m_sourceDescriptor.width, m_sourceDescriptor.height,
                                                       m_sourceDescriptor.colorFormat, depthBufferBits);
        modifiedDesc.dimension = m_sourceDescriptor.dimension;
        modifiedDesc.volumeDepth = m_sourceDescriptor.volumeDepth;
        modifiedDesc.vrUsage = m_sourceDescriptor.vrUsage;
        modifiedDesc.msaaSamples = m_sourceDescriptor.msaaSamples;
        modifiedDesc.memoryless = m_sourceDescriptor.memoryless;

        modifiedDesc.useMipMap = m_sourceDescriptor.useMipMap;
        modifiedDesc.autoGenerateMips = m_sourceDescriptor.autoGenerateMips;
        modifiedDesc.enableRandomWrite = m_sourceDescriptor.enableRandomWrite;
        modifiedDesc.shadowSamplingMode = m_sourceDescriptor.shadowSamplingMode;

        if (data.allowDynamicResolution)
            modifiedDesc.useDynamicScale = true;

        if (colorFormat != RenderTextureFormat.Default)
            modifiedDesc.colorFormat = colorFormat;

        if (readWrite == RenderTextureReadWrite.sRGB)
            modifiedDesc.sRGB = true;
        else if (readWrite == RenderTextureReadWrite.Linear)
            modifiedDesc.sRGB = false;
        else if (readWrite == RenderTextureReadWrite.Default)
            modifiedDesc.sRGB = isLinearColorSpace;

        return modifiedDesc;
    }
    private RenderTexture GetScreenSpaceRT(int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
                                       FilterMode filter = FilterMode.Bilinear, int widthOverride = 0, int heightOverride = 0)
    {
        var desc = GetDefaultDescriptor(depthBufferBits, colorFormat, readWrite);
        if (widthOverride > 0)
            desc.width = widthOverride;
        if (heightOverride > 0)
            desc.height = heightOverride;

        //intermediates in VR are unchanged
        if (stereoActive && desc.dimension == TextureDimension.Tex2DArray)
            desc.dimension = TextureDimension.Tex2D;

        var rt = new RenderTexture(desc);
        rt.filterMode = filter;
        return rt;
    }
    private void GetScreenSpaceTemporaryRT(CommandBuffer cmd, int nameID,
                                       int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
                                       FilterMode filter = FilterMode.Bilinear, int widthOverride = 0, int heightOverride = 0)
    {
        var desc = GetDefaultDescriptor(depthBufferBits, colorFormat, readWrite);
        if (widthOverride > 0)
            desc.width = widthOverride;
        if (heightOverride > 0)
            desc.height = heightOverride;

        //intermediates in VR are unchanged
        if (stereoActive && desc.dimension == TextureDimension.Tex2DArray)
            desc.dimension = TextureDimension.Tex2D;

        cmd.GetTemporaryRT(nameID, desc, filter);
    }

    private static void ApplyFlip(CommandBuffer cmd, bool flip = true)
    {
        if (flip)
            cmd.SetGlobalVector(ShaderConstant.uvTransform, new Vector4(1f, -1f, 0f, 1f));
        else
            cmd.SetGlobalVector(ShaderConstant.uvTransform, new Vector4(1f, 1f, 0f, 0f));
    }
}