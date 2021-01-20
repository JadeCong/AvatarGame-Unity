using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace AvatarRetarget.Core
{
    public class Poses8
    {
        public int Frame;
        public List<Vector3> Positions { get; set; }
        public List<float> AvatarBoneLengths { get; set; }
        public bool UseAbsoluteCoordinate;
        public bool UseLocalRotation;
        public bool UseForwardKinematics;
        public bool SetFootOnGround;

        public List<Quaternion> Rotations { get; set; }
        public List<Quaternion> LocalRotations { get; set; }
        public List<Vector3> HumanBoneVectors { get; set; }
        public List<float> HumanBoneLengths { get; set; }
        public List<Vector3> FKPositions { get; set; }
        
        // Poses8 is called for initializing the Poses8 class
        public Poses8(int frame, List<Vector3> positions, List<float> avatarBoneLens, bool useAbsCoord, bool useLocalRot, bool useFK, bool setFootOnGround)
        {
            Frame = frame;
            Positions = new List<Vector3>();
            AvatarBoneLengths = new List<float>();
            UseAbsoluteCoordinate = useAbsCoord;
            UseLocalRotation = useLocalRot;
            UseForwardKinematics = useFK;
            SetFootOnGround = setFootOnGround;

            if(positions.Count == 0) // positions.Count=0 while executed for the first cycle
            {
                throw new ArgumentException("Bad data: Positions list are empty......", nameof(positions));
                return;
            }
            if(avatarBoneLens.Count == 0) // positions.Count=0 while executed for the first cycle
            {
                throw new ArgumentException("Bad data: AvatarBoneLengths list are empty......", nameof(avatarBoneLens));
                return;
            }

            Positions = positions; // world positions
            AvatarBoneLengths = avatarBoneLens;

            // calculate the positions and rotations(global/local) for driving avatar
            Refresh();
        }

        // Refresh is called for calculating the positions and rotations(global/local) for driving avatar
        public void Refresh()
        {
            // initialize the variables
            Rotations = new List<Quaternion>(); // world rotations
            LocalRotations = new List<Quaternion>(); // local rotations relative to parents
            HumanBoneVectors = new List<Vector3>(); // direction vector for human bones
            HumanBoneLengths = new List<float>(); // human bone lengths based on raw joint keypoints
            FKPositions = new List<Vector3>(); // world positions

            // calculate the bone rotations(global/local) in the method of joint angle mapping
            CalculateBoneRotations();

            // calculate the joint keypoint positions in the method of joint keypoint retargeting
            if(UseForwardKinematics)
            {
                // calculate the human bone vectors and human bone lengths
                CalculateBoneLengths();

                // calculate the joint keypoint positions based on forward kinematics
                ForwardKinematics();
            }
        }

        // CalculateBoneRotations is called for calculating the bone rotations(global/local) in the method of joint angle mapping
        public void CalculateBoneRotations()
        {
            // calculate the Bone Global Rotations
            for(int i = 0; i < 16; i++)
            {
                Rotations.Add(Quaternion.identity); // initialize the Rotations
            }

            // check whether using absolute coordinate rotation and pick the different methods(aligning coordinates/relatively rotating) to calculate the bone rotations
            if(UseAbsoluteCoordinate)
            {
                // calculate the bone global rotations using the method of aligning bone coordinates(make sure that the coordinate system will be the same(fixed to the bones) for every frame based on the human joint keypoints)
                Rotations[0] = CalculateGlobalRotation(Positions[6]-Positions[7], Positions[3]-Positions[2], 3); // rightHipBone(rotTypeFlag:3)
                Rotations[1] = CalculateGlobalRotation(Positions[2]-Positions[1], Positions[1]-Positions[0], 1); // rightThigh(rotTypeFlag:1)
                Rotations[2] = CalculateGlobalRotation(Positions[1]-Positions[0], Positions[1]-Positions[2], 1); // rightCalf(rotTypeFlag:1)

                Rotations[3] = CalculateGlobalRotation(Positions[6]-Positions[7], Positions[3]-Positions[2], 3); // leftHipBone(rotTypeFlag:3)
                Rotations[4] = CalculateGlobalRotation(Positions[3]-Positions[4], Positions[4]-Positions[5], 1); // leftThigh(rotTypeFlag:1)
                Rotations[5] = CalculateGlobalRotation(Positions[4]-Positions[5], Positions[4]-Positions[3], 1); // leftCalf(rotTypeFlag:1)

                Rotations[6] = CalculateGlobalRotation(Positions[6]-Positions[7], Positions[3]-Positions[2], 3); // waist(rotTypeFlag:3)
                Rotations[7] = CalculateGlobalRotation(Positions[7]-Positions[8], Positions[6]-Positions[7], 1); // chest(rotTypeFlag:1)
                Rotations[8] = CalculateGlobalRotation(Positions[8]-Positions[9], Positions[7]-Positions[8], 1) * new Quaternion(0.0f, 0.0f, 0.342f, 0.940f); // neck(rotTypeFlag:1)(correct the neck up for 40 degrees in z axis rotation direction)
                Rotations[9] = CalculateGlobalRotation(Positions[9]-Positions[10], Positions[8]-Positions[9], 1); // head(rotTypeFlag:1)

                Rotations[10] = CalculateGlobalRotation(Positions[8]-Positions[13], Vector3.Cross(Positions[8]-Positions[13], Positions[8]-Positions[7]), 2); // rightClavicle(rotTypeFlag:2)
                Rotations[11] = CalculateGlobalRotation(Positions[13]-Positions[12], Positions[12]-Positions[11], 1); // rightUpperArm(rotTypeFlag:1)
                Rotations[12] = CalculateGlobalRotation(Positions[12]-Positions[11], Positions[12]-Positions[13], 1); // rightForearm(rotTypeFlag:1)

                Rotations[13] = CalculateGlobalRotation(Positions[8]-Positions[14], Vector3.Cross(Positions[8]-Positions[14], Positions[7]-Positions[8]), 2); // leftClavicle(rotTypeFlag:2)
                Rotations[14] = CalculateGlobalRotation(Positions[14]-Positions[15], Positions[15]-Positions[16], 1); // leftUpperArm(rotTypeFlag:1)
                Rotations[15] = CalculateGlobalRotation(Positions[15]-Positions[16], Positions[15]-Positions[14], 1); // leftForearm(rotTypeFlag:1)
            }
            else
            {
                // define the initial reference coordinate rotations
                var rotLeftForward = Quaternion.LookRotation(Vector3.left, Vector3.forward);
                var rotLeftBack = Quaternion.LookRotation(Vector3.left, Vector3.back);
                var rotLeftDown = Quaternion.LookRotation(Vector3.left, Vector3.down);
                var rotLeftUp = Quaternion.LookRotation(Vector3.left, Vector3.up);

                // calculate the bone global rotations using the method of relatively rotating
                Rotations[0] = CalculateGlobalRotation(Positions[2]-Positions[6], Positions[7]-Positions[6], rotLeftDown); // rightHipBone
                Rotations[1] = CalculateGlobalRotation(Positions[1]-Positions[2], Positions[0]-Positions[1], rotLeftForward); // rightThigh
                Rotations[2] = CalculateGlobalRotation(Positions[0]-Positions[1], Positions[2]-Positions[1], rotLeftForward); // rightCalf

                Rotations[3] = CalculateGlobalRotation(Positions[3]-Positions[6], Positions[6]-Positions[7], rotLeftDown); // leftHipBone
                Rotations[4] = CalculateGlobalRotation(Positions[4]-Positions[3], Positions[5]-Positions[4], rotLeftForward); // leftThigh
                Rotations[5] = CalculateGlobalRotation(Positions[5]-Positions[4], Positions[3]-Positions[4], rotLeftForward); // leftCalf

                Rotations[6] = CalculateGlobalRotation(Positions[7]-Positions[6], Positions[3]-Positions[2], rotLeftDown); // waist
                Rotations[7] = CalculateGlobalRotation(Positions[8]-Positions[7], Positions[6]-Positions[7], rotLeftDown); // chest
                Rotations[8] = CalculateGlobalRotation(Positions[9]-Positions[8], Positions[13]-Positions[12], rotLeftDown); // neck
                Rotations[9] = CalculateGlobalRotation(Positions[10]-Positions[9], Positions[8]-Positions[9], rotLeftDown); // head

                Rotations[10] = CalculateGlobalRotation(Positions[13]-Positions[8], Positions[8]-Positions[7], rotLeftUp); // rightClavicle
                Rotations[11] = CalculateGlobalRotation(Positions[12]-Positions[13], Positions[11]-Positions[12], rotLeftBack); // rightUpperArm
                Rotations[12] = CalculateGlobalRotation(Positions[11]-Positions[12], Positions[13]-Positions[12], rotLeftBack); // rightForearm

                Rotations[13] = CalculateGlobalRotation(Positions[14]-Positions[8], Positions[7]-Positions[8], rotLeftUp); // leftClavicle
                Rotations[14] = CalculateGlobalRotation(Positions[15]-Positions[14], Positions[16]-Positions[15], rotLeftBack); // leftUpperArm
                Rotations[15] = CalculateGlobalRotation(Positions[16]-Positions[15], Positions[14]-Positions[15], rotLeftBack); // leftForearm
            }

            // calculate the Bone Local Rotations
            for(int j = 0; j < 16; j++)
            {
                LocalRotations.Add(Quaternion.identity); // initialize the LocalRotations
            }

            // check whether using local rotation of bones for driving avatar
            if(UseLocalRotation)
            {
                // calculate the bone local rotations using the method of relatively rotating
                LocalRotations[0] = CalculateLocalRotation(Rotations[6], Rotations[0]); // rightHipBone
                LocalRotations[1] = CalculateLocalRotation(Rotations[0], Rotations[1]); // rightThigh
                LocalRotations[2] = CalculateLocalRotation(Rotations[1], Rotations[2]); // rightCalf

                LocalRotations[3] = CalculateLocalRotation(Rotations[6], Rotations[3]); // leftHipBone
                LocalRotations[4] = CalculateLocalRotation(Rotations[3], Rotations[4]); // leftThigh
                LocalRotations[5] = CalculateLocalRotation(Rotations[4], Rotations[5]); // leftCalf

                // LocalRotations[6] = Rotations[6]; // waist
                LocalRotations[6] = CalculateLocalRotation(new Quaternion(-0.5f, 0.5f, 0.5f, 0.5f), Rotations[6]); // waist(relative rotation of pelvis to the world coordinate system)
                LocalRotations[7] = CalculateLocalRotation(Rotations[6], Rotations[7]); // chest
                LocalRotations[8] = CalculateLocalRotation(Rotations[7], Rotations[8]); // neck
                LocalRotations[9] = CalculateLocalRotation(Rotations[8], Rotations[9]); // head

                LocalRotations[10] = CalculateLocalRotation(Rotations[8], Rotations[10]); // rightClavicle
                LocalRotations[11] = CalculateLocalRotation(Rotations[10], Rotations[11]); // rightUpperArm
                LocalRotations[12] = CalculateLocalRotation(Rotations[11], Rotations[12]); // rightForearm

                LocalRotations[13] = CalculateLocalRotation(Rotations[8], Rotations[13]); // leftClavicle
                LocalRotations[14] = CalculateLocalRotation(Rotations[13], Rotations[14]); // leftUpperArm
                LocalRotations[15] = CalculateLocalRotation(Rotations[14], Rotations[15]); // leftForearm
            }
        }

        // CalculateGlobalRotation is called for calculating the global rotation by aligning the bone coordinate system of avatar
        public Quaternion CalculateGlobalRotation(Vector3 boneDirection, Vector3 leftReference, int rotTypeFlag)
        {
            // initialize the return global rotation with unit quaternion
            Quaternion rot = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);

            // check the rotTypeFlag for different calculations of global rotation
            if(rotTypeFlag == 1) // for arms, legs and torsos
            {
                var right = boneDirection;
                var leftRef = leftReference;
                // construct the normal direction vector(forward/upward) using left-hand coordinate rule
                var forward = Vector3.Cross(right, leftRef);
                var up = Vector3.Cross(forward, right);
                forward.Normalize();
                up.Normalize();
                
                // construct the rotation relative to world coordinate system using forward and upward
                rot = Quaternion.LookRotation(forward, up);
            }
            else if(rotTypeFlag == 2) // for shoulders
            {
                var right = boneDirection;
                var up = leftReference;
                // construct the forward direction vector(left-hand coordinate)
                var forward = Vector3.Cross(right, up);
                forward.Normalize();
                up.Normalize();

                // construct the rotation relative to world coordinate system using forward and upward
                rot = Quaternion.LookRotation(forward, up);
            }
            else if(rotTypeFlag == 3) // for waist and hipbones
            {
                var right = boneDirection;
                var forward = leftReference;
                // construct the up direction vector(left-hand coordinate)
                var up = Vector3.Cross(forward, right);
                forward.Normalize();
                up.Normalize();

                // construct the rotation relative to world coordinate system using forward and upward
                rot = Quaternion.LookRotation(forward, up);
            }
            else
            {
                throw new ArgumentException("Wrong rotation type flag for global rotation calculation of avatar's bones.", nameof(rotTypeFlag));
                return rot;
            }
            
            return rot;
        }
        
        // CalculateGlobalRotation is called for calculating the global rotation by relatively rotating based on the initial T-Pose of avatar
        public Quaternion CalculateGlobalRotation(Vector3 boneDirection, Vector3 leftReference, Quaternion initReference)
        {
            var forward = boneDirection;
            var leftRef = leftReference;
            // construct the up direction vector(left-hand coordinate)
            var up = Vector3.Cross(forward, leftRef);
            forward.Normalize();
            up.Normalize();
            // get the current rotation of the specified coordinate
            var target = Quaternion.LookRotation(forward, up);

            // rotate the rotation for referring to world coordinate system(make sure the init on the left or on the right)
            var rot = target * Quaternion.Inverse(initReference);

            return rot;
        }

        // CalculateLocalRotation is called for calculating the local rotation by constructing the rotation relative to bone's parent rotation
        public Quaternion CalculateLocalRotation(Quaternion parentRotation, Quaternion boneRotation)
        {
            // construct the reference coordinate rotation
            var invParentRot = Quaternion.Inverse(parentRotation);

            // calculate the local rotation of bone
            var rot = invParentRot * boneRotation;

            return rot;
        }

        // CalculateBoneLengths is called for calculating the bone lengths of avatar
        public void CalculateBoneLengths()
        {
            // Positions of human joint keypoints are in the MPII order
            int[] boneJoints = new int[]
            {
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
            var positions = Positions.ToList();

            for(int i = 0; i < 16; i++)
            {   
                int src = boneJoints[2 * i];
                int dst = boneJoints[2 * i + 1];
                var translate = positions[dst] - positions[src]; // vector points to dst from src
                HumanBoneVectors.Add(translate); // local vector

                var boneLength = translate.magnitude;
                HumanBoneLengths.Add(boneLength);
            }
        }

        // ForwardKinematics is called for getting the joint keypoint positions of the avatar in Unity by the forward kinematics
        public void ForwardKinematics()
        {
            // initialize the new joint positions of the avatar
            for(int i = 0; i < 17; i++)
            {
                FKPositions.Add(new Vector3(0.0f, 0.0f, 0.0f));
            }

            // get the joint keypoint positions one by one from pelvis to the limb's end
            FKPositions[6] = Positions[6]; // pelvis
            // right leg
            FKPositions[2] = FKPositions[6] + HumanBoneVectors[0] / HumanBoneLengths[0] * AvatarBoneLengths[0]; // rightHip
            FKPositions[1] = FKPositions[2] + HumanBoneVectors[1] / HumanBoneLengths[1] * AvatarBoneLengths[1]; // rightKnee
            FKPositions[0] = FKPositions[1] + HumanBoneVectors[2] / HumanBoneLengths[2] * AvatarBoneLengths[2]; // rightAnkle
            // left leg
            FKPositions[3] = FKPositions[6] + HumanBoneVectors[3] / HumanBoneLengths[3] * AvatarBoneLengths[3]; // leftHip
            FKPositions[4] = FKPositions[3] + HumanBoneVectors[4] / HumanBoneLengths[4] * AvatarBoneLengths[4]; // leftKnee
            FKPositions[5] = FKPositions[4] + HumanBoneVectors[5] / HumanBoneLengths[5] * AvatarBoneLengths[5]; // leftAnkle
            // torso
            FKPositions[7] = FKPositions[6] + HumanBoneVectors[6] / HumanBoneLengths[6] * AvatarBoneLengths[6]; // spine
            FKPositions[8] = FKPositions[7] + HumanBoneVectors[7] / HumanBoneLengths[7] * AvatarBoneLengths[7]; // thorax
            FKPositions[9] = FKPositions[8] + HumanBoneVectors[8] / HumanBoneLengths[8] * AvatarBoneLengths[8]; // upperNeck
            FKPositions[10] = FKPositions[9] + HumanBoneVectors[9] / HumanBoneLengths[9] * AvatarBoneLengths[9]; // headTop
            // right arm
            FKPositions[13] = FKPositions[8] + HumanBoneVectors[10] / HumanBoneLengths[10] * AvatarBoneLengths[10]; // rightShoulder
            FKPositions[12] = FKPositions[13] + HumanBoneVectors[11] / HumanBoneLengths[11] * AvatarBoneLengths[11]; // rightElbow
            FKPositions[11] = FKPositions[12] + HumanBoneVectors[12] / HumanBoneLengths[12] * AvatarBoneLengths[12]; // rightWrist
            // left arm
            FKPositions[14] = FKPositions[8] + HumanBoneVectors[13] / HumanBoneLengths[13] * AvatarBoneLengths[13]; // leftShoulder
            FKPositions[15] = FKPositions[14] + HumanBoneVectors[14] / HumanBoneLengths[14] * AvatarBoneLengths[14]; // leftElbow
            FKPositions[16] = FKPositions[15] + HumanBoneVectors[15] / HumanBoneLengths[15] * AvatarBoneLengths[15]; // leftWrist

            // set the foot of the avatar on the ground if necessary
            if(SetFootOnGround)
            {
                float offsetY = 0.0f;
                if(FKPositions[0].y <= FKPositions[5].y) // check which foot(left/right) is lower in Y axis coordinate
                {
                    offsetY = Mathf.Abs(FKPositions[0].y); // get the offset from the feet to the ground(zero plane)
                    if(FKPositions[0].y <= 0.0f) // check whether the foot under ground or not and correct the offset
                    {
                        for(int i = 0; i < 17; i++)
                        {
                            FKPositions[i].Set(FKPositions[i].x, FKPositions[i].y + offsetY, FKPositions[i].z);
                        }
                    }
                    else
                    {
                        for(int i = 0; i < 17; i++)
                        {
                            FKPositions[i].Set(FKPositions[i].x, FKPositions[i].y - offsetY, FKPositions[i].z);
                        }
                    }
                }
                else
                {
                    offsetY = Mathf.Abs(FKPositions[5].y);
                    if(FKPositions[5].y <= 0.0f)
                    {
                        for(int i = 0; i < 17; i++)
                        {
                            FKPositions[i].Set(FKPositions[i].x, FKPositions[i].y + offsetY, FKPositions[i].z);
                        }
                    }
                    else
                    {
                        for(int i = 0; i < 17; i++)
                        {
                            FKPositions[i].Set(FKPositions[i].x, FKPositions[i].y - offsetY, FKPositions[i].z);
                        }
                    }
                }
            }
        }

        // AllClear is called for clearing the class buffer for Positions/AvatarBoneLengths/Rotations/LocalRotations/HumanBoneVectors/HumanBoneLengths/FKPositions and Frame/UseAbsoluteCoordinate/UseLocalRotation/UseForwardKinematics/SetFootOnGround
        public void AllClear()
        {
            Frame = 0;
            Positions.Clear();
            AvatarBoneLengths.Clear();
            UseAbsoluteCoordinate = false;
            UseLocalRotation = false;
            UseForwardKinematics = false;
            SetFootOnGround = false;

            Rotations.Clear();
            LocalRotations.Clear();
            HumanBoneVectors.Clear();
            HumanBoneLengths.Clear();
            FKPositions.Clear();
        }
    } // end class
} // end namespace
