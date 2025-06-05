using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums.Samples;
using ShadowShard.AmbientOcclusionMaster.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings
{
    [Serializable]
    public class GtaoSettings : IAmbientOcclusionSettings
    {
        [SerializeField] private float _intensity = 3.0f;

        [SerializeField] private float _radius = 0.3f;

        [SerializeField] private float _falloff = 100.0f;

        [SerializeField] public int MaxRadiusPixel = 40;

        [SerializeField] public int Directions = 2;

        [SerializeField] public GtaoSamples Samples = GtaoSamples.Samples4;

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

        internal static GtaoSettings GetFromVolumeComponent(AmbientOcclusionMasterComponent volumeComponent,
            AomSettings defaultSettings)
        {
            return new GtaoSettings
            {
                Intensity = GetSetting(volumeComponent.GtaoIntensity, defaultSettings.GtaoSettings.Intensity),
                Radius = GetSetting(volumeComponent.GtaoRadius, defaultSettings.GtaoSettings.Radius),
                Falloff = GetSetting(volumeComponent.GtaoFalloff, defaultSettings.GtaoSettings.Falloff),
                MaxRadiusPixel = GetSetting(volumeComponent.GtaoMaxRadiusInPixels,
                    defaultSettings.GtaoSettings.MaxRadiusPixel),
                Directions = GetSetting(volumeComponent.GtaoDirections, defaultSettings.GtaoSettings.Directions),
                Samples = GetSetting(volumeComponent.GtaoSamples, defaultSettings.GtaoSettings.Samples),
            };
        }

        private static T GetSetting<T>(VolumeParameter<T> setting, T defaultValue) =>
            setting.overrideState ? setting.value : defaultValue;
    }
}