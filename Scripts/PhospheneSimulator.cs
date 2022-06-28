using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using  UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Xarphos.Scripts
{
    public class PhospheneSimulator : MonoBehaviour
    {
        public Camera targetCamera;
        public bool setManualEyePos;
        public Vector2 ManualEyePos = new (.5f, .5f);
        
        // stimulation parameters
        [SerializeField]
        private float inputEffect = 0.7f;// The factor by which stimulation accumulates to phosphene activation
        [SerializeField]
        private float intensityDecay = 0.8f; // The factor by which previous activation still influences current activation
        [SerializeField]
        private float traceIncrease = 0.1f; // The habituation strength: the factor by which stimulation leads to buildup of memory trace
        [SerializeField]
        private float traceDecay = 0.9f; // The factor by which the stimulation memory trace decreases

        // Image processing settings
        private float _phospheneFiltering;
        private bool _edgeDetection;

        private Vector2Int viveResolution, maskResolution;
        protected RenderTextureDescriptor actvTexDesc, renderTexDesc;
        [SerializeField] protected SurfaceReplacement.ReplacementModes surfaceReplacementMode;
        private readonly int _nSurfaceModes = Enum.GetValues(typeof(SurfaceReplacement.ReplacementModes)).Length;

        // For reading phosphene configuration from JSON
        public bool initialiseFromFile;
        [SerializeField] private string phospheneConfigFile;
        private PhospheneConfig _phosphenes;
        private int _nPhosphenes;
        private ComputeBuffer _phospheneBuffer;

        // Shaders and materials
        protected Material ImageProcessingMaterial;
        protected Material FocusDotMaterial;
        [SerializeField] protected Shader imageProcessingShader;
        [SerializeField] protected ComputeShader temporalDynamicsCs;
        
        // Eye tracking
        [SerializeField] protected EyeTracking.EyeTrackingConditions eyeTrackingCondition;
        private readonly int _nEyeTrackingModes = Enum.GetValues(typeof(EyeTracking.EyeTrackingConditions)).Length;
        private Vector2 eyePosLeft, eyePosRight, eyePosCentre;
        
        // simulation auxillaries
        private int kernelActivations, kernelSpread, kernelClean;
        private int threadX, threadY;
        private bool headsetInitialised = false;
        
        // ToDo: Clean Up and unify
        #region Shader Properties Name-To-Int
        private static readonly int PhosMatMask = Shader.PropertyToID("_ActivationMask");
        private static readonly int PhosMatPhosphenes = Shader.PropertyToID("phosphenes");
        private static readonly int PhosMatPhospheneTexture = Shader.PropertyToID("PhospheneTexture");
        private static readonly int PhosMatNPhosphenes = Shader.PropertyToID("_nPhosphenes");
        private static readonly int PhosMatFilter = Shader.PropertyToID("_PhospheneFilter");
        private static readonly int PhosMatGazeLocked = Shader.PropertyToID("_GazeLocked");
        private static readonly int PhosMatGazeAssisted = Shader.PropertyToID("_GazeAssisted");
        private static readonly int PhosMatScreenResolutionX = Shader.PropertyToID("_ScreenResolutionX");
          
        private static readonly int TempDynResolution = Shader.PropertyToID("resolution");
        private static readonly int TempDynScreenResolution = Shader.PropertyToID("screenResolution");
        private static readonly int TempDynMask = Shader.PropertyToID("ActivationMask");
        private static readonly int TempDynInputEffect = Shader.PropertyToID("input_effect");
        private static readonly int TempDynIntensityDecay = Shader.PropertyToID("intensity_decay");
        private static readonly int TempDynTraceIncrease = Shader.PropertyToID("trace_increase");
        private static readonly int TempDynTraceDecay = Shader.PropertyToID("trace_decay");
        private static readonly int TempDynPhosphenes = Shader.PropertyToID("phosphenes");
        private static readonly int TempDynPhospheneTexture = Shader.PropertyToID("PhospheneTexture");
        private static readonly int TempDynPhospheneRenderTexture = Shader.PropertyToID("PhospheneRender");
        private static readonly int TempDynGazeAssisted = Shader.PropertyToID("gazeAssisted");
        private static readonly int TempDynGazeLocked = Shader.PropertyToID("gazeLocked");
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
          targetCamera ??= GetComponent<Camera>();

          // Initialize the array of phosphenes
          if (initialiseFromFile && phospheneConfigFile != null)
          {
            try
            {
              _phosphenes = PhospheneConfig.InitPhosphenesFromJSON(phospheneConfigFile);
            } catch (FileNotFoundException){ }
          }
          // if boolean is false, the file path is not given or the initialising from file failed, initialise probabilistic
          _phosphenes ??= PhospheneConfig.InitPhosphenesProbabilistically(1000, .3f, PhospheneConfig.Monopole);
          
          _nPhosphenes = _phosphenes.phosphenes.Length;
          _phospheneBuffer = new ComputeBuffer(_nPhosphenes, sizeof(float)*7);
          _phospheneBuffer.SetData(_phosphenes.phosphenes);

          // Initialize materials with shaders
          ImageProcessingMaterial = new Material(imageProcessingShader);
          
          // Set the compute shader with the temporal dynamics variables
          temporalDynamicsCs.SetFloat(TempDynInputEffect, inputEffect);
          temporalDynamicsCs.SetFloat(TempDynIntensityDecay, intensityDecay);
          temporalDynamicsCs.SetFloat(TempDynTraceIncrease, traceIncrease);
          temporalDynamicsCs.SetFloat(TempDynTraceDecay, traceDecay);

          temporalDynamicsCs.SetBuffer(0, TempDynPhosphenes, _phospheneBuffer);
          // Set the default EyeTrackingCondition (Ignore Gaze)
          temporalDynamicsCs.SetInt(TempDynGazeLocked, 0);
          temporalDynamicsCs.SetInt(TempDynGazeAssisted, 0);

          // get kernel references
          kernelActivations = temporalDynamicsCs.FindKernel("CalculateActivations");
          kernelSpread = temporalDynamicsCs.FindKernel("SpreadActivations");
          kernelClean = temporalDynamicsCs.FindKernel("ClearActivations");
          
          // set up shader for focusdot
          FocusDotMaterial = new Material(Shader.Find("Custom/FocusDot"));
          FocusDotMaterial.SetInt("_RenderPoint", 1);
        }

        private void Start()
        {
          // replace surfaces with in editor selected variant
          SurfaceReplacement.ActivateReplacementShader(targetCamera, surfaceReplacementMode);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture target)
        {
          InitialiseTextures(src); // set up textures, resolutions and parameters
          // if headset is not yet available: skip
          if (target == null || !headsetInitialised) return;

          // in between texture to put processed image on before blitting from this to target
          var preTargetPing = RenderTexture.GetTemporary(target.descriptor);
          
          // if phosphene simulator is off, only need to run image through image processing for edge detection
          if (_edgeDetection)
            Graphics.Blit(src, preTargetPing, ImageProcessingMaterial);
          // if edge detection is off, just blit without any processing
          else
            Graphics.Blit(src, preTargetPing);

          // if phosphene simulation is turned on
          if ((int)_phospheneFiltering != 0)
          {
            var inputTex = RenderTexture.GetTemporary(src.descriptor);
            Graphics.Blit(preTargetPing, inputTex);
            RenderTexture.ReleaseTemporary(preTargetPing);
            temporalDynamicsCs.SetTexture(kernelActivations, TempDynMask, inputTex);

            // temp for storing phosphenes activions
            var actvTex = RenderTexture.GetTemporary(actvTexDesc);
            // get temporary output texture for simulation
            var simulationTex = RenderTexture.GetTemporary(renderTexDesc);

            // pass texture references to compute shader
            temporalDynamicsCs.SetTexture(kernelActivations, TempDynPhospheneTexture, actvTex);
            temporalDynamicsCs.SetTexture(kernelSpread, TempDynPhospheneTexture, actvTex);
            temporalDynamicsCs.SetTexture(kernelSpread, TempDynPhospheneRenderTexture, simulationTex);
            
            // calculate activations
            temporalDynamicsCs.Dispatch(kernelActivations, Mathf.CeilToInt(_nPhosphenes / 32), 1, 1);
            // render phosphene simulation
            temporalDynamicsCs.Dispatch(kernelSpread, threadX, threadY, 1);
            
            // reinit & copy simulation to pre-out
            preTargetPing = RenderTexture.GetTemporary(target.descriptor);
            Graphics.Blit(simulationTex, preTargetPing);
            // release temporaries. don't want memory leaks
            RenderTexture.ReleaseTemporary(simulationTex);
            RenderTexture.ReleaseTemporary(actvTex);
          }

          // lastly render the focus dot on top
          Graphics.Blit(preTargetPing, target, FocusDotMaterial);
          RenderTexture.ReleaseTemporary(preTargetPing);
        }

        /// <summary>
        /// Sets up the parameters relating to image processing and simulation, like resolution
        /// </summary>
        /// <param name="src">a render texture that is VR ready to get parameters from</param>
        private void InitialiseTextures(RenderTexture src)
        {
          if (headsetInitialised || XRSettings.eyeTextureWidth == 0) return;
          
          headsetInitialised = true;
            
          // set up resolution
          var w = XRSettings.eyeTextureWidth;
          var h = XRSettings.eyeTextureHeight;
          viveResolution = new Vector2Int(w, h);
          ImageProcessingMaterial.SetInt(ImgProcResX, w);
          ImageProcessingMaterial.SetInt(ImgProcResY, h);
          Debug.Log($"Set Res to: {w}, {h}");

          // set up input texture for simulation
          var compressionFactor = 1;
          maskResolution = viveResolution / compressionFactor;
          actvTexDesc = new RenderTextureDescriptor(maskResolution.x, maskResolution.y, src.graphicsFormat, 0, 0)
            {
              dimension = src.dimension,
              vrUsage = src.descriptor.vrUsage,
              volumeDepth = src.descriptor.volumeDepth,
              enableRandomWrite = true
            };


          renderTexDesc = new RenderTextureDescriptor(src.width, src.height, src.graphicsFormat, src.depth, 0)
          {
            dimension = src.dimension,
            vrUsage = src.descriptor.vrUsage,
            volumeDepth = src.descriptor.volumeDepth,
            enableRandomWrite = true
          };
          // Initialize the render textures & Set the shaders with the shared render textures
          temporalDynamicsCs.SetInts(TempDynScreenResolution, viveResolution.x, viveResolution.y);  
          temporalDynamicsCs.SetInts(TempDynResolution, maskResolution.x, maskResolution.y);

          // calculate the thread count necessary to cover the entire texture
          temporalDynamicsCs.GetKernelThreadGroupSizes(kernelSpread, out var xGroup, out var yGroup, out _);
          threadX = Mathf.CeilToInt(viveResolution.x / xGroup);
          threadY = Mathf.CeilToInt(viveResolution.y / yGroup);
          
          // calculate the center position for each eye corrected for visual transform
          var (lViewSpace, rViewSpace, cViewSpace) = EyePosFromScreenPoint(0.5f, 0.5f);
          SetEyePosition(lViewSpace, rViewSpace, cViewSpace);
          temporalDynamicsCs.SetVector("_LeftEyeCenter", lViewSpace);
          temporalDynamicsCs.SetVector("_RightEyeCenter", rViewSpace);
        }

        /// <summary>
        /// calculate the screen position for each eye from a 2d point on the "center" view
        /// </summary>
        /// <param name="x">x position on screen in 0..1, left is 0</param>
        /// <param name="y">y position on screen in 0..1, bottom is 0</param>
        /// <returns>tuple of left, right and center screen position according to input. center should be roughly equal to input</returns>
        private (Vector2, Vector2, Vector2) EyePosFromScreenPoint(float x, float y)
        {
          // set eye position to centre of screen and calculate correct offsets
          var P = targetCamera.ViewportToWorldPoint(
            new Vector3(x, y, 10f)); 
          // projection from local space to clip space
          var lMat = targetCamera.GetStereoNonJitteredProjectionMatrix(Camera.StereoscopicEye.Left);
          var rMat = targetCamera.GetStereoNonJitteredProjectionMatrix(Camera.StereoscopicEye.Right);
          var cMat = targetCamera.nonJitteredProjectionMatrix;
          // projection from world space into local space
          var world2cam = targetCamera.worldToCameraMatrix;
          // 4th dimension necessary in graphics to get scale
          var P4d = new Vector4(P.x, P.y, P.z, 1f); 
          // point in world space * world2cam -> local space point
          // local space point * projection matrix = clip space point
          var lProjection = lMat * world2cam * P4d;
          var rProjection = rMat * world2cam * P4d;
          var cProjection = cMat * world2cam * P4d;
          // scale and shift from clip space [-1,1] into view space [0,1]
          var lViewSpace = (new Vector2(lProjection.x, lProjection.y) / lProjection.w) * .5f + .5f * Vector2.one;
          var rViewSpace = (new Vector2(rProjection.x, rProjection.y) / rProjection.w) * .5f + .5f * Vector2.one;
          var cViewSpace = (new Vector2(cProjection.x, cProjection.y) / cProjection.w) * .5f + .5f * Vector2.one;
          
          return (lViewSpace, rViewSpace, cViewSpace);
        }

        private void OnDestroy(){
          _phospheneBuffer.Release();
        }

        #region Input Handling
        // cycle surface replacement
        public void NextSurfaceReplacementMode(InputAction.CallbackContext ctx) => NextSurfaceReplacementMode();
        private void NextSurfaceReplacementMode(){
          surfaceReplacementMode = (SurfaceReplacement.ReplacementModes)((int)(surfaceReplacementMode + 1) % _nSurfaceModes);
          // Replace surfaces with the surface replacement shader
          SurfaceReplacement.ActivateReplacementShader(targetCamera, surfaceReplacementMode);
        }

        // cycle eye tracking conditions
        public void NextEyeTrackingCondition(InputAction.CallbackContext ctx) => NextEyeTrackingCondition();
        private void NextEyeTrackingCondition()
        {
          eyeTrackingCondition = (EyeTracking.EyeTrackingConditions)((int)(eyeTrackingCondition + 1) % _nEyeTrackingModes);

          switch (eyeTrackingCondition)
          {
            // reset and don't use gaze info
            case EyeTracking.EyeTrackingConditions.GazeIgnored:
              temporalDynamicsCs.SetInt(TempDynGazeAssisted, 0);
              temporalDynamicsCs.SetInt(TempDynGazeLocked, 0);
              break;
            // add lock to gaze
            case EyeTracking.EyeTrackingConditions.SimulationFixedToGaze:
              temporalDynamicsCs.SetInt(TempDynGazeAssisted, 0);
              temporalDynamicsCs.SetInt(TempDynGazeLocked, 1);
              break;
            // add gaze assisted sampling on top
            case EyeTracking.EyeTrackingConditions.GazeAssistedSampling:
              temporalDynamicsCs.SetInt(TempDynGazeAssisted, 1);
              temporalDynamicsCs.SetInt(TempDynGazeLocked, 1);
              break;
          }
        }
        
        public void ToggleEdgeDetection(InputAction.CallbackContext ctx) => ToggleEdgeDetection();
        private void ToggleEdgeDetection()
        {
          _edgeDetection = !_edgeDetection;
        }
        
        public void TogglePhospheneSim(InputAction.CallbackContext ctx) => TogglePhospheneSim();
        public void TogglePhospheneSim()
        {
          _phospheneFiltering = 1-_phospheneFiltering;
        }
        #endregion

        /// <summary>
        /// Update class variables and pass new positions to shaders
        /// </summary>
        /// <param name="leftViewport">left eye screen position in 0..1</param>
        /// <param name="rightViewport">right eye screen position in 0..1</param>
        /// <param name="centreViewport">centre screen position in 0..1</param>
        public void SetEyePosition(Vector2 leftViewport, Vector2 rightViewport, Vector2 centreViewport)
        {
          eyePosLeft = leftViewport;
          eyePosRight = rightViewport;
          eyePosCentre = centreViewport;

          FocusDotMaterial.SetVector(ShPropEyePosLeft, eyePosLeft);
          FocusDotMaterial.SetVector(ShPropEyePosRight, eyePosRight);
          
          temporalDynamicsCs.SetVector(ShPropEyePosLeft, eyePosLeft);
          temporalDynamicsCs.SetVector(ShPropEyePosRight, eyePosRight);
        }
      
        protected void Update()
        {
          if (setManualEyePos)
          {
            if (Keyboard.current[Key.J].isPressed) ManualEyePos.y -= .05f * Time.deltaTime;
            else if (Keyboard.current[Key.U].isPressed) ManualEyePos.y += .05f * Time.deltaTime;

            if (Keyboard.current[Key.K].isPressed) ManualEyePos.x += .05f * Time.deltaTime;
            else if (Keyboard.current[Key.H].isPressed) ManualEyePos.x -= .05f * Time.deltaTime;
            
            var (lViewSpace, rViewSpace, cViewSpace) = EyePosFromScreenPoint(ManualEyePos.x, ManualEyePos.y);
            
            SetEyePosition(lViewSpace, rViewSpace, cViewSpace);
          }

          if (Keyboard.current[Key.C].isPressed)
          {
              NextEyeTrackingCondition();
              print("Condition: " + eyeTrackingCondition);
          }
        }
    }
}
