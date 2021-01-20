using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeAvatarRetarget : MonoBehaviour
{
    public Transform srcModel;
    Animator srcAnimator;
    Animator selfAnimator;

    List<Transform> srcJoints = new List<Transform>();
    List<Transform> selfJoints = new List<Transform>();
    Quaternion srcInitRotation = new Quaternion();
    Quaternion selfInitRotation = new Quaternion();
    List<Quaternion> srcJointsInitRotation = new List<Quaternion>();
    List<Quaternion> selfJointsInitRotation = new List<Quaternion>();

    Transform srcRoot;
    Transform selfRoot;
    Vector3 srcInitPosition = new Vector3();
    Vector3 selfInitPosition = new Vector3();

    static HumanBodyBones[] bonesToUse = new[]{
        HumanBodyBones.Neck,
        HumanBodyBones.Head,

        HumanBodyBones.Hips,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.UpperChest,

        HumanBodyBones.LeftShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftHand,

        HumanBodyBones.RightShoulder,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand,

        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.LeftToes,

        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.RightFoot,
        HumanBodyBones.RightToes,
    };

    void Start()
    {
        srcAnimator = srcModel.GetComponent<Animator>();
        selfAnimator = gameObject.GetComponent<Animator>();

        InitBones();
        SetJointsInitRotation();
        SetInitPosition();
    }

    void LateUpdate()
    {
        SetJointsRotation();
        SetPosition();
    }

    // get the initial transform of srcJoints and SelfJoints for all joints
    private void InitBones()
    {
        for (int i = 0; i < bonesToUse.Length; i++)
        {
            srcJoints.Add(srcAnimator.GetBoneTransform(bonesToUse[i]));
            selfJoints.Add(selfAnimator.GetBoneTransform(bonesToUse[i]));
        }
    }

    private void SetJointsInitRotation()
    {
        // get the rotation of the origin for srcModel and self gameObject in the world space
        srcInitRotation = srcModel.transform.rotation;
        selfInitRotation = gameObject.transform.rotation;
        for (int i = 0; i < bonesToUse.Length; i++)
        {
            // get the rotation difference of the all joints comparing to the origin
            srcJointsInitRotation.Add(srcJoints[i].rotation * Quaternion.Inverse(srcInitRotation));
            selfJointsInitRotation.Add(selfJoints[i].rotation * Quaternion.Inverse(selfInitRotation));
        }
    }

    private void SetInitPosition()
    {
        // get the transform of the root(hips)
        srcRoot = srcAnimator.GetBoneTransform(HumanBodyBones.Hips);
        selfRoot = selfAnimator.GetBoneTransform(HumanBodyBones.Hips);
        // get the localPosition of the root relative to the world
        srcInitPosition = srcRoot.localPosition;
        selfInitPosition = selfRoot.localPosition;
    }

    private void SetJointsRotation()
    {
        for (int i = 0; i < bonesToUse.Length; i++)
        {
            selfJoints[i].rotation = selfInitRotation; // get the rotation of the self gameObject in the world space
            selfJoints[i].rotation *= (srcJoints[i].rotation * Quaternion.Inverse(srcJointsInitRotation[i]));
            selfJoints[i].rotation *= selfJointsInitRotation[i];
        }
    }

    private void SetPosition()
    {
        // update the self gameObject root localPosition
        selfRoot.localPosition = (srcRoot.localPosition - srcInitPosition) + selfInitPosition;
    }
}
