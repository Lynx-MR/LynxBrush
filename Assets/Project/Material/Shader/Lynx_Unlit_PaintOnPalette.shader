Shader "Lynx/Unlit/PaintOnPalette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff("alpha cutout", Range(0.0,1.0)) = 0.5
        _Color("Color", Color) = (1.0,1.0,1.0,1.0)
        _atlasSize("atlas size", Vector) = (0.0,0.0,0.0,0.0)
        _idx ("Atlas index", float) = 0
    }
    SubShader
    {
        Tags {
            "Queue"      = "AlphaTest"
            "RenderType" = "TransparentCutout"
        }
        LOD 100
        Cull Off
        ZWrite On
        AlphaTest Greater [_Cutoff]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag alphatest:_Cutoff
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;
            float4 _atlasSize;
            float _idx;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture

                float2 nuv = i.uv;
                nuv /= _atlasSize.xy;
                nuv.x += floor(_idx%_atlasSize.y) * (1.0/_atlasSize.x);
                nuv.y += floor(_idx/_atlasSize.x) * (1.0/_atlasSize.y);
                fixed4 col = tex2D(_MainTex, nuv);
                col.a = col.x;
                col.rgb = _Color.rgb;
                clip(col.a - _Cutoff);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
