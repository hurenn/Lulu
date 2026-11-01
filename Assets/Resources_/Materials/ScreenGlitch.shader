// 画面全体のグリッチノイズ(Full Screen Pass Renderer Featureで使う)
// 強さ(_GlitchIntensity)と時間(_GlitchTime)は、ScreenGlitchからグローバルに設定する。強さが0の時は何もしない
Shader "Lulu/ScreenGlitch"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "ScreenGlitch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _GlitchIntensity; // 0〜1
            float _GlitchTime;      // 時間を止めている間も動かすため、実時間を渡す

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float intensity = saturate(_GlitchIntensity);
                if (intensity <= 0.0)
                {
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                }

                // 時間を細かく区切って、ノイズを不規則に切り替える
                float step_time = floor(_GlitchTime * 15.0);

                // 太い帯: 強いほど多くの帯が、大きく横にずれる
                float band = floor(uv.y * 18.0);
                float band_rand = Hash(float2(band, step_time));
                float band_on = step(1.0 - intensity * 0.5, band_rand);
                float shift = (Hash(float2(band, step_time + 7.0)) - 0.5) * 0.2 * intensity * band_on;

                // 細い線: 全体を細かく揺らす
                float fine = floor(uv.y * 240.0);
                shift += (Hash(float2(fine, step_time + 13.0)) - 0.5) * 0.008 * intensity;

                // RGBを左右にずらす(帯がずれている所は大きくずらす)
                float split = (0.004 + 0.02 * band_on) * intensity;
                float2 base_uv = float2(uv.x + shift, uv.y);
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, base_uv);
                col.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, base_uv + float2(split, 0.0)).r;
                col.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, base_uv - float2(split, 0.0)).b;

                // 砂嵐のようなノイズと、走査線
                float noise = Hash(floor(uv * float2(320.0, 180.0)) + step_time);
                col.rgb = lerp(col.rgb, noise.xxx, 0.18 * intensity);
                col.rgb *= 1.0 - 0.15 * intensity * step(0.5, frac(uv.y * 180.0));

                // ずれた帯の一部を、色の付いたブロックにする
                float block = step(1.0 - intensity * 0.15, Hash(float2(floor(uv.x * 12.0), band + step_time)));
                col.rgb = lerp(col.rgb, float3(noise, 1.0 - noise, 1.0), block * band_on * 0.5);

                return col;
            }
            ENDHLSL
        }
    }
}
