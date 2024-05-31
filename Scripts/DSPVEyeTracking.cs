using System;
using UnityEngine;
using ViveSR.anipal.Eye;
using EyeFramework = ViveSR.anipal.Eye.SRanipal_Eye_Framework;


namespace Xarphos.Scripts
{
    // [RequireComponent(typeof(DSPV_SimulationController))]
    public class DSPVEyeTracking : MonoBehaviour
    {

      public enum EyeTrackingConditions
      {
        GazeIgnored = 0,
        SimulationFixedToGaze = 1,
        GazeAssistedSampling	= 2,
      }

        // Added for Eye Tracking Implementation
        [SerializeField] private Camera simCam;
        internal bool EyeTrackingAvailable { get; private set; }

        private void Start()
        {
            EyeTrackingAvailable = SRanipal_Eye_Framework.Instance.EnableEye;
        }

        private void FixedUpdate()
        {
            EyeTrackingAvailable = CheckFrameworkStatusErrors();
        }

        internal bool EyeTrackingStep(out Vector2 eyePosition)
        {
            eyePosition = Vector2.zero;
            if (!EyeTrackingAvailable)
            {
                return false;
            }
            VerboseData vData;
            SRanipal_Eye_v2.GetVerboseData(out vData);
            var gazeDir = vData.combined.eye_data.gaze_direction_normalized;
            gazeDir.x *= -1; // ToDo: This should not be necessary? Why is ray mirrored?

            RaycastHit hit;
            if (Physics.Raycast(simCam.transform.position, gazeDir, out hit))
            {
                eyePosition = hit.textureCoord;
                return true;
            }

            return false;
        }

        private bool CheckFrameworkStatusErrors()
        {
            return EyeFramework.Status == EyeFramework.FrameworkStatus.WORKING &&
                   EyeFramework.Status != EyeFramework.FrameworkStatus.NOT_SUPPORT;
        }

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
    }
}
