// ワープした時の集中線(画面全体に貼ったUIの板に、計算で描く)
// 漫画の集中線のように、画面の外周から中心へ向かって先の尖った線を並べる。白い芯に紺の縁を付けて、明るい背景でも見えるようにする
// 位置・太さ・縁の太さは画面の縦の長さに対する割合で決めるので、解像度(4Kなど)が変わっても見え方は変わらない
// 線の並び(_FocusSeed)と、線が中心へ入り込む量(_FocusProgress)は、WarpFocusLinesから渡す。濃さは頂点色のアルファ
Shader "Lulu/UI/WarpFocusLines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _LineColor ("Line Color", Color) = (1,1,1,1)
        _EdgeColor ("Edge Color", Color) = (0.11,0.16,0.43,1)
        _LineCount ("Line Count", Float) = 72          // 一周に並べる線の数
        _Density ("Density", Range(0,1)) = 0.8         // 線を引く割合(残りは隙間)
        _InnerMin ("Inner Radius Min", Float) = 0.55   // 線の先端が届く、中心からの距離(画面の縦の半分=1)
        _InnerMax ("Inner Radius Max", Float) = 0.85
        _TaperLength ("Taper Length", Float) = 0.5     // 先端から、最も太くなるまでの長さ
        _EdgeWidth ("Edge Width", Float) = 0.006       // 紺の縁の太さ(画面の縦の半分=1)
        _FocusSeed ("Seed", Float) = 0
        _FocusProgress ("Progress", Range(0,1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

        Stencil
        {
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

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float4 screenPos : TEXCOORD0;
            };

            fixed4 _LineColor;
            fixed4 _EdgeColor;
            float _LineCount;
            float _Density;
            float _InnerMin;
            float _InnerMax;
            float _TaperLength;
            float _EdgeWidth;
            float _FocusSeed;
            float _FocusProgress;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                OUT.color = v.color;
                return OUT;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 画面の中心を原点に、縦の半分を1とした座標(横長の画面でも線の形が歪まない)
                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                float2 p = (uv - 0.5) * 2.0;
                p.x *= _ScreenParams.x / _ScreenParams.y;
                float r = length(p);
                float angle = atan2(p.y, p.x) / (2.0 * UNITY_PI) + 0.5; // 0〜1

                // 一周を_LineCount個の区画に分け、区画ごとに線の有無・先端の位置・太さ・ずれを決める
                float cell = floor(angle * _LineCount);
                float local = frac(angle * _LineCount) - 0.5;
                float r1 = Hash(float2(cell, _FocusSeed));
                float r2 = Hash(float2(cell + 31.0, _FocusSeed));
                float r3 = Hash(float2(cell + 77.0, _FocusSeed));
                float r4 = Hash(float2(cell + 113.0, _FocusSeed));
                if (r1 > _Density) return 0;

                // 先端の位置(時間とともに少し中心へ入り込む)
                float inner = lerp(_InnerMin, _InnerMax, r2) - _FocusProgress * 0.08;
                // 先端で0、外へ向かって太くなる(区画の幅に対する割合)
                float max_half = lerp(0.18, 0.42, r3);
                float half_width = max_half * saturate((r - inner) / _TaperLength);
                float center = (r4 - 0.5) * (0.5 - max_half);

                // 線の輪郭からの距離を、画面上の長さ(縦の半分=1)に直す
                float cell_length = 2.0 * UNITY_PI * r / _LineCount;
                float dist = (abs(local - center) - half_width) * cell_length;
                if (r < inner) dist = max(dist, (inner - r));
                float aa = fwidth(dist);

                float body = 1.0 - smoothstep(-aa, aa, dist + _EdgeWidth); // 白い芯
                float shape = 1.0 - smoothstep(-aa, aa, dist);             // 縁を含めた形
                fixed4 col = lerp(_EdgeColor, _LineColor, body);
                col.a = shape * IN.color.a;
                return col;
            }
        ENDCG
        }
    }
}
