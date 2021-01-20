using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JustWithJoints.Core
{
    // The joint order is according to Leeds Sports Pose Dataset
    enum JointType
    {
        RightAnkle = 0,
        RightKnee,
        RightHip,
        LeftHip,
        LeftKnee,
        LeftAnkle,
        RightWrist,
        RightElbow,
        RightShoulder,
        LeftShoulder,
        LeftElbow,
        LeftWrist,
        Neck,
        Head,
    }
}