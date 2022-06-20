﻿Shader "Custom/ImageProcessing"
{
    Properties
    {
       // _PhosheneMapping("PhospheneMapping", 2D) = "black" { }
       _MainTex ("_MainTex", 2D) = "black" {}
       _ResX("Resolution_x", Int) = 512
       _ResY("Resolution_y", Int) = 512
    }

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #include "PostFunctions.cginc"


            #pragma vertex vertex_program
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            struct AppData
            {
                float2 uv: TEXCOORD0;
                float4 vertex: POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexData
            {
                float2 uv: TEXCOORD0;
                float4 vertex: SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };


            // Texture that determines where phosphenes should be activated
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            float4 _MainTex_ST;

            int _ResX;
            int _ResY;


            VertexData vertex_program(AppData inputs)
            {
                VertexData outputs;

                UNITY_SETUP_INSTANCE_ID(inputs);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(outputs);
                
                outputs.uv = TRANSFORM_TEX(inputs.uv.xy, _MainTex);
                outputs.vertex = UnityObjectToClipPos(inputs.vertex);
                return outputs;
            }

            fixed4 frag(VertexData inputs) : SV_Target
            {
                // Sobel Edge detection
                // sample the texture
        		float2 offsets[9];
        		GetOffsets3x3(_ResX, _ResY, offsets);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(inputs);

        		fixed3 textures[9];
        		for (int j = 0; j < 9; j++)
        		{
        			textures[j] = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, inputs.uv + offsets[j]).rgb;
        		}

        		fixed4 FragColor = ApplySobel(textures);
                float bright = 10 * Luminance(FragColor);
                return bright;
            }
            ENDCG
        }
    }
}
