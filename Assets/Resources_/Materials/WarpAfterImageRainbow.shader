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
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half _Blend, _Glow, _Saturation;
            float _StripeFreq, _ScrollSpeed;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            half3 HueToRGB(float h) {
                half3 rgb = saturate(abs(frac(h + float3(0, 2.0 / 3.0, 1.0 / 3.0)) * 6 - 3) - 1);
                return lerp(half3(1, 1, 1), rgb, _Saturation);
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float hue = (i.worldPos.x + i.worldPos.y) * _StripeFreq - _Time.y * _ScrollSpeed;
                half3 rgb = lerp(tex.rgb, HueToRGB(hue), _Blend) * _Glow;
                return fixed4(rgb * i.color.rgb, tex.a * i.color.a);
            }
            ENDCG
        }
    }
}
