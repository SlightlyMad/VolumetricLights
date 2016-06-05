//  Copyright(c) 2016, Michal Skalsky
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification,
//  are permitted provided that the following conditions are met:
//
//  1. Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//
//  2. Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//
//  3. Neither the name of the copyright holder nor the names of its contributors
//     may be used to endorse or promote products derived from this software without
//     specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
//  EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
//  OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.IN NO EVENT
//  SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
//  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT
//  OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
//  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
//  TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
//  EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.



using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System;

[RequireComponent(typeof(Camera))]
public class VolumetricLightRenderer : MonoBehaviour 
{
    public enum VolumtericResolution
    {
        Full,
        Half,
        Quarter
    };

    public static event Action<VolumetricLightRenderer, Matrix4x4> PreRenderEvent;

    private static Mesh _pointLightMesh;
    private static Mesh _spotLightMesh;
    private static Mesh _dirLightMesh;
    private static Material _lightMaterial;

    private Camera _camera;
    private CommandBuffer _preLightPass;
    private CommandBuffer _postLightPass;
    private CommandBuffer _preFinalPass;

    private Matrix4x4 _viewProj;
    private Material _blitAddMaterial;
    private Material _bilateralBlurMaterial;

    private RenderTexture _volumeLightTexture;
    private RenderTexture _halfVolumeLightTexture;
    private RenderTexture _quarterVolumeLightTexture;
    private static Texture _defaultSpotCookie;
    
    private RenderTexture _halfDepthBuffer;
    private RenderTexture _quarterDepthBuffer;
    private VolumtericResolution _currentResolution = VolumtericResolution.Full;
    private Texture2D _ditheringTexture;
    private Texture3D _noiseTexture;

    public VolumtericResolution Resolution = VolumtericResolution.Full;
    public Texture DefaultSpotCookie;

    public CommandBuffer GlobalCommandBuffer { get { return _preLightPass; } }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Material GetLightMaterial()
    {   
        return _lightMaterial;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Mesh GetPointLightMesh()
    {
        return _pointLightMesh;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Mesh GetSpotLightMesh()
    {
        return _spotLightMesh;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Mesh GetDirLightMesh()
    {
        return _dirLightMesh;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public RenderTexture GetVolumeLightBuffer()
    {
        if (Resolution == VolumtericResolution.Quarter)
            return _quarterVolumeLightTexture;
        else if (Resolution == VolumtericResolution.Half)
            return _halfVolumeLightTexture;
        else
            return _volumeLightTexture;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public RenderTexture GetVolumeLightDepthBuffer()
    {
        if (Resolution == VolumtericResolution.Quarter)
            return _quarterDepthBuffer;
        else if (Resolution == VolumtericResolution.Half)
            return _halfDepthBuffer;
        else
            return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Texture GetDefaultSpotCookie()
    {
        return _defaultSpotCookie;
    }

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        //Application.targetFrameRate = 1000;
        _camera = GetComponent<Camera>();

        _currentResolution = Resolution;
        
        _blitAddMaterial = new Material(Shader.Find("Hidden/BlitAdd"));
        _bilateralBlurMaterial = new Material(Shader.Find("Hidden/BilateralBlur"));

        _preLightPass = new CommandBuffer();
        _preLightPass.name = "PreLight";

        _postLightPass = new CommandBuffer();
        _postLightPass.name = "PostLight";

        _preFinalPass = new CommandBuffer();
        _preFinalPass.name = "PreFinal";

        ChangeResolution();

        if(_pointLightMesh == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _pointLightMesh = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
        }

        if (_spotLightMesh == null)
        {
            _spotLightMesh = CreateSpotLightMesh();
        }

        if (_dirLightMesh == null)
        {
            _dirLightMesh = CreateDirLightMesh();
        }

        if (_lightMaterial == null)
        {
            _lightMaterial = new Material(Shader.Find("Sandbox/VolumetricLight"));
        }
        
        if (_defaultSpotCookie == null)
        {
            _defaultSpotCookie = DefaultSpotCookie;
        }

        LoadNoise3dTexture();
        GenerateDitherTexture();
    }

    /// <summary>
    /// 
    /// </summary>
    void OnEnable()
    {
        //_camera.RemoveAllCommandBuffers();
        _camera.AddCommandBuffer(CameraEvent.BeforeLighting, _preLightPass);
        _camera.AddCommandBuffer(CameraEvent.AfterLighting, _postLightPass);
        _camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _preFinalPass);
    }

    /// <summary>
    /// 
    /// </summary>
    void OnDisable()
    {
        //_camera.RemoveAllCommandBuffers();
        _camera.RemoveCommandBuffer(CameraEvent.BeforeLighting, _preLightPass);
        _camera.RemoveCommandBuffer(CameraEvent.AfterLighting, _postLightPass);
        _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _preFinalPass);
    }
    
    /// <summary>
    /// 
    /// </summary>
    void ChangeResolution()
    {
        int width = _camera.pixelWidth;
        int height = _camera.pixelHeight;
        
        if (_volumeLightTexture != null)
            Destroy(_volumeLightTexture);            
        
        _volumeLightTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        _volumeLightTexture.name = "VolumeLightBuffer";
        _volumeLightTexture.filterMode = FilterMode.Point;
        
        _bilateralBlurMaterial.SetVector("_RenderTargetSize", new Vector4(_volumeLightTexture.width, _volumeLightTexture.height, 1.0f / _volumeLightTexture.width, 1.0f / _volumeLightTexture.height));


        if (_halfDepthBuffer != null)
            Destroy(_halfDepthBuffer);
        if (_halfVolumeLightTexture != null)
            Destroy(_halfVolumeLightTexture);

        if (Resolution == VolumtericResolution.Half || Resolution == VolumtericResolution.Quarter)
        {
            _halfVolumeLightTexture = new RenderTexture(width/2, height/2, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            _halfVolumeLightTexture.name = "VolumeLightBufferHalf";
            _halfVolumeLightTexture.filterMode = FilterMode.Point;

            _halfDepthBuffer = new RenderTexture(width/2, height/2, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            _halfDepthBuffer.name = "VolumeLightHalfDepth";
            _halfDepthBuffer.Create();
            _halfDepthBuffer.filterMode = FilterMode.Point;                    

            _bilateralBlurMaterial.SetTexture("_HalfResDepthBuffer", _halfDepthBuffer);
            _bilateralBlurMaterial.SetTexture("_HalfResColor", _halfVolumeLightTexture);
            _bilateralBlurMaterial.SetVector("_HalfResTexelSize", new Vector4(1.0f / _halfDepthBuffer.width, 1.0f / _halfDepthBuffer.height, 0, 0));
            _bilateralBlurMaterial.SetVector("_FullResTexelSize", new Vector4(1.0f / _camera.pixelWidth, 1.0f / _camera.pixelHeight, 0, 0));
        }

        if (_quarterVolumeLightTexture != null)
            Destroy(_quarterVolumeLightTexture);
        if (_quarterDepthBuffer != null)
            Destroy(_quarterDepthBuffer);

        if (Resolution == VolumtericResolution.Quarter)
        {
            _quarterVolumeLightTexture = new RenderTexture(width / 4, height / 4, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            _quarterVolumeLightTexture.name = "VolumeLightBufferQuarter";
            _quarterVolumeLightTexture.filterMode = FilterMode.Point;

            _quarterDepthBuffer = new RenderTexture(width / 4, height / 4, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            _quarterDepthBuffer.name = "VolumeLightQuarterDepth";
            _quarterDepthBuffer.Create();
            _quarterDepthBuffer.filterMode = FilterMode.Point;

            _bilateralBlurMaterial.SetTexture("_QuarterResDepthBuffer", _quarterDepthBuffer);
            _bilateralBlurMaterial.SetTexture("_QuarterResColor", _quarterVolumeLightTexture);
            _bilateralBlurMaterial.SetVector("_QuarterResTexelSize", new Vector4(1.0f / _quarterDepthBuffer.width, 1.0f / _quarterDepthBuffer.height, 0, 0));
        }

        _postLightPass.Clear();

        if (Resolution == VolumtericResolution.Quarter)
        {            
            _postLightPass.GetTemporaryRT(Shader.PropertyToID("VolumeLightBufferTemp"), _quarterDepthBuffer.width, _quarterDepthBuffer.height, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            // horizontal bilateral blur at full res
            _postLightPass.Blit(new RenderTargetIdentifier(_quarterVolumeLightTexture), new RenderTargetIdentifier("VolumeLightBufferTemp"), _bilateralBlurMaterial, 8);
            // vertical bilateral blur at full res
            _postLightPass.Blit(new RenderTargetIdentifier("VolumeLightBufferTemp"), new RenderTargetIdentifier(_quarterVolumeLightTexture), _bilateralBlurMaterial, 9);

            // upscale to full res
            _postLightPass.Blit(new RenderTargetIdentifier(_quarterVolumeLightTexture), new RenderTargetIdentifier(_halfVolumeLightTexture), _bilateralBlurMaterial, 7);
            _postLightPass.Blit(new RenderTargetIdentifier(_halfVolumeLightTexture), new RenderTargetIdentifier(_volumeLightTexture), _bilateralBlurMaterial, 5);
            
            _postLightPass.ReleaseTemporaryRT(Shader.PropertyToID("VolumeLightBufferTemp"));
        }
        else if (Resolution == VolumtericResolution.Half)
        {
            _postLightPass.GetTemporaryRT(Shader.PropertyToID("VolumeLightBufferTemp"), _halfVolumeLightTexture.width, _halfVolumeLightTexture.height, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            // horizontal bilateral blur at full res
            _postLightPass.Blit(new RenderTargetIdentifier(_halfVolumeLightTexture), new RenderTargetIdentifier("VolumeLightBufferTemp"), _bilateralBlurMaterial, 2);
            // vertical bilateral blur at full res
            _postLightPass.Blit(new RenderTargetIdentifier("VolumeLightBufferTemp"), new RenderTargetIdentifier(_halfVolumeLightTexture), _bilateralBlurMaterial, 3);

            // upscale to full res
            _postLightPass.Blit(new RenderTargetIdentifier(_halfVolumeLightTexture), new RenderTargetIdentifier(_volumeLightTexture), _bilateralBlurMaterial, 5);
            _postLightPass.ReleaseTemporaryRT(Shader.PropertyToID("VolumeLightBufferTemp"));
        }
        else
        {
            _postLightPass.GetTemporaryRT(Shader.PropertyToID("VolumeLightBufferTemp"), _volumeLightTexture.width, _volumeLightTexture.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            // horizontal bilateral blur at full res
            _postLightPass.Blit(new RenderTargetIdentifier(_volumeLightTexture), new RenderTargetIdentifier("VolumeLightBufferTemp"), _bilateralBlurMaterial, 0);
            // vertical bilateral blur at full res
            _postLightPass.Blit(new RenderTargetIdentifier("VolumeLightBufferTemp"), new RenderTargetIdentifier(_volumeLightTexture), _bilateralBlurMaterial, 1);
            _postLightPass.ReleaseTemporaryRT(Shader.PropertyToID("VolumeLightBufferTemp"));
        }

        _preFinalPass.Clear();        
        // add to Unity's light buffer
        _preFinalPass.Blit(new RenderTargetIdentifier(_volumeLightTexture), BuiltinRenderTextureType.CurrentActive, _blitAddMaterial, 0);        
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnPreRender()
    {
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, true);

        _viewProj = proj * _camera.worldToCameraMatrix;

        _preLightPass.Clear();

        if (Resolution == VolumtericResolution.Quarter)
        {
            Texture nullTexture = null;
            // down sample depth to half res
            _preLightPass.Blit(nullTexture, _halfDepthBuffer, _bilateralBlurMaterial, 4);
            // down sample depth to quarter res
            _preLightPass.Blit(nullTexture, _quarterDepthBuffer, _bilateralBlurMaterial, 6);
            
            _preLightPass.SetRenderTarget(_quarterVolumeLightTexture);
        }
        else if (Resolution == VolumtericResolution.Half)
        {
            Texture nullTexture = null;
            // down sample depth to half res
            _preLightPass.Blit(nullTexture, _halfDepthBuffer, _bilateralBlurMaterial, 4);
            
            _preLightPass.SetRenderTarget(_halfVolumeLightTexture);
        }
        else
        {
            _preLightPass.SetRenderTarget(_volumeLightTexture);
        }

        _preLightPass.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));
        
        if (PreRenderEvent != null)
            PreRenderEvent(this, _viewProj);                
    }
    
    /// <summary>
    /// 
    /// </summary>
    void Update()
    {
        Shader.SetGlobalTexture("_DitherTexture", _ditheringTexture);
        Shader.SetGlobalTexture("_NoiseTexture", _noiseTexture);

        //#if UNITY_EDITOR
        if (_currentResolution != Resolution)
        {
            _currentResolution = Resolution;
            ChangeResolution();
        }

        if ((_volumeLightTexture.width != _camera.pixelWidth || _volumeLightTexture.height != _camera.pixelHeight))
            ChangeResolution();
        //#endif
    }

    /// <summary>
    /// 
    /// </summary>
    void LoadNoise3dTexture()
    {
        // dds loader hard coded for 128x128x128 3d texture

        // doesn't work with TextureFormat.Alpha8 for soem reason
        _noiseTexture = new Texture3D(128, 128, 128, TextureFormat.RGBA32, false);
        _noiseTexture.name = "3D Noise";

        TextAsset data = Resources.Load("NoiseVolume") as TextAsset;

        byte[] bytes = data.bytes;

        uint height = BitConverter.ToUInt32(data.bytes, 12);
        uint width = BitConverter.ToUInt32(data.bytes, 16);
        uint pitch = BitConverter.ToUInt32(data.bytes, 20);
        uint depth = BitConverter.ToUInt32(data.bytes, 24);
        uint bitdepth = BitConverter.ToUInt32(data.bytes, 22 * 4);

        Color[] c = new Color[128 * 128 * 128];
        uint index = 128;
        pitch = (width * bitdepth + 7) / 8;

        Color m = new Color(0, 0, 0, 0);
        for (int d = 0; d < 128; ++d)
        {
            //index = 128;
            for (int i = 0; i < 128; ++i)
            {
                for (int j = 0; j < 128; ++j)
                {
                    float v = (bytes[index + j] / 255.0f);
                    c[i + j * 128 + d * 128 * 128] = new Color(v, v, v, v);
                }

                index += pitch;
            }
        }

        _noiseTexture.SetPixels(c);
        _noiseTexture.Apply();
    }

    /// <summary>
    /// 
    /// </summary>
    private void GenerateDitherTexture()
    {
        if(_ditheringTexture != null)
        {
            return;
        }

        int size = 8;
#if DITHER_4_4
        size = 4;
#endif
        // again, I couldn't make it work with Alpha8
        _ditheringTexture = new Texture2D(size, size, TextureFormat.Alpha8, false, true);
        _ditheringTexture.filterMode = FilterMode.Point;
        Color32[] c = new Color32[size * size];

        byte b;
#if DITHER_4_4
        b = (byte)(0.0f / 16.0f * 255); c[0] = new Color32(b, b, b, b);
        b = (byte)(8.0f / 16.0f * 255); c[1] = new Color32(b, b, b, b);
        b = (byte)(2.0f / 16.0f * 255); c[2] = new Color32(b, b, b, b);
        b = (byte)(10.0f / 16.0f * 255); c[3] = new Color32(b, b, b, b);

        b = (byte)(12.0f / 16.0f * 255); c[4] = new Color32(b, b, b, b);
        b = (byte)(4.0f / 16.0f * 255); c[5] = new Color32(b, b, b, b);
        b = (byte)(14.0f / 16.0f * 255); c[6] = new Color32(b, b, b, b);
        b = (byte)(6.0f / 16.0f * 255); c[7] = new Color32(b, b, b, b);

        b = (byte)(3.0f / 16.0f * 255); c[8] = new Color32(b, b, b, b);
        b = (byte)(11.0f / 16.0f * 255); c[9] = new Color32(b, b, b, b);
        b = (byte)(1.0f / 16.0f * 255); c[10] = new Color32(b, b, b, b);
        b = (byte)(9.0f / 16.0f * 255); c[11] = new Color32(b, b, b, b);

        b = (byte)(15.0f / 16.0f * 255); c[12] = new Color32(b, b, b, b);
        b = (byte)(7.0f / 16.0f * 255); c[13] = new Color32(b, b, b, b);
        b = (byte)(13.0f / 16.0f * 255); c[14] = new Color32(b, b, b, b);
        b = (byte)(5.0f / 16.0f * 255); c[15] = new Color32(b, b, b, b);
#else
        int i = 0;
        b = (byte)(1.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(49.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(13.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(61.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(4.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(52.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(16.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(64.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

        b = (byte)(33.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(17.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(45.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(29.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(36.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(20.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(48.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(32.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

        b = (byte)(9.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(57.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(5.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(53.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(12.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(60.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(8.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(56.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

        b = (byte)(41.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(25.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(37.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(21.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(44.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(28.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(40.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(24.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

        b = (byte)(3.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(51.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(15.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(63.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(2.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(50.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(14.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(62.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

        b = (byte)(35.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(19.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(47.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(31.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(34.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(18.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(46.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(30.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

        b = (byte)(11.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(59.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(7.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(55.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(10.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(58.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(6.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(54.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

        b = (byte)(43.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(27.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(39.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(23.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(42.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(26.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(38.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
        b = (byte)(22.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
#endif

        _ditheringTexture.SetPixels32(c);
        _ditheringTexture.Apply();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private Mesh CreateSpotLightMesh()
    {
        // copy & pasted from other project, the geometry is too complex, should be simplified
        Mesh mesh = new Mesh();

        const int segmentCount = 16;
        Vector3[] vertices = new Vector3[2 + segmentCount * 3];
        Color32[] colors = new Color32[2 + segmentCount * 3];

        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(0, 0, 1);

        float angle = 0;
        float step = Mathf.PI * 2.0f / segmentCount;
        float ratio = 0.9f;

        for (int i = 0; i < segmentCount; ++i)
        {
            vertices[i + 2] = new Vector3(-Mathf.Cos(angle) * ratio, Mathf.Sin(angle) * ratio, ratio);
            colors[i + 2] = new Color32(255, 255, 255, 255);
            vertices[i + 2 + segmentCount] = new Vector3(-Mathf.Cos(angle), Mathf.Sin(angle), 1);
            colors[i + 2 + segmentCount] = new Color32(255, 255, 255, 0);
            vertices[i + 2 + segmentCount * 2] = new Vector3(-Mathf.Cos(angle) * ratio, Mathf.Sin(angle) * ratio, 1);
            colors[i + 2 + segmentCount * 2] = new Color32(255, 255, 255, 255);
            angle += step;
        }

        mesh.vertices = vertices;
        mesh.colors32 = colors;

        int[] indices = new int[segmentCount * 3 * 2 + segmentCount * 6 * 2];
        int index = 0;

        for (int i = 2; i < segmentCount + 1; ++i)
        {
            indices[index++] = 0;
            indices[index++] = i;
            indices[index++] = i + 1;
        }

        indices[index++] = 0;
        indices[index++] = segmentCount + 1;
        indices[index++] = 2;

        for (int i = 2; i < segmentCount + 1; ++i)
        {
            indices[index++] = i;
            indices[index++] = i + segmentCount;
            indices[index++] = i + 1;

            indices[index++] = i + 1;
            indices[index++] = i + segmentCount;
            indices[index++] = i + segmentCount + 1;
        }

        indices[index++] = 2;
        indices[index++] = 1 + segmentCount;
        indices[index++] = 2 + segmentCount;

        indices[index++] = 2 + segmentCount;
        indices[index++] = 1 + segmentCount;
        indices[index++] = 1 + segmentCount + segmentCount;

        //------------
        for (int i = 2 + segmentCount; i < segmentCount + 1 + segmentCount; ++i)
        {
            indices[index++] = i;
            indices[index++] = i + segmentCount;
            indices[index++] = i + 1;

            indices[index++] = i + 1;
            indices[index++] = i + segmentCount;
            indices[index++] = i + segmentCount + 1;
        }

        indices[index++] = 2 + segmentCount;
        indices[index++] = 1 + segmentCount * 2;
        indices[index++] = 2 + segmentCount * 2;

        indices[index++] = 2 + segmentCount * 2;
        indices[index++] = 1 + segmentCount * 2;
        indices[index++] = 1 + segmentCount * 3;

        ////-------------------------------------
        for (int i = 2 + segmentCount * 2; i < segmentCount * 3 + 1; ++i)
        {
            indices[index++] = 1;
            indices[index++] = i + 1;
            indices[index++] = i;
        }

        indices[index++] = 1;
        indices[index++] = 2 + segmentCount * 2;
        indices[index++] = segmentCount * 3 + 1;

        mesh.triangles = indices;
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private Mesh CreateDirLightMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];

        vertices[0] = new Vector3(-1, -1, 1);
        vertices[1] = new Vector3(-1,  1, 1);
        vertices[2] = new Vector3( 1, -1, 1);
        vertices[3] = new Vector3( 1,  1, 1);
                
        mesh.vertices = vertices;
        
        int[] indices = new int[6];

        indices[0] = 0;
        indices[1] = 1;
        indices[2] = 2;

        indices[3] = 2;
        indices[4] = 1;
        indices[5] = 3;
        
        mesh.triangles = indices;
        mesh.RecalculateBounds();

        return mesh;
    }
}
