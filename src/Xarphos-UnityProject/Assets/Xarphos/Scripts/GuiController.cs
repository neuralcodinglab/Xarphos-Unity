using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Xarphos.Phisions3
{
    public class GuiController : MonoBehaviour
    {
        [SerializeField] private AppData appData;
        [SerializeField] private Button buttonRun;
        [SerializeField] private Dropdown dropdownState;
        [SerializeField] private Dropdown dropdownWebcam;
        [SerializeField] private Slider sliderSize_0;
        [SerializeField] private Slider sliderFramerate;
        [SerializeField] private Slider sliderResolution;
        [SerializeField] private Slider sliderMagnification;
        [SerializeField] private Slider sliderJitter;
        [SerializeField] private Slider sliderSize_var;
        [SerializeField] private Slider sliderIntensity_var;
        [SerializeField] private Slider sliderThreshold_1;
        [SerializeField] private Slider sliderThreshold_2;
        [SerializeField] private Slider sliderBrightness;
        [SerializeField] private Slider sliderSaturation;
        [SerializeField] private Slider sliderDropout;
        [SerializeField] private Slider sliderDilation;
        [SerializeField] private Slider sliderBlur;
        [SerializeField] private Text textSize_0;
        [SerializeField] private Text textFramerate;
        [SerializeField] private Text textResolution;
        [SerializeField] private Text textMagnification;
        [SerializeField] private Text textJitter;
        [SerializeField] private Text textIntensity_var;
        [SerializeField] private Text textSize_var;
        [SerializeField] private Text textThreshold_1;
        [SerializeField] private Text textThreshold_2;
        [SerializeField] private Text textBrightness;
        [SerializeField] private Text textSaturation;
        [SerializeField] private Text textDropout;
        [SerializeField] private Text textDilation;
        [SerializeField] private Text textBlur;

        private void Awake()
        {
            buttonRun.interactable = false;
        }

        private void Start()
        {
            StartCoroutine(Authorize());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        }

        private IEnumerator Authorize()
        {
            while (!Application.HasUserAuthorization(UserAuthorization.WebCam))
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (WebCamTexture.devices.Length > 0) Init();
            else Application.Quit();
        }

        private void Init()
        {
            OnSize_0();
            OnFramerate();
            OnResolution();
            OnState();
            OnMagnification();
            OnJitter();
            dropdownWebcam.ClearOptions();
            dropdownWebcam.AddOptions(WebCamTexture.devices.Select(i => i.name).ToList());
            OnSize_var();
            OnIntensity_var();
            OnThreshold_1();
            OnThreshold_2();
            OnBrightness();
            OnSaturation();
            OnDropout();
            OnDilation();
            OnBlur();
            dropdownState.ClearOptions();
            dropdownState.AddOptions(Enum.GetValues(typeof(State)).Cast<State>().Select(i => i.ToString()).ToList());
            OnWebcam();
            buttonRun.interactable = true;
        }

        public void OnSize_0()
        {
            appData.Size_0 = sliderSize_0.value;
            textSize_0.text = $"{appData.Size_0:F4}";
        }


        public void OnFramerate()
        {
            appData.Framerate = Mathf.RoundToInt(sliderFramerate.value);
            textFramerate.text = $"{appData.Framerate:N0}";
        }

        public void OnResolution()
        {
            appData.Resolution = Mathf.RoundToInt(sliderResolution.value);
            textResolution.text = $"{appData.Resolution:N0}";
        }

        public void OnRun()
        {
            SceneManager.LoadScene((int)appData.State + 1);
        }

        public void OnMagnification()
        {
            appData.Magnification = sliderMagnification.value;
            textMagnification.text = $"{appData.Magnification:F4}";
        }

        public void OnJitter()
        {
            appData.Jitter = sliderJitter.value;
            textJitter.text = $"{appData.Jitter:F4}";
        }

        public void OnState()
        {
            appData.State = (State)dropdownState.value;
        }

        public void OnSize_var()
        {
            appData.Size_var = sliderSize_var.value;
            textSize_var.text = $"{appData.Size_var:F4}";
        }

        public void OnIntensity_var()
        {
            appData.Intensity_var = sliderIntensity_var.value;
            textIntensity_var.text = $"{appData.Intensity_var:F4}";
        }

        public void OnThreshold_1()
        {
            appData.Threshold_1 = sliderThreshold_1.value;
            textThreshold_1.text = $"{appData.Threshold_1:N0}";
        }


        public void OnThreshold_2()
        {
            appData.Threshold_2 = sliderThreshold_2.value;
            textThreshold_2.text = $"{appData.Threshold_2:N0}";
        }

        public void OnBrightness()
        {
            appData.Brightness = sliderBrightness.value;
            textBrightness.text = $"{appData.Brightness:F4}";
        }

        public void OnDropout()
        {
            appData.Dropout = sliderDropout.value;
            textDropout.text = $"{appData.Dropout:F4}";
        }


        public void OnSaturation()
        {
            appData.Saturation = sliderSaturation.value;
            textSaturation.text = $"{appData.Saturation:F4}";
        }

        public void OnDilation()
        {
            appData.Dilation = sliderDilation.value;
            textDilation.text = $"{appData.Dilation:F4}";
        }

        public void OnBlur()
        {
            appData.Blur = sliderBlur.value;
            textBlur.text = $"{appData.Blur:F4}";
        }

        public void OnWebcam()
        {
            appData.Webcam = dropdownWebcam.value;
        }
    }
}