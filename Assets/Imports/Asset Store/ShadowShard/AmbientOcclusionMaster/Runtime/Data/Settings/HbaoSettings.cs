using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums.Samples;
using ShadowShard.AmbientOcclusionMaster.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings
{
    [Serializable]
    public class HbaoSettings : IAmbientOcclusionSettings
    {
        [SerializeField] private float _intensity = 3.0f;

        [SerializeField] private float _radius = 0.3f;

        [SerializeField] private float _falloff = 100.0f;

        [SerializeField] public int MaxRadiusPixel = 40;

        [SerializeField] public float AngleBias = 0.1f;

        [SerializeField] public HbaoDirections HbaoDirections = HbaoDirections.Directions2;

        [SerializeField] public HbaoSamples HbaoSamples = HbaoSamples.Samples4;

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

        public static HbaoSettings GetFromVolumeComponent(AmbientOcclusionMasterComponent volumeComponent,
            AomSettings defaultSettings)
        {
            return new HbaoSettings
            {
                Intensity = GetSetting(volumeComponent.HbaoIntensity, defaultSettings.HbaoSettings.Intensity),
                Radius = GetSetting(volumeComponent.HbaoRadius, defaultSettings.HbaoSettings.Radius),
                Falloff = GetSetting(volumeComponent.HbaoFalloff, defaultSettings.HbaoSettings.Falloff),
                MaxRadiusPixel = GetSetting(volumeComponent.HbaoMaxRadiusInPixels,
                    defaultSettings.HbaoSettings.MaxRadiusPixel),
                AngleBias = GetSetting(volumeComponent.HbaoAngleBias, defaultSettings.HbaoSettings.AngleBias),
                HbaoDirections =
                    GetSetting(volumeComponent.HbaoDirections, defaultSettings.HbaoSettings.HbaoDirections),
                HbaoSamples = GetSetting(volumeComponent.HbaoSamples, defaultSettings.HbaoSettings.HbaoSamples),
            };
        }

        public static T GetSetting<T>(VolumeParameter<T> setting, T defaultValue) =>
            setting.overrideState ? setting.value : defaultValue;
    }
}