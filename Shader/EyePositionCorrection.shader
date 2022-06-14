Shader "Custom/EyePositionCorrection" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
        _EyePositionLeft("_EyePositionLeft", Vector) = (0.5, 0.5, 0., 0.)
        _EyePositionRight("_EyePositionRight", Vector) = (0.5, 0.5, 0., 0.)
		_EyePositionCentre("_EyePositionCentre", Vector) = (0.5, 0.5, 0., 0.)
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
            #pragma multi_compile_instancing

            float4 _EyePositionLeft;
            float4 _EyePositionRight;
            float4 _EyePositionCentre;
            
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            
            #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }
            
            fixed4 frag(v2f  i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 eyepos = lerp(_EyePositionLeft, _EyePositionRight, unity_StereoEyeIndex);
                fixed4 offset = eyepos - _EyePositionCentre;

                fixed2 shiftedCoords = i.uv + offset.xy;
                int2 meep = shiftedCoords > 1.0h;
                int2 moop = shiftedCoords < 0.0h;
                int valid = clamp(meep.x + meep.y + moop.x + moop.y, 0, 1);
                
                fixed4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, shiftedCoords);

                return lerp(col, fixed4(0,0,0,0), valid);
            }
            ENDCG
        }
    }
}