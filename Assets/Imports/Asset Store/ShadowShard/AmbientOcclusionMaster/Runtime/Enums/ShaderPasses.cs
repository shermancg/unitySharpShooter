namespace ShadowShard.AmbientOcclusionMaster.Runtime.Enums
{
    internal enum ShaderPasses
    {
        SSAO = 0,
        HDAO = 1,
        HBAO = 2,
        GTAO = 3,

        BilateralBlurHorizontal = 4,
        BilateralBlurVertical = 5,
        BilateralBlurFinal = 6,
        BilateralAfterOpaque = 7,

        GaussianBlurHorizontal = 8,
        GaussianBlurVertical = 9,
        GaussianAfterOpaque = 10,

        KawaseBlur = 11,
        KawaseAfterOpaque = 12,
            
        TemporalFilter = 13,
        TemporalFilterAfterOpaque = 14,

        Debug = 15,
    }
}