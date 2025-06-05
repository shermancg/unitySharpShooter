using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using ShadowShard.AmbientOcclusionMaster.Runtime.Volume;
using UnityEngine.Rendering;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings
{
    internal class AomSettingsService
    {
        internal AomSettings GetFromVolumeComponent(AomSettings defaultSettings)
        {
            AmbientOcclusionMasterComponent volumeComponent =
                AmbientOcclusionMasterComponent.GetAmbientOcclusionMasterComponent();
            
            if (volumeComponent == null)
                return defaultSettings;

            SsaoSettings ssaoSettings = SsaoSettings.GetFromVolumeComponent(volumeComponent, defaultSettings);
            HdaoSettings hdaoSettings = HdaoSettings.GetFromVolumeComponent(volumeComponent, defaultSettings);
            HbaoSettings hbaoSettings = HbaoSettings.GetFromVolumeComponent(volumeComponent, defaultSettings);
            GtaoSettings gtaoSettings = GtaoSettings.GetFromVolumeComponent(volumeComponent, defaultSettings);

            return new AomSettings
            {
                AmbientOcclusionMode = GetSetting(volumeComponent.Mode, defaultSettings.AmbientOcclusionMode),

                SsaoSettings = ssaoSettings,
                HdaoSettings = hdaoSettings,
                HbaoSettings = hbaoSettings,
                GtaoSettings = gtaoSettings,

                MultiBounce = GetSetting(volumeComponent.MultiBounce, defaultSettings.MultiBounce),
                DirectLightingStrength = GetSetting(volumeComponent.DirectLightingStrength, defaultSettings.DirectLightingStrength),
                NoiseMethod = GetSetting(volumeComponent.NoiseType, defaultSettings.NoiseMethod),
                BlurQuality = GetSetting(volumeComponent.BlurMode, defaultSettings.BlurQuality),
                
                TemporalFiltering = GetSetting(volumeComponent.TemporalFiltering, defaultSettings.TemporalFiltering),
                TemporalScale = GetSetting(volumeComponent.TemporalScale, defaultSettings.TemporalScale),
                TemporalResponse = GetSetting(volumeComponent.TemporalResponse, defaultSettings.TemporalResponse),

                DebugMode = GetSetting(volumeComponent.DebugMode, defaultSettings.DebugMode),
                RenderingPath = GetSetting(volumeComponent.RenderPath, defaultSettings.RenderingPath),
                AfterOpaque = GetSetting(volumeComponent.AfterOpaque, defaultSettings.AfterOpaque),
                Downsample = GetSetting(volumeComponent.Downsample, defaultSettings.Downsample),
                DepthSource = GetSetting(volumeComponent.Source, defaultSettings.DepthSource),
                NormalQuality = GetSetting(volumeComponent.NormalsQuality, defaultSettings.NormalQuality)
            };
        }
        
        internal IAmbientOcclusionSettings GetAmbientOcclusionSettings(AomSettings settings)
        {
            return settings.AmbientOcclusionMode switch
            {
                AmbientOcclusionMode.None => null,
                AmbientOcclusionMode.SSAO => settings.SsaoSettings,
                AmbientOcclusionMode.HDAO => settings.HdaoSettings,
                AmbientOcclusionMode.HBAO => settings.HbaoSettings,
                AmbientOcclusionMode.GTAO => settings.GtaoSettings,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        internal bool IsAmbientOcclusionModeNone(AomSettings settings) =>
            settings.AmbientOcclusionMode == AmbientOcclusionMode.None;

        private static T GetSetting<T>(VolumeParameter<T> setting, T defaultValue) =>
            setting.overrideState ? setting.value : defaultValue;
    }
}