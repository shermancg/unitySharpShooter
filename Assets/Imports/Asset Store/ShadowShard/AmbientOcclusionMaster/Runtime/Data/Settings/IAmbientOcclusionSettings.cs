namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings
{
    internal interface IAmbientOcclusionSettings
    {
        public float Intensity { get; set; }
        public float Radius { get; set; }
        public float Falloff { get; set; }
    }
}