using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.NiceVibrations;
using UnityEngine;

namespace KienNT
{
    public class VibrationController : MonoBehaviour
    {
        public static VibrationController Instance;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Vibrate()
        {
            MMVibrationManager.Vibrate();
        }

        public void HapticPulse(HapticTypes type)
        {
            switch (type)
            {
                case HapticTypes.Selection:
                    MMVibrationManager.Haptic (HapticTypes.Selection);
                    break;
                case HapticTypes.Success:
                    MMVibrationManager.Haptic (HapticTypes.Success);
                    break;
                case HapticTypes.Warning:
                    MMVibrationManager.Haptic (HapticTypes.Warning);
                    break;
                case HapticTypes.Failure:
                    MMVibrationManager.Haptic (HapticTypes.Failure);
                    break;
                case HapticTypes.LightImpact:
                    MMVibrationManager.Haptic (HapticTypes.LightImpact);
                    break;
                case HapticTypes.MediumImpact:
                    MMVibrationManager.Haptic (HapticTypes.MediumImpact);
                    break;
                case HapticTypes.HeavyImpact:
                    MMVibrationManager.Haptic (HapticTypes.HeavyImpact);
                    break;
                case HapticTypes.RigidImpact:
                    MMVibrationManager.Haptic (HapticTypes.RigidImpact);
                    break;
                case HapticTypes.SoftImpact:
                    MMVibrationManager.Haptic (HapticTypes.SoftImpact);
                    break;
                case HapticTypes.None:
                    break;
                default:
                    break;
                    // throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
  
}