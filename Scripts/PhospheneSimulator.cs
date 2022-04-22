using UnityEngine;
using Rect = UnityEngine.Rect;
using OpenCVForUnity;
using System;
using System.IO;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using static ImageSynthesis;
using  UnityEngine.InputSystem;
using System.Collections;

namespace Xarphos.Scripts
{
    // [RequireComponent(typeof(Renderer))]

    public class PhospheneSimulator : MonoBehaviour
    {

        // Image processing settings
        float phospheneFiltering = 0.0f;
        float edgeDetection = 0.0f;

        // EyeTracking
        protected Vector2 eyePosition;
        protected int gazeLocking; // used as boolean (sent to shader)
        protected bool camLocking;

        // Image processing settings
        [SerializeField] protected SurfaceReplacement.ReplacementModes surfaceReplacementMode;
        [SerializeField] protected Vector2Int resolution;

        // Render textures
        protected RenderTexture inputRT;
        protected RenderTexture activationMask;
        protected RenderTexture phospheneRT;

        // For reading phosphene configuration from JSON
        [SerializeField] string phospheneConfigFile;
        private Phosphene[] phosphenes;
        int nPhosphenes;
        ComputeBuffer phospheneBuffer;

        // Shaders and materials
        protected Material imageProcessingMaterial;
        protected Material phospheneMaterial;
        [SerializeField] protected Shader phospheneShader;
        [SerializeField] protected Shader imageProcessingShader;
        [SerializeField] protected ComputeShader temporalDynamicsCS;


        // stimulation parameters
        [SerializeField] float input_effect = 0.7f;// The factor by which stimulation accumulates to phosphene activation
        [SerializeField] float intensity_decay = 0.8f; // The factor by which previous activation still influences current activation
        [SerializeField] float trace_increase = 0.1f; // The habituation strength: the factor by which stimulation leads to buildup of memory trace
        [SerializeField] float trace_decay = 0.9f; // The factor by which the stimulation memory trace decreases

        protected void Awake()
        {
            // Initialize the array of phosphenes
            phosphenes = PhospheneConfig.InitPhosphenesFromJSON(phospheneConfigFile);
            nPhosphenes = phosphenes.Length;
            phospheneBuffer = new ComputeBuffer(nPhosphenes, sizeof(float)*5);
            phospheneBuffer.SetData(phosphenes);


            // Initialize materials with shaders
            phospheneMaterial = new Material(phospheneShader);
            imageProcessingMaterial = new Material(imageProcessingShader);// TODO

            // Initialize the render textures
            inputRT = new RenderTexture(resolution.x, resolution.y, 24);
            activationMask = new RenderTexture(resolution.x, resolution.y, 24);
            activationMask.enableRandomWrite = true;
            activationMask.Create();
            phospheneRT = new RenderTexture(resolution.x, resolution.y, 24);

            // Set the shaders with the shared render textures
            imageProcessingMaterial.SetTexture("_MainTex", inputRT);
            temporalDynamicsCS.SetInts("resolution", new int[]{resolution.x, resolution.y});
            temporalDynamicsCS.SetTexture(0,"ActivationMask", activationMask);
            phospheneMaterial.SetTexture("_ActivationMask", activationMask);

            // Set the compute shader with the temporal dynamics variables
            temporalDynamicsCS.SetFloat("input_effect", input_effect);
            temporalDynamicsCS.SetFloat("intensity_decay", intensity_decay);
            temporalDynamicsCS.SetFloat("trace_increase", trace_increase);
            temporalDynamicsCS.SetFloat("trace_decay", trace_decay);

            // Set the shader properties with the shared phosphene buffer
            phospheneMaterial.SetBuffer("phosphenes", phospheneBuffer);
            phospheneMaterial.SetInt("_nPhosphenes", nPhosphenes);
            phospheneMaterial.SetFloat("_PhospheneFilter", phospheneFiltering);
            temporalDynamicsCS.SetBuffer(0,"phosphenes", phospheneBuffer);


        }

        void computePhospheneActivity(){
          // Perform image processing and compute the phosphene activity
          Graphics.Blit(inputRT, activationMask, imageProcessingMaterial); // using imageProcessingshader
          temporalDynamicsCS.Dispatch(0,phosphenes.Length/10,1,1);
        }

        void OnPreRender() {
        // Render to the input render texture
          Camera.main.targetTexture = inputRT;
        }


        IEnumerator blitToScreen()
        {
          // Wait until all reder textures are rendered
          yield return new WaitForEndOfFrame();

          // Target tex has to be (temporarilty) set to null
          Camera.main.targetTexture = null;

          // Simulate the phosphenes (based on the shared phosphene buffer)
          Graphics.Blit(null as RenderTexture, phospheneRT, phospheneMaterial);

          // Blit to the screen
          Graphics.Blit(phospheneRT, null as RenderTexture);
          yield return null;
        }

        void OnDisable(){
          phospheneBuffer.Release();
        }

        void nextSurfaceReplacementMode(){
          var nModes = System.Enum.GetValues(typeof(SurfaceReplacement.ReplacementModes)).Length;
          surfaceReplacementMode += 1;

          if((int)surfaceReplacementMode < nModes){
            // Replace surfaces with the surface replacement shader
            SurfaceReplacement.ActivateReplacementShader(surfaceReplacementMode);
          }
          else{
            // If cycled through all modes -> set back to first element
            surfaceReplacementMode = 0;
            SurfaceReplacement.DeactivateReplacementShader(); // (first mode is no surface replacement)
          }
        }


        protected void Update()
        {
            computePhospheneActivity();
            StartCoroutine(blitToScreen());


            if (Keyboard.current[Key.U].isPressed || Keyboard.current[Key.J].isPressed)
            {
                if (Keyboard.current[Key.U].isPressed) {eyePosition.y = 0.8f;}
                if (Keyboard.current[Key.J].isPressed) {eyePosition.y = 0.2f;}
            }
            else
            {
              eyePosition.y = 0.5f;
            }

            if (Keyboard.current[Key.H].isPressed || Keyboard.current[Key.K].isPressed)
            {
                if (Keyboard.current[Key.H].isPressed) {eyePosition.x = 0.2f;}
                if (Keyboard.current[Key.K].isPressed) {eyePosition.x = 0.8f;}
            }
            else
            {
              eyePosition.x = 0.5f;
            }

            if (Keyboard.current[Key.G].isPressed)
            {
              gazeLocking = 1-gazeLocking;
            }

            if (Keyboard.current[Key.C].isPressed)
            {
              camLocking = !camLocking;
            }

            if (Keyboard.current[Key.T].isPressed)
            {
              nextSurfaceReplacementMode();
            }

            if (Keyboard.current[Key.E].isPressed)
            {
              edgeDetection = 1-edgeDetection;
              imageProcessingMaterial.SetFloat("_Mode", edgeDetection);
            }

            if (Keyboard.current[Key.P].isPressed)
            {
              phospheneFiltering = 1-phospheneFiltering;
              phospheneMaterial.SetFloat("_PhospheneFilter", phospheneFiltering);
            }

            phospheneMaterial.SetVector("_EyePosition", eyePosition);
            phospheneMaterial.SetInt("_GazeLocked", gazeLocking);
        }

    }
}
