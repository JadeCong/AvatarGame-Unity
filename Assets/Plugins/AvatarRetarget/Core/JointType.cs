using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AvatarRetarget.Core
{
    // The joint order in enum list is according to MPII Human Pose Dataset(16 keypoints)
    enum JointType
    {
        rightAnkle, // vectorHumanKPs[0]
        rightKnee, // vectorHumanKPs[1]
        rightHip, // vectorHumanKPs[2]
        leftHip, // vectorHumanKPs[3]
        leftKnee, // vectorHumanKPs[4]
        leftAnkle, // vectorHumanKPs[5]

        pelvis, // vectorHumanKPs[6]
        thorax, // vectorHumanKPs[7]
        upperNeck, // vectorHumanKPs[8]
        headTop, // vectorHumanKPs[9]

        rightWrist, // vectorHumanKPs[10]
        rightElbow, // vectorHumanKPs[11]
        rightShoulder, // vectorHumanKPs[12]
        leftShoulder, // vectorHumanKPs[13]
        leftElbow, // vectorHumanKPs[14]
        leftWrist, // vectorHumanKPs[15]
    }
}
