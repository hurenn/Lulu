// 草花や垂れた配線を風で揺らすスプライト用シェーダー(光の影響を受けない描き方)
// 頂点を横にずらして揺らす。絵の下端(または上端)は固定し、反対側ほど大きく動く。
// 揺れのタイミングはワールド座標のxでずらすので、同じTilemapに並んだ草でも風が横へ流れていくように見える。
// (AllIn1SpriteShaderの草の揺れは絵の下端固定しか選べず、Tilemap全体が同じタイミングで揺れるため、こちらを使う)
Shader "Lulu/Sprite/Sway"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SwayAmount ("揺れ幅(ワールド単位)", Range(0, 1)) = 0.12
        _SwaySpeed ("揺れの速さ", Range(0, 10)) = 2
        _SwayPhaseScale ("位置によるずれ(1ユニットあたり)", Range(0, 2)) = 0.35
        _GustAmount ("突風の強さ", Range(0, 1)) = 0.5
        [Toggle] _HangFromTop ("上端を固定する(垂れ下がる物)", Float) = 0
        _PushRadius ("ルルに押しのけられる範囲(ワールド単位)", Range(0, 3)) = 0.9
        _PushAmount ("ルルに押しのけられる強さ(ワールド単位)", Range(0, 1)) = 0.3
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _SwayAmount, _SwaySpeed, _SwayPhaseScale, _GustAmount, _HangFromTop;
            float _PushRadius, _PushAmount;
            // ルルの位置(xy)と動きの速さ(z: 0=止まっている〜1=ダッシュ)、有効か(w)。StageAmbienceが毎フレーム全体に渡す
            float4 _LuluPos;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                // 固定する端からの距離(0〜1)。タイルの絵は1枚の画像なのでUVのyがそのまま使える
                float weight = _HangFromTop > 0.5 ? (1.0 - v.uv.y) : v.uv.y;
                float t = _Time.y * _SwaySpeed;
                float phase = world.x * _SwayPhaseScale;
                // ゆっくりした揺れ + ときどき強くなる突風
                float gust = 1.0 + _GustAmount * saturate(sin(t * 0.23 + phase * 0.5) * 1.6 - 0.6);
                float offset = sin(t + phase) * _SwayAmount * gust;
                // ルルの近くでは、ルルから遠ざかる向きに押しのける(速く動いているほど強い)
                float2 d = world.xy - _LuluPos.xy;
                float near = saturate(1.0 - abs(d.x) / _PushRadius) * saturate(1.0 - abs(d.y) / (_PushRadius * 1.6));
                offset += sign(d.x) * near * _PushAmount * (1.0 + _LuluPos.z * 1.5) * _LuluPos.w; // w=1の時だけ有効(ルルがいない場面では押さない)
                world.x += offset * weight;
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
        ENDCG
        }
    }
}
