Shader "Custom/DSPV_PhosShader"
{
    Properties
    {
        _ActivationMask ("ActivationMask", 2D) = "black" { }
        _MainTex ("_MainTex", 2D) = "black" { }
        _SizeCoefficient ("SizeCoefficient", Range(0.001, 2)) = 0.03
        _Brightness ("Brightness", Range(0, 2)) = 0.005
        _Dropout("Dropout", Range(0.0,0.5)) = 0
        _PhospheneFilter("PhospheneFilter", Float) = 0
        _EyePositionLeft("_EyePositionLeft", Vector) = (0., 0., 0., 0.)
        _EyePositionRight("_EyePositionRight", Vector) = (0., 0., 0., 0.)
        _EyeToRender("_EyeToRender", Int) = 0
        _GazeLocked("_GazeLocked", Int) = 0
        _GazeAssisted("_GazeAssisted", Int) = 0
        _MaskResFracX("Resolution_x", Float) = .5
        _MaskResFracY("Resolution_y", Float) = .5
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
            // UNITY_DECLARE_SCREENSPACE_TEXTURE(_ActivationMask);
            // float4 _ActivationMask_ST;
            // float4 _ActivationMask_TexelSize;
            sampler2D _MainTex;
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
            int _EyeToRender;
            int _GazeLocked;
            int _GazeAssisted;

            int _MaskResFracX;
            int _MaskResFracY;

            int _nPhosphenes;
            float4 _pSpecs[1000];

            StructuredBuffer<Phosphene> phosphenes;

            VertexData vertex_program(AppData inputs)
            {
                VertexData outputs;
                
                outputs.uv = TRANSFORM_TEX(inputs.uv, _MainTex);
                outputs.vertex = UnityObjectToClipPos(inputs.vertex);
                
                return outputs;
            }

            float4 frag(VertexData inputs) : SV_Target
            {
                fixed4 eyepos = lerp(_EyePositionLeft, _EyePositionRight, _EyeToRender);

                // Simulate phosphenes
                return DSPV_phospheneSimulation(
                    phosphenes,
                    _GazeLocked,
                    eyepos.rg,
                    unity_StereoEyeIndex,
                    _nPhosphenes,
                    _SizeCoefficient,
                    _Brightness,
                    _Dropout,
                    inputs.uv
                );
            }
            ENDCG
        }
    }
}
