Shader "Custom/DSPV_PhosShader"
{
    Properties
    {
       // _PhosheneMapping("PhospheneMapping", 2D) = "black" { }
        _MainTex ("_MainTex", 2D) = "black" { }
        _SizeCoefficient ("SizeCoefficient", Range(0.001, 2)) = 0.03
        _Brightness ("Brightness", Range(0, 2)) = 0.005
        _Dropout("Dropout", Range(0.0,0.5)) = 0
        _PhospheneFilter("PhospheneFilter", Float) = 0
        _EyePositionLeft("_EyePositionLeft", Vector) = (0.5, 0.5, 0., 0.)
        _EyePositionRight("_EyePositionRight", Vector) = (0.5, 0.5, 0., 0.)
        _GazeLocked("_GazeLocked", Int) = 0
        _GazeAssisted("_GazeAssisted", Int) = 0
        _ScreenResolutionX("_ScreenResolution", Int) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" }
        
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
                float4 screenPos: TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Toggle phosphene filtering
            float _PhospheneFilter;

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            float4 _MainTex_ST;

            // Float array instead:
            float activation[1000];

            // Other parameters
            float _SizeCoefficient;
            float _Brightness;
            float _Dropout;

            // EyePosition
            float4 _EyePositionLeft;
            float4 _EyePositionRight;
            int _GazeLocked;
            int _GazeAssisted;

            int _nPhosphenes;
            float4 _pSpecs[1000];

            StructuredBuffer<Phosphene> phosphenes;
            StructuredBuffer<float> pixelLuminance;
            int _ScreenResolutionX;

            VertexData vertex_program(AppData inputs)
            {
                VertexData outputs;

                UNITY_SETUP_INSTANCE_ID(inputs);
                UNITY_INITIALIZE_OUTPUT(VertexData, outputs);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(outputs);
                
                outputs.uv = TRANSFORM_TEX(inputs.uv, _MainTex);
                outputs.vertex = UnityObjectToClipPos(inputs.vertex);
                outputs.screenPos = ComputeScreenPos(outputs.vertex);
                
                return outputs;
            }

            float4 frag(VertexData inputs) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(inputs);
                fixed4 eyepos = lerp(_EyePositionLeft, _EyePositionRight, unity_StereoEyeIndex);

                // int idx = inputs.screenPos.x + inputs.screenPos.y * _ScreenResolutionX;
                // return fixed4(1,1,1,1) * pixelLuminance[idx] * _Brightness;

                // Simulate phosphenes
                return DSPV_phospheneSimulation(
                    phosphenes,
                    _GazeLocked,
                    eyepos.rg,
                    _nPhosphenes,
                    _SizeCoefficient,
                    _Brightness,
                    inputs.uv
                );
            }
            ENDCG
        }
    }
}
