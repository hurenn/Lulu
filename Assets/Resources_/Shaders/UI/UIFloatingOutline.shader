// UIの画像に白いアウトラインと浮いたような影を付けるシェーダー(UI/Defaultと同じ描き方)
// UIFloatingOutlineが画像のメッシュを外側へ広げ、TEXCOORD1に「線の太さ・影のずれをUVに直した値」、
// TEXCOORD2に「絵の範囲(UV)」を入れる。シェーダーは絵の周りを見て、線と影を絵の外側だけに描く。
// (標準のOutline/Shadowは絵の色に頂点色を掛けるため白い線が作れず、半透明の窓では下に敷いた線が透けてしまう)
Shader "Lulu/UI/FloatingOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.05,0.05,0.15,0.45)

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
                float4 offsets  : TEXCOORD1; // xy: 線の太さ(UV), zw: 影のずれ(UV)
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
            fixed4 _OutlineColor;
            fixed4 _ShadowColor;
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

            // 8方向の周りの透明度の最大値(絵を太らせた形)
            half DilatedAlpha(float2 uv, float2 width, float4 rect)
            {
                const float D = 0.7071;
                half a = SampleAlpha(uv + float2( width.x, 0), rect);
                a = max(a, SampleAlpha(uv + float2(-width.x, 0), rect));
                a = max(a, SampleAlpha(uv + float2(0,  width.y), rect));
                a = max(a, SampleAlpha(uv + float2(0, -width.y), rect));
                a = max(a, SampleAlpha(uv + float2( width.x,  width.y) * D, rect));
                a = max(a, SampleAlpha(uv + float2(-width.x,  width.y) * D, rect));
                a = max(a, SampleAlpha(uv + float2( width.x, -width.y) * D, rect));
                a = max(a, SampleAlpha(uv + float2(-width.x, -width.y) * D, rect));
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

                // 白い線(絵を太らせた形)と、その形を下へずらした影
                half texA = SampleAlpha(IN.texcoord, IN.uvRect);
                half outlineA = max(texA, DilatedAlpha(IN.texcoord, IN.offsets.xy, IN.uvRect));
                float2 shadowUV = IN.texcoord - IN.offsets.zw;
                half shadowA = max(SampleAlpha(shadowUV, IN.uvRect), DilatedAlpha(shadowUV, IN.offsets.xy, IN.uvRect));
                outlineA *= _OutlineColor.a * IN.color.a;
                shadowA *= _ShadowColor.a * IN.color.a;

                // 影の上に線、その上に絵を重ねる(乗算済みアルファで合成)
                // 線と影は絵の外側だけに置く(半透明の窓の下に敷くと、窓が白っぽく透けるため)
                half3 underRGB = _OutlineColor.rgb * outlineA + _ShadowColor.rgb * shadowA * (1 - outlineA);
                half underA = outlineA + shadowA * (1 - outlineA);
                // 窓の中央のように絵そのものが半透明な所も内側として扱う(不透明度25%以上で内側)
                half outside = 1 - saturate(texA * 4);
                half4 color;
                color.rgb = base.rgb * base.a + underRGB * outside;
                color.a = base.a + underA * outside;

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
