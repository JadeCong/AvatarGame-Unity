using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeAvatarMotionCopy : MonoBehaviour
{
    // define the settings for inspector
    public GameObject motionSource;

    // define the source and destination human pose handler
    // TODO: figure out whether the method only fits humanoid model(only for humanoid determined)
    HumanPoseHandler srcPoseHandler;
    HumanPoseHandler destPoseHandler;

    // Start is called before the first frame update
    void Start()
    {
        // get the source and destination human pose handler
        srcPoseHandler = new HumanPoseHandler(motionSource.GetComponent<Animator>().avatar, motionSource.transform);
        destPoseHandler = new HumanPoseHandler(GetComponent<Animator>().avatar, transform);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // FixedUpdate is called once per frame after Update with fixed FPS(0.02s)
    void FixedUpdate()
    {

    }

    // LateUpdate is called once per frame after FixedUpdate
    void LateUpdate()
    {
        // define the human pose variable
        HumanPose humanPose = new HumanPose();

        // update the destination human pose
        srcPoseHandler.GetHumanPose(ref humanPose);
        destPoseHandler.SetHumanPose(ref humanPose);
    }
}
