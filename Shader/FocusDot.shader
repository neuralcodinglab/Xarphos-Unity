Shader "Custom/FocusDot"
{
    Properties
    {
        _EyePositionLeft("_EyePositionLeft", Vector) = (0., 0., 0., 0.)
        _EyePositionRight("_EyePositionRight", Vector) = (0., 0., 0., 0.)
        _EyeToRender("_EyeToRender", Int) = 0
        _MainTex ("_MainTex", 2D) = "black" {}
    }

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            float4 _EyePositionLeft;
            float4 _EyePositionRight;
            int _EyeToRender;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            
            #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }
            
            fixed4 frag(v2f  i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                fixed4 eyepos = lerp(_EyePositionLeft, _EyePositionRight, _EyeToRender);

                if (distance(i.uv, eyepos.rg) < 0.005)
                {
                    col = fixed4(1,0,0,0);
                }
                return col;
            }
            ENDCG
        }
    }
}
