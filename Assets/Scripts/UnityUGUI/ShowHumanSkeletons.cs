using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowHumanSkeletons : MonoBehaviour
{
    // define the calsses and variables
    public class HumanSkeleton {
        public GameObject SkeletonObject;
        public LineRenderer Skeleton;

        public Transform Start;
        public Transform End;
    }
    private List<HumanSkeleton> humanSkeletons = new List<HumanSkeleton>();

    private List<Transform> humanKPs = new List<Transform>();
    private int[] boneJoints;

    // AddSkeleton is called for adding human skeleton to the human skeleton list
    private void AddSkeleton(Transform startPos, Transform endPos)
    {
        // define the human bone skeleton
        var sk = new HumanSkeleton(){
            SkeletonObject = new GameObject("HumanSkeleton"),
            Start = startPos,
            End = endPos,
        };

        // add the LineRenderer object
        sk.Skeleton = sk.SkeletonObject.AddComponent<LineRenderer>();

        // configure the LineRenderer settings
        sk.Skeleton.positionCount = 2;
        sk.Skeleton.startWidth = 0.04f;
        sk.Skeleton.endWidth = 0.01f;
        sk.Skeleton.material.color = Color.green;

        // add the human bone skeleton into the human skeleton list
        humanSkeletons.Add(sk);
    }

    // Start is called before the first frame update
    void Start()
    {
        // initialize the variables
        humanKPs.AddRange(new Transform[] {
            GameObject.Find("RightAnkle").transform, // 0
            GameObject.Find("RightKnee").transform, // 1
            GameObject.Find("RightHip").transform, // 2
            GameObject.Find("LeftHip").transform, // 3
            GameObject.Find("LeftKnee").transform, // 4
            GameObject.Find("LeftAnkle").transform, // 5
            GameObject.Find("Pelvis").transform, // 6
            GameObject.Find("Spine").transform, // 7
            GameObject.Find("Thorax").transform, // 8
            GameObject.Find("UpperNeck").transform, // 9
            GameObject.Find("HeadTop").transform, // 10
            GameObject.Find("RightWrist").transform, // 11
            GameObject.Find("RightElbow").transform, // 12
            GameObject.Find("RightShoulder").transform, // 13
            GameObject.Find("LeftShoulder").transform, // 14
            GameObject.Find("LeftElbow").transform, // 15
            GameObject.Find("LeftWrist").transform, // 16
        });
        
        boneJoints = new int[]{
                // 16 human bones in total and 6 means pelvis(hip)
                6, 2, // rightHipBone
                2, 1, // rightThigh
                1, 0, // rightCalf

                6, 3, // leftHipBone
                3, 4, // leftThigh
                4, 5, // leftCalf

                6, 7, // waist
                7, 8, // chest
                8, 9, // neck
                9, 10, // head
                
                8, 13, // rightClavicle
                13, 12, // rightUpperArm
                12, 11, // rightForearm

                8, 14, // leftClavicle
                14, 15, // leftUpperArm
                15, 16, // leftForearm
            };
        
        // construct the humanSkeletons list
        for(int i = 0; i < 16; i++)
        {
            int src = boneJoints[2 * i];
            int dst = boneJoints[2 * i + 1];
            AddSkeleton(humanKPs[src], humanKPs[dst]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // update the human bone skeletons per frame
        foreach(var sk in humanSkeletons)
        {
            sk.Skeleton.SetPosition(0,sk.Start.position);
            sk.Skeleton.SetPosition(1,sk.End.position);
        }
    }
}
