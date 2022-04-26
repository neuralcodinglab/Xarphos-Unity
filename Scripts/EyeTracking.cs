using System;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using ViveSR.anipal;
using ViveSR.anipal.Eye;
using EyeFramework = ViveSR.anipal.Eye.SRanipal_Eye_Framework;


namespace Xarphos.Scripts
{
    public class EyeTracking : MonoBehaviour
    {
        private static EyeData_v2 eyeData;
        public EyeParameter eye_parameter;
        public GazeRayParameter gaze;
        private static bool eye_callback_registered;
        private static UInt64 eye_valid_L, eye_valid_R;                 // The bits explaining the validity of eye data.
        private static float openness_L, openness_R;                    // The level of eye openness.
        private static float pupil_diameter_L, pupil_diameter_R;        // Diameter of pupil dilation.
        private static Vector2 pos_sensor_L, pos_sensor_R;              // Positions of pupils.
        private static Vector3 gaze_origin_L, gaze_origin_R;            // Position of gaze origin.
        private static Vector3 gaze_direct_L, gaze_direct_R;            // Direction of gaze ray.
        private static float frown_L, frown_R;                          // The level of user's frown.
        private static float squeeze_L, squeeze_R;                      // The level to show how the eye is closed tightly.
        private static float wide_L, wide_R;                            // The level to show how the eye is open widely.
        private static double gaze_sensitive;                           // The sensitive factor of gaze ray.
        private static float distance_C;                                // Distance from the central point of right and left eyes.
        private static bool distance_valid_C;                           // Validity of combined data of right and left eyes.
        
        private static int track_imp_cnt = 0;
        private static TrackingImprovement[] track_imp_item;
        
        private static long MeasureTime, CurrentTime, MeasureEndTime;
        private static float time_stamp;
        private static int frame;


        internal bool EyeTrackingAvailable { get; private set; }
        
#region Unity Event Functioens
        private void Start()
        {
            // SRanipal_Eye_v2.LaunchEyeCalibration();     // Perform calibration for eye tracking.
            Invoke(nameof(SystemCheck), .5f);
            Invoke(nameof(RegisterCallback), .5f);
        }
        
        private void FixedUpdate()
        {
            if (!CheckFrameworkStatusErrors())
            {
                EyeTrackingAvailable = false;
                // Debug.LogWarning("Framework Responded failure to work.");
            }
        }

        private void Update()
        {
            if (Keyboard.current[Key.F8].wasPressedThisFrame)
            {
                SetDebugGazeRender(!renderGazeRays);
            }
            if (Keyboard.current[Key.F9].wasPressedThisFrame)
            {
                freezeGazeRays = !freezeGazeRays;
            }
            if (renderGazeRays && !freezeGazeRays)
            {
                UpdateGazeRays();
            }
        }
#endregion

#region EyeData Setup
        private void SystemCheck()
        {
            if (EyeFramework.Status == EyeFramework.FrameworkStatus.NOT_SUPPORT)
            {
                EyeTrackingAvailable = false;
                Debug.LogWarning("Eye Tracking Not Supported");
                return;
            }
            

            var eyeDataResult = SRanipal_Eye_API.GetEyeData_v2(ref eyeData);
            var eyeParamResult = SRanipal_Eye_API.GetEyeParameter(ref eye_parameter);
            var resultEyeInit = SRanipal_API.Initial(SRanipal_Eye_v2.ANIPAL_TYPE_EYE_V2, IntPtr.Zero);
            
            if (
                eyeDataResult != ViveSR.Error.WORK ||
                eyeParamResult != ViveSR.Error.WORK ||
                resultEyeInit != ViveSR.Error.WORK
            )
            {
                Debug.LogError("Inital Check failed.\n" +
                               $"[SRanipal] Eye Data Call v2 : {eyeDataResult}" +
                               $"[SRanipal] Eye Param Call v2: {eyeParamResult}" +
                               $"[SRanipal] Initial Eye v2   : {resultEyeInit}"
                );
                EyeTrackingAvailable = false;
                return;
            }

            EyeTrackingAvailable = true;
        }

        private void RegisterCallback()
        {
            var eyeParameter = new EyeParameter();
            SRanipal_Eye_API.GetEyeParameter(ref eyeParameter);

            if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback && !eye_callback_registered)
            {
                SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(
                    Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback)
                );
                eye_callback_registered = true;
            }

            else if (!SRanipal_Eye_Framework.Instance.EnableEyeDataCallback && eye_callback_registered)
            {
                SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(
                    Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback)
                );
                eye_callback_registered = false;
            }
        }
#endregion

