Shader "Custom/DSPV_PhosShader"
{
    Properties
    {
       // _PhosheneMapping("PhospheneMapping", 2D) = "black" { }
       _ActivationMask ("ActivationMask", 2D) = "black" { }
       _SizeCoefficient ("SizeCoefficient", Range(0.001, 2)) = 0.03
       _Brightness ("Brightness", Range(0, 2)) = 0.005
       _Dropout("Dropout", Range(0.0,0.5)) = 0
       _PhospheneFilter("PhospheneFilter", Float) = 0
       _EyePosition("EyePosition", Vector) = (0., 0., 0., 0.)
       _GazeLocked("GazeLocked", Int) = 0
    }

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #include "DSPV_phosphene_vision.cginc"


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

            // Toggle phosphene filtering
            float _PhospheneFilter;

            // Texture that determines where phosphenes should be activated
            sampler2D _ActivationMask;
            float4 _ActivationMask_ST;
            float4 _ActivationMask_TexelSize;

            // Float array instead:
            float activation[1000];

            // Other parameters
            float _SizeCoefficient;
            float _Brightness;
            float _Dropout;

            // EyePosition
            float4 _EyePosition;
            int _GazeLocked;

            int _nPhosphenes;
            float4 _pSpecs[1000];

            StructuredBuffer<Phosphene> phosphenes;

            VertexData vertex_program(AppData inputs)
            {
                VertexData outputs;
                outputs.uv = TRANSFORM_TEX(inputs.uv, _ActivationMask);
                outputs.vertex = UnityObjectToClipPos(inputs.vertex);
                return outputs;
            }

            float4 frag(VertexData inputs) : SV_Target
            {

                if (_PhospheneFilter==1){
                  // Simulate phosphenes
                  return DSPV_phospheneSimulation(phosphenes, _GazeLocked, _EyePosition.rg, _nPhosphenes, _SizeCoefficient, _Brightness, _Dropout, inputs.uv);
                }
                else {
                  // Return the pixels in main texture
                  return tex2D(_ActivationMask, inputs.uv);
                }
            }
            ENDCG
        }
    }
}
