// 画面全体の放射状ブラー(Full Screen Pass Renderer Featureで使う)
// 強さ(_ScreenBlurIntensity)と中心(_ScreenBlurCenter、画面上の0〜1)は、ScreenBlurからグローバルに設定する。強さが0の時は何もしない
Shader "Lulu/ScreenBlur"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "ScreenBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ScreenBlurIntensity; // 0〜1
            float2 _ScreenBlurCenter;   // ブラーの中心(画面上の0〜1)

            static const int SAMPLE_COUNT = 10;
            static const float MAX_LENGTH = 0.06; // 強さ1の時、中心から離れた所が中心の方向へ伸びる長さ(中心との距離に対する割合)

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float intensity = saturate(_ScreenBlurIntensity);
                if (intensity <= 0.0)
                {
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                }

                // 中心から外へ広がるように、中心の方向へ少しずつずらした色を平均する
                float2 step_uv = (_ScreenBlurCenter - uv) * MAX_LENGTH * intensity / SAMPLE_COUNT;
                half4 col = 0;
                for (int i = 0; i < SAMPLE_COUNT; i++)
                {
                    col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + step_uv * i);
                }
                return col / SAMPLE_COUNT;
            }
            ENDHLSL
        }
    }
}
