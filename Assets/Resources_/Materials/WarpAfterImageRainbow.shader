// ワープの残像用: スプライトの形を虹色に光らせる(ライトの影響を受けない)
// 色相はワールド座標の斜め方向に並び、時間で流れる(スプライトシートのUVに依存しない)
// 輪郭は周りの色・透明度を平均してぼかす(スプライトのメッシュの外へは広がらないので、内側に向かってぼける)
Shader "Lulu/WarpAfterImageRainbow" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Blend("Rainbow Blend", Range(0, 1)) = 0.8      // 元の色に虹色を混ぜる割合
        _Glow("Glow", Range(1, 5)) = 1.5                // 明るさの倍率
        _Saturation("Saturation", Range(0, 1)) = 0.8    // 虹色の鮮やかさ
        _StripeFreq("Stripe Frequency", Float) = 0.6    // 1ワールド単位あたりの色相の変化量(大きいほど帯が細かい)
        _ScrollSpeed("Scroll Speed", Float) = 3         // 1秒間に色相が何周流れるか
        _Blur("Blur (pixels)", Float) = 12              // 輪郭をぼかす幅(テクスチャのピクセル数。大きくするとスプライトシートの隣の絵が混ざることがある)
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            HLSLPROGRAM
            #pragma vertex LuluSpriteVertex
            #pragma fragment frag
            #include "LuluSprite.hlsl"

            half _Blend, _Glow, _Saturation;
            float _StripeFreq, _ScrollSpeed, _Blur;

            // 中心と、半径の半分・半径の2つの輪(各8方向)を平均する
            half4 BlurredSample(float2 uv) {
                float2 r = _Blur * _MainTex_TexelSize.xy;
                half4 sum = LuluSampleMain(uv) * 2.0;
                [unroll] for (int k = 0; k < 8; k++) {
                    float angle = k * 0.7853982; // 45度ずつ
                    float2 dir = float2(cos(angle), sin(angle));
                    sum += LuluSampleMain(uv + dir * r * 0.5);
                    sum += LuluSampleMain(uv + dir * r);
                }
                return sum / 18.0;
            }

            half3 HueToRGB(float h) {
                half3 rgb = saturate(abs(frac(h + float3(0, 2.0 / 3.0, 1.0 / 3.0)) * 6 - 3) - 1);
                return lerp(half3(1, 1, 1), rgb, _Saturation);
            }

            half4 frag(LuluSpriteVaryings i) : SV_Target {
                half4 tex = BlurredSample(i.uv);
                float hue = (i.positionWS.x + i.positionWS.y) * _StripeFreq - _Time.y * _ScrollSpeed;
                half3 rgb = lerp(tex.rgb, HueToRGB(hue), _Blend) * _Glow;
                return half4(rgb * i.color.rgb, tex.a * i.color.a);
            }
            ENDHLSL
        }
    }
}
