Shader "Hidden/ShadowShard/AmbientOcclusionMaster"
{
    HLSLINCLUDE
    #pragma editor_sync_compilation
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "SSAO"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment SSAO
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_local_fragment _ _ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local_fragment _ _PSEUDO_RANDOM_NOISE _BLUE_NOISE
            #pragma multi_compile_local_fragment _ _TEMPORAL_FILTERING
            #pragma multi_compile_local_fragment _SAMPLE_COUNT_LOW _SAMPLE_COUNT_MEDIUM _SAMPLE_COUNT_HIGH
            #pragma multi_compile_local_fragment _DEPTH_NORMALS_LOW _DEPTH_NORMALS_MEDIUM _DEPTH_NORMALS_HIGH _DEPTH_NORMALS_PREPASS

            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/SSAO/SSAO.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "HDAO"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment HDAO
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_local_fragment _ _ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local_fragment _ _PSEUDO_RANDOM_NOISE _BLUE_NOISE
            #pragma multi_compile_local_fragment _ _TEMPORAL_FILTERING
            #pragma multi_compile_local_fragment _SAMPLE_COUNT_LOW _SAMPLE_COUNT_MEDIUM _SAMPLE_COUNT_HIGH _SAMPLE_COUNT_ULTRA
            #pragma multi_compile_local_fragment _ _HDAO_USE_NORMALS

            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/HDAO/HDAO.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "HBAO"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment HBAO
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_local_fragment _ _ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local_fragment _ _PSEUDO_RANDOM_NOISE _BLUE_NOISE
            #pragma multi_compile_local_fragment _ _TEMPORAL_FILTERING
            #pragma multi_compile_local_fragment _DIRECTIONS_2 _DIRECTIONS_4 _DIRECTIONS_6
            #pragma multi_compile_local_fragment _SAMPLES_2 _SAMPLES_4 _SAMPLES_6 _SAMPLES_8
            #pragma multi_compile_local_fragment _DEPTH_NORMALS_LOW _DEPTH_NORMALS_MEDIUM _DEPTH_NORMALS_HIGH _DEPTH_NORMALS_PREPASS

            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/HBAO/HBAO.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "GTAO"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GTAO
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_local_fragment _ _ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local_fragment _ _PSEUDO_RANDOM_NOISE _BLUE_NOISE
            #pragma multi_compile_local_fragment _ _TEMPORAL_FILTERING
            #pragma multi_compile_local_fragment _SAMPLES_2 _SAMPLES_4 _SAMPLES_6 _SAMPLES_8 _SAMPLES_12 _SAMPLES_16
            #pragma multi_compile_local_fragment _DEPTH_NORMALS_LOW _DEPTH_NORMALS_MEDIUM _DEPTH_NORMALS_HIGH _DEPTH_NORMALS_PREPASS

            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/GTAO/GTAO.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Bilateral Blur
        // ------------------------------------------------------------------

        // 4 - Horizontal
        Pass
        {
            Name "AO_Bilateral_HorizontalBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment HorizontalBlur
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Blur/BilateralBlur.hlsl"
            ENDHLSL
        }

        // 5 - Vertical
        Pass
        {
            Name "AO_Bilateral_VerticalBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment VerticalBlur
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Blur/BilateralBlur.hlsl"
            ENDHLSL
        }

        // 6 - Final
        Pass
        {
            Name "AO_Bilateral_FinalBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FinalBlur
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Blur/BilateralBlur.hlsl"
            ENDHLSL
        }

        // 7 - After Opaque
        Pass
        {
            Name "AO_Bilateral_FinalBlur_AfterOpaque"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend DstColor Zero, One OneMinusSrcAlpha
            BlendOp Add, Add

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBilateralAfterOpaque
            #pragma multi_compile_local_fragment _ _MULTI_BOUNCE

            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Blur/BilateralBlur.hlsl"
            #include "ShaderLibrary/Utils/AOM_MultiBounce.hlsl"

            half4 FragBilateralAfterOpaque(Varyings input) : SV_Target
            {
                half ao = FinalBlur(input).r;

                #if defined(_MULTI_BOUNCE)
                return half4(UniversalMultiBounce(ao, input.texcoord, input.positionCS), ao);
                #else
                return half4(ao, ao, ao, ao);
                #endif
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Gaussian Blur
        // ------------------------------------------------------------------

        // 8 - Horizontal
        Pass
        {
            Name "AO_Gaussian_HorizontalBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment HorizontalGaussianBlur
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Blur/GaussianBlur.hlsl"
            ENDHLSL
        }

        // 9 - Vertical
        Pass
        {
            Name "AO_Gaussian_VerticalBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment VerticalGaussianBlur
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Blur/GaussianBlur.hlsl"
            ENDHLSL
        }

        // 10 - After Opaque
        Pass
        {
            Name "AO_Gaussian_VerticalBlur_AfterOpaque"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend DstColor Zero, One OneMinusSrcAlpha
            BlendOp Add, Add

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragGaussianAfterOpaque
            #pragma multi_compile_local_fragment _ _MULTI_BOUNCE

            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Blur/GaussianBlur.hlsl"
            #include "ShaderLibrary/Utils/AOM_MultiBounce.hlsl"

            half4 FragGaussianAfterOpaque(Varyings input) : SV_Target
            {
                half ao = VerticalGaussianBlur(input);

                #if defined(_MULTI_BOUNCE)
                return half4(UniversalMultiBounce(ao, input.texcoord, input.positionCS), ao);
                #else
                return half4(ao, ao, ao, ao);
                #endif
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Kawase Blur
        // ------------------------------------------------------------------

        // 11 - Kawase Blur
        Pass
        {
            Name "AO_Kawase"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment KawaseBlur
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Blur/KawaseBlur.hlsl"
            ENDHLSL
        }

        // 12 - After Opaque Kawase
        Pass
        {
            Name "AO_Kawase_AfterOpaque"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend DstColor Zero, One OneMinusSrcAlpha
            BlendOp Add, Add

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragKawaseAfterOpaque
            #pragma multi_compile_local_fragment _ _MULTI_BOUNCE

            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Blur/KawaseBlur.hlsl"
            #include "ShaderLibrary/Utils/AOM_MultiBounce.hlsl"

            half4 FragKawaseAfterOpaque(Varyings input) : SV_Target
            {
                half ao = KawaseBlur(input);

                #if defined(_MULTI_BOUNCE)
                return half4(UniversalMultiBounce(ao, input.texcoord, input.positionCS), ao);
                #else
                return half4(ao, ao, ao, ao);
                #endif
            }
            ENDHLSL
        }

        // 13 - Temporal Filter
        Pass
        {
            Name "AO_TemporalFilter"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment AOM_TemporalFilter
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Utils/AOM_TemporalFilter.hlsl"
            ENDHLSL
        }

        // 14 - After Opaque Temporal Filter
        Pass
        {
            Name "AO_TemporalFilter_AfterOpaque"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend DstColor Zero, One OneMinusSrcAlpha
            BlendOp Add, Add

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragTemporalFilterAfterOpaque
            #pragma multi_compile_local_fragment _ _MULTI_BOUNCE
            
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "ShaderLibrary/Utils/AOM_TemporalFilter.hlsl"
            #include "ShaderLibrary/Utils/AOM_MultiBounce.hlsl"

            half4 FragTemporalFilterAfterOpaque(Varyings input) : SV_Target
            {
                half ao = AOM_TemporalFilter(input);

                #if defined(_MULTI_BOUNCE)
                return half4(UniversalMultiBounce(ao, input.texcoord, input.positionCS), ao);
                #else
                return half4(ao, ao, ao, ao);
                #endif
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // UTILS
        // ------------------------------------------------------------------

        // 14 - AO Debug
        Pass
        {
            Name "AO_Debug"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DebugAOFragment
            #pragma multi_compile_local_fragment _ _MULTI_BOUNCE
            
            #include "ShaderLibrary/Utils/AOM_MultiBounce.hlsl"

            TEXTURE2D_X(_ScreenSpaceOcclusionTexture);

            half4 DebugAOFragment(Varyings input) : SV_Target
            {
                half aoDebug = SAMPLE_TEXTURE2D_X(_ScreenSpaceOcclusionTexture, sampler_LinearClamp, input.texcoord).r;

                #if defined(_MULTI_BOUNCE)
                return half4(lerp(UniversalMultiBounce(aoDebug, input.texcoord, input.positionCS), 1.0, aoDebug), 1.0);
                #else
                return half4(aoDebug, aoDebug, aoDebug, 1.0);
                #endif
            }
            ENDHLSL
        }
    }
}