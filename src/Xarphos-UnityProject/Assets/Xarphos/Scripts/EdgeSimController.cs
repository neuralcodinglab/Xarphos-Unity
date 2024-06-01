using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering;

namespace Xarphos.Phisions3
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Renderer))]
    public class EdgeSimController : MonoBehaviour
    {
        [SerializeField] protected AppData appData;
        protected AudioSource AudioSource;
        protected Material phospheneMaterial;
        [SerializeField] protected Shader shader;

        // Webcam
        protected bool CameraInitialized;
        protected WebCamTexture WebCamTexture;
        protected int RequestedWidth = 240;//640;
        protected int RequestedHeight = 240;//480;
        
        // Temporary render texture
        [SerializeField] private Material imageProcessingMaterial;
        [SerializeField] protected Shader edgeDetectionShader;
        [SerializeField] private RenderTexture edgeTexture;


        private static readonly int MaskTex = Shader.PropertyToID("_MaskTex");
        private static readonly int Resolution = Shader.PropertyToID("_Resolution");
        private static readonly int Size_0 = Shader.PropertyToID("_Size_0");
        private static readonly int Magnification = Shader.PropertyToID("_Magnification");
        private static readonly int Jitter = Shader.PropertyToID("_Jitter");
        private static readonly int Size_var = Shader.PropertyToID("_Size_var");
        private static readonly int Intensity_var = Shader.PropertyToID("_Intensity_var");
        private static readonly int Brightness = Shader.PropertyToID("_Brightness");
        private static readonly int Saturation = Shader.PropertyToID("_Saturation");
        private static readonly int Dropout = Shader.PropertyToID("_Dropout");
        private static readonly int resolution_x = Shader.PropertyToID("ResX");
        private static readonly int resolution_y = Shader.PropertyToID("ResY");

        private static readonly int _MainTex = Shader.PropertyToID("_MainTex");

        protected Texture2D activation_mask;

        protected virtual void Awake()
        {
            // Play audio clip
            AudioSource = GetComponent<AudioSource>();
            AudioSource.PlayOneShot(AudioSource.clip);

            // Initialize material, shader, texture
            GetComponent<Renderer>().material = phospheneMaterial = new Material(shader);
            WebCamTexture = new WebCamTexture(WebCamTexture.devices[appData.Webcam].name, RequestedWidth, RequestedHeight, appData.Framerate);
            WebCamTexture.Play();

            // Camera Intialization
            StartCoroutine(_InitializeCamera());                     
            Init();
        }

        // Coroutine for camera-initialization
        private IEnumerator _InitializeCamera()
        {
            while (true)
            {
                if (WebCamTexture.didUpdateThisFrame)
                {
                    CameraInitialized = true;
                    UponCameraInitialization();
                    break;
                }
                yield return null;
            }
        }

        protected void UponCameraInitialization()
        {
            int h = WebCamTexture.height;
            int w = WebCamTexture.width;
            activation_mask = new Texture2D(appData.Resolution, appData.Resolution, TextureFormat.RGBA32, false);

            // Initialize materials with shaders
            imageProcessingMaterial = new Material(edgeDetectionShader);

            var descr = new RenderTextureDescriptor(w,h);
            edgeTexture = new RenderTexture(descr)
            {
                width = w,
                height = h,
                graphicsFormat = GraphicsFormat.R32G32_SFloat,
                depth = 0,
                enableRandomWrite = true
            };
            edgeTexture.Create();

        }

        private void OnDisable()
        {
            WebCamTexture.Stop();
        }

        protected void Init()
        {
            phospheneMaterial.SetVector(Resolution, new Vector2(appData.Resolution, appData.Resolution));
            phospheneMaterial.SetFloat(Size_0, appData.Size_0);
            phospheneMaterial.SetFloat(Magnification, appData.Magnification);
            phospheneMaterial.SetFloat(Jitter, appData.Jitter);
            phospheneMaterial.SetFloat(Size_var, appData.Size_var);
            phospheneMaterial.SetFloat(Intensity_var, appData.Size_var);
            phospheneMaterial.SetFloat(Brightness, appData.Brightness);
            phospheneMaterial.SetFloat(Saturation, appData.Saturation);
            phospheneMaterial.SetFloat(Dropout, appData.Dropout);
            imageProcessingMaterial.SetInt(resolution_x, RequestedWidth);
            imageProcessingMaterial.SetInt(resolution_y, RequestedWidth);
        }

        protected void Update()
        {
            if (CameraInitialized && WebCamTexture.isPlaying && WebCamTexture.didUpdateThisFrame)
            {
                imageProcessingMaterial.mainTexture = WebCamTexture;
                Graphics.Blit(imageProcessingMaterial.mainTexture, edgeTexture, imageProcessingMaterial);
                phospheneMaterial.SetTexture(MaskTex, edgeTexture);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Return to menu
                SceneManager.LoadScene(0);
            }
        }
    }


}


