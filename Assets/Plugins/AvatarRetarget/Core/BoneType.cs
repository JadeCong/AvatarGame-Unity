using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AvatarRetarget.Core
{
    // The bone of avatar is named after the enum list in order(15 bones in total:reference JointType)
    enum BoneType
    {
        rightHipBone, // 0
        rightThigh, // 1
        rightCalf, // 2
        
        leftHipBone, // 3
        leftThigh, // 4
        leftCalf, // 5
        
        waist, // 6
        chest, // 7
        neck, // 8

        rightClavicle, // 9
        rightUpperArm, // 10
        rightForearm, // 11

        leftClavicle, // 12
        leftUpperArm, // 13
        leftForearm, // 14
    }
}
