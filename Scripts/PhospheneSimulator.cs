using UnityEngine;
using  UnityEngine.InputSystem;

namespace Xarphos.Scripts
{
    public class PhospheneSimulator : MonoBehaviour
    {
        public Camera targetCamera;


        // Image processing settings
        private float _phospheneFiltering;
        private float _edgeDetection;

        // EyeTracking
        protected Vector2 EyePosition;
        protected int GazeLocking; // used as boolean (sent to shader)
        protected bool CamLocking;

        // Image processing settings
        [SerializeField] protected SurfaceReplacement.ReplacementModes surfaceReplacementMode;
        private readonly int _nModes = System.Enum.GetValues(typeof(SurfaceReplacement.ReplacementModes)).Length;
        [SerializeField] protected Vector2Int resolution;

        // Render textures
        protected RenderTexture ActivationMask;

        // For reading phosphene configuration from JSON
        [SerializeField] private string phospheneConfigFile;
        private Phosphene[] _phosphenes;
        private int _nPhosphenes;
        private ComputeBuffer _phospheneBuffer;

        // Shaders and materials
        protected Material ImageProcessingMaterial;
        protected Material PhospheneMaterial;
        [SerializeField] protected Shader phospheneShader;
        [SerializeField] protected Shader imageProcessingShader;
        [SerializeField] protected ComputeShader temporalDynamicsCs;


        // stimulation parameters
        [SerializeField]
        private float inputEffect = 0.7f;// The factor by which stimulation accumulates to phosphene activation
        [SerializeField]
        private float intensityDecay = 0.8f; // The factor by which previous activation still influences current activation
        [SerializeField]
        private float traceIncrease = 0.1f; // The habituation strength: the factor by which stimulation leads to buildup of memory trace
        [SerializeField]
        private float traceDecay = 0.9f; // The factor by which the stimulation memory trace decreases
        
        #region Shader Properties Name-To-Int
        private static readonly int PhosMatMask = Shader.PropertyToID("_ActivationMask");
        private static readonly int PhosMatPhosphenes = Shader.PropertyToID("phosphenes");
        private static readonly int PhosMatNPhosphenes = Shader.PropertyToID("_nPhosphenes");
        private static readonly int PhosMatFilter = Shader.PropertyToID("_PhospheneFilter");
        private static readonly int PhosMatPosition = Shader.PropertyToID("_EyePosition");
        private static readonly int PhosMatGazeLocked = Shader.PropertyToID("_GazeLocked");
        
        private static readonly int TempDynResolution = Shader.PropertyToID("resolution");
        private static readonly int TempDynMask = Shader.PropertyToID("ActivationMask");
        private static readonly int TempDynInputEffect = Shader.PropertyToID("input_effect");
        private static readonly int TempDynIntensityDecay = Shader.PropertyToID("intensity_decay");
        private static readonly int TempDynTraceIncrease = Shader.PropertyToID("trace_increase");
        private static readonly int TempDynTraceDecay = Shader.PropertyToID("trace_decay");
        private static readonly int TempDynPhosphenes = Shader.PropertyToID("phosphenes");
        
        private static readonly int ImgProcMode = Shader.PropertyToID("_Mode");
        private static readonly int ImgMatMainTex = Shader.PropertyToID("_MainTex");

        #endregion

        protected void Awake()
        {
            targetCamera ??= GetComponent<Camera>();

            // Initialize the array of phosphenes
            _phosphenes = PhospheneConfig.InitPhosphenesFromJSON(phospheneConfigFile);
            _nPhosphenes = _phosphenes.Length;
            _phospheneBuffer = new ComputeBuffer(_nPhosphenes, sizeof(float)*5);
            _phospheneBuffer.SetData(_phosphenes);


            // Initialize materials with shaders
            PhospheneMaterial = new Material(phospheneShader);
            ImageProcessingMaterial = new Material(imageProcessingShader);// TODO

            // Initialize the render textures
            ActivationMask = new RenderTexture(resolution.x, resolution.y, 24);
            ActivationMask.enableRandomWrite = true;
            ActivationMask.Create();
            ImageProcessingMaterial.SetTexture(ImgMatMainTex, ActivationMask);

            // Set the shaders with the shared render textures
            temporalDynamicsCs.SetInts(TempDynResolution, new int[] {resolution.x, resolution.y});
            temporalDynamicsCs.SetTexture(0,TempDynMask, ActivationMask);
            PhospheneMaterial.SetTexture(PhosMatMask, ActivationMask);

            // Set the compute shader with the temporal dynamics variables
            temporalDynamicsCs.SetFloat(TempDynInputEffect, inputEffect);
            temporalDynamicsCs.SetFloat(TempDynIntensityDecay, intensityDecay);
            temporalDynamicsCs.SetFloat(TempDynTraceIncrease, traceIncrease);
            temporalDynamicsCs.SetFloat(TempDynTraceDecay, traceDecay);

            // Set the shader properties with the shared phosphene buffer
            PhospheneMaterial.SetBuffer(PhosMatPhosphenes, _phospheneBuffer);
            PhospheneMaterial.SetInt(PhosMatNPhosphenes, _nPhosphenes);
            PhospheneMaterial.SetFloat(PhosMatFilter, _phospheneFiltering);
            temporalDynamicsCs.SetBuffer(0,TempDynPhosphenes, _phospheneBuffer);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture target)
        {
          // Compute Phosphene Activity
          if ((int)_phospheneFiltering == 1)
          {
            Graphics.Blit(src, ActivationMask, ImageProcessingMaterial); // using imageProcessingShader
            temporalDynamicsCs.Dispatch(0, _phosphenes.Length / 10, 1, 1);
            // Simulate the phosphenes (based on the shared phosphene buffer)
            Graphics.Blit(null, target, PhospheneMaterial);
          }
          else
          {
            Graphics.Blit(src, target, ImageProcessingMaterial);
          }
        }

        private void OnDisable(){
          _phospheneBuffer.Release();
        }

        public void NextSurfaceReplacementMode(InputAction.CallbackContext ctx) => NextSurfaceReplacementMode();
        
        private void NextSurfaceReplacementMode(){
          surfaceReplacementMode = (SurfaceReplacement.ReplacementModes)((int)(surfaceReplacementMode + 1) % _nModes);
          // Replace surfaces with the surface replacement shader
          SurfaceReplacement.ActivateReplacementShader(targetCamera, surfaceReplacementMode);
        }

        public void TogglePhospheneSim(InputAction.CallbackContext ctx) => TogglePhospheneSim();

        public void TogglePhospheneSim()
        {
          _phospheneFiltering = 1-_phospheneFiltering;
          PhospheneMaterial.SetFloat(PhosMatFilter, _phospheneFiltering);
        }

        public void ToggleEdgeDetection(InputAction.CallbackContext ctx) => ToggleEdgeDetection();

        private void ToggleEdgeDetection()
        {
          _edgeDetection = 1-_edgeDetection;
          ImageProcessingMaterial.SetFloat(ImgProcMode, _edgeDetection);
        }
        
        public void ToggleCamLocking(InputAction.CallbackContext ctx) => ToggleCamLocking();

        public void ToggleCamLocking()
        {
          CamLocking = !CamLocking;
        }
        
        public void ToggleGazeLocking(InputAction.CallbackContext ctx) => ToggleGazeLocking();

        public void ToggleGazeLocking()
        {
          GazeLocking = 1-GazeLocking;
          PhospheneMaterial.SetInt(PhosMatGazeLocked, GazeLocking);
        }

        protected void Update()
        {
            if (Keyboard.current[Key.U].isPressed || Keyboard.current[Key.J].isPressed)
            {
                if (Keyboard.current[Key.U].isPressed) {EyePosition.y = 0.8f;}
                if (Keyboard.current[Key.J].isPressed) {EyePosition.y = 0.2f;}
            }
            else
            {
              EyePosition.y = 0.5f;
            }

            if (Keyboard.current[Key.H].isPressed || Keyboard.current[Key.K].isPressed)
            {
                if (Keyboard.current[Key.H].isPressed) {EyePosition.x = 0.2f;}
                if (Keyboard.current[Key.K].isPressed) {EyePosition.x = 0.8f;}
            }
            else
            {
              EyePosition.x = 0.5f;
            }

            PhospheneMaterial.SetVector(PhosMatPosition, EyePosition);
        }
    }
}
