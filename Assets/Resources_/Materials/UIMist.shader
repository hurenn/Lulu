// 画面全体を覆うUIの霧(UI/Mist)の見た目: もやっとした濃淡がゆっくり流れる
// ・Imageの色・透明度がそのまま霧の色・平均の濃さになる(LightHiddenObjectのフェードもそのまま効く)
// ・濃い所ほど明るい色(_DenseColor)に寄せ、薄い所はところどころ透ける
// ・模様はワールド座標で作る(MistOverlayがカメラ位置を渡す)ので、画面にくっつかずに景色と一緒に流れる
Shader "Lulu/UIMist" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _DenseColor("Dense Color", Color) = (1, 0.78, 0.95, 1)  // 霧の濃い所の色
        _DenseMix("Dense Mix", Range(0, 1)) = 0.4               // 濃い所をどれだけ_DenseColorに寄せるか
        _MinDensity("Min Density", Range(0, 1)) = 0.25          // 薄い所の濃さ(Imageの透明度に掛ける)
        _MaxDensity("Max Density", Range(1, 3)) = 1.4           // 濃い所の濃さ(Imageの透明度に掛ける)
        _NoiseScale("Noise Scale", Float) = 0.08                // 霧の模様の大きさ(小さいほど大きな模様)
        _FlowSpeed("Flow Speed", Vector) = (0.6, 0.12, 0, 0)    // 霧が流れる速さ(ワールド単位/秒)

        // MistOverlayが毎フレーム設定する
        [HideInInspector] _CameraPos("Camera Pos", Vector) = (0, 0, 0, 0)
        [HideInInspector] _ViewHeight("View Height (world)", Float) = 10

        // UIのマスク(Stencil)用。UI/Defaultと同じ
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }
    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
            };

            half4 _DenseColor;
            half _DenseMix, _MinDensity, _MaxDensity;
            float _NoiseScale, _ViewHeight;
            float4 _FlowSpeed, _CameraPos;

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
                for (int i = 0; i < 3; i++) { // 細かい模様が出るとまだら模様に見えるので3層まで
                    v += a * ValueNoise(p);
                    p = p * 2.05 + float2(13.3, 7.9);
                    a *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes v) {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target {
                float t = _Time.y;

                // 画面上の位置 → ワールド座標(画面中心=カメラ位置、画面の高さ=_ViewHeight)
                float2 screen = i.positionCS.xy / _ScreenParams.xy - 0.5;
                screen.x *= _ScreenParams.x / _ScreenParams.y;
                float2 world = screen * _ViewHeight + _CameraPos.xy;

                // 霧の濃淡(大小2層を少しずらした向きに流して、うごめくように見せる)
                float2 flow = _FlowSpeed.xy * t;
                float fog_a = Fbm((world - flow) * _NoiseScale);
                float fog_b = Fbm((world - float2(flow.x * 1.6, -flow.y)) * _NoiseScale * 1.6 + float2(4.1, 9.3));
                float fog = smoothstep(0.2, 0.85, fog_a * 0.65 + fog_b * 0.45);

                half3 rgb = lerp(i.color.rgb, _DenseColor.rgb, fog * _DenseMix);
                half alpha = i.color.a * lerp(_MinDensity, _MaxDensity, fog);
                return half4(rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
