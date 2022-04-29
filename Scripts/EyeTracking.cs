using System;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using ViveSR.anipal;
using ViveSR.anipal.Eye;
using EyeFramework = ViveSR.anipal.Eye.SRanipal_Eye_Framework;


namespace Xarphos.Scripts
{
    public class EyeTracking : MonoBehaviour
    {
        [Header("Debug Toggle")]
        public bool DebugGazeRays;
        public bool DebugGazeCalcs;
        
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

        private PhospheneSimulator sim;


        internal bool EyeTrackingAvailable { get; private set; }
        
#region Unity Event Functioens
        private void Start()
        {
            // SRanipal_Eye_v2.LaunchEyeCalibration();     // Perform calibration for eye tracking.
            Invoke(nameof(SystemCheck), .5f);
            Invoke(nameof(RegisterCallback), .5f);

            sim = GetComponent<PhospheneSimulator>();
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
            // ToDo: Set camera to only render clip of actual screen

            // Gaze Debugging
            GazeTestsUpdate();
            
            FocusInfo focusInfo;
            if (SRanipal_Eye_v2.Focus(GazeIndex.COMBINE, out _, out focusInfo, 0.01f, 10f, HallwayLayerMask, eyeData)) { }
            else if (SRanipal_Eye_v2.Focus(GazeIndex.LEFT, out _, out focusInfo, 0.01f, 10f, HallwayLayerMask, eyeData)) {}
            else if (SRanipal_Eye_v2.Focus(GazeIndex.RIGHT, out _, out focusInfo, 0.01f, 10f, HallwayLayerMask, eyeData)) {}

            Vector2 eyePos = sim.targetCamera.WorldToViewportPoint(focusInfo.point);
            // ToDo: Update only when valid; Consider gaze smoothing; Play with SrAnipal GazeParameter
            sim.SetEyePosition(eyePos);
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
        void OnApplicationQuit()
        {
            Release();
        }

        private void OnDisable()
        {
            Release();
        }
        
                

        /// <summary>
        /// Release callback thread when disabled or quit
        /// </summary>
        private void Release()
        {
            Debug.Log("Releasing...");
            if (eye_callback_registered)
            {
                SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(
                    Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback)
                );
                eye_callback_registered = false;
            }
            // Gaze Debugging
            if (DebugGazeRays) GazeTestsRelease();
            Debug.Log("Releasing Done.");
        }
        
        /// <summary>
        /// Required class for IL2CPP scripting backend support
        /// </summary>
        internal class MonoPInvokeCallbackAttribute : Attribute { }
#endregion

        private void GazeTestsUpdate()
        {
            if (DebugGazeCalcs)
            {
                GazeIntersection();
                GazeDebugUI();
            }

            if (DebugGazeRays)
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
        }

        private void GazeTestsRelease()
        {
            Debug.Log("Destroying");
            if (leftVisual != null)
            {
                Destroy(leftGazeLine.material);
                Destroy(leftVisual);
            }
            if (rightVisual != null)
            {
                Destroy(rightGazeLine.material);
                Destroy(rightVisual);
            }
            if (centreVisual != null)
            {
                Destroy(centreGazeLine.material);
                Destroy(centreVisual);
            }
        }

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
                leftVisual.transform.SetParent(transform, false);
                leftVisual.transform.localPosition = Vector3.zero;
                rightVisual = new GameObject("RightGazeRender");
                rightVisual.transform.SetParent(transform, false);
                rightVisual.transform.localPosition = Vector3.zero;
                centreVisual = new GameObject("CentreGazeRender");
                centreVisual.transform.SetParent(transform, false);
                centreVisual.transform.localPosition = Vector3.zero;
                leftGazeLine = leftVisual.AddComponent<LineRenderer>();
                {
                    leftGazeLine.material = new Material(Shader.Find("Standard"));
                    leftGazeLine.material.SetColor("_Color", Color.green);
                    // leftGazeLine.startColor = Color.green;
                    // leftGazeLine.endColor = Color.green;
                    leftGazeLine.startWidth = 0.005f;
                    leftGazeLine.endWidth = 0.005f;
                }
                rightGazeLine = rightVisual.AddComponent<LineRenderer>();
                {
                    rightGazeLine.material = new Material(Shader.Find("Standard"));
                    rightGazeLine.material.SetColor("_Color", Color.red);
                    rightGazeLine.startWidth = 0.005f;
                    rightGazeLine.endWidth = 0.005f;
                }
                centreGazeLine = centreVisual.AddComponent<LineRenderer>();
                {
                    centreGazeLine.material = new Material(Shader.Find("Standard"));
                    centreGazeLine.material.SetColor("_Color", Color.cyan);
                    centreGazeLine.startWidth = 0.005f;
                    centreGazeLine.endWidth = 0.005f;
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

            bool validityLeft =
                eyeData.verbose_data.left.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY) &&
                eyeData.verbose_data.left.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY);
            bool validityRight =
                eyeData.verbose_data.right.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY) &&
                eyeData.verbose_data.right.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY);
            bool validityCentre =
                eyeData.verbose_data.combined.eye_data.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY) &&
                eyeData.verbose_data.combined.eye_data.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY);

            // update render
            leftGazeLine.SetPositions(
                validityLeft ? new[] { leftOrigin, leftOrigin + leftDir * 20 } : new [] { Vector3.zero, Vector3.zero });
            rightGazeLine.SetPositions(
                validityRight ? new [] { rightOrigin, rightOrigin + rightDir * 20 } : new [] { Vector3.zero, Vector3.zero });
            centreGazeLine.SetPositions(
                validityCentre ? new[] { centreOrigin, centreOrigin + centreDir * 20 } : new [] { Vector3.zero, Vector3.zero });
        }
