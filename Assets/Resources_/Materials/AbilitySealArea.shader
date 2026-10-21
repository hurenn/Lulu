// 能力が使えないエリアの見た目: 闇の能力由来の、黒く淀んで揺らぐもや
// ・中はノイズを歪ませた黒〜暗い紫の淀みがゆっくりうごめく
// ・縁はノイズで揺らぎながら、外側へぼやけて消える
// エリアの範囲(ワールド座標)はAbilitySealAreaが_AreaRectに渡す(大きさが変わっても縁の太さを揃えるため)
Shader "Lulu/AbilitySealArea" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _ColorDark("Dark Color", Color) = (0, 0, 0, 1)                 // 淀みの暗い部分
        _ColorMid("Murk Color", Color) = (0.16, 0.05, 0.24, 1)         // 淀みの明るい部分(暗い紫)
        _Alpha("Alpha", Range(0, 1)) = 0.85                            // 全体の濃さ
        _NoiseScale("Noise Scale", Float) = 0.45                       // 淀みの模様の細かさ
        _FlowSpeed("Flow Speed", Float) = 0.12                         // 淀みがうごめく速さ
        _EdgeWidth("Edge Width", Float) = 0.7                          // 縁がぼやける幅(ワールド単位)
        _EdgeWobble("Edge Wobble", Float) = 0.35                       // 縁の揺らぎの大きさ(ワールド単位)
        _EdgeWobbleSpeed("Edge Wobble Speed", Float) = 0.5             // 縁が揺らぐ速さ
        _AreaRect("Area Rect (minX, minY, maxX, maxY)", Vector) = (-0.5, -0.5, 0.5, 0.5)
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColorDark, _ColorMid;
            half _Alpha;
            float _NoiseScale, _FlowSpeed, _EdgeWidth, _EdgeWobble, _EdgeWobbleSpeed;
            float4 _AreaRect;

            struct appdata {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            float Hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            float Fbm(float2 p) {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++) {
                    v += a * ValueNoise(p);
                    p = p * 2.03 + float2(17.1, 9.2);
                    a *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y;
                float2 p = i.worldPos * _NoiseScale;

                // ノイズで座標を歪ませて、ゆっくり渦を巻くような淀みにする
                float2 q = float2(Fbm(p + float2(0.0, t * _FlowSpeed)),
                                  Fbm(p + float2(5.2, -t * _FlowSpeed * 0.8)));
                float murk = Fbm(p + q * 2.2 + float2(t * _FlowSpeed * 0.5, 0.0));
                murk = saturate((murk - 0.3) * 1.8);
                fixed3 rgb = lerp(_ColorDark.rgb, _ColorMid.rgb, murk * murk);

                // 縁: エリアの端までの距離(ワールド単位)をノイズで揺らし、外側へぼかす
                // (縦横それぞれでぼかして掛け合わせ、角を丸くする。揺らいでも描画範囲の端で切れないよう、端の手前で必ず消す)
                float2 to_min = i.worldPos - _AreaRect.xy;
                float2 to_max = _AreaRect.zw - i.worldPos;
                float2 raw = min(to_min, to_max);
                float2 wobble = float2(
                    Fbm(i.worldPos * 0.9 + float2(t * _EdgeWobbleSpeed, -t * _EdgeWobbleSpeed * 0.7)),
                    Fbm(i.worldPos * 0.9 + float2(31.7 - t * _EdgeWobbleSpeed * 0.8, 12.3 + t * _EdgeWobbleSpeed))) - 0.5;
                float2 dist = raw + wobble * 2.0 * _EdgeWobble;
                float edge = smoothstep(0.0, _EdgeWidth, dist.x) * smoothstep(0.0, _EdgeWidth, dist.y);
                edge *= smoothstep(0.0, 0.3, raw.x) * smoothstep(0.0, 0.3, raw.y);

                // 淀みの薄い所はわずかに透けさせる
                half alpha = _Alpha * edge * lerp(0.85, 1.0, 1.0 - murk);
                return fixed4(rgb * i.color.rgb, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
