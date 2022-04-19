using UnityEngine;
using Rect = UnityEngine.Rect;
using System;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine.InputSystem;


namespace Xarphos.Scripts
{
    [RequireComponent(typeof(Camera))]
    public class DSPV_SimulationPostProcessor : MonoBehaviour
    {
        // // The material texture will be used as phosphene activation mask
        protected Material material;
        protected Texture2D input_texture;   //The input texture (from webcam or virtual camera)
        protected Texture2D activation_mask;  //The output texture after preprocessing

        // // The shader that is used for the phosphene simulation
        [SerializeField] protected Shader shader;
        private static readonly int _PhospheneActivationMask = Shader.PropertyToID("_ActivationMask");
        private static readonly int _PhospheneActivation = Shader.PropertyToID("activation");
        private static readonly int _PhospheneSpecs = Shader.PropertyToID("_pSpecs");
        private static readonly int _PhospheneFilter = Shader.PropertyToID("_PhospheneFilter");
        private static readonly int _PhospheneEyePos = Shader.PropertyToID("_EyePosition");
        private static readonly int _PhospheneGazeLocked = Shader.PropertyToID("_GazeLocked");


        // // TODO use separate image processing shader
        // protected Shader imgprocShader;
        // protected Material imgprocMaterial;

        // // TODO: image processing
        // // OpenCVForUnity
        protected Mat in_mat;
        protected Mat out_mat;
        protected Mat dilateElement;
        protected bool cannyFiltering = true;

        // EyeTracking
        protected Vector2 eyePosition;
        protected int gazeLocking; // used as boolean (sent to shader)
        protected bool camLocking;

        // For reading phosphene configuration from JSON
        [SerializeField] string phospheneConfigurationFile;
        PhospheneConfiguration phospheneConfiguration; // is loaded from the above JSON file

        // stimulation parameters
        protected float stim;
        protected float[] activation;
        protected float[] memoryTrace;
        [SerializeField] float input_effect = 0.7f;
        [SerializeField] float intensity_decay = 0.8f;
        [SerializeField] float trace_increase = 0.1f;
        [SerializeField] float trace_decay = 0.9f;

        protected void Awake()
        {
            // Initialize material, input texture and shader
            material = new Material(shader);
            // imgprocMaterial = new Material(imgprocShader); TODO

            input_texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);


            // Load phosphene configuration and pass to shader
            phospheneConfiguration = PhospheneConfiguration.load(phospheneConfigurationFile);
            material.SetVectorArray(_PhospheneSpecs, phospheneConfiguration.specifications);
            material.SetInt("_nPhosphenes", phospheneConfiguration.phospheneCount);

            // Initialize other simulation variables
            activation = new float[phospheneConfiguration.phospheneCount];
            memoryTrace = new float[phospheneConfiguration.phospheneCount];

            // // OPENCV TODO
            in_mat = new Mat(512, 512, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
            out_mat = new Mat(512, 512, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
            dilateElement = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3)); // was new Size(9, 9))
        }

        // Postprocess the image
        void OnRenderImage (RenderTexture source, RenderTexture destination)
        {
            //1.  Read the pixels from 'source' (which is the active rendertexture by default)
            input_texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            input_texture.Apply();

            //2. Sample a subsection of the input texture for the activation  mask
            // TODO
            
            activation_mask = input_texture;

            //3. Apply image processing
            if (cannyFiltering){
                Utils.texture2DToMat(input_texture,in_mat);
                Imgproc.Canny(in_mat, out_mat, 110, 220);
                Imgproc.dilate(out_mat, out_mat, dilateElement);
                Utils.matToTexture2D(out_mat, activation_mask);
            }

            //4. Calculate the phosphene activations
            for (int i = 0; i < phospheneConfiguration.phospheneCount; i++){
              Vector2 pos = phospheneConfiguration.specifications[i]; // x and y coordinates
              if (camLocking) {
                pos += eyePosition- new Vector2(0.5f,0.5f);}

              if (pos.x > 0 && pos.x < 1 && pos.y > 0 && pos.y < 1) {
                stim = activation_mask.GetPixelBilinear(pos.x, pos.y).grayscale;}
              else {
              stim = 0;}
              activation[i] *= intensity_decay;
              activation[i] += Math.Max(0,input_effect*(stim-memoryTrace[i]));//);;
              memoryTrace[i] = memoryTrace[i] * trace_decay + trace_increase*stim;
            }

            // 5. apply the phosphene shader
            material.SetTexture(_PhospheneActivationMask, activation_mask);
            material.SetFloatArray(_PhospheneActivation, activation);
            material.SetVectorArray(_PhospheneSpecs, phospheneConfiguration.specifications);
            Graphics.Blit (source, destination, material);
        }

        protected void Update()
        {

            ProcessKeyboardInput();

            //
            material.SetVector(_PhospheneEyePos, eyePosition);
            material.SetInt(_PhospheneGazeLocked, gazeLocking);
        }

        private void ProcessKeyboardInput()
        {
            if (Keyboard.current.digit1Key.isPressed)
            {
                cannyFiltering = false;
                material.SetFloat(_PhospheneFilter, 0f);
            }

            if (Keyboard.current.digit2Key.isPressed)
            {
                cannyFiltering = true;
                material.SetFloat(_PhospheneFilter, 1f);
            }

            if (Keyboard.current.digit3Key.isPressed)
            {
                cannyFiltering = false;
                material.SetFloat(_PhospheneFilter, 1f);
            }

            if (Keyboard.current.digit4Key.isPressed)
            {
                cannyFiltering = true;
                material.SetFloat(_PhospheneFilter, 0f);
            }

            if (Keyboard.current.gKey.isPressed)
            {
                gazeLocking = 1 - gazeLocking;
            }

            if (Keyboard.current.cKey.isPressed)
            {
                camLocking = !camLocking;
            }

            if (Keyboard.current.escapeKey.isPressed)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

    }
}
