// ワープの残像用: スプライトの形を虹色に光らせる(ライトの影響を受けない)
// 色相はワールド座標の斜め方向に並び、時間で流れる(スプライトシートのUVに依存しない)
Shader "Lulu/WarpAfterImageRainbow" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Blend("Rainbow Blend", Range(0, 1)) = 0.8      // 元の色に虹色を混ぜる割合
        _Glow("Glow", Range(1, 5)) = 1.5                // 明るさの倍率
        _Saturation("Saturation", Range(0, 1)) = 0.8    // 虹色の鮮やかさ
        _StripeFreq("Stripe Frequency", Float) = 0.6    // 1ワールド単位あたりの色相の変化量(大きいほど帯が細かい)
        _ScrollSpeed("Scroll Speed", Float) = 3         // 1秒間に色相が何周流れるか
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
            float _StripeFreq, _ScrollSpeed;

            half3 HueToRGB(float h) {
                half3 rgb = saturate(abs(frac(h + float3(0, 2.0 / 3.0, 1.0 / 3.0)) * 6 - 3) - 1);
                return lerp(half3(1, 1, 1), rgb, _Saturation);
            }

            half4 frag(LuluSpriteVaryings i) : SV_Target {
                half4 tex = LuluSampleMain(i.uv);
                float hue = (i.positionWS.x + i.positionWS.y) * _StripeFreq - _Time.y * _ScrollSpeed;
                half3 rgb = lerp(tex.rgb, HueToRGB(hue), _Blend) * _Glow;
                return half4(rgb * i.color.rgb, tex.a * i.color.a);
            }
            ENDHLSL
        }
    }
}