#region EyeTracking Data Collection
        [MonoPInvokeCallback]
        private static void EyeCallback(ref EyeData_v2 eyeDataRef)
        {
            var eyeParameter = new EyeParameter();
            SRanipal_Eye_API.GetEyeParameter(ref eyeParameter);
            eyeData = eyeDataRef;
            
            var fetchResult = SRanipal_Eye_API.GetEyeData_v2(ref eyeData);
            if (fetchResult != ViveSR.Error.WORK) return;
            
            MeasureTime = DateTime.Now.Ticks;
            time_stamp = eyeData.timestamp;
            frame = eyeData.frame_sequence;
            eye_valid_L = eyeData.verbose_data.left.eye_data_validata_bit_mask;
            eye_valid_R = eyeData.verbose_data.right.eye_data_validata_bit_mask;
            openness_L = eyeData.verbose_data.left.eye_openness;
            openness_R = eyeData.verbose_data.right.eye_openness;
            pupil_diameter_L = eyeData.verbose_data.left.pupil_diameter_mm;
            pupil_diameter_R = eyeData.verbose_data.right.pupil_diameter_mm;
            pos_sensor_L = eyeData.verbose_data.left.pupil_position_in_sensor_area;
            pos_sensor_R = eyeData.verbose_data.right.pupil_position_in_sensor_area;
            gaze_origin_L = eyeData.verbose_data.left.gaze_origin_mm;
            gaze_origin_R = eyeData.verbose_data.right.gaze_origin_mm;
            gaze_direct_L = eyeData.verbose_data.left.gaze_direction_normalized;
            gaze_direct_R = eyeData.verbose_data.right.gaze_direction_normalized;
            gaze_sensitive = eyeParameter.gaze_ray_parameter.sensitive_factor;
            frown_L = eyeData.expression_data.left.eye_frown;
            frown_R = eyeData.expression_data.right.eye_frown;
            squeeze_L = eyeData.expression_data.left.eye_squeeze;
            squeeze_R = eyeData.expression_data.right.eye_squeeze;
            wide_L = eyeData.expression_data.left.eye_wide;
            wide_R = eyeData.expression_data.right.eye_wide;
            distance_valid_C = eyeData.verbose_data.combined.convergence_distance_validity;
            distance_C = eyeData.verbose_data.combined.convergence_distance_mm;
            track_imp_cnt = eyeData.verbose_data.tracking_improvements.count;
        }

        internal bool EyeTrackingStep(out Vector2 eyePosition)
        {
            eyePosition = Vector2.zero;
            // if (!EyeTrackingAvailable)
            // {
            //     return false;
            // }
            // VerboseData vData;
            // SRanipal_Eye_v2.GetVerboseData(out vData);
            // var gazeDir = vData.combined.eye_data.gaze_direction_normalized;
            // gazeDir.x *= -1; // ToDo: This should not be necessary? Why is ray mirrored?
            //
            // RaycastHit hit;
            // if (Physics.Raycast(simCam.transform.position, gazeDir, out hit))
            // {
            //     eyePosition = hit.textureCoord;
            //     return true;
            // }
            //
            return false;
        }

        private bool CheckFrameworkStatusErrors()
        {
            return EyeFramework.Status == EyeFramework.FrameworkStatus.WORKING;
        }
#endregion

        private int Gcf(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
       
#region EyeData Clean Up & Necessities
        private void OnDisable()
        {
            Release();
        }

        void OnApplicationQuit()
        {
            Release();
        }
        
        /// <summary>
        /// Release callback thread when disabled or quit
        /// </summary>
        private static void Release()
        {
            if (eye_callback_registered == true)
            {
                SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(
                    Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback)
                );
                eye_callback_registered = false;
            }
            
            Destroy(leftVisual);
            Destroy(rightVisual);
        }
        
        /// <summary>
        /// Required class for IL2CPP scripting backend support
        /// </summary>
        internal class MonoPInvokeCallbackAttribute : System.Attribute
        {
            public MonoPInvokeCallbackAttribute() { }
        }
#endregion

#region Debug Gaze Rendering
        private static bool renderGazeRays, freezeGazeRays;
        private static GameObject leftVisual, rightVisual, centreVisual;
        private static LineRenderer leftGazeLine, rightGazeLine, centreGazeLine;

        private static Camera mainCam;

        private void SetDebugGazeRender(bool status)
        {
            // first acticvation: set up objects and references
            if (status && (leftVisual == null || rightVisual == null || centreVisual == null))
            {
                mainCam = Camera.main;
                leftVisual = new GameObject("LeftGazeRender");
                rightVisual = new GameObject("RightGazeRender");
                centreVisual = new GameObject("CentreGazeRender");
                leftGazeLine = leftVisual.AddComponent<LineRenderer>();
                {
                    leftGazeLine.startColor = Color.green;
                    leftGazeLine.endColor = Color.green;
                    leftGazeLine.startWidth = 0.005f;
                    leftGazeLine.endWidth = 0.005f;
                }
                rightGazeLine = rightVisual.AddComponent<LineRenderer>();
                {
                    rightGazeLine.startColor = Color.red;
                    rightGazeLine.endColor = Color.red;
                    rightGazeLine.startWidth = 0.005f;
                    rightGazeLine.endWidth = 0.005f;
                }
                centreGazeLine = centreVisual.AddComponent<LineRenderer>();
                {
                    rightGazeLine.startColor = Color.white;
                    rightGazeLine.endColor = Color.white;
                    rightGazeLine.startWidth = 0.005f;
                    rightGazeLine.endWidth = 0.005f;
                }
            }
            
            renderGazeRays = status;
            leftVisual.SetActive(status);
            rightVisual.SetActive(status);
            centreVisual.SetActive(status);
        }

        private void UpdateGazeRays()
        {
            Vector3 leftOrigin, rightOrigin, centreOrigin, leftDir, rightDir, centreDir;
            
            // update gaze data
            SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out leftOrigin, out leftDir, eyeData);
            SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out rightOrigin, out rightDir, eyeData);
            SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out centreOrigin, out centreDir, eyeData);

            // transform from local space 
            var t = mainCam.transform;
            leftOrigin = t.TransformPoint(leftOrigin);
            rightOrigin = t.TransformPoint(rightOrigin);
            centreOrigin = t.TransformPoint(centreOrigin);
            leftDir = t.TransformDirection(leftDir);
            rightDir = t.TransformDirection(rightDir);
            centreDir = t.TransformDirection(centreDir);

            // update render
            leftGazeLine.SetPositions(new [] { leftOrigin, leftOrigin + leftDir * 20 });
            rightGazeLine.SetPositions(new [] { rightOrigin, rightOrigin + rightDir * 20 });
            centreGazeLine.SetPositions(new[] { centreOrigin, centreOrigin + centreDir * 20 });
        }
#endregion
    }
}
