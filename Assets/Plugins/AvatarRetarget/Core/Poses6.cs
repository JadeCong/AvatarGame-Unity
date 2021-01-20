using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AvatarRetarget.Core
{
    public class Poses6
    {
        // public int Frame;
        public List<Vector3> Positions { get; set; }
        public List<float> AvatarBoneLengths { get; set; }

        public List<Vector3> HumanBoneVectors { get; set; }
        public List<float> HumanBoneLengths { get; set; }
        public List<Quaternion> Rotations { get; set; }
        public List<Quaternion> LocalRotations { get; set; }
        
        public List<Vector3> FKPositions { get; set; }
        
        // initialize the Poses6 class
        public Poses6(List<Vector3> positions, List<float> avatarBoneLens)
        {
            // Frame = frame;
            Positions = new List<Vector3>();
            AvatarBoneLengths = new List<float>();

            if(positions.Count == 0) // positions.Count=0 while executed for the first cycle
            {
                Debug.Log("Bad data: Positions.Count=0......");
                return;
            }
            if(avatarBoneLens.Count == 0) // positions.Count=0 while executed for the first cycle
            {
                Debug.Log("Bad data: AvatarBoneLengths.Count=0......");
                return;
            }

            Positions = positions; // world positions
            AvatarBoneLengths = avatarBoneLens;

            Refresh();
        }

        public void Refresh()
        {
            // initialize the variables
            HumanBoneVectors = new List<Vector3>();
            HumanBoneLengths = new List<float>();
            Rotations = new List<Quaternion>(); // world rotations
            LocalRotations = new List<Quaternion>(); // local rotations relative to parents
            FKPositions = new List<Vector3>(); // world positions

            // calculate the bone lengths and bone rotations in the method of joint angle mapping
            CalculateBoneLengths();
            CalculateBoneRotations();

            // calculate the bone positions in the method of joint keypoint retargeting
            ForwardKinematics();
        }

        public void CalculateBoneLengths()
        {
            // Positions of human joint keypoints are in the MPII order
            int[] boneJoints = new int[]
            {
                // 15 human bones in total and 6 means pelvis(hip)
                6, 2, // rightHipBone
                2, 1, // rightThigh
                1, 0, // rightCalf

                6, 3, // leftHipBone
                3, 4, // leftThigh
                4, 5, // leftCalf

                6, 7, // waist
                7, 8, // chest
                8, 9, // neck
                
                8, 12, // rightClavicle
                12, 11, // rightUpperArm
                11, 10, // rightForearm

                8, 13, // leftClavicle
                13, 14, // leftUpperArm
                14, 15, // leftForearm
            };
            var positions = Positions.ToList();

            for(int i = 0; i < 15; i++)
            {   
                int src = boneJoints[2 * i];
                int dst = boneJoints[2 * i + 1];
                var translate = positions[dst] - positions[src]; // vector points to dst from src
                HumanBoneVectors.Add(translate); // local vector

                var boneLength = translate.magnitude;
                HumanBoneLengths.Add(boneLength);
            }
        }

        public void CalculateBoneRotations()
        {
            // calculate the Bone Global Rotations
            for(int i = 0; i < 15; i++)
            {
                Rotations.Add(Quaternion.identity); // initialize the Rotations
            }

            // define the initial reference coordinate rotations
            var rotLeftForward = Quaternion.LookRotation(Vector3.left, Vector3.forward);
            var rotLeftBack = Quaternion.LookRotation(Vector3.left, Vector3.back);
            var rotLeftDown = Quaternion.LookRotation(Vector3.left, Vector3.down);
            var rotLeftUp = Quaternion.LookRotation(Vector3.left, Vector3.up);

            // TODO: make sure that the coordinate system will be the same(fixed to the bones) for every frame based on the human joint keypoints
            Rotations[0] = CalculateGlobalRotation(Positions[2]-Positions[6], Positions[7]-Positions[6], rotLeftDown);
            Rotations[1] = CalculateGlobalRotation(Positions[1]-Positions[2], Positions[0]-Positions[1], rotLeftForward);
            Rotations[2] = CalculateGlobalRotation(Positions[0]-Positions[1], Positions[2]-Positions[1], rotLeftForward);

            Rotations[3] = CalculateGlobalRotation(Positions[3]-Positions[6], Positions[6]-Positions[7], rotLeftDown);
            Rotations[4] = CalculateGlobalRotation(Positions[4]-Positions[3], Positions[5]-Positions[4], rotLeftForward);
            Rotations[5] = CalculateGlobalRotation(Positions[5]-Positions[4], Positions[3]-Positions[4], rotLeftForward);

            Rotations[6] = CalculateGlobalRotation(Positions[7]-Positions[6], Positions[3]-Positions[2], rotLeftDown);
            Rotations[7] = CalculateGlobalRotation(Positions[8]-Positions[7], Positions[6]-Positions[7], rotLeftDown);
            Rotations[8] = CalculateGlobalRotation(Positions[9]-Positions[8], Positions[13]-Positions[12], rotLeftDown);

            Rotations[9] = CalculateGlobalRotation(Positions[12]-Positions[8], Positions[8]-Positions[7], rotLeftUp);
            Rotations[10] = CalculateGlobalRotation(Positions[11]-Positions[12], Positions[10]-Positions[11], rotLeftBack);
            Rotations[11] = CalculateGlobalRotation(Positions[10]-Positions[11], Positions[12]-Positions[11], rotLeftBack);

            Rotations[12] = CalculateGlobalRotation(Positions[13]-Positions[8], Positions[7]-Positions[8], rotLeftUp);
            Rotations[13] = CalculateGlobalRotation(Positions[14]-Positions[13], Positions[15]-Positions[14], rotLeftBack);
            Rotations[14] = CalculateGlobalRotation(Positions[15]-Positions[14], Positions[13]-Positions[14], rotLeftBack);
            // Rotations[0] = CalculateGlobalRotation(Positions[2]-Positions[6], Positions[7]-Positions[6]); // for debug
            // Rotations[1] = CalculateGlobalRotation(Positions[1]-Positions[2], Positions[0]-Positions[1]);
            // Rotations[2] = CalculateGlobalRotation(Positions[0]-Positions[1], Positions[2]-Positions[1]);

            // Rotations[3] = CalculateGlobalRotation(Positions[3]-Positions[6], Positions[6]-Positions[7]);
            // Rotations[4] = CalculateGlobalRotation(Positions[4]-Positions[3], Positions[5]-Positions[4]);
            // Rotations[5] = CalculateGlobalRotation(Positions[5]-Positions[4], Positions[3]-Positions[4]);

            // Rotations[6] = CalculateGlobalRotation(Positions[7]-Positions[6], Positions[3]-Positions[2]);
            // Rotations[7] = CalculateGlobalRotation(Positions[8]-Positions[7], Positions[6]-Positions[7]);
            // Rotations[8] = CalculateGlobalRotation(Positions[9]-Positions[8], Positions[13]-Positions[12]);

            // Rotations[9] = CalculateGlobalRotation(Positions[12]-Positions[8], Positions[8]-Positions[7]);
            // Rotations[10] = CalculateGlobalRotation(Positions[11]-Positions[12], Positions[10]-Positions[11]);
            // Rotations[11] = CalculateGlobalRotation(Positions[10]-Positions[11], Positions[12]-Positions[11]);

            // Rotations[12] = CalculateGlobalRotation(Positions[13]-Positions[8], Positions[7]-Positions[8]);
            // Rotations[13] = CalculateGlobalRotation(Positions[14]-Positions[13], Positions[15]-Positions[14]);
            // Rotations[14] = CalculateGlobalRotation(Positions[15]-Positions[14], Positions[13]-Positions[14]);

            // calculate the Bone Local Rotations
            LocalRotations = Rotations.ToList(); // initialize the LocalRotations

            // construct the reference coordinate rotations
            var invRightHipBine = Quaternion.Inverse(Rotations[0]);
            var invRightThigh = Quaternion.Inverse(Rotations[1]);

            var invLeftHipBone = Quaternion.Inverse(Rotations[3]);
            var invLeftThigh = Quaternion.Inverse(Rotations[4]);

            var invWaist = Quaternion.Inverse(Rotations[6]);
            var invCheset = Quaternion.Inverse(Rotations[7]);
            var invNeck = Quaternion.Inverse(Rotations[8]);

            var invRightClavicle = Quaternion.Inverse(Rotations[9]);
            var invRigthUpperArm = Quaternion.Inverse(Rotations[10]);

            var invLeftClavicle = Quaternion.Inverse(Rotations[12]);
            var invLeftUpperArm = Quaternion.Inverse(Rotations[13]);

            // calculate the bone local roataions of human
            LocalRotations[0] = invWaist * LocalRotations[0];
            LocalRotations[1] = invRightHipBine * LocalRotations[1];
            LocalRotations[2] = invRightThigh * LocalRotations[2];

            LocalRotations[3] = invWaist * LocalRotations[3];
            LocalRotations[4] = invLeftHipBone * LocalRotations[4];
            LocalRotations[5] = invLeftThigh * LocalRotations[5];

            // LocalRotations[6] = invWaist * LocalRotations[6];
            LocalRotations[7] = invWaist * LocalRotations[7];
            LocalRotations[8] = invCheset * LocalRotations[8];

            LocalRotations[9] = invNeck * LocalRotations[9];
            LocalRotations[10] = invRightClavicle * LocalRotations[10];
            LocalRotations[11] = invRigthUpperArm * LocalRotations[11];

            LocalRotations[12] = invNeck * LocalRotations[12];
            LocalRotations[13] = invLeftClavicle * LocalRotations[13];
            LocalRotations[14] = invLeftUpperArm * LocalRotations[14];
        }

        public Quaternion CalculateGlobalRotation(Vector3 boneDirection, Vector3 left, Quaternion init)
        // public Quaternion CalculateGlobalRotation(Vector3 boneDirection, Vector3 left) // for debug
        {
            var d = boneDirection;
            var up = Vector3.Cross(d, left); // construct the up direction(left-hand coordinate)
            d.Normalize();
            up.Normalize();
            var target = Quaternion.LookRotation(d, up); // get the current rotation of the specified coordinate

            // TODO: make sure the init on the left or on the right
            var rot = target * Quaternion.Inverse(init);
            // var rot = init * target; // for debug

            return rot;
            // return target; // for debug
        }

        // Get the joint keypoint positions of the avatar in Unity by the forward kinematics
        public void ForwardKinematics()
        {
            // initialize the new joint positions of the avatar
            for(int i = 0; i < 16; i++)
            {
                FKPositions.Add(new Vector3(0f, 0f, 0f));
            }

            FKPositions[6] = Positions[6]; // pelvis
            FKPositions[2] = FKPositions[6] + HumanBoneVectors[0] / HumanBoneLengths[0] * AvatarBoneLengths[0]; // rightHip
            FKPositions[1] = FKPositions[2] + HumanBoneVectors[1] / HumanBoneLengths[1] * AvatarBoneLengths[1]; // rightKnee
            FKPositions[0] = FKPositions[1] + HumanBoneVectors[2] / HumanBoneLengths[2] * AvatarBoneLengths[2]; // rightAnkle

            FKPositions[3] = FKPositions[6] + HumanBoneVectors[3] / HumanBoneLengths[3] * AvatarBoneLengths[3]; // leftHip
            FKPositions[4] = FKPositions[3] + HumanBoneVectors[4] / HumanBoneLengths[4] * AvatarBoneLengths[4]; // leftKnee
            FKPositions[5] = FKPositions[4] + HumanBoneVectors[5] / HumanBoneLengths[5] * AvatarBoneLengths[5]; // leftAnkle

            FKPositions[7] = FKPositions[6] + HumanBoneVectors[6] / HumanBoneLengths[6] * AvatarBoneLengths[6]; // thorax
            FKPositions[8] = FKPositions[7] + HumanBoneVectors[7] / HumanBoneLengths[7] * AvatarBoneLengths[7]; // upperNeck
            FKPositions[9] = FKPositions[8] + HumanBoneVectors[8] / HumanBoneLengths[8] * AvatarBoneLengths[8]; // headTop

            FKPositions[12] = FKPositions[8] + HumanBoneVectors[9] / HumanBoneLengths[9] * AvatarBoneLengths[9]; // rightShoulder
            FKPositions[11] = FKPositions[12] + HumanBoneVectors[10] / HumanBoneLengths[10] * AvatarBoneLengths[10]; // rightElbow
            FKPositions[10] = FKPositions[11] + HumanBoneVectors[11] / HumanBoneLengths[11] * AvatarBoneLengths[11]; // rightWrist

            FKPositions[13] = FKPositions[8] + HumanBoneVectors[12] / HumanBoneLengths[12] * AvatarBoneLengths[12]; // leftShoulder
            FKPositions[14] = FKPositions[13] + HumanBoneVectors[13] / HumanBoneLengths[13] * AvatarBoneLengths[13]; // leftElbow
            FKPositions[15] = FKPositions[14] + HumanBoneVectors[14] / HumanBoneLengths[14] * AvatarBoneLengths[14]; // leftWrist

            // set the foot of the avatar on the ground(for actual need)
            float offsetY = 0f;
            if(FKPositions[0].y <= FKPositions[5].y) // check which foot(left/right) is lower in Y axis coordinate
            {
                offsetY = Mathf.Abs(FKPositions[0].y); // get the offset from the feet to the ground(zero plane)
                if(FKPositions[0].y <= 0) // check whether the foot under ground or not and correct the offset
                {
                    for(int i = 0; i < 16; i++)
                    {
                        FKPositions[i].Set(FKPositions[i].x, FKPositions[i].y + offsetY, FKPositions[i].z);
                    }
                }
                else
                {
                    for(int i = 0; i < 16; i++)
                    {
                        FKPositions[i].Set(FKPositions[i].x, FKPositions[i].y - offsetY, FKPositions[i].z);
                    }
                }
            }
            else
            {
                offsetY = Mathf.Abs(FKPositions[5].y);
                if(FKPositions[5].y <= 0)
                {
                    for(int i = 0; i < 16; i++)
                    {
                        FKPositions[i].Set(FKPositions[i].x, FKPositions[i].y + offsetY, FKPositions[i].z);
                    }
                }
                else
                {
                    for(int i = 0; i < 16; i++)
                    {
                        FKPositions[i].Set(FKPositions[i].x, FKPositions[i].y - offsetY, FKPositions[i].z);
                    }
                }
            }
        }

        // Clear the class buffer for Positions/AvatarBoneLengths/HumanBoneVectors/HumanBoneLengths/Rotations/LocalRotations/FKPositions
        public void AllClear()
        {
            Positions.Clear();
            AvatarBoneLengths.Clear();

            HumanBoneVectors.Clear();
            HumanBoneLengths.Clear();
            Rotations.Clear();
            LocalRotations.Clear();

            FKPositions.Clear();
        }
    }
}
