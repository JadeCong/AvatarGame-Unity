using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JustWithJoints.Avatars
{
    public class UnityChanAvatar : MonoBehaviour
    {
        public GameObject MotionProvider;
        public bool RetargetPose = true;
        public bool RetargetRootLocation = true;

        List<GameObject> joints_ = new List<GameObject>();
        List<GameObject> bones_ = new List<GameObject>();
        
        // Use this for initialization
        void Start()
        {
            var root = gameObject.transform.Find("Character1_Reference").gameObject;
            var hips = root.gameObject.transform.Find("Character1_Hips").gameObject;
            var rightUpLeg = hips.transform.Find("Character1_LeftUpLeg").gameObject;
            var rightLeg = rightUpLeg.transform.Find("Character1_LeftLeg").gameObject;
            var rightFoot = rightLeg.transform.Find("Character1_LeftFoot").gameObject;
            var rightToeBase = rightFoot.transform.Find("Character1_LeftToeBase").gameObject;
            var leftUpLeg = hips.transform.Find("Character1_RightUpLeg").gameObject;
            var leftLeg = leftUpLeg.transform.Find("Character1_RightLeg").gameObject;
            var leftFoot = leftLeg.transform.Find("Character1_RightFoot").gameObject;
            var leftToeBase = leftFoot.transform.Find("Character1_RightToeBase").gameObject;
            var spine = hips.transform.Find("Character1_Spine").gameObject;
            var spine1 = spine.transform.Find("Character1_Spine1").gameObject;
            var spine2 = spine1.transform.Find("Character1_Spine2").gameObject;
            var rightShoulder = spine2.transform.Find("Character1_LeftShoulder").gameObject;
            var rightArm = rightShoulder.transform.Find("Character1_LeftArm").gameObject;
            var rightForeArm = rightArm.transform.Find("Character1_LeftForeArm").gameObject;
            var rightHand = rightForeArm.transform.Find("Character1_LeftHand").gameObject;
            var leftShoulder = spine2.transform.Find("Character1_RightShoulder").gameObject;
            var leftArm = leftShoulder.transform.Find("Character1_RightArm").gameObject;
            var leftForeArm = leftArm.transform.Find("Character1_RightForeArm").gameObject;
            var leftHand = leftForeArm.transform.Find("Character1_RightHand").gameObject;
            var neck = spine2.transform.Find("Character1_Neck").gameObject;
            var head = neck.transform.Find("Character1_Head").gameObject;

            joints_.AddRange(new GameObject[]
            {
                rightFoot,
                rightLeg,
                rightUpLeg,
                leftUpLeg,
                leftLeg,
                leftFoot,
                rightHand,
                rightForeArm,
                rightShoulder,
                leftShoulder,
                leftForeArm,
                leftHand,
                neck,
                head,
            });

            bones_.AddRange(new GameObject[]
            {
                hips,
                rightUpLeg,
                rightLeg,
                leftUpLeg,
                leftLeg,
                spine,
                rightShoulder,
                rightArm,
                rightForeArm,
                leftShoulder,
                leftArm,
                leftForeArm,
                neck,
            });
        }

        void LateUpdate()
        {
            Core.Pose pose = null;
            if (MotionProvider)
            {
                var component = MotionProvider.GetComponent<CMUMotionPlayer>();
                if (component)
                {
                    pose = component.GetCurrentPose();
                }
            }

            if (pose != null)
            {
                if (RetargetRootLocation)
                {
                    bones_[0].transform.position = (pose.Positions[2] + pose.Positions[3]) * 0.5f + gameObject.transform.position;
                }

                if (RetargetPose)
                {
                    for (int i = 0; i < 13; i++)
                    {
                        int boneIndex = i;
                        if (i == (int)Core.BoneType.Trans)
                        {
                            // Use spine of Core.Pose as root of avatar
                            boneIndex = (int)Core.BoneType.Spine;
                        }
                        if (i == (int)Core.BoneType.Spine)
                        {
                            // Skip spine
                            continue;
                        }
                        bones_[i].transform.rotation = pose.Rotations[boneIndex];
                    }
                }
            }
        }
    }
}