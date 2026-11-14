// スプライトのシルエットの縁(内側)だけを光らせる(加算合成)。元のスプライトに重ねて、リムライトとして使う
// 周りを何方向か少し外側まで調べ、外が透明な所を縁とみなす(色の境目ではなく形の縁だけが光るので、服のしわなどは光らない)
// 色と強さはSpriteRenderer.colorで決める。縁の幅(テクスチャのピクセル数)は_RimWidthで決める
Shader "Lulu/SpriteRimLight" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _RimWidth("Rim Width (texels)", Range(0.5, 12)) = 5
        _RimDir("Light Direction (UV)", Vector) = (0, 1, 0, 0)
        _RimDirBias("Direction Bias", Range(0, 1)) = 0.3
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass {
            HLSLPROGRAM
            #pragma vertex LuluSpriteVertex
            #pragma fragment frag
            #include "LuluSprite.hlsl"

            float _RimWidth;
            float4 _RimDir;
            float _RimDirBias;

            #define RIM_DIRECTIONS 12

            half4 frag(LuluSpriteVaryings i) : SV_Target {
                half a = LuluSampleMain(i.uv).a;
                if (a < 0.01) return half4(0, 0, 0, 0);

                float2 light_dir = normalize(_RimDir.xy + float2(0.0001, 0));
                half rim = 0;
                [unroll]
                for (int k = 0; k < RIM_DIRECTIONS; k++) {
                    float ang = k * (6.2831853 / RIM_DIRECTIONS);
                    float2 d = float2(cos(ang), sin(ang));
                    // 近い所と遠い所を調べて、縁に近いほど強く(柔らかい縁にする)
                    float2 step_uv = d * _RimWidth * _MainTex_TexelSize.xy;
                    half outside = (1 - LuluSampleMain(i.uv + step_uv * 0.5).a) * 0.6
                                 + (1 - LuluSampleMain(i.uv + step_uv).a) * 0.4;
                    // 光の来る方向を向いた縁ほど強く
                    half facing = saturate(dot(d, light_dir) * 0.5 + 0.5);
                    half weight = lerp(1, facing * facing * 2, _RimDirBias);
                    rim = max(rim, outside * weight);
                }
                rim = saturate(rim);
                return half4(i.color.rgb, rim * a * i.color.a);
            }
            ENDHLSL
        }
    }
}
