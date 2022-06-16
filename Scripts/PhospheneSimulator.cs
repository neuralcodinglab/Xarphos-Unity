using System;
using System.Collections.Generic;
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
        protected RenderTexture PhospheneTexture;

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

        private Vector2Int viveResolution, maskResolution;
        private Vector2 eyePosLeft, eyePosRight, eyePosCentre;
        
        private bool setImageProcessingResolution = false;
        
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
          if (target == null || !setImageProcessingResolution) return;
          // in between texture to put processed image on before blitting from this to target
          var preTargetPing = RenderTexture.GetTemporary(target.descriptor);
          // if phosphene simulator is off, only need to run image through image processing for edge detection

          if (_edgeDetection)
            Graphics.Blit(src, preTargetPing, ImageProcessingMaterial);
          // if edge detection is off, just blit without any processing
          else
            Graphics.Blit(src, preTargetPing);

          if ((int)_phospheneFiltering != 0)
          {
            // blit to activation mask for compression
            Graphics.Blit(preTargetPing, ActivationMask);
            RenderTexture.ReleaseTemporary(preTargetPing);
            preTargetPing = RenderTexture.GetTemporary(target.descriptor);
            
            // run simulation on scaled texture
            temporalDynamicsCs.Dispatch(0, Mathf.CeilToInt(_phosphenes.Length / 32), 1, 1);
            // render phosphene simulation
            // Graphics.Blit(null, preTargetPing, PhospheneMaterial);
            Graphics.Blit(PhospheneTexture, preTargetPing);
          }

          // lastly render the focus dot on top
          Graphics.Blit(preTargetPing, target, FocusDotMaterial);

          RenderTexture.ReleaseTemporary(preTargetPing);
        }

        private void OnPreRender()
        {
          if (setImageProcessingResolution && (int)_phospheneFiltering != 0)
          {
            PhospheneTexture = RenderTexture.GetTemporary(XRSettings.eyeTextureDesc);
            PhospheneTexture.enableRandomWrite = true;
            temporalDynamicsCs.SetTexture(0, TempDynPhospheneTexture, PhospheneTexture);
            PhospheneMaterial.SetTexture(PhosMatPhospheneTexture, PhospheneTexture);
          }
        }

        private void OnPostRender()
        {
          if (setImageProcessingResolution && (int)_phospheneFiltering != 0)
          {
            RenderTexture.ReleaseTemporary(PhospheneTexture);
          }
        }

        private void OnDestroy(){
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
      
        protected void Update()
        {
          if (!setImageProcessingResolution && XRSettings.eyeTextureWidth != 0)
          {
            var w = XRSettings.eyeTextureWidth;
            var h = XRSettings.eyeTextureHeight;
            viveResolution = new Vector2Int(w, h);
            ImageProcessingMaterial.SetInt(ImgProcResX, w);
            ImageProcessingMaterial.SetInt(ImgProcResY, h);
            setImageProcessingResolution = true;
            Debug.Log($"Set Res to: {w}, {h}");

            var compressionFactor = 4;
            maskResolution = viveResolution / compressionFactor;
            
            ActivationMask = new RenderTexture(maskResolution.x, maskResolution.y, XRSettings.eyeTextureDesc.depthBufferBits);
            ActivationMask.enableRandomWrite = true;
            ActivationMask.Create();
            
            // Initialize the render textures & Set the shaders with the shared render textures
            temporalDynamicsCs.SetInts(TempDynScreenResolution, viveResolution.x, viveResolution.y);  
            temporalDynamicsCs.SetInts(TempDynResolution, maskResolution.x, maskResolution.y);
            temporalDynamicsCs.SetTexture(0,TempDynMask, ActivationMask);

            // set up buffer to hold pixel activations
            PhospheneMaterial.SetInt(PhosMatScreenResolutionX, viveResolution.x);
            
            // ToDo: 2D array [nPhosphenes x PixelInfluences]
            // var influences = new List<Vector2Int>[_nPhosphenes];
            //
            // for (uint i = 0; i < _nPhosphenes; i += 1)
            // {
            //   var ph = _phosphenes[i];
            //   Vector2Int pxlPos = Vector2Int.RoundToInt(Vector2.Scale(ph.position, viveResolution));
            //   var r = Mathf.RoundToInt(ph.size * viveResolution.x);
            //
            //   for (int xOffset = 0; xOffset < r; xOffset += 1)
            //   {
            //     for (int yOffset = 0; yOffset < r; yOffset += 1)
            //     {
            //       if ((xOffset * xOffset + yOffset * yOffset) >= (r * r)) continue;
            //       // position is inside of circle
            //       var offsets = new[] { (xOffset, yOffset), (xOffset,-yOffset), (-xOffset, yOffset), (-xOffset,-yOffset) };
            //       foreach (var offset in offsets)
            //       {
            //         int idx = 0;
            //         var xPos = pxlPos.x + offset.Item1;
            //         var yPos = pxlPos.y + offset.Item2;
            //         if (xPos < 0 || xPos > w || yPos < 0 || yPos > h) continue;
            //
            //         influences[i] ??= new List<Vector2Int>();
            //         influences[i].Add(new Vector2Int(xPos, yPos));
            //       }
            //     }
            //   }
            // }
            // Debug.Log($"Max: {influences.Max(x => x.Count)}");
            
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
