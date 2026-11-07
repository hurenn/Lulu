// コイン取得時などの虹色の波紋: 四角い板の中心から虹色の輪が広がって消える(ライトの影響を受けない)
// 広がり具合は_Progress(0〜1)で渡す。RainbowRippleがMaterialPropertyBlockで毎フレーム更新する
// 色相は輪の角度方向に並び、時間で回る。加算合成で光らせる
Shader "Lulu/RainbowRipple" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Progress("Progress", Range(0, 1)) = 0
        _StartWidth("Start Width", Range(0, 1)) = 0.25  // 広がり始めの輪の太さ(板の半径に対する割合)
        _EndWidth("End Width", Range(0, 1)) = 0.04      // 広がりきった時の輪の太さ
        _Glow("Glow", Range(0, 5)) = 1.6                // 明るさの倍率
        _Saturation("Saturation", Range(0, 1)) = 0.85   // 虹色の鮮やかさ
        _HueSpin("Hue Spin", Float) = 1.5               // 1秒間に色相が何周回るか
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass {
            HLSLPROGRAM
            #pragma vertex LuluSpriteVertex
            #pragma fragment frag
            #include "LuluSprite.hlsl"

            float _Progress, _StartWidth, _EndWidth, _HueSpin;
            half _Glow, _Saturation;

            half3 HueToRGB(float h) {
                half3 rgb = saturate(abs(frac(h + float3(0, 2.0 / 3.0, 1.0 / 3.0)) * 6 - 3) - 1);
                return lerp(half3(1, 1, 1), rgb, _Saturation);
            }

            half4 frag(LuluSpriteVaryings i) : SV_Target {
                float2 p = (i.uv - 0.5) * 2.0; // 中心0、板の端で1
                float d = length(p);
                // 始めは素早く、終わりにかけてゆっくり広がる
                float t = 1.0 - (1.0 - _Progress) * (1.0 - _Progress);
                float radius = t * 0.92;
                float width = lerp(_StartWidth, _EndWidth, _Progress);
                float ring = saturate(1.0 - abs(d - radius) / max(width, 1e-4));
                ring *= ring;
                float hue = atan2(p.y, p.x) * 0.1591549 + _Time.y * _HueSpin + d * 0.5;
                float fade = (1.0 - _Progress) * (1.0 - _Progress);
                return half4(HueToRGB(hue) * _Glow * i.color.rgb, ring * fade * i.color.a);
            }
            ENDHLSL
        }
    }
}
