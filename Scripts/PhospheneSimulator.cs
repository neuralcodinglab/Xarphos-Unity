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
        private float _edgeDetection;

        // EyeTracking
        protected int GazeLocking; // used as boolean (sent to shader)
        protected bool CamLocking;

        // Image processing settings
        [SerializeField] protected EyeTracking.EyeTrackingConditions eyeTrackingCondition;
        [SerializeField] protected SurfaceReplacement.ReplacementModes surfaceReplacementMode;
        private readonly int _nSurfaceModes = System.Enum.GetValues(typeof(SurfaceReplacement.ReplacementModes)).Length;
        private readonly int _nEyeTrackingModes = System.Enum.GetValues(typeof(EyeTracking.EyeTrackingConditions)).Length;
        // [SerializeField] protected Vector2Int resolution;
        // [SerializeField] protected Vector2 FOV;

        // Render textures
        protected RenderTexture ActivationMaskL, ActivationMaskR;//, LeftEyeSim, RightEyeSim;

        // For reading phosphene configuration from JSON
        [SerializeField] private string phospheneConfigFile;
        private Phosphene[] _phosphenes;
        private int _nPhosphenes;
        private ComputeBuffer _phospheneBufferL;
        private ComputeBuffer _phospheneBufferR;

        // Shaders and materials
        protected Material ImageProcessingMaterial;
        protected Material PhospheneMaterial_Left;
        protected Material FocusDotMaterial_Left;
        protected Material PhospheneMaterial_Right;
        protected Material FocusDotMaterial_Right;
        [SerializeField] protected Shader phospheneShader;
        [SerializeField] protected Shader imageProcessingShader;
        [SerializeField] protected ComputeShader temporalDynamicsCsL;
        [SerializeField] protected ComputeShader temporalDynamicsCsR;


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
        private Vector2 screenSpacePart;
        
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
            _phospheneBufferL = new ComputeBuffer(_nPhosphenes, sizeof(float)*5);
            _phospheneBufferL.SetData(_phosphenes);
            
            _phospheneBufferR = new ComputeBuffer(_nPhosphenes, sizeof(float)*5);
            _phospheneBufferR.SetData(_phosphenes);


            // Initialize materials with shaders
            PhospheneMaterial_Left = new Material(phospheneShader);
            PhospheneMaterial_Right = new Material(phospheneShader);
            ImageProcessingMaterial = new Material(imageProcessingShader);// TODO
            
            // Set the compute shader with the temporal dynamics variables
            temporalDynamicsCsL.SetFloat(TempDynInputEffect, inputEffect);
            temporalDynamicsCsL.SetFloat(TempDynIntensityDecay, intensityDecay);
            temporalDynamicsCsL.SetFloat(TempDynTraceIncrease, traceIncrease);
            temporalDynamicsCsL.SetFloat(TempDynTraceDecay, traceDecay);
            
            temporalDynamicsCsR.SetFloat(TempDynInputEffect, inputEffect);
            temporalDynamicsCsR.SetFloat(TempDynIntensityDecay, intensityDecay);
            temporalDynamicsCsR.SetFloat(TempDynTraceIncrease, traceIncrease);
            temporalDynamicsCsR.SetFloat(TempDynTraceDecay, traceDecay);

            // Set the shader properties with the shared phosphene buffer
            PhospheneMaterial_Left.SetBuffer(PhosMatPhosphenes, _phospheneBufferL);
            PhospheneMaterial_Left.SetInt(PhosMatNPhosphenes, _nPhosphenes);
            PhospheneMaterial_Left.SetFloat(PhosMatFilter, _phospheneFiltering);
            PhospheneMaterial_Left.SetBuffer(PhosMatPhosphenes, _phospheneBufferL);
            PhospheneMaterial_Left.SetInt(PhosMatNPhosphenes, _nPhosphenes);
            PhospheneMaterial_Left.SetFloat(PhosMatFilter, _phospheneFiltering);

            PhospheneMaterial_Right.SetBuffer(PhosMatPhosphenes, _phospheneBufferR);
            PhospheneMaterial_Right.SetInt(PhosMatNPhosphenes, _nPhosphenes);
            PhospheneMaterial_Right.SetFloat(PhosMatFilter, _phospheneFiltering);
            PhospheneMaterial_Right.SetBuffer(PhosMatPhosphenes, _phospheneBufferR);
            PhospheneMaterial_Right.SetInt(PhosMatNPhosphenes, _nPhosphenes);
            PhospheneMaterial_Right.SetFloat(PhosMatFilter, _phospheneFiltering);
            
            PhospheneMaterial_Left.SetInt(ShPropEyeToRender, 0);
            PhospheneMaterial_Right.SetInt(ShPropEyeToRender, 1);

            temporalDynamicsCsL.SetBuffer(0, TempDynPhosphenes, _phospheneBufferL);
            temporalDynamicsCsR.SetBuffer(0,TempDynPhosphenes, _phospheneBufferR);
            // Set the default EyeTrackingCondition (Ignore Gaze)
            PhospheneMaterial_Left.SetInt(PhosMatGazeLocked, 0);
            PhospheneMaterial_Right.SetInt(PhosMatGazeLocked, 0);
            temporalDynamicsCsL.SetInt(TempDynGazeAssistedSampling, 0);
            temporalDynamicsCsR.SetInt(TempDynGazeAssistedSampling, 0);

            FocusDotMaterial_Left = new Material(Shader.Find("Custom/FocusDot"));
            FocusDotMaterial_Right = new Material(Shader.Find("Custom/FocusDot"));
            FocusDotMaterial_Left.SetInt(ShPropEyeToRender, 0);
            FocusDotMaterial_Right.SetInt(ShPropEyeToRender, 1);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture target)
        {
          if (target == null) return;
          var pre_target = RenderTexture.GetTemporary(target.descriptor);
          // if phosphene simulator is off, only need to run image through image processing for edge detection
          if ((int) _phospheneFiltering == 0)
          {
            Graphics.Blit(src, pre_target, ImageProcessingMaterial);
          }
          else
          {
            if (targetCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
            {
              Graphics.Blit(src, ActivationMaskL, ImageProcessingMaterial);
              temporalDynamicsCsL.Dispatch(0, _phosphenes.Length / 10, 1, 1);
              Graphics.Blit(null, pre_target, PhospheneMaterial_Left);
            }
            else
            {
              Graphics.Blit(src, ActivationMaskR, ImageProcessingMaterial);
              temporalDynamicsCsR.Dispatch(0, _phosphenes.Length / 10, 1, 1);
              Graphics.Blit(null, pre_target, PhospheneMaterial_Right);
            }
          }
          
          if (targetCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
            Graphics.Blit(pre_target, target, FocusDotMaterial_Left);
          else
            Graphics.Blit(pre_target, target, FocusDotMaterial_Right);
          RenderTexture.ReleaseTemporary(pre_target);
          return;
          // If gaze assissted sampling is on, we sample around the foveal point, otherwise, sample around camera centre
          // Vector2Int offset = (eyeTrackingCondition == EyeTracking.EyeTrackingConditions.GazeAssistedSampling)
          //   ? new Vector2Int((int) (eyePos.x * viveResolution.x), (int) (eyePos.y * viveResolution.y))
          //   : viveResolution / 2;
          // // since rectangle definitions start at corner, but we defined centre, shift area to centre align
          // offset -= maskResolution / 2;
          // // clamp to actual resolution
          // offset.Clamp(Vector2Int.zero, viveResolution);
          // // clamp copied texture to source size, in case we are at the edge of the canvas
          // var cpWidth = Mathf.Min(maskResolution.x, viveResolution.x - offset.x);
          // var cpHeight = Mathf.Min(maskResolution.y, viveResolution.y - offset.y);
          //
          // var tmp = RenderTexture.GetTemporary(maskResolution.x, maskResolution.y, src.depth, src.graphicsFormat);
          // Graphics.CopyTexture(
          //   src, 0, 0, offset.x, offset.y, cpWidth, cpHeight,
          //   tmp, 0, 0, 0, 0
          // );
          // Graphics.Blit(tmp, ActivationMask, ImageProcessingMaterial);
          // temporalDynamicsCs.SetFloats(TempDynEyePos, eyePos.x, eyePos.y);
          // temporalDynamicsCs.Dispatch(0, _phosphenes.Length / 10, 1, 1);
          // RenderTexture.ReleaseTemporary(tmp);
          // Graphics.Blit(src, target, ImageProcessingMaterial);
          //
          // // get new temp texture with correct output graphics format and render simulation onto it
          // tmp = RenderTexture.GetTemporary(maskResolution.x, maskResolution.y, target.depth, target.graphicsFormat);
          // Graphics.Blit(ActivationMask, tmp, PhospheneMaterial);
          //
          // // in fixed-to-gaze condition, we sample from camera centre but render around fovea, so adjust centre accordingly
          // if (eyeTrackingCondition == EyeTracking.EyeTrackingConditions.SimulationFixedToGaze)
          // {
          //   offset = new Vector2Int((int) (eyePos.x * viveResolution.x), (int) (eyePos.y * viveResolution.y));
          //   offset -= maskResolution / 2;
          //   offset.Clamp(Vector2Int.zero, viveResolution);
          //   cpWidth = Mathf.Min(maskResolution.x, viveResolution.x - offset.x);
          //   cpHeight = Mathf.Min(maskResolution.y, viveResolution.y - offset.y);
          // }
          // // get empty canvas & position simulation on it centred on eye position
          // var clean = RenderTexture.GetTemporary(target.descriptor);
          // Graphics.CopyTexture(
          //   tmp, 0, 0, 0, 0, cpWidth, cpHeight,
          //   clean, 0, 0, offset.x,offset.y
          // );
          // // blit completed composition onto screen and release temp textures
          // Graphics.Blit(clean, target);
          // RenderTexture.ReleaseTemporary(tmp);
          // RenderTexture.ReleaseTemporary(clean);
        }

        private void OnDisable(){
          _phospheneBufferL.Release();
          _phospheneBufferR.Release();
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
              PhospheneMaterial_Left.SetInt(PhosMatGazeLocked, 0);
              PhospheneMaterial_Right.SetInt(PhosMatGazeLocked, 0);
              PhospheneMaterial_Left.SetInt(PhosMatGazeAssisted, 0);
              PhospheneMaterial_Right.SetInt(PhosMatGazeAssisted, 0);
              temporalDynamicsCsL.SetInt(TempDynGazeAssistedSampling, 0);
              temporalDynamicsCsR.SetInt(TempDynGazeAssistedSampling, 0);
              break;
            // add lock to gaze
            case EyeTracking.EyeTrackingConditions.SimulationFixedToGaze:
              PhospheneMaterial_Left.SetInt(PhosMatGazeLocked, 1);
              PhospheneMaterial_Left.SetInt(PhosMatGazeAssisted, 0);
              temporalDynamicsCsL.SetInt(TempDynGazeAssistedSampling, 0);
              PhospheneMaterial_Right.SetInt(PhosMatGazeLocked, 1);
              PhospheneMaterial_Right.SetInt(PhosMatGazeAssisted, 0);
              temporalDynamicsCsR.SetInt(TempDynGazeAssistedSampling, 0);
              break;
            // add gaze assisted sampling on top
            case EyeTracking.EyeTrackingConditions.GazeAssistedSampling:
              PhospheneMaterial_Left.SetInt(PhosMatGazeLocked, 1);
              PhospheneMaterial_Left.SetInt(PhosMatGazeAssisted, 1);
              temporalDynamicsCsL.SetInt(TempDynGazeAssistedSampling, 1);
              PhospheneMaterial_Right.SetInt(PhosMatGazeLocked, 1);
              PhospheneMaterial_Right.SetInt(PhosMatGazeAssisted, 1);
              temporalDynamicsCsR.SetInt(TempDynGazeAssistedSampling, 1);
              break;
          }
        }

        public void TogglePhospheneSim(InputAction.CallbackContext ctx) => TogglePhospheneSim();

        public void TogglePhospheneSim()
        {
          _phospheneFiltering = 1-_phospheneFiltering;
          PhospheneMaterial_Left.SetFloat(PhosMatFilter, _phospheneFiltering);
          PhospheneMaterial_Right.SetFloat(PhosMatFilter, _phospheneFiltering);
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
          PhospheneMaterial_Left.SetInt(PhosMatGazeLocked, GazeLocking);
          PhospheneMaterial_Right.SetInt(PhosMatGazeLocked, GazeLocking);
        }

        public void SetEyePosition(Vector2 leftViewport, Vector2 rightViewport)
        {
          PhospheneMaterial_Left.SetVector(ShPropEyePosLeft, leftViewport);
          PhospheneMaterial_Left.SetVector(ShPropEyePosRight, rightViewport);
          
          PhospheneMaterial_Right.SetVector(ShPropEyePosLeft, leftViewport);
          PhospheneMaterial_Right.SetVector(ShPropEyePosRight, rightViewport);
          
          ImageProcessingMaterial.SetVector(ShPropEyePosLeft, leftViewport);
          ImageProcessingMaterial.SetVector(ShPropEyePosRight, rightViewport);
          
          FocusDotMaterial_Left.SetVector(ShPropEyePosLeft, leftViewport);
          FocusDotMaterial_Left.SetVector(ShPropEyePosRight, rightViewport);
          
          FocusDotMaterial_Right.SetVector(ShPropEyePosLeft, rightViewport);
          FocusDotMaterial_Right.SetVector(ShPropEyePosRight, rightViewport);
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
            var maskW = Mathf.RoundToInt(w * screenFraction * compression);
            var maskH = Mathf.RoundToInt(h * screenFraction * compression);
            maskResolution = new Vector2Int(maskW, maskH);
            Debug.Log($"Set Mask to: {maskW}, {maskH}");
            
            ActivationMaskL = new RenderTexture(maskW, maskH, XRSettings.eyeTextureDesc.depthBufferBits);
            ActivationMaskL.enableRandomWrite = true;
            ActivationMaskL.Create();
            
            ActivationMaskR = new RenderTexture(maskW, maskH, XRSettings.eyeTextureDesc.depthBufferBits);
            ActivationMaskR.enableRandomWrite = true;
            ActivationMaskR.Create();
            
            // Initialize the render textures & Set the shaders with the shared render textures
            temporalDynamicsCsL.SetInts(TempDynResolution, maskW, maskH);
            temporalDynamicsCsR.SetInts(TempDynResolution, maskW, maskH);
            
            temporalDynamicsCsL.SetTexture(0,TempDynMask, ActivationMaskL);
            temporalDynamicsCsR.SetTexture(0,TempDynMask, ActivationMaskR);
            
            PhospheneMaterial_Left.SetTexture(PhosMatMask, ActivationMaskL);
            PhospheneMaterial_Right.SetTexture(PhosMatMask, ActivationMaskR);
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
            
            SetEyePosition(eyePos, eyePos);
          }

          if (Keyboard.current[Key.C].isPressed)
          {
              NextEyeTrackingCondition();
              print("Condition: " + eyeTrackingCondition);
          }
        }
    }
}
