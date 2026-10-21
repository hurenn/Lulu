// Lulu用の自作スプライトシェーダー(SpriteRenderer・TilemapRenderer向け)の共通処理
// Unity 6のURPでは、SpriteRendererの色(color)はunity_SpriteColorで、左右反転(flipX/Y)はunity_SpritePropsで
// シェーダーへ渡される(頂点カラーには入らない)。URPのSprite-Unlit-Defaultと同じ方法でこれらを反映する
#ifndef LULU_SPRITE_INCLUDED
#define LULU_SPRITE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;

struct LuluSpriteAttributes {
    float3 positionOS : POSITION;
    float4 color      : COLOR;
    float2 uv         : TEXCOORD0;
    UNITY_SKINNED_VERTEX_INPUTS
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct LuluSpriteVaryings {
    float4 positionCS : SV_POSITION;
    half4  color      : COLOR;      // 頂点カラー × SpriteRendererの色
    float2 uv         : TEXCOORD0;
    float2 positionWS : TEXCOORD1;  // ワールド座標(模様をワールド座標で作る時に使う)
    UNITY_VERTEX_OUTPUT_STEREO
};

LuluSpriteVaryings LuluSpriteVertex(LuluSpriteAttributes v) {
    LuluSpriteVaryings o = (LuluSpriteVaryings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    UNITY_SKINNED_VERTEX_COMPUTE(v);

    v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
    o.positionCS = TransformObjectToHClip(v.positionOS);
    o.positionWS = TransformObjectToWorld(v.positionOS).xy;
    o.uv = v.uv;
    o.color = v.color * unity_SpriteColor;
    return o;
}

half4 LuluSampleMain(float2 uv) {
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
}

// ---- ノイズ ----
float LuluHash(float2 p) {
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float LuluValueNoise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(LuluHash(i), LuluHash(i + float2(1, 0)), u.x),
                lerp(LuluHash(i + float2(0, 1)), LuluHash(i + float2(1, 1)), u.x), u.y);
}

float LuluFbm(float2 p) {
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++) {
        v += a * LuluValueNoise(p);
        p = p * 2.05 + float2(13.3, 7.9);
        a *= 0.5;
    }
    return v;
}

#endif
