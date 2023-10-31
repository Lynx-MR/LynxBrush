
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Lynx
{
    public class PaintManager : MonoBehaviour
    {
        [SerializeField] private StrokeGenerator gen;

        #region SETTER FOR STROKE

        private Color m_paintColor;
        public Color paintCol
        {
            get
            {
                return m_paintColor;
            }
            set
            {
                m_paintColor = value;
                gen.color = value;
            }
        }

        private int m_paintAlpha;
        public int paintAlpha
        {
            get
            {
                return m_paintAlpha;
            }
            set
            {
                m_paintAlpha = value;
                gen.atlasIndex = value;
            }
        }

        private float m_paintSize;
        public float paintSize
        {
            get
            {
                return m_paintSize;
            }
            set
            {
                m_paintSize = value;
                gen.size = value;
            }
        }
        #endregion

        [HideInInspector]

        [SerializeField] private float pinchActivationDistance = 0.02f;

        private bool pinchFirstFrame = false;
        private bool leftPinch = false;

        //Bind OnUpdatedHands to hands update
        private void Start()
        {
            LynxHandtrackingAPI.HandSubsystem.updatedHands += OnUpdatedHands;
        }



        void OnUpdatedHands(XRHandSubsystem subsystem,
            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            if (updateType == XRHandSubsystem.UpdateType.Dynamic)
            {
                //left hand is used to undo
                if (subsystem.leftHand.isTracked)
                {
                    XRHand left = LynxHandtrackingAPI.LeftHand;
                    Vector3 indexPos = Vector3.positiveInfinity;
                    Vector3 thumbPos = Vector3.negativeInfinity;
                    //get thumb and index tip position
                    if (left.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose Ipose))
                        indexPos = Ipose.position;
                    if (left.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose Tpose))
                        thumbPos = Tpose.position;

                    //check distance between thumb and index for pinch
                    if (Vector3.Distance(indexPos, thumbPos) <= pinchActivationDistance)
                    {
                        if (!leftPinch)
                        {
                            leftPinch = true;
                            StartCoroutine(PinchTapCheck());
                        }
                    }
                    else
                        leftPinch = false;
                }

                //right hand is used to draw
                if (subsystem.rightHand.isTracked)
                {
                    XRHand right = LynxHandtrackingAPI.RightHand;
                    Vector3 indexPos = Vector3.positiveInfinity;
                    Vector3 thumbPos = Vector3.negativeInfinity;

                    if (right.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose Ipose))
                        indexPos = Ipose.position;
                    if (right.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose Tpose))
                        thumbPos = Tpose.position;
                    //set hand normal for plane generation 
                    if (right.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose Ppose))
                        gen.normal = Quaternion.LookRotation(indexPos - Ppose.position);

                    if (Vector3.Distance(indexPos, thumbPos) <= pinchActivationDistance)
                    {
                        Vector3 pinchPos = (indexPos + thumbPos) / 2.0f;
                        if (pinchFirstFrame)
                        {
                            //start a new strock 
                            pinchFirstFrame = false;
                            gen.pinchPos = pinchPos;
                            gen.NewStroke();
                        }
                        //update pinch position in stroke generator
                        gen.pinchPos = pinchPos;
                    }
                    else
                    {
                        gen.isPainting = false;
                        pinchFirstFrame = true;
                    }

                }
            }
        }

        /// <summary>
        /// if pinch is realesed in less than 0.5s undo last stroke
        /// </summary>
        /// <returns></returns>
        IEnumerator PinchTapCheck()
        {
            float t = 0;
            while (leftPinch)
            {
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            if (t < 0.5)
                gen.UndoStroke();
        }
    }
}