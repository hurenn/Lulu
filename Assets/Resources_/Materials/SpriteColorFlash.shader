// スプライトの形のまま、指定の色で光らせる(加算合成)
// 色と強さはSpriteRenderer.colorで決める。元のスプライトに重ねて使う
Shader "Lulu/SpriteColorFlash" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
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

            half4 frag(LuluSpriteVaryings i) : SV_Target {
                return half4(i.color.rgb, LuluSampleMain(i.uv).a * i.color.a);
            }
            ENDHLSL
        }
    }
}
