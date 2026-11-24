// UIの画像の周りに、ぼんやり光るアウトラインを描くシェーダー(メインメニューで選択中の家具の強調などに使う)
// UIGlowOutlineが画像のメッシュを外側へ広げ、TEXCOORD1のxyに「光の幅をUVに直した値」、
// TEXCOORD2に「絵の範囲(UV)」を入れる(UIFloatingOutlineと同じ広げ方)。
// 光は絵の外側だけに置き、絵に近いほど明るい細い芯と、外へ薄くなる光の2層で描く。
// AllIn1SpriteShaderのアウトライン(グロー)は絵の切り抜き枠の内側にしか描けず、
// 家具の絵は枠ぎりぎり(箱は画面端で切れている)なので光が欠けるため使わない
Shader "Lulu/UI/GlowOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,0.92,0.5,1)
        _CoreColor ("Core Color", Color) = (1,1,0.9,1)
        _CoreWidth ("Core Width (rate of glow width)", Range(0.05, 1)) = 0.2
        _Strength ("Strength", Range(0, 2)) = 1
        _WidthScale ("Width Scale (rate of mesh margin)", Range(0, 1)) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 offsets  : TEXCOORD1; // xy: 光の幅(UV)
                float4 uvRect   : TEXCOORD2; // 絵の範囲(UVの最小xy・最大xy)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                half4  mask : TEXCOORD2;
                float4 offsets : TEXCOORD3;
                float4 uvRect : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _GlowColor;
            fixed4 _CoreColor;
            float _CoreWidth;
            float _Strength;
            float _WidthScale;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.color = v.color * _Color;
                OUT.offsets = v.offsets;
                OUT.uvRect = v.uvRect;
                return OUT;
            }

            // 絵の範囲の外は透明として扱う(メッシュを広げた部分や、アトラスの隣の絵を拾わないため)
            half SampleAlpha(float2 uv, float4 rect)
            {
                float2 inside = step(rect.xy, uv) * step(uv, rect.zw);
                return tex2D(_MainTex, uv).a * inside.x * inside.y;
            }

            // 12方向の周りの透明度の最大値(絵を太らせた形)
            half DilatedAlpha(float2 uv, float2 width, float4 rect)
            {
                half a = 0;
                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    float ang = i * (6.2831853 / 12.0);
                    a = max(a, SampleAlpha(uv + float2(cos(ang), sin(ang)) * width, rect));
                }
                return a;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                float2 inside = step(IN.uvRect.xy, IN.texcoord) * step(IN.texcoord, IN.uvRect.zw);
                half4 base = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                base.a *= inside.x * inside.y;

                // 太らせる幅を4段階に変えて重ね、絵から離れるほど薄くなる光を作る
                // 幅はメッシュを広げた最大幅に_WidthScaleを掛けて伸び縮みさせる(メッシュは作り直さない)
                float2 width = IN.offsets.xy * _WidthScale;
                half glow = 0;
                [unroll]
                for (int r = 1; r <= 4; r++)
                {
                    glow += DilatedAlpha(IN.texcoord, width * (r / 4.0), IN.uvRect);
                }
                glow *= 0.25;
                glow *= glow; // 外側ほど急に弱める

                // 絵のすぐ外側の明るい芯
                half core = DilatedAlpha(IN.texcoord, width * _CoreWidth, IN.uvRect);

                half texA = SampleAlpha(IN.texcoord, IN.uvRect);
                half outside = 1 - saturate(texA * 4);
                half strength = _Strength * IN.color.a * outside;

                half glowA = saturate(glow * _GlowColor.a * strength);
                half coreA = saturate(core * _CoreColor.a * strength);
                // 光の上に芯を重ねる(乗算済みアルファ)。光は加算寄りに見せるため不透明度を控えめにする
                half3 lightRGB = _CoreColor.rgb * coreA + _GlowColor.rgb * glowA * (1 - coreA);
                half lightA = coreA + glowA * 0.6 * (1 - coreA);

                half4 color;
                color.rgb = base.rgb * base.a + lightRGB * (1 - base.a);
                color.a = base.a + lightA * (1 - base.a);

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