#endregion

#region eye position tests

[Header("Debug Stuff")] public TMP_Text MinGazeDistText;
public TMP_Text LeftOriginText, RightOriginText, CombOriginText, focusCastRadiusText, GazePointText;
[SerializeField] private LayerMask HallwayLayerMask;

private Vector3 left2Frustrum, right2Frustrum, comb2Frustrum, combConverged2Screen2Frustrum;
private GameObject FocusSphere = null, ConvergenceSphere = null, ConvergenceLeftPointSphere = null, ConvergenceRightPointSphere = null;
private float focusCastRadius = .01f;
        void GazeIntersection()
        {
            var cam = sim.targetCamera;
            var bottomLeft = cam.transform.InverseTransformPoint(cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane)));
            var upperLeft = cam.transform.InverseTransformPoint(cam.ViewportToWorldPoint(new Vector3(0, 1, cam.nearClipPlane)));
            var upperRight = cam.transform.InverseTransformPoint(cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane)));
            Plane clippingPlane = new Plane(upperRight, upperLeft, bottomLeft);

            var eyeOriginLeft = eyeData.verbose_data.left.gaze_origin_mm * .001f; // converted to "m", which is the space unity should be in
            var eyeDirLeft = eyeData.verbose_data.left.gaze_direction_normalized;
            var eyeOriginRight = eyeData.verbose_data.right.gaze_origin_mm * .001f;
            var eyeDirRight = eyeData.verbose_data.right.gaze_direction_normalized;
            var eyeOriginCombined = eyeData.verbose_data.combined.eye_data.gaze_origin_mm * .001f;
            var eyeDirCombined = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized;

            var gazeRayLeft = new Ray(eyeOriginLeft, eyeDirLeft);
            var gazeRayRight = new Ray(eyeOriginRight, eyeDirRight);
            var gazeRayCombined = new Ray(eyeOriginCombined, eyeDirCombined);

            clippingPlane.Raycast(gazeRayLeft, out var distLeft);
            clippingPlane.Raycast(gazeRayRight, out var distRight);
            clippingPlane.Raycast(gazeRayCombined, out var distComb);

            var intersectionLeft = gazeRayLeft.GetPoint(distLeft);
            var intersectionRight = gazeRayRight.GetPoint(distRight);
            var intersectionCombined = gazeRayCombined.GetPoint(distComb);

            left2Frustrum = cam.transform.TransformPoint(intersectionLeft);
            right2Frustrum = cam.transform.TransformPoint(intersectionRight);
            comb2Frustrum = cam.transform.TransformPoint(intersectionCombined);

            SRanipal_Eye_v2.Focus(GazeIndex.COMBINE, out _, out var focusInfo, focusCastRadius, 20f, HallwayLayerMask, eyeData);
            var frustrumPos = cam.WorldToViewportPoint(focusInfo.point);
            frustrumPos.z = cam.nearClipPlane;
            combConverged2Screen2Frustrum = cam.ViewportToWorldPoint(frustrumPos);
            
            if (FocusSphere is null)
            {
                FocusSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                {
                    FocusSphere.name = "FocusSphere";
                    FocusSphere.transform.parent = this.transform;
                    FocusSphere.transform.localScale = Vector3.one * .05f;
                    FocusSphere.GetComponent<Renderer>().material.color = Color.yellow;
                } 
            }

            FocusSphere.transform.position = focusInfo.point;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(left2Frustrum, .01f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(right2Frustrum, .01f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(comb2Frustrum, .01f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(combConverged2Screen2Frustrum, .01f);
        }

        float MinDistanceGazeVectors()
        {
            var cross = Vector3.Cross(eyeData.verbose_data.left.gaze_direction_normalized,
                eyeData.verbose_data.right.gaze_direction_normalized);
            var d = Vector3.Scale(
                eyeData.verbose_data.left.gaze_origin_mm * 0.001f - eyeData.verbose_data.right.gaze_origin_mm * 0.001f,
                cross.normalized
            ).sqrMagnitude;

            // Point of closest distance
            float tL, tR, tP;
            var vR = gaze_direct_R;
            var vL = gaze_direct_L;
            var vP = cross;
            var RHS = gaze_origin_R - gaze_origin_L;
            tP = (RHS[0] - RHS[2] * vL[0] / vL[2]) / 
                 ( vP[0] - ( vL[0]*vP[2]/vL[2] ) +
                     ( (vL[0]*vR[2] - vR[0]*vL[2]) / 
                     (vR[2]*vL[1] - vR[1]*vL[2])) * (vL[1]*vP[2]/vL[2]) 
                 );
            tR = (RHS[1] - vL[1]/vL[2] * (RHS[2] - tP * vP[2])) / ((vR[2] * vL[1]) / vL[2] - vR[1]);
            tL = RHS[2] + tR * vR[2] - tP * vP[2];

            if (ConvergenceSphere is null)
            {
                ConvergenceSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                {
                    ConvergenceSphere.name = "ConvergenceSphere";
                    ConvergenceSphere.transform.parent = transform;
                    ConvergenceSphere.transform.localScale = Vector3.one * .05f;
                    ConvergenceSphere.GetComponent<Renderer>().material.color = new Color(.8f, .5f, .2f);
                }
                ConvergenceLeftPointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                {
                    ConvergenceLeftPointSphere.name = "ConvergenceLeftPointSphere";
                    ConvergenceLeftPointSphere.transform.parent = transform;
                    ConvergenceLeftPointSphere.transform.localScale = Vector3.one * .01f;
                    ConvergenceLeftPointSphere.GetComponent<Renderer>().material.color = Color.green;
                }
                ConvergenceRightPointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                {
                    ConvergenceRightPointSphere.name = "ConvergenceRightPointSphere";
                    ConvergenceRightPointSphere.transform.parent = transform;
                    ConvergenceRightPointSphere.transform.localScale = Vector3.one * .01f;
                    ConvergenceRightPointSphere.GetComponent<Renderer>().material.color = Color.red;
                }
            }

            ConvergenceLeftPointSphere.transform.position = gaze_origin_L + tL * vL;
            ConvergenceRightPointSphere.transform.position = gaze_origin_R + tR * vR;
            ConvergenceSphere.transform.position = gaze_origin_R + tR * vR + tP / 2f * vP;
            
            return Mathf.Sqrt(d);
        }

        void GazeDebugUI()
        {
            if (Keyboard.current.upArrowKey.isPressed)
            {
                focusCastRadius += .01f;
            } else if (Keyboard.current.downArrowKey.isPressed)
            {
                focusCastRadius += -0.01f;
            }
            
            var gaze_origin_C = eyeData.verbose_data.combined.eye_data.gaze_origin_mm;
            MinGazeDistText.text = $"minD_LR: {MinDistanceGazeVectors() * 1000f:F}mm";
            LeftOriginText.text  = $"Left : {gaze_origin_L.x:F1},{gaze_origin_L.y:F1},{gaze_origin_L.z:F1}";
            RightOriginText.text = $"Right: {gaze_origin_R.x:F1},{gaze_origin_R.y:F1},{gaze_origin_R.z:F1}";
            CombOriginText.text  = $"Comb : {gaze_origin_C.x:F1},{gaze_origin_C.y:F1},{gaze_origin_C.z:F1}";
            focusCastRadiusText.text = $"FocusRayR: {focusCastRadius:F}mm";

            
            
            var tmp1 = gaze_origin_L - gaze_origin_R + Vector3.Cross(gaze_direct_L.normalized, gaze_direct_R.normalized);
            var tmp2 = gaze_direct_R - gaze_direct_L;
            var t = tmp1.x / tmp2.x;
            var PL = gaze_origin_L + t * gaze_direct_L;
            var PR = gaze_origin_R + t * gaze_direct_R;
            var posL = ConvergenceLeftPointSphere.transform.position;
            var posR = ConvergenceRightPointSphere.transform.position;
            var posC = ConvergenceSphere.transform.position;
            GazePointText.text = $"L:{posL.x:F1},{posL.y:F1},{posL.z:F1}\n" +
                                 $"R:{posR.x:F1},{posR.y:F1},{posR.z:F1}\n" +
                                 $"C:{posC.x:F1},{posC.y:F1},{posC.z:F1}";
        }

        #endregion
    }
}
