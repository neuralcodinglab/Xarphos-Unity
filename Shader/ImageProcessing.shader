Shader "Custom/ImageProcessing"
{
    Properties
    {
       // _PhosheneMapping("PhospheneMapping", 2D) = "black" { }
       _MainTex ("_MainTex", 2D) = "black" {}
       _Mode("Mode", Float) = 0.0
        _ResX("Resolution_x", Int) = 512
       _ResY("Resolution_y", Int) = 512
        _EyePositionLeft("_EyePositionLeft", Vector) = (0., 0., 0., 0.)
        _EyePositionRight("_EyePositionRight", Vector) = (0., 0., 0., 0.)
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

            struct AppData
            {
                float2 uv: TEXCOORD0;
                float4 vertex: POSITION;
            };

            struct VertexData
            {
                float2 uv: TEXCOORD0;
                float4 vertex: SV_POSITION;
            };


            // Texture that determines where phosphenes should be activated
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            // EyePosition
            float4 _EyePositionLeft;
            float4 _EyePositionRight;
            float _Mode;

            int _ResX;
            int _ResY;


            VertexData vertex_program(AppData inputs)
            {
                VertexData outputs;
                
                outputs.uv = TRANSFORM_TEX(inputs.uv, _MainTex);
                outputs.vertex = UnityObjectToClipPos(inputs.vertex);
                return outputs;
            }

            fixed4 frag(VertexData inputs) : SV_Target
            {                
              if (_Mode==1){
                // Sobel Edge detection

                // sample the texture
        				float2 offsets[9];
        				GetOffsets3x3(_ResX, _ResY, offsets);

        				fixed3 textures[9];
        				for (int j = 0; j < 9; j++)
        				{
        					textures[j] = tex2D(_MainTex, inputs.uv + offsets[j]).rgb;
        				}

        				fixed4 FragColor = ApplySobel(textures);
                float bright = 10 * Luminance(FragColor);
                return bright;
              }

              if (_Mode==2){
                // Edge detection

                // sample the texture
                float2 offsets[9];
                GetOffsets3x3(_ResX, _ResY, offsets);

                fixed3 textures[9];
                for (int j = 0; j < 9; j++)
                {
                  textures[j] = tex2D(_MainTex, inputs.uv + offsets[j]).rgb;
                }

                float threshold = 0.1;
                fixed4 FragColor = ApplyRobert(textures);
                float bright = 10 * Luminance(FragColor);
                return bright;
              }


              return tex2D(_MainTex, inputs.uv);
            }
            ENDCG
        }
    }
}
