using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
[AmbientOcclusion(AmbientOcclusionType.GTAO)]
public class GTAOpass : AOPass
{
    AOGTAO parameter;
    public override void Initialize(Camera Cam, PPResourceAndSetting Resource, RenderData Data, AOComponent Parameter)
    {
        parameter = (AOGTAO)Parameter;
        data = Data;
        m_Camera = Cam;
        RTIS = new RenderTargetIdentifier[2] { _GTAO_Texture_ID, _BentNormal_Texture_ID };

        material = Utils.CreateMaterial(Resource.shaders.GTAO);
        if (material == null)
            return;

        if (m_Camera.actualRenderingPath != RenderingPath.DeferredShading)
        {
            Debug.LogError("GTAO only work on Deferred Rendering");
            return;
        }
        enabled = true;
    }




    int Width;
    int Height;


    public override void Execute(CommandBuffer cmd)
    {
        if (enabled)
        {
            Width = data.pixelWidth;
            Height = data.pixelHeight;

            if (cmd != null)
            {
                m_Camera.depthTextureMode |= DepthTextureMode.Depth;
                m_Camera.depthTextureMode |= DepthTextureMode.MotionVectors;
                ClearCommandBuffer(cmd);
                UpdateVariable();
                RenderSSAO(cmd);
                m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, cmd);
            }
        }
    }

    void ClearCommandBuffer(CommandBuffer cmd)
    {
        if (cmd != null)
        {
            if (m_Camera != null)
            {
                cmd.Clear();
                m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, cmd);
            }
        }
    }





    //private OutPass AODeBug = OutPass.Combien;
    RenderTargetIdentifier[] RTIS;
    //private Matrix4x4 LastFrameViewProjectionMatrix;
    //private Matrix4x4 View_ProjectionMatrix;
    private Vector2 CameraSize;
    private RenderTexture Prev_RT;

    private void UpdateVariable()
    {
        material.shaderKeywords = null;
        if (parameter.MultiBounce)
            material.EnableKeyword("_AO_MultiBounce");
        //----------------------------------------------------------------------------------
        material.SetVector(_AOParams0_ID, new Vector4(parameter.Intensity, parameter.Radius, parameter.DirSampler, parameter.SliceSampler));

        //----------------------------------------------------------------------------------
        float fovRad = data.fieldOfView * Mathf.Deg2Rad;
        float invHalfTanFov = 1 / Mathf.Tan(fovRad * 0.5f);
        Vector2 invFocalLen = new Vector2(1 / (invHalfTanFov * ((float)Height / (float)Width)), 1 / invHalfTanFov);

        material.SetVector(_AO_UVToView_ID, new Vector4(2 * invFocalLen.x, 2 * invFocalLen.y, -1 * invFocalLen.x, -1 * invFocalLen.y));
        //----------------------------------------------------------------------------------
        float projScale = (float)Height / (Mathf.Tan(fovRad * 0.5f) * 2) * 0.5f;

        float upperNudgeFactor = 1.0f - parameter.GhostingReduction;
        const float maxUpperNudgeLimit = 5.0f;
        const float minUpperNudgeLimit = 0.25f;
        upperNudgeFactor = minUpperNudgeLimit + (upperNudgeFactor * (maxUpperNudgeLimit - minUpperNudgeLimit));

        material.SetVector(_AOParams1_ID, new Vector4(projScale, upperNudgeFactor, minUpperNudgeLimit, parameter.SpatialBilateralAggressiveness));

        //----------------------------------------------------------------------------------
        var oneOverSize_Size = new Vector4(1 / (float)Width, 1 / (float)Height, (float)Width, (float)Height);
        material.SetVector(_AO_RT_TexelSize_ID, oneOverSize_Size);

        UpdateAfterSet(null);

        //----------------------------------------------------------------------------------
        //if (AO_BentNormal_RT[0] != null)
        //{
        //    AO_BentNormal_RT[0].Release();
        //}
        //if (AO_BentNormal_RT[1] != null)
        //{
        //    AO_BentNormal_RT[1].Release();
        //}
        //AO_BentNormal_RT[0] = new RenderTexture((int)RenderResolution.x, (int)RenderResolution.y, 0, RenderTextureFormat.RGHalf);
        //AO_BentNormal_RT[1] = new RenderTexture((int)RenderResolution.x, (int)RenderResolution.y, 0, RenderTextureFormat.ARGBHalf);
        //AO_BentNormal_ID[0] = AO_BentNormal_RT[0].colorBuffer;
        //AO_BentNormal_ID[1] = AO_BentNormal_RT[1].colorBuffer;
        //----------------------------------------------------------------------------------
        var currentCameraSize = new Vector2(Width, Height);
        if (Prev_RT ==null || CameraSize != currentCameraSize)
        {
            CameraSize = currentCameraSize;

            //----------------------------------------------------------------------------------
            if (Prev_RT != null)
            {
                Prev_RT.Release();
            }
            Prev_RT = new RenderTexture(Width, Height, 0, RenderTextureFormat.RGHalf);
            Prev_RT.filterMode = FilterMode.Point;

        }
    }

    RenderTextureDescriptor GetDescriptor(RenderTextureFormat format)
    {
        return new RenderTextureDescriptor(Width, Height, format);
    }


    private void RenderSSAO(CommandBuffer cmd)
    {
        cmd.BeginSample("GTAO");

        cmd.GetTemporaryRT(_AO_Scene_Color_ID, GetDescriptor(RenderTextureFormat.DefaultHDR), FilterMode.Point);
        cmd.Blit(BuiltinRenderTextureType.CameraTarget, _AO_Scene_Color_ID);
        //////Resolve GTAO
        cmd.GetTemporaryRT(_GTAO_Texture_ID, GetDescriptor(RenderTextureFormat.RGHalf));
        cmd.SetGlobalTexture(_GTAO_Texture_ID, _GTAO_Texture_ID);
        cmd.GetTemporaryRT(_BentNormal_Texture_ID, GetDescriptor(RenderTextureFormat.ARGBHalf));
        cmd.SetGlobalTexture(_BentNormal_Texture_ID, _BentNormal_Texture_ID);
        BlitMRT(cmd, RTIS, _GTAO_Texture_ID, material, 0);

        //////Spatial filter
        cmd.GetTemporaryRT(_GTAO_Spatial_Texture_ID, GetDescriptor(RenderTextureFormat.RGHalf), FilterMode.Point);
        BlitSRT(cmd ,_GTAO_Spatial_Texture_ID, material, 1);

        //////Temporal filter
        cmd.SetGlobalTexture(_PrevRT_ID, Prev_RT);
        cmd.GetTemporaryRT(_CurrRT_ID, GetDescriptor(RenderTextureFormat.RGHalf), FilterMode.Point);
        BlitSRT(cmd, _CurrRT_ID, material, 2);
        cmd.CopyTexture(_CurrRT_ID, Prev_RT);

        cmd.GetTemporaryRT(_Tmp_ID, GetDescriptor(RenderTextureFormat.RHalf), FilterMode.Point);
        cmd.SetGlobalTexture(ShaderConstant.mainTex, _CurrRT_ID);
        BlitSRT(cmd, _Tmp_ID, material, 3);
        cmd.SetGlobalTexture(ShaderConstant.mainTex, _Tmp_ID);
        BlitSRT(cmd, _CurrRT_ID, material, 4);

        ////// Combien Scene Color
        BlitSRT(cmd, _AO_Scene_Color_ID, BuiltinRenderTextureType.CameraTarget, material, 5/*(int)AODeBug*/);

        cmd.ReleaseTemporaryRT(_AO_Scene_Color_ID);
        cmd.ReleaseTemporaryRT(_GTAO_Texture_ID);
        cmd.ReleaseTemporaryRT(_BentNormal_Texture_ID);
        cmd.ReleaseTemporaryRT(_GTAO_Spatial_Texture_ID);
        cmd.ReleaseTemporaryRT(_CurrRT_ID);
        cmd.EndSample("GTAO");
    }



    public override void UpdateAfterSet(CommandBuffer cmd)
    {
        if (enabled)
        {
#if UNITY_EDITOR
            m_Camera.depthTextureMode |= DepthTextureMode.Depth;
            m_Camera.depthTextureMode |= DepthTextureMode.MotionVectors;
#endif

            var worldToCameraMatrix = m_Camera.worldToCameraMatrix;
            material.SetMatrix(_WorldToCameraMatrix_ID, worldToCameraMatrix);
            material.SetMatrix(_CameraToWorldMatrix_ID, worldToCameraMatrix.inverse);
            var projectionMatrix = GL.GetGPUProjectionMatrix(m_Camera.projectionMatrix, false);
            var View_ProjectionMatrix = projectionMatrix * worldToCameraMatrix;
            material.SetMatrix(_Inverse_View_ProjectionMatrix_ID, View_ProjectionMatrix.inverse);

            float temporalRotation = m_temporalRotations[m_sampleStep % 6];
            float temporalOffset = m_spatialOffsets[(m_sampleStep / 6) % 4];
            m_sampleStep++;

            material.SetVector(_AOParams2_ID, new Vector2(temporalRotation, temporalOffset));
        }
        else
        {
            if (material == null)
                return;
            if (m_Camera.actualRenderingPath != RenderingPath.DeferredShading)
            {
                return;
            }
            enabled = true;
            Execute(cmd);
        }
    }

    public override void CleanUp(CommandBuffer cmd)
    {
        if (cmd != null)
        {
            m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, cmd);
            cmd.Clear();
        }
        if (Prev_RT != null)
        {
            Prev_RT.Release();
            Prev_RT = null;
        }
    }



    private uint m_sampleStep = 0;
    private static readonly float[] m_temporalRotations = { 60, 300, 180, 240, 120, 0 };
    private static readonly float[] m_spatialOffsets = { 0, 0.5f, 0.25f, 0.75f };
    //////Shader Property
    ///Public
    private static int _Inverse_View_ProjectionMatrix_ID = Shader.PropertyToID("_Inverse_View_ProjectionMatrix");
    private static int _WorldToCameraMatrix_ID = Shader.PropertyToID("_WorldToCameraMatrix");
    private static int _CameraToWorldMatrix_ID = Shader.PropertyToID("_CameraToWorldMatrix");

    private static int _AOParams0_ID = Shader.PropertyToID("_AOParams0");
    private static int _AOParams1_ID = Shader.PropertyToID("_AOParams1");
    private static int _AOParams2_ID = Shader.PropertyToID("_AOParams2");

    ///Private
    private static int _AO_Scene_Color_ID = Shader.PropertyToID("_AO_Scene_Color");

    private static int _AO_UVToView_ID = Shader.PropertyToID("_AO_UVToView");
    private static int _AO_RT_TexelSize_ID = Shader.PropertyToID("_AO_RT_TexelSize");

    private static int _BentNormal_Texture_ID = Shader.PropertyToID("_BentNormal_Texture");
    private static int _GTAO_Texture_ID = Shader.PropertyToID("_GTAO_Texture");
    private static int _GTAO_Spatial_Texture_ID = Shader.PropertyToID("_GTAO_Spatial_Texture");
    private static int _PrevRT_ID = Shader.PropertyToID("_PrevRT");
    private static int _CurrRT_ID = Shader.PropertyToID("_CurrRT");
    private static int _Tmp_ID = Shader.PropertyToID("_Tmp");
    public static Mesh mesh
    {
        get
        {
            if (m_mesh != null)
                return m_mesh;
            m_mesh = new Mesh();
            m_mesh.vertices = new Vector3[] {
                new Vector3(-1,-1,0.5f),
                new Vector3(-1,1,0.5f),
                new Vector3(1,1,0.5f),
                new Vector3(1,-1,0.5f)
            };
            m_mesh.uv = new Vector2[] {
                new Vector2(0,1),
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1)
            };

            m_mesh.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
            return m_mesh;
        }
    }

    public static Mesh m_mesh;
    public static void BlitMRT(CommandBuffer buffer, RenderTargetIdentifier[] colorIdentifier, RenderTargetIdentifier depthIdentifier, Material mat, int pass)
    {
        buffer.SetRenderTarget(colorIdentifier, depthIdentifier);
        buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, pass);
    }

    public static void BlitSRT(CommandBuffer buffer, RenderTargetIdentifier destination, Material mat, int pass)
    {
        buffer.SetRenderTarget(destination);
        buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, pass);
    }

    public static void BlitMRT(CommandBuffer buffer, Texture source, RenderTargetIdentifier[] colorIdentifier, RenderTargetIdentifier depthIdentifier, Material mat, int pass)
    {
        buffer.SetRenderTarget(colorIdentifier, depthIdentifier);
        buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, pass);
    }

    public static void BlitSRT(CommandBuffer buffer, Texture source, RenderTargetIdentifier destination, Material mat, int pass)
    {
        buffer.SetGlobalTexture(ShaderConstant.mainTex, source);
        buffer.SetRenderTarget(destination);
        buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, pass);
    }

    public static void BlitSRT(CommandBuffer buffer, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material mat, int pass)
    {
        buffer.SetGlobalTexture(ShaderConstant.mainTex, source);
        buffer.SetRenderTarget(destination);
        buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, pass);
    }//Use This

    public static void BlitStencil(CommandBuffer buffer, RenderTargetIdentifier colorSrc, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthStencilBuffer, Material mat, int pass)
    {
        buffer.SetGlobalTexture(ShaderConstant.mainTex, colorSrc);
        buffer.SetRenderTarget(colorBuffer, depthStencilBuffer);
        buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, pass);
    }//UseThis

    public static void BlitStencil(CommandBuffer buffer, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthStencilBuffer, Material mat, int pass)
    {
        buffer.SetRenderTarget(colorBuffer, depthStencilBuffer);
        buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, pass);
    }

    public enum OutPass
    {
        Combien = 3,
        AO = 4,
        RO = 5,
        BentNormal = 6
    };

}