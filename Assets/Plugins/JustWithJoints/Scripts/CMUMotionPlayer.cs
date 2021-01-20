using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JustWithJoints
{
    public class CMUMotionPlayer : MonoBehaviour
    {
        public string DataPath = "Assets/FAvatarRetarget/Resources/sample.txt";
        public float Timer = 0.0f;
        public float FPS = 120.0f;
        public bool Loop = true;
        public bool Play = true;

        public bool DebugFixedPoseMode = false;
        const float DebugFixedPoseScale = 0.5f;
        public Vector3[] DebugFixedPose = new Vector3[] {
            new Vector3(-1, 0, 0) * DebugFixedPoseScale,
            new Vector3(-1, 1, 0) * DebugFixedPoseScale,
            new Vector3(-1, 2, 0) * DebugFixedPoseScale,
            new Vector3(1, 2, 0) * DebugFixedPoseScale,
            new Vector3(1, 1, 0) * DebugFixedPoseScale,
            new Vector3(1, 0, 0) * DebugFixedPoseScale,
            new Vector3(-3, 4, 0) * DebugFixedPoseScale,
            new Vector3(-2, 4, 0) * DebugFixedPoseScale,
            new Vector3(-1, 4, 0) * DebugFixedPoseScale,
            new Vector3(1, 4, 0) * DebugFixedPoseScale,
            new Vector3(2, 4, 0) * DebugFixedPoseScale,
            new Vector3(3, 4, 0) * DebugFixedPoseScale,
            new Vector3(0, 4, 0) * DebugFixedPoseScale,
            new Vector3(0, 5, 0) * DebugFixedPoseScale,
        };

        private Core.Motion motion_ = null;
        private int frame_ = 0;

        // Use this for initialization
        void Start()
        {
            motion_ = CMUMotionLoader.Load(DataPath);
        }

        // Update is called once per frame
        void Update()
        {
            if (Play)
            {
                Timer += Time.deltaTime;
            }

            frame_ = (int)(Timer * FPS);
            if (frame_ >= motion_.Poses.Count)
            {
                Timer = 0.0f;
            }
        }

        public Core.Pose GetCurrentPose()
        {
            if (DebugFixedPoseMode)
            {
                return new Core.Pose(0, DebugFixedPose.ToList());
            }

            if (frame_ < 0 || motion_.Poses.Count <= frame_)
            {
                return null;
            }
            var pose = motion_.Poses[frame_];
            return pose;
        }
    }
}