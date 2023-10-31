/**
 * @file LynxHandtrackingAPI.cs
 * 
 * @author Geoffrey Marhuenda
 * 
 * @brief API to easily access Hands through XR Hands subsystem.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Lynx
{
    public class LynxHandtrackingAPI
    {
        private static XRHandSubsystem m_handSubsystem = null;

        public static void Init()
        {
            List<XRHandSubsystem> handSusbsytems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(handSusbsytems);
            if (handSusbsytems.Count > 0)
                m_handSubsystem = handSusbsytems[0];
        }

        public static XRHandSubsystem HandSubsystem  {
            get
            {
                if (m_handSubsystem == null)
                    Init();

                return m_handSubsystem;
            }
        }
        public static XRHand LeftHand => HandSubsystem.leftHand;
        public static XRHand RightHand => HandSubsystem.rightHand;

    }
}