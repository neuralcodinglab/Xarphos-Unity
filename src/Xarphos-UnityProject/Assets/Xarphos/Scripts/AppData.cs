using UnityEngine;

namespace Xarphos.Phisions3
{
    [CreateAssetMenu(menuName = "Phisions3/AppData")]
    public class AppData : ScriptableObject
    {
        [SerializeField] private float size_0;
        [SerializeField] private int framerate;
        [SerializeField] private bool locked;
        [SerializeField] private int resolution;
        [SerializeField] private float magnification;
        [SerializeField] private float jitter;
        [SerializeField] private State state;
        [SerializeField] private float size_var;
        [SerializeField] private float intensity_var;
        [SerializeField] private float threshold_1;
        [SerializeField] private float threshold_2;
        [SerializeField] private int webcam;
        [SerializeField] private float brightness;
        [SerializeField] private float saturation;
        [SerializeField] private float dropout;
        [SerializeField] private float blur;
        [SerializeField] private float dilation;



        // Webcam settings (finds closest available resolution)
        public int requestedWidth = 240;
        public int requestedHeight = 240;


        public float Size_0
        {
            get => size_0;
            set => size_0 = value;
        }



        public int Framerate
        {
            get => framerate;
            set => framerate = value;
        }

        public bool Locked
        {
            get => locked;
            set => locked = value;
        }

        public int Resolution
        {
            get => resolution;
            set => resolution = value;
        }

        public float Magnification
        {
            get => magnification;
            set => magnification = value;
        }

        public float Jitter
        {
            get => jitter;
            set => jitter = value;
        }

        public State State
        {
            get => state;
            set => state = value;
        }

        public float Size_var
        {
            get => size_var;
            set => size_var = value;
        }

        public float Intensity_var
        {
            get => intensity_var;
            set => intensity_var = value;
        }

        public float Threshold_1
        {
            get => threshold_1;
            set => threshold_1 = value;
        }

        public float Threshold_2
        {
            get => threshold_2;
            set => threshold_2 = value;
        }

        public int Webcam
        {
            get => webcam;
            set => webcam = value;
        }

        public float Brightness
        {
            get => brightness;
            set => brightness = value;
        }

        public float Saturation
        {
            get => saturation;
            set => saturation = value;
        }

        public float Dropout
        {
            get => dropout;
            set => dropout = value;
        }

        public float Dilation
        {
            get => dilation;
            set => dilation = value;
        }

        public float Blur
        {
            get => blur;
            set => blur = value;
        }

        private void OnEnable()
        {
            hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        private void Reset()
        {
            size_0 = 0.2f;
            framerate = 24;
            locked = true;
            resolution = 50;
            magnification = 0.0f;
            jitter = 0.5f;
            state = State.EdgePhosphenes;
            size_var = 1.0f;
            intensity_var = 1.0f;
            threshold_1 = 90.0f;
            threshold_2 = 150.0f;
            webcam = 0;
            brightness = 0.1f;
            saturation = 0.0f;
            dropout = 0.1f;
            dilation = 5;
            blur = 5;
        }
    }
}