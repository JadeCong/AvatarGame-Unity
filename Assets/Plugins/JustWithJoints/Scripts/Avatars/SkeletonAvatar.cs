using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JustWithJoints.Avatars
{
    public class SkeletonAvatar : MonoBehaviour
    {
        public GameObject MotionProvider;
        public bool UseFK = false;
        private GameObject[] bones_ = null;

        void Start()
        {
            bones_ = new GameObject[13];
            for (int i = 0; i < bones_.Length; i++)
            {
                bones_[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bones_[i].transform.parent = gameObject.transform;
            }
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
                if (UseFK)
                {
                    moveSkeleton(pose.FK());
                }
                else
                {
                    moveSkeleton(pose.Positions);
                }
                // drawSkeletonLines(pose.Positions, new Vector3(1.5f, 0, 0));
            }
        }
        
        void moveSkeleton(List<Vector3> joints3D)
        {
            joints3D = joints3D.ToList();
            for (int i = 0; i < joints3D.Count; i++)
            {
                joints3D[i] += gameObject.transform.position;
            }
            var light_pink = color(233, 163, 201);
            var pink = color(197, 27, 125);
            var light_blue = color(145, 191, 219);
            var blue = color(69, 117, 180);
            var purple = color(118, 42, 131);
            int boneId = 0;
            moveBone(boneId++, joints3D[(int)Core.JointType.RightAnkle], joints3D[(int)Core.JointType.RightKnee], light_pink);
            moveBone(boneId++, joints3D[(int)Core.JointType.RightKnee], joints3D[(int)Core.JointType.RightHip], light_pink);
            moveBone(boneId++, joints3D[(int)Core.JointType.RightShoulder], joints3D[(int)Core.JointType.RightHip], light_pink);
            moveBone(boneId++, joints3D[(int)Core.JointType.LeftHip], joints3D[(int)Core.JointType.LeftKnee], pink);
            moveBone(boneId++, joints3D[(int)Core.JointType.LeftKnee], joints3D[(int)Core.JointType.LeftAnkle], pink);
            moveBone(boneId++, joints3D[(int)Core.JointType.LeftShoulder], joints3D[(int)Core.JointType.LeftHip], pink);
            moveBone(boneId++, joints3D[(int)Core.JointType.RightWrist], joints3D[(int)Core.JointType.RightElbow], light_blue);
            moveBone(boneId++, joints3D[(int)Core.JointType.RightElbow], joints3D[(int)Core.JointType.RightShoulder], light_blue);
            moveBone(boneId++, joints3D[(int)Core.JointType.RightShoulder], joints3D[(int)Core.JointType.Neck], light_blue);
            moveBone(boneId++, joints3D[(int)Core.JointType.LeftShoulder], joints3D[(int)Core.JointType.LeftElbow], blue);
            moveBone(boneId++, joints3D[(int)Core.JointType.LeftElbow], joints3D[(int)Core.JointType.LeftWrist], blue);
            moveBone(boneId++, joints3D[(int)Core.JointType.LeftShoulder], joints3D[(int)Core.JointType.Neck], blue);
            moveBone(boneId++, joints3D[(int)Core.JointType.Neck], joints3D[(int)Core.JointType.Head], purple);
        }

        void moveBone(int boneId, Vector3 src, Vector3 tgt, Color c)
        {
            float len = (tgt - src).magnitude;
            bones_[boneId].transform.localScale = new Vector3(0.1f, len, 0.1f);
            bones_[boneId].transform.rotation = Quaternion.FromToRotation(Vector3.up, tgt - src);
            bones_[boneId].transform.position = (tgt + src) * 0.5f;
            bones_[boneId].GetComponent<Renderer>().material.color = c;
        }

        void drawSkeletonLines(List<Vector3> joints3D, Vector3 offset)
        {
            joints3D = joints3D.ToList();
            for (int i = 0; i < joints3D.Count; i++)
            {
                joints3D[i] += offset;
            }
            var light_pink = color(233, 163, 201);
            var pink = color(197, 27, 125);
            var light_blue = color(145, 191, 219);
            var blue = color(69, 117, 180);
            var purple = color(118, 42, 131);
            Debug.DrawLine(joints3D[(int)Core.JointType.RightAnkle], joints3D[(int)Core.JointType.RightKnee], light_pink);
            Debug.DrawLine(joints3D[(int)Core.JointType.RightKnee], joints3D[(int)Core.JointType.RightHip], light_pink);
            Debug.DrawLine(joints3D[(int)Core.JointType.RightShoulder], joints3D[(int)Core.JointType.RightHip], light_pink);
            Debug.DrawLine(joints3D[(int)Core.JointType.LeftHip], joints3D[(int)Core.JointType.LeftKnee], pink);
            Debug.DrawLine(joints3D[(int)Core.JointType.LeftKnee], joints3D[(int)Core.JointType.LeftAnkle], pink);
            Debug.DrawLine(joints3D[(int)Core.JointType.LeftShoulder], joints3D[(int)Core.JointType.LeftHip], pink);
            Debug.DrawLine(joints3D[(int)Core.JointType.RightWrist], joints3D[(int)Core.JointType.RightElbow], light_blue);
            Debug.DrawLine(joints3D[(int)Core.JointType.RightElbow], joints3D[(int)Core.JointType.RightShoulder], light_blue);
            Debug.DrawLine(joints3D[(int)Core.JointType.RightShoulder], joints3D[(int)Core.JointType.Neck], light_blue);
            Debug.DrawLine(joints3D[(int)Core.JointType.LeftShoulder], joints3D[(int)Core.JointType.LeftElbow], blue);
            Debug.DrawLine(joints3D[(int)Core.JointType.LeftElbow], joints3D[(int)Core.JointType.LeftWrist], blue);
            Debug.DrawLine(joints3D[(int)Core.JointType.LeftShoulder], joints3D[(int)Core.JointType.Neck], blue);
            Debug.DrawLine(joints3D[(int)Core.JointType.Neck], joints3D[(int)Core.JointType.Head], purple);
        }

        Color color(int r, int g, int b)
        {
            return new Color(r / 255.0f, g / 255.0f, b / 255.0f);
        }        
    }
}