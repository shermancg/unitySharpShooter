using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums.Samples;
using ShadowShard.AmbientOcclusionMaster.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings
{
    [Serializable]
    public class HdaoSettings : IAmbientOcclusionSettings
    {
        [SerializeField] private float _intensity = 3.0f;

        [SerializeField] private float _radius = 0.8f; // Reject Radius

        [SerializeField] private float _falloff = 100.0f;

        [SerializeField] public float AcceptRadius = 0.003f;

        [SerializeField] public HdaoSamples Samples = HdaoSamples.Medium;

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

        public static HdaoSettings GetFromVolumeComponent(AmbientOcclusionMasterComponent volumeComponent,
            AomSettings defaultSettings)
        {
            return new HdaoSettings
            {
                Intensity = GetSetting(volumeComponent.HdaoIntensity, defaultSettings.HdaoSettings.Intensity),
                Radius = GetSetting(volumeComponent.HdaoRejectRadius, defaultSettings.HdaoSettings.Radius),
                AcceptRadius = GetSetting(volumeComponent.HdaoAcceptRadius, defaultSettings.HdaoSettings.AcceptRadius),
                Falloff = GetSetting(volumeComponent.HdaoFalloff, defaultSettings.HdaoSettings.Falloff),
                Samples = GetSetting(volumeComponent.HdaoSamples, defaultSettings.HdaoSettings.Samples),
            };
        }

        public static T GetSetting<T>(VolumeParameter<T> setting, T defaultValue) =>
            setting.overrideState ? setting.value : defaultValue;
    }
}