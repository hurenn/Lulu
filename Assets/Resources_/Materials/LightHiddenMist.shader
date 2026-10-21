// LightHiddenObjectの見た目: 霧に覆われたような、モヤがかかった感じ
// ・白っぽい霧の色が、ゆっくり流れる濃淡でかぶさる
// ・ところどころ薄く透けるモヤが流れる
// ・輪郭がゆらぎながら、若干ぼやける
// SpriteRenderer・TilemapRendererどちらにも使える(色・透明度は頂点カラーを使うので、LightHiddenObjectのフェードもそのまま効く)
// 模様はワールド座標で作るので、タイルの継ぎ目でも途切れない
// 輪郭のぼかし:
// ・タイルマップ: タイルは絵の端まで不透明で外周が分からないので、LightHiddenObjectが作るタイルの有無のマスク(_MaskTex)で外周を求めてぼかす
// ・スプライト: 周りの透明度を平均してぼかす
Shader "Lulu/LightHiddenMist" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _FogColor("Fog Color", Color) = (0.86, 0.89, 0.94, 1)  // 霧の色
        _FogStrength("Fog Strength", Range(0, 1)) = 0.7        // 霧のかかり具合
        _Brightness("Brightness", Range(0, 2)) = 0.9           // 元の絵の明るさ(2Dライトの影響を受けないので、ここで合わせる)
        _WispAmount("Wisp Amount", Range(0, 1)) = 0.5          // モヤで透ける量
        _NoiseScale("Noise Scale", Float) = 0.35               // 霧の模様の大きさ(小さいほど大きな模様)
        _FlowSpeed("Flow Speed", Vector) = (0.3, 0.08, 0, 0)   // 霧が流れる速さ(ワールド単位/秒)
        _Distort("Distort (pixels)", Float) = 1.5              // 絵のゆらぎ(テクスチャのピクセル数。大きくするとタイルの継ぎ目が見えることがある)
        _EdgeSoftness("Edge Softness", Range(0.05, 0.5)) = 0.35 // 輪郭をぼかす幅(タイル: マスの何割か)
        _EdgeWobble("Edge Wobble", Range(0, 0.5)) = 0.22       // 輪郭のゆらぎの大きさ(タイル: マスの何割か)
        _EdgeWobbleSpeed("Edge Wobble Speed", Float) = 0.6     // 輪郭がゆらぐ速さ
        _SpriteBlur("Sprite Blur (pixels)", Float) = 3         // スプライトの輪郭をぼかす幅(テクスチャのピクセル数)
        [HideInInspector] _MaskTex("Tile Mask", 2D) = "white" {}
        [HideInInspector] _MaskRect("Mask Rect (minX, minY, sizeX, sizeY)", Vector) = (0, 0, 1, 1)
        [HideInInspector] _UseMask("Use Mask", Float) = 0
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
            float4 _MainTex_TexelSize;
            sampler2D _MaskTex;
            float4 _MaskRect;
            float _UseMask;
            fixed4 _FogColor;
            half _FogStrength, _Brightness, _WispAmount, _EdgeSoftness, _EdgeWobble;
            float _NoiseScale, _Distort, _EdgeWobbleSpeed, _SpriteBlur;
            float4 _FlowSpeed;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float2 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
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
                    p = p * 2.07 + float2(13.3, 7.9);
                    a *= 0.5;
                }
                return v;
            }

            // スプライトの輪郭のぼかし: 周り8方向の透明度を平均する
            half BlurredAlpha(float2 uv, half center) {
                float2 r = _SpriteBlur * _MainTex_TexelSize.xy;
                half sum = center * 2.0;
                sum += tex2D(_MainTex, uv + float2( r.x, 0)).a;
                sum += tex2D(_MainTex, uv + float2(-r.x, 0)).a;
                sum += tex2D(_MainTex, uv + float2(0,  r.y)).a;
                sum += tex2D(_MainTex, uv + float2(0, -r.y)).a;
                sum += tex2D(_MainTex, uv + r * 0.7071).a;
                sum += tex2D(_MainTex, uv - r * 0.7071).a;
                sum += tex2D(_MainTex, uv + float2(r.x, -r.y) * 0.7071).a;
                sum += tex2D(_MainTex, uv + float2(-r.x, r.y) * 0.7071).a;
                return sum / 10.0;
            }

            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y;
                float2 flow = _FlowSpeed.xy * t;
                float2 p = (i.worldPos - flow) * _NoiseScale;

                // 絵のゆらぎ: 数ピクセルだけUVをずらす(大きくずらすと、アトラスの隣の絵が見えてしまう)
                float2 wobble = float2(Fbm(i.worldPos * 1.3 + float2(t * 0.7, 0.0)),
                                       Fbm(i.worldPos * 1.3 + float2(7.7, -t * 0.6))) - 0.5;
                float2 uv = i.uv + wobble * 2.0 * _Distort * _MainTex_TexelSize.xy;
                fixed4 tex = tex2D(_MainTex, uv);

                // 霧の濃淡(2層を逆向きに流して、うごめくように見せる)
                float fog_a = Fbm(p);
                float fog_b = Fbm(p * 1.7 + float2(4.1, 9.3) + float2(-flow.x, flow.y) * _NoiseScale * 0.8);
                float fog = saturate(fog_a * 0.6 + fog_b * 0.6 - 0.1);

                half fog_amount = _FogStrength * lerp(0.6, 1.25, fog);
                fixed3 rgb = lerp(tex.rgb * _Brightness, _FogColor.rgb, saturate(fog_amount));

                // 輪郭: ゆらぎながら若干ぼかす
                float edge_noise = Fbm(i.worldPos * 1.6 + float2(t * _EdgeWobbleSpeed, -t * _EdgeWobbleSpeed * 0.8)) - 0.5;
                half alpha;
                if (_UseMask > 0.5) {
                    // タイルの有無のマスクは、外周でちょうど0.5になる(1マス=1テクセルを線形補間しているため)
                    float2 mask_uv = (i.worldPos - _MaskRect.xy) / _MaskRect.zw;
                    float mask = tex2D(_MaskTex, mask_uv).a + edge_noise * 2.0 * _EdgeWobble;
                    alpha = tex.a * smoothstep(0.5, 0.5 + _EdgeSoftness, mask);
                } else {
                    float2 edge_uv = uv + edge_noise * 2.0 * _SpriteBlur * _MainTex_TexelSize.xy;
                    alpha = BlurredAlpha(edge_uv, tex2D(_MainTex, edge_uv).a);
                    alpha = smoothstep(0.1, 0.9, alpha);
                }

                // モヤの薄い所はところどころ透ける
                half wisp = lerp(1.0, smoothstep(0.15, 0.75, fog), _WispAmount);
                return fixed4(rgb * i.color.rgb, alpha * wisp * i.color.a);
            }
            ENDCG
        }
    }
}
