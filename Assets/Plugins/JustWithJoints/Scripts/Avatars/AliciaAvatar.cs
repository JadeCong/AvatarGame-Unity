using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JustWithJoints.Avatars
{
    public class AliciaAvatar : MonoBehaviour
    {
        public GameObject MotionProvider;
        public bool EnableRetargetting = true;
        public bool RetargetTranslation = true;

        List<GameObject> joints = new List<GameObject>();
        List<GameObject> bones_ = new List<GameObject>();

        // The local coordinates of Alicia are not same as Core.Pose.
        // Fix the differences by multiplying these correction rotations
        /*public*/ Vector3[] correctionRightEulers = new Vector3[13]
        {
            new Vector3(0, -90, 180),
            new Vector3(0, 90, -90),
            new Vector3(0, 90, -90),
            new Vector3(0, 90, -90),
            new Vector3(0, 90, -90),
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(0, -90, 180),
            new Vector3(0, -90, 180),
            new Vector3(0, 0, 0),
            new Vector3(0, 90, 180),
            new Vector3(0, 90, 180),
            new Vector3(0, -90, 180),
        };

        void Start()
        {
            var root = gameObject.transform.Find("Character001").transform.Find("root").gameObject;
            var hips = root.gameObject.transform.Find("waist").gameObject;
            var rightUpLeg = hips.transform.Find("lowerbody").transform.Find("leg_L").gameObject;
            var rightLeg = rightUpLeg.transform.Find("knee_L").gameObject;
            var rightFoot = rightLeg.transform.Find("ankle_L").gameObject;
            var rightToeBase = rightFoot.transform.Find("toe_L").gameObject;
            var leftUpLeg = hips.transform.Find("lowerbody").transform.Find("leg_R").gameObject;
            var leftLeg = leftUpLeg.transform.Find("knee_R").gameObject;
            var leftFoot = leftLeg.transform.Find("ankle_R").gameObject;
            var leftToeBase = leftFoot.transform.Find("toe_R").gameObject;
            var spine = hips.transform.Find("upperbody").gameObject;
            var spine1 = spine.transform.Find("upperbody01").gameObject;
            var rightShoulder = spine1.transform.Find("shoulder_L").gameObject;
            var rightArm = rightShoulder.transform.Find("arm_L").gameObject;
            var rightForeArm = rightArm.transform.Find("elbow_L").gameObject;
            var rightHand = rightForeArm.transform.Find("wrist_L").gameObject;
            var leftShoulder = spine1.transform.Find("shoulder_R").gameObject;
            var leftArm = leftShoulder.transform.Find("arm_R").gameObject;
            var leftForeArm = leftArm.transform.Find("elbow_R").gameObject;
            var leftHand = leftForeArm.transform.Find("wrist_R").gameObject;
            var neck = spine1.transform.Find("neck").gameObject;
            var head = neck.transform.Find("head").gameObject;

            joints.AddRange(new GameObject[]
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
            // Get pose
            Core.Pose pose = null;
            if (MotionProvider)
            {
                var component = MotionProvider.GetComponent<CMUMotionPlayer>();
                if (component)
                {
                    pose = component.GetCurrentPose();
                }
            }

            // Retarget
            if (pose != null)
            {
                // Retarget positions
                if (RetargetTranslation)
                {
                    bones_[0].transform.position = (pose.Positions[2] + pose.Positions[3]) * 0.5f + gameObject.transform.position;
                }

                if (EnableRetargetting)
                {
                    // Retarget rotations
                    for (int i = 0; i < 13; i++)
                    {
                        int boneIndex = i;
                        if (i == (int)Core.BoneType.Trans)
                        {
                            // Use spine as root of avatar
                            boneIndex = (int)Core.BoneType.Spine;
                        }
                        if (i == (int)Core.BoneType.Spine)
                        {
                            // Skip spine
                            continue;
                        }
                        if (i == (int)Core.BoneType.RightShoulder || i == (int)Core.BoneType.LeftShoulder)
                        {
                            // Skip shoulders because bone hierarchy is not same as Core.Pose
                            continue;
                        }
                        bones_[i].transform.rotation = pose.Rotations[boneIndex] * Quaternion.Euler(correctionRightEulers[i]);
                    }
                }
            }
        }
    }
}