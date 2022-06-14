using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using  UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Xarphos.Scripts
{
    public class PhospheneSimulator : MonoBehaviour
    {
        public Camera targetCamera;
        public bool manualEyePos;

        // Image processing settings
        [SerializeField]
        private float _phospheneFiltering;
        private bool _edgeDetection;

        // EyeTracking
        protected int GazeLocking; // used as boolean (sent to shader)
        protected bool CamLocking;
        protected bool RenderFocusPoint = true;

        // Image processing settings
        [SerializeField] protected EyeTracking.EyeTrackingConditions eyeTrackingCondition;
        [SerializeField] protected SurfaceReplacement.ReplacementModes surfaceReplacementMode;
        private readonly int _nSurfaceModes = Enum.GetValues(typeof(SurfaceReplacement.ReplacementModes)).Length;
        private readonly int _nEyeTrackingModes = Enum.GetValues(typeof(EyeTracking.EyeTrackingConditions)).Length;

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
        protected Material FocusDotMaterial;
        protected Material eyePosCorrectionMaterial;
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

        private Vector2Int viveResolution, maskResolution, cutOutResolution;
        private Vector2 screenSpacePart;
        private Vector2 eyePosLeft, eyePosRight, eyePosCentre;
        
        #region Shader Properties Name-To-Int
        private static readonly int PhosMatMask = Shader.PropertyToID("_ActivationMask");
        private static readonly int PhosMatPhosphenes = Shader.PropertyToID("phosphenes");
        private static readonly int PhosMatNPhosphenes = Shader.PropertyToID("_nPhosphenes");
        private static readonly int PhosMatFilter = Shader.PropertyToID("_PhospheneFilter");
        private static readonly int PhosMatGazeLocked = Shader.PropertyToID("_GazeLocked");
        private static readonly int PhosMatGazeAssisted = Shader.PropertyToID("_GazeAssisted");
          
        private static readonly int TempDynResolution = Shader.PropertyToID("resolution");
        private static readonly int TempDynMask = Shader.PropertyToID("ActivationMask");
        private static readonly int TempDynInputEffect = Shader.PropertyToID("input_effect");
        private static readonly int TempDynIntensityDecay = Shader.PropertyToID("intensity_decay");
        private static readonly int TempDynTraceIncrease = Shader.PropertyToID("trace_increase");
        private static readonly int TempDynTraceDecay = Shader.PropertyToID("trace_decay");
        private static readonly int TempDynPhosphenes = Shader.PropertyToID("phosphenes");
        private static readonly int TempDynGazeAssistedSampling = Shader.PropertyToID("gazeAssistedSampling");
        private static readonly int TempDynEyePos = Shader.PropertyToID("eyePos");
        
        private static readonly int ImgProcMode = Shader.PropertyToID("_Mode");
        private static readonly int ImgProcResX = Shader.PropertyToID("Resolution_x");
        private static readonly int ImgProcResY = Shader.PropertyToID("Resolution_y");
        private static readonly int ImgMatMainTex = Shader.PropertyToID("_MainTex");
        
        private static readonly int ShPropEyePosLeft = Shader.PropertyToID("_EyePositionLeft");
        private static readonly int ShPropEyePosRight = Shader.PropertyToID("_EyePositionRight");
        private static readonly int ShPropEyePosCentre = Shader.PropertyToID("_EyePositionCentre");
        private static readonly int ShPropEyeToRender = Shader.PropertyToID("_EyeToRender");

        #endregion

        protected void Awake()
        {
          // Vive Pro Resolution
          // resolution = new Vector2Int(1440, 1600);
          
            targetCamera ??= GetComponent<Camera>();

            // Initialize the array of phosphenes
            _phosphenes = PhospheneConfig.InitPhosphenesFromJSON(phospheneConfigFile);
            _nPhosphenes = _phosphenes.Length;
            _phospheneBuffer = new ComputeBuffer(_nPhosphenes, sizeof(float)*5);
            _phospheneBuffer.SetData(_phosphenes);

            // Initialize materials with shaders
            PhospheneMaterial = new Material(phospheneShader);
            ImageProcessingMaterial = new Material(imageProcessingShader);// TODO
            
            // Set the compute shader with the temporal dynamics variables
            temporalDynamicsCs.SetFloat(TempDynInputEffect, inputEffect);
            temporalDynamicsCs.SetFloat(TempDynIntensityDecay, intensityDecay);
            temporalDynamicsCs.SetFloat(TempDynTraceIncrease, traceIncrease);
            temporalDynamicsCs.SetFloat(TempDynTraceDecay, traceDecay);
            
            // Set the shader properties with the shared phosphene buffer
            PhospheneMaterial.SetBuffer(PhosMatPhosphenes, _phospheneBuffer);
            PhospheneMaterial.SetInt(PhosMatNPhosphenes, _nPhosphenes);
            PhospheneMaterial.SetFloat(PhosMatFilter, _phospheneFiltering);
            PhospheneMaterial.SetBuffer(PhosMatPhosphenes, _phospheneBuffer);
            PhospheneMaterial.SetInt(PhosMatNPhosphenes, _nPhosphenes);
            PhospheneMaterial.SetFloat(PhosMatFilter, _phospheneFiltering);

            temporalDynamicsCs.SetBuffer(0, TempDynPhosphenes, _phospheneBuffer);
            // Set the default EyeTrackingCondition (Ignore Gaze)
            PhospheneMaterial.SetInt(PhosMatGazeLocked, 0);
            temporalDynamicsCs.SetInt(TempDynGazeAssistedSampling, 0);

            FocusDotMaterial = new Material(Shader.Find("Custom/FocusDot"));
            eyePosCorrectionMaterial = new Material(Shader.Find("Custom/EyePositionCorrection"));
        }

        private void Start()
        {
          SurfaceReplacement.ActivateReplacementShader(targetCamera, surfaceReplacementMode);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture target)
        {
          if (target == null) return;
          // in between texture to put processed image on before blitting from this to target
          var preTargetPing = RenderTexture.GetTemporary(target.descriptor);
          // if phosphene simulator is off, only need to run image through image processing for edge detection
          if ((int) _phospheneFiltering == 0)
          {
            if (_edgeDetection)
              Graphics.Blit(src, preTargetPing, ImageProcessingMaterial);
            // if edge detection is off, just blit without any processing
            else
              Graphics.Blit(src, preTargetPing);
          }
          else
          {
            // determine rectangle for cut out and placement
            // point in the middle of the screen (never changes)
            Vector2 centrePxl = .5f * (Vector2)viveResolution;
            // pixel currently on eye position
            // ToDo: Invert eyeposcentre.y 
            Vector2 eyePosPxl = Vector2.Scale(eyePosCentre, viveResolution);;
            // since rectangles are defined by corner, width & height; calculate corner from centre
            Vector2Int cornerCentre = Vector2Int.RoundToInt(centrePxl - cutOutResolution / 2);
            Vector2Int cornerEyePos = Vector2Int.RoundToInt(eyePosPxl - cutOutResolution / 2);
            // short hands for width & height
            int w = cutOutResolution.x;
            int h = cutOutResolution.y;

            // texture to store cut out from camera view on
            var cutOutTexture = RenderTexture.GetTemporary(w, h, src.depth, src.graphicsFormat);

            // in assisted sampling the cut out happens around the eye position
            if (eyeTrackingCondition == EyeTracking.EyeTrackingConditions.GazeAssistedSampling)
            {
              Graphics.CopyTexture(
                src, 0, 0, cornerEyePos.x, cornerEyePos.y, w, h,
                cutOutTexture, 0, 0, 0, 0
              );
            } 
            // in the other conditions cut out happens from the center of the camera view
            else
            {
              Graphics.CopyTexture(
                src, 0, 0, cornerCentre.x, cornerCentre.y, w, h,
                cutOutTexture, 0, 0, 0, 0
              );
            }
            
            // blit to activation mask, effectively down-scaling the cut out for better performance
            Graphics.Blit(cutOutTexture, ActivationMask);
            var cutInTexture = new RenderTexture(cutOutTexture.descriptor);
            RenderTexture.ReleaseTemporary(cutOutTexture);
            // run simulation on scaled down cut out
            temporalDynamicsCs.Dispatch(0, _phosphenes.Length / 10, 1, 1);
            // Render Simulation back onto CutOut, ToDo: This might need to be a new temp texture
            // using texture of same size as cut out should avoid distortions
            Graphics.Blit(null, cutInTexture, PhospheneMaterial);
            
            // Copy the simulation of the cut-out back onto the screen
            // in gaze-ignored we render the simulation in the centre of the screen
            if (eyeTrackingCondition == EyeTracking.EyeTrackingConditions.GazeIgnored)
            {
              Graphics.CopyTexture(
                cutInTexture, 0, 0, 0, 0, w, h,
                preTargetPing, 0, 0, cornerCentre.x, cornerCentre.y
              );
            }
            // in the gaze-tracked conditions rendering is happening around the foveal point
            else
            {
              // using an extra render texture, because we will need to adjust for each eye's position
              var preTargetPong = RenderTexture.GetTemporary(target.descriptor);
              Graphics.CopyTexture(
                cutInTexture, 0, 0, 0, 0, w, h,
                preTargetPong, 0, 0, cornerEyePos.x, cornerEyePos.y
              );
              // using a shader to correct eye position // ToDo: Check reliability
              Graphics.Blit(preTargetPong, preTargetPing, eyePosCorrectionMaterial);
              RenderTexture.ReleaseTemporary(preTargetPong);
            }
            RenderTexture.ReleaseTemporary(cutInTexture);
          }
          
          // lastly render the focus dot on top
          Graphics.Blit(preTargetPing, target, FocusDotMaterial);

          RenderTexture.ReleaseTemporary(preTargetPing);
        }

        private void OnDisable(){
          _phospheneBuffer.Release();
        }

        public void NextSurfaceReplacementMode(InputAction.CallbackContext ctx) => NextSurfaceReplacementMode();
        
        private void NextSurfaceReplacementMode(){
          surfaceReplacementMode = (SurfaceReplacement.ReplacementModes)((int)(surfaceReplacementMode + 1) % _nSurfaceModes);
          // Replace surfaces with the surface replacement shader
          SurfaceReplacement.ActivateReplacementShader(targetCamera, surfaceReplacementMode);
        }

        public void NextEyeTrackingCondition(InputAction.CallbackContext ctx) => NextEyeTrackingCondition();

        private void NextEyeTrackingCondition()
        {
          eyeTrackingCondition = (EyeTracking.EyeTrackingConditions)((int)(eyeTrackingCondition + 1) % _nEyeTrackingModes);

          switch (eyeTrackingCondition)
          {
            // reset and don't use gaze info
            case EyeTracking.EyeTrackingConditions.GazeIgnored:
              PhospheneMaterial.SetInt(PhosMatGazeLocked, 0);
              PhospheneMaterial.SetInt(PhosMatGazeAssisted, 0);
              temporalDynamicsCs.SetInt(TempDynGazeAssistedSampling, 0);
              break;
            // add lock to gaze
            case EyeTracking.EyeTrackingConditions.SimulationFixedToGaze:
              PhospheneMaterial.SetInt(PhosMatGazeLocked, 1);
              PhospheneMaterial.SetInt(PhosMatGazeAssisted, 0);
              temporalDynamicsCs.SetInt(TempDynGazeAssistedSampling, 0);
              break;
            // add gaze assisted sampling on top
            case EyeTracking.EyeTrackingConditions.GazeAssistedSampling:
              PhospheneMaterial.SetInt(PhosMatGazeLocked, 1);
              PhospheneMaterial.SetInt(PhosMatGazeAssisted, 1);
              temporalDynamicsCs.SetInt(TempDynGazeAssistedSampling, 1);
              break;
          }
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
          _edgeDetection = !_edgeDetection;
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

        public void SetEyePosition(Vector2 leftViewport, Vector2 rightViewport, Vector2 centreViewport)
        {
          eyePosLeft = leftViewport;
          eyePosRight = rightViewport;
          eyePosCentre = centreViewport;
          
          PhospheneMaterial.SetVector(ShPropEyePosLeft, eyePosLeft);
          PhospheneMaterial.SetVector(ShPropEyePosRight, eyePosRight);
          
          ImageProcessingMaterial.SetVector(ShPropEyePosLeft, eyePosLeft);
          ImageProcessingMaterial.SetVector(ShPropEyePosRight, eyePosRight);
          
          FocusDotMaterial.SetVector(ShPropEyePosLeft, eyePosLeft);
          FocusDotMaterial.SetVector(ShPropEyePosRight, eyePosRight);
          
          eyePosCorrectionMaterial.SetVector(ShPropEyePosLeft, eyePosLeft);
          eyePosCorrectionMaterial.SetVector(ShPropEyePosRight, eyePosRight);
          eyePosCorrectionMaterial.SetVector(ShPropEyePosCentre, eyePosCentre);
        }

        private bool setImageProcessingResoultion = false;
        protected void Update()
        {
          if (!setImageProcessingResoultion && XRSettings.eyeTextureWidth != 0)
          {
            var w = XRSettings.eyeTextureWidth;
            var h = XRSettings.eyeTextureHeight;
            viveResolution = new Vector2Int(w, h);
            ImageProcessingMaterial.SetInt(ImgProcResX, w);
            ImageProcessingMaterial.SetInt(ImgProcResY, h);
            setImageProcessingResoultion = true;
            Debug.Log($"Set Res to: {w}, {h}");
            
            // max eccentricity (in cfg3: ~ 30 deg from fovea)
            var maxEcc = PhospheneConfig.load(phospheneConfigFile).eccentricities.Max();
            // fraction of FOV covered is max eccentricity in both direction / total FOV
            // Human FOV is ~120 deg
            var screenFraction = (maxEcc*2) / 120f;
            var compression = .4f;
            var maskW = Mathf.RoundToInt(w * screenFraction);
            var maskH = Mathf.RoundToInt(h * screenFraction);
            cutOutResolution = new Vector2Int(maskW, maskH);
            maskResolution = new Vector2Int(
              Mathf.RoundToInt(maskW * compression), 
              Mathf.RoundToInt(maskH*compression)
            );
            Debug.Log($"Set Mask to: {maskW}, {maskH}");
            
            ActivationMask = new RenderTexture(maskW, maskH, XRSettings.eyeTextureDesc.depthBufferBits);
            ActivationMask.enableRandomWrite = true;
            ActivationMask.Create();
            
            // Initialize the render textures & Set the shaders with the shared render textures
            temporalDynamicsCs.SetInts(TempDynResolution, maskW, maskH);

            temporalDynamicsCs.SetTexture(0,TempDynMask, ActivationMask);

            PhospheneMaterial.SetTexture(PhosMatMask, ActivationMask);
          }
          
          if (manualEyePos)
          {
            var eyePos = Vector2.zero;
            if (Keyboard.current[Key.J].isPressed) eyePos.y = .2f;
            else if (Keyboard.current[Key.U].isPressed) eyePos.y = .8f;
            else eyePos.y = .5f;

            if (Keyboard.current[Key.K].isPressed) eyePos.x = .8f;
            else if (Keyboard.current[Key.H].isPressed) eyePos.x = .2f;
            else eyePos.x = .5f;
            
            SetEyePosition(eyePos, eyePos, eyePos);
          }

          if (Keyboard.current[Key.C].isPressed)
          {
              NextEyeTrackingCondition();
              print("Condition: " + eyeTrackingCondition);
          }
        }
    }
}
