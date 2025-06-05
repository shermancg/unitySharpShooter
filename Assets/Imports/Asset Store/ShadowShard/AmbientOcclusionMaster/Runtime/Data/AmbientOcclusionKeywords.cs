namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data
{
    public static class AmbientOcclusionKeywords
    {
        public const string OrthographicCamera = "_ORTHOGRAPHIC_PROJECTION";

        public const string MultiBounce = "_MULTI_BOUNCE";
        
        public const string PseudoRandomNoise = "_PSEUDO_RANDOM_NOISE";
        public const string BlueNoise = "_BLUE_NOISE";
        
        public const string TemporalFiltering = "_TEMPORAL_FILTERING";

        public const string SourceDepthLow = "_DEPTH_NORMALS_LOW";
        public const string SourceDepthMedium = "_DEPTH_NORMALS_MEDIUM";
        public const string SourceDepthHigh = "_DEPTH_NORMALS_HIGH";
        public const string SourceDepthNormals = "_DEPTH_NORMALS_PREPASS";
        public const string HdaoUseNormals = "_HDAO_USE_NORMALS";

        public const string SampleCountLow = "_SAMPLE_COUNT_LOW";
        public const string SampleCountMedium = "_SAMPLE_COUNT_MEDIUM";
        public const string SampleCountHigh = "_SAMPLE_COUNT_HIGH";
        public const string SampleCountUltra = "_SAMPLE_COUNT_ULTRA";

        public const string TwoDirections = "_DIRECTIONS_2";
        public const string FourDirections = "_DIRECTIONS_4";
        public const string SixDirections = "_DIRECTIONS_6";

        public const string TwoSamples = "_SAMPLES_2";
        public const string FourSamples = "_SAMPLES_4";
        public const string SixSamples = "_SAMPLES_6";
        public const string EightSamples = "_SAMPLES_8";
        public const string TwelveSamples = "_SAMPLES_12";
        public const string SixteenSteps = "_SAMPLES_16";
    }
}