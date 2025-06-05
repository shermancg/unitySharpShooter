using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Parameters;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Services
{
    internal class AomParametersService
    {
        private readonly AomKeywordsService _keywordsService = new();

        private readonly Matrix4x4[] _cameraViewProjections = new Matrix4x4[2];
        private readonly Vector4[] _cameraTopLeftCorner = new Vector4[2];
        private readonly Vector4[] _cameraXExtent = new Vector4[2];
        private readonly Vector4[] _cameraYExtent = new Vector4[2];
        private readonly Vector4[] _cameraZExtent = new Vector4[2];
        
        private Texture2D[] _blueNoiseTextures;
        private int _blueNoiseTextureIndex;
        
        private uint _frameCount;
        private static readonly float[] TemporalRotations = {60, 300, 180, 240, 120, 0};
        private static readonly float[] SpatialOffsets = {0, 0.5f, 0.25f, 0.75f};

        internal void InitBlueNoise(Texture2D[] blueNoiseTextures) =>
            _blueNoiseTextures = blueNoiseTextures;

        internal void SetGlobalSsaoKeyword(CommandBuffer cmd, bool isAfterOpaque, bool isDisabled) =>
            _keywordsService.SetGlobalSsaoKeyword(cmd, isAfterOpaque, isDisabled);

        internal void SetAmbientOcclusionParams(CommandBuffer cmd, float directLightingStrength) =>
            cmd.SetGlobalVector(AmbientOcclusionConstants.AmbientOcclusionParamName,
                new Vector4(1f, 0f, 0f, directLightingStrength));

        internal void SetupKeywordsAndParameters(Material material, AomSettings aomSettings, UniversalCameraData cameraData)
        {
            SetGeneralParameters(material, aomSettings, cameraData);

            switch (aomSettings.AmbientOcclusionMode)
            {
                case AmbientOcclusionMode.SSAO:
                    SetSsaoParameters(material, aomSettings);
                    break;
                case AmbientOcclusionMode.HDAO:
                    SetHdaoParameters(material, aomSettings, cameraData.camera);
                    break;
                case AmbientOcclusionMode.HBAO:
                    SetHbaoParameters(material, aomSettings, cameraData.camera);
                    break;
                case AmbientOcclusionMode.GTAO:
                    SetGtaoParameters(material, ref aomSettings, cameraData.camera);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!aomSettings.TemporalFiltering) 
                return;
            
            float temporalRotation = TemporalRotations[_frameCount % 6];
            float temporalOffset = SpatialOffsets[(_frameCount / 6) % 4];
            material.SetFloat(PropertiesIDs.AomTemporalRotation, temporalRotation / 360);
            material.SetFloat(PropertiesIDs.AomTemporalOffset, temporalOffset);
            _frameCount++;
        }

        private void SetGeneralParameters(Material material, AomSettings aomSettings, UniversalCameraData cameraData)
        {
            SetCameraViewProjection(material, cameraData);
            SetBlueNoise(material, cameraData.camera, aomSettings.NoiseMethod == NoiseMethod.BlueNoise);

            GeneralParameters aoGeneralParameters = new(aomSettings, cameraData.camera.orthographic);
            SetDownsample(material, aomSettings.Downsample);
            SetTemporalFilteringParameters(material, aomSettings.TemporalScale, aomSettings.TemporalResponse);
            _keywordsService.UpdateGeneralKeywords(material, aoGeneralParameters);
        }

        private void SetCameraViewProjection(Material material, UniversalCameraData cameraData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            int eyeCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
#else
            int eyeCount = 1;
#endif

            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 view = cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 proj = cameraData.GetProjectionMatrix(eyeIndex);
                _cameraViewProjections[eyeIndex] = proj * view;

                Matrix4x4 cview = view;
                cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                Matrix4x4 cviewProjInv = (proj * cview).inverse;

                Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
                Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
                Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
                Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));

                _cameraTopLeftCorner[eyeIndex] = topLeftCorner;
                _cameraXExtent[eyeIndex] = topRightCorner - topLeftCorner;
                _cameraYExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
                _cameraZExtent[eyeIndex] = farCentre;
            }

            Vector4 projectionParams = new(1.0f / cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f);
            material.SetVector(PropertiesIDs.ProjectionParams2, projectionParams);
            material.SetMatrixArray(PropertiesIDs.CameraViewProjections, _cameraViewProjections);
            material.SetVectorArray(PropertiesIDs.CameraViewTopLeftCorner, _cameraTopLeftCorner);
            material.SetVectorArray(PropertiesIDs.CameraViewXExtent, _cameraXExtent);
            material.SetVectorArray(PropertiesIDs.CameraViewYExtent, _cameraYExtent);
            material.SetVectorArray(PropertiesIDs.CameraViewZExtent, _cameraZExtent);
        }

        private void SetBlueNoise(Material material, Camera camera, bool isBlueNoiseMethod)
        {
            if (!isBlueNoiseMethod)
                return;

            _blueNoiseTextureIndex = (_blueNoiseTextureIndex + 1) % _blueNoiseTextures.Length;
            Texture2D noiseTexture = _blueNoiseTextures[_blueNoiseTextureIndex];

            Vector4 blueNoiseParams = new(
                camera.pixelWidth / (float)_blueNoiseTextures[_blueNoiseTextureIndex].width, // X Scale
                camera.pixelHeight / (float)_blueNoiseTextures[_blueNoiseTextureIndex].height, // Y Scale
                Random.value, // X Offset
                Random.value // Y Offset
            );

            // For testing we use a single blue noise texture and a single set of blue noise params.
#if UNITY_INCLUDE_TESTS
            noiseTexture = _blueNoiseTextures[0];
            blueNoiseParams.z = 1;
            blueNoiseParams.w = 1;
#endif

            material.SetTexture(PropertiesIDs.BlueNoiseTexture, noiseTexture);
            material.SetVector(PropertiesIDs.AomBlueNoiseParameters, blueNoiseParams);
        }

        private void SetDownsample(Material material, bool downsample) =>
            material.SetFloat(PropertiesIDs.Downsample, 1.0f / (downsample ? 2 : 1));
        
        private void SetTemporalFilteringParameters(Material material, float scale, float response)
        {
            material.SetFloat(PropertiesIDs.AomTemporalScale, scale);
            material.SetFloat(PropertiesIDs.AomTemporalResponse, response);
        }

        private void SetSsaoParameters(Material material, AomSettings aomSettings)
        {
            SsaoMaterialParameters ssaoMaterialParameters = new(aomSettings);
            _keywordsService.UpdateSsaoKeywords(material, ssaoMaterialParameters);
            material.SetVector(PropertiesIDs.SsaoParameters, ssaoMaterialParameters.SsaoParameters);
        }

        private void SetHdaoParameters(Material material, AomSettings aomSettings, Camera camera)
        {
            HdaoMaterialParameters hdaoMaterialParameters = new(aomSettings, camera);
            _keywordsService.UpdateHdaoKeywords(material, hdaoMaterialParameters);
            material.SetVector(PropertiesIDs.HdaoParameters, hdaoMaterialParameters.HdaoParameters);
            material.SetVector(PropertiesIDs.HdaoParameters2, hdaoMaterialParameters.HdaoParameters2);
        }

        private void SetHbaoParameters(Material material, AomSettings aomSettings, Camera camera)
        {
            HbaoMaterialParameters hbaoMaterialParameters = new(aomSettings, camera);
            _keywordsService.UpdateHbaoKeywords(material, hbaoMaterialParameters);
            material.SetVector(PropertiesIDs.HbaoParameters, hbaoMaterialParameters.HbaoParameters);
            material.SetVector(PropertiesIDs.HbaoParameters2, hbaoMaterialParameters.HbaoParameters2);
        }

        private void SetGtaoParameters(Material material, ref AomSettings aomSettings, Camera camera)
        {
            GtaoMaterialParameters gtaoMaterialParameters = new(aomSettings, camera);
            _keywordsService.UpdateGtaoKeywords(material, gtaoMaterialParameters);
            material.SetVector(PropertiesIDs.GtaoParameters, gtaoMaterialParameters.GtaoParameters);
            material.SetVector(PropertiesIDs.GtaoParameters2, gtaoMaterialParameters.GtaoParameters2);
        }
    }
}