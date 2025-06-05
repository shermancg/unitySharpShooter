using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums.Samples;
using ShadowShard.AmbientOcclusionMaster.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings
{
    [Serializable]
    public class SsaoSettings : IAmbientOcclusionSettings
    {
        [SerializeField] private float _intensity = 3.0f;

        [SerializeField] private float _radius = 0.1f;

        [SerializeField] private float _falloff = 100.0f;

        [SerializeField] public SsaoSamples SamplesCount = SsaoSamples.Medium;

        // Properties implementation for IAmbientOcclusionSettings
        public float Intensity
        {
            get => _intensity;
            set => _intensity = value;
        }

        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        public float Falloff
        {
            get => _falloff;
            set => _falloff = value;
        }

        public static SsaoSettings GetFromVolumeComponent(AmbientOcclusionMasterComponent volumeComponent,
            AomSettings defaultSettings)
        {
            return new SsaoSettings
            {
                Intensity = GetSetting(volumeComponent.SsaoIntensity, defaultSettings.SsaoSettings.Intensity),
                Radius = GetSetting(volumeComponent.SsaoRadius, defaultSettings.SsaoSettings.Radius),
                Falloff = GetSetting(volumeComponent.SsaoFalloff, defaultSettings.SsaoSettings.Falloff),
                SamplesCount = GetSetting(volumeComponent.SsaoSamplesCount, defaultSettings.SsaoSettings.SamplesCount),
            };
        }

        public static T GetSetting<T>(VolumeParameter<T> setting, T defaultValue) =>
            setting.overrideState ? setting.value : defaultValue;
    }
}