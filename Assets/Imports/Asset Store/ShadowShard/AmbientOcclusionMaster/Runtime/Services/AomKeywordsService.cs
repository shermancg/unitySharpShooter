using ShadowShard.AmbientOcclusionMaster.Runtime.Data;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Parameters;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Services
{
    internal class AomKeywordsService
    {
        internal void SetGlobalSsaoKeyword(CommandBuffer cmd, bool isAfterOpaque, bool isDisabled)
        {
            if (!isAfterOpaque && !isDisabled)
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);
        }

        internal void UpdateGeneralKeywords(Material material, GeneralParameters aomParameters)
        {
            SetOrthographicsKeyword(material, aomParameters.OrthographicCamera);
            SetDepthNormalsKeywords(material, aomParameters);
            SetNoiseMethodKeywords(material, aomParameters);
            SetTemporalFilteringKeyword(material, aomParameters);
            SetMultiBounceKeywords(material, aomParameters);
        }

        private void SetOrthographicsKeyword(Material material, bool isOrthographic) =>
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.OrthographicCamera, isOrthographic);

        private void SetDepthNormalsKeywords(Material material, GeneralParameters aomParameters)
        {
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SourceDepthLow, aomParameters.SourceDepthLow);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SourceDepthMedium, aomParameters.SourceDepthMedium);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SourceDepthHigh, aomParameters.SourceDepthHigh);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SourceDepthNormals, aomParameters.SourceDepthNormals);
        }

        private void SetNoiseMethodKeywords(Material material, GeneralParameters aomParameters)
        {
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.PseudoRandomNoise, aomParameters.NoiseMethodPseudoRandom);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.BlueNoise, aomParameters.NoiseMethodBlueNoise);
        }

        private void SetTemporalFilteringKeyword(Material material, GeneralParameters aomParameters) => 
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.TemporalFiltering, aomParameters.TemporalFiltering);
        
        private void SetMultiBounceKeywords(Material material, GeneralParameters aomParameters)
        {
            bool multiBounceDeferred = aomParameters is { IsAfterOpaque: true, MultiBounce: true, IsDeferredRendering: true };
            
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.MultiBounce, multiBounceDeferred);
        }

        internal void UpdateSsaoKeywords(Material material, SsaoMaterialParameters ssaoMaterialParameters)
        {
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SampleCountLow, ssaoMaterialParameters.SampleCountLow);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SampleCountMedium, ssaoMaterialParameters.SampleCountMedium);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SampleCountHigh, ssaoMaterialParameters.SampleCountHigh);
        }

        internal void UpdateHdaoKeywords(Material material, HdaoMaterialParameters hdaoMaterialParameters)
        {
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SampleCountLow, hdaoMaterialParameters.SampleCountLow);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SampleCountMedium, hdaoMaterialParameters.SampleCountMedium);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SampleCountHigh, hdaoMaterialParameters.SampleCountHigh);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SampleCountUltra, hdaoMaterialParameters.SampleCountUltra);
        }

        internal void UpdateHbaoKeywords(Material material, HbaoMaterialParameters hbaoMaterialParameters)
        {
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.TwoDirections, hbaoMaterialParameters.DirectionCountTwo);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.FourDirections, hbaoMaterialParameters.DirectionCountFour);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SixDirections, hbaoMaterialParameters.DirectionCountSix);

            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.TwoSamples, hbaoMaterialParameters.SampleCountTwo);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.FourSamples, hbaoMaterialParameters.SampleCountFour);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SixSamples, hbaoMaterialParameters.SampleCountSix);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.EightSamples, hbaoMaterialParameters.SampleCountEight);
        }

        internal void UpdateGtaoKeywords(Material material, GtaoMaterialParameters gtaoMaterialParameters)
        {
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.TwoSamples, gtaoMaterialParameters.SampleCountTwo);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.FourSamples, gtaoMaterialParameters.SampleCountFour);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SixSamples, gtaoMaterialParameters.SampleCountSix);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.EightSamples, gtaoMaterialParameters.SampleCountEight);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.TwelveSamples, gtaoMaterialParameters.SampleCountTwelve);
            CoreUtils.SetKeyword(material, AmbientOcclusionKeywords.SixteenSteps, gtaoMaterialParameters.SampleCountSixteen);
        }
    }
}