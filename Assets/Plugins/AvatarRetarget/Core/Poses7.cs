using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace AvatarRetarget.Core
{
    public class Poses7
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
        
        // Poses7 is called for initializing the Poses7 class
        public Poses7(int frame, List<Vector3> positions, List<float> avatarBoneLens, bool useAbsCoord, bool useLocalRot, bool useFK, bool setFootOnGround)
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
            for(int i = 0; i < 15; i++)
            {
                Rotations.Add(Quaternion.identity); // initialize the Rotations
            }

            // check whether using absolute coordinate rotation and pick the different methods(aligning coordinates/relatively rotating) to calculate the bone rotations
            if(UseAbsoluteCoordinate)
            {
                // calculate the bone global rotations using the method of aligning bone coordinates(make sure that the coordinate system will be the same(fixed to the bones) for every frame based on the human joint keypoints)
                Rotations[0] = CalculateGlobalRotation(Positions[6]-Positions[2], Positions[7]-Positions[6]); //
                Rotations[1] = CalculateGlobalRotation(Positions[2]-Positions[1], Positions[1]-Positions[0]); // rightThigh
                Rotations[2] = CalculateGlobalRotation(Positions[1]-Positions[0], Positions[1]-Positions[2]); // rightCalf

                Rotations[3] = CalculateGlobalRotation(Positions[6]-Positions[3], Positions[6]-Positions[7]); //
                Rotations[4] = CalculateGlobalRotation(Positions[3]-Positions[4], Positions[4]-Positions[5]); // leftThigh
                Rotations[5] = CalculateGlobalRotation(Positions[4]-Positions[5], Positions[4]-Positions[3]); // leftCalf

                Rotations[6] = CalculateGlobalRotation(Positions[6]-Positions[7], Positions[3]-Positions[2]); //
                Rotations[7] = CalculateGlobalRotation(Positions[7]-Positions[8], Positions[6]-Positions[7]); //
                Rotations[8] = CalculateGlobalRotation(Positions[8]-Positions[9], Positions[7]-Positions[8]); // 

                Rotations[9] = CalculateGlobalRotation(Positions[8]-Positions[12], Positions[8]-Positions[7]); // rightClavicle
                Rotations[10] = CalculateGlobalRotation(Positions[12]-Positions[11], Positions[11]-Positions[10]); // rightUpperArm
                Rotations[11] = CalculateGlobalRotation(Positions[11]-Positions[10], Positions[11]-Positions[12]); // rightForearm

                Rotations[12] = CalculateGlobalRotation(Positions[8]-Positions[13], Positions[7]-Positions[8]); //
                Rotations[13] = CalculateGlobalRotation(Positions[13]-Positions[14], Positions[14]-Positions[15]); // leftUpperArm
                Rotations[14] = CalculateGlobalRotation(Positions[14]-Positions[15], Positions[14]-Positions[13]); // leftForearm
            }
            else
            {
                // define the initial reference coordinate rotations
                var rotLeftForward = Quaternion.LookRotation(Vector3.left, Vector3.forward);
                var rotLeftBack = Quaternion.LookRotation(Vector3.left, Vector3.back);
                var rotLeftDown = Quaternion.LookRotation(Vector3.left, Vector3.down);
                var rotLeftUp = Quaternion.LookRotation(Vector3.left, Vector3.up);

                // TODO: calculate the bone global rotations using the method of relatively rotating
                Rotations[0] = CalculateGlobalRotation(Positions[2]-Positions[6], Positions[7]-Positions[6], rotLeftDown); // rightHipBone
                Rotations[1] = CalculateGlobalRotation(Positions[1]-Positions[2], Positions[0]-Positions[1], rotLeftForward); // rightThigh
                Rotations[2] = CalculateGlobalRotation(Positions[0]-Positions[1], Positions[2]-Positions[1], rotLeftForward); // rightCalf

                Rotations[3] = CalculateGlobalRotation(Positions[3]-Positions[6], Positions[6]-Positions[7], rotLeftDown); // leftHipBone
                Rotations[4] = CalculateGlobalRotation(Positions[4]-Positions[3], Positions[5]-Positions[4], rotLeftForward); // leftThigh
                Rotations[5] = CalculateGlobalRotation(Positions[5]-Positions[4], Positions[3]-Positions[4], rotLeftForward); // leftCalf

                Rotations[6] = CalculateGlobalRotation(Positions[7]-Positions[6], Positions[3]-Positions[2], rotLeftDown); // waist
                Rotations[7] = CalculateGlobalRotation(Positions[8]-Positions[7], Positions[6]-Positions[7], rotLeftDown); // chest
                Rotations[8] = CalculateGlobalRotation(Positions[9]-Positions[8], Positions[13]-Positions[12], rotLeftDown); // neck

                Rotations[9] = CalculateGlobalRotation(Positions[12]-Positions[8], Positions[8]-Positions[7], rotLeftUp); // rightClavicle
                Rotations[10] = CalculateGlobalRotation(Positions[11]-Positions[12], Positions[10]-Positions[11], rotLeftBack); // rightUpperArm
                Rotations[11] = CalculateGlobalRotation(Positions[10]-Positions[11], Positions[12]-Positions[11], rotLeftBack); // rightForearm

                Rotations[12] = CalculateGlobalRotation(Positions[13]-Positions[8], Positions[7]-Positions[8], rotLeftUp); // leftClavicle
                Rotations[13] = CalculateGlobalRotation(Positions[14]-Positions[13], Positions[15]-Positions[14], rotLeftBack); // leftUpperArm
                Rotations[14] = CalculateGlobalRotation(Positions[15]-Positions[14], Positions[13]-Positions[14], rotLeftBack); // leftForearm
            }

            // calculate the Bone Local Rotations
            for(int j = 0; j < 15; j++)
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

                LocalRotations[6] = Rotations[6]; // waist
                LocalRotations[7] = CalculateLocalRotation(Rotations[6], Rotations[7]); // chest
                LocalRotations[8] = CalculateLocalRotation(Rotations[7], Rotations[8]); // neck

                LocalRotations[9] = CalculateLocalRotation(Rotations[8], Rotations[9]); // rightClavicle
                LocalRotations[10] = CalculateLocalRotation(Rotations[9], Rotations[10]); // rightUpperArm
                LocalRotations[11] = CalculateLocalRotation(Rotations[10], Rotations[11]); // rightForearm

                LocalRotations[12] = CalculateLocalRotation(Rotations[8], Rotations[12]); // leftClavicle
                LocalRotations[13] = CalculateLocalRotation(Rotations[12], Rotations[13]); // leftUpperArm
                LocalRotations[14] = CalculateLocalRotation(Rotations[13], Rotations[14]); // leftForearm
            }
        }

        // CalculateGlobalRotation is called for calculating the global rotation by aligning the bone coordinate system of avatar
        public Quaternion CalculateGlobalRotation(Vector3 boneDirection, Vector3 leftReference)
        {
            var right = boneDirection;
            var leftRef = leftReference;
            // construct the normal direction vector(forward/upward) using left-hand coordinate rule
            var forward = Vector3.Cross(right, leftRef);
            var up = Vector3.Cross(forward, right);
            forward.Normalize();
            up.Normalize();
            
            // construct the rotation relative to world coordinate system using forward and upward
            var rot = Quaternion.LookRotation(forward, up);

            return rot;
        }
        
        // CalculateGlobalRotation is called for calculating the global rotation by relatively rotating based on the initial T-Pose of avatar
        public Quaternion CalculateGlobalRotation(Vector3 boneDirection, Vector3 left, Quaternion init)
        {
            var d = boneDirection;
            var up = Vector3.Cross(d, left); // construct the up direction(left-hand coordinate)
            d.Normalize();
            up.Normalize();
            var target = Quaternion.LookRotation(d, up); // get the current rotation of the specified coordinate

            // TODO: rotate the rotation for referring to world coordinate system(make sure the init on the left or on the right)
            var rot = target * Quaternion.Inverse(init);

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

        // ForwardKinematics is called for getting the joint keypoint positions of the avatar in Unity by the forward kinematics
        public void ForwardKinematics()
        {
            // initialize the new joint positions of the avatar
            for(int i = 0; i < 16; i++)
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
            FKPositions[7] = FKPositions[6] + HumanBoneVectors[6] / HumanBoneLengths[6] * AvatarBoneLengths[6]; // thorax
            FKPositions[8] = FKPositions[7] + HumanBoneVectors[7] / HumanBoneLengths[7] * AvatarBoneLengths[7]; // upperNeck
            FKPositions[9] = FKPositions[8] + HumanBoneVectors[8] / HumanBoneLengths[8] * AvatarBoneLengths[8]; // headTop
            // right arm
            FKPositions[12] = FKPositions[8] + HumanBoneVectors[9] / HumanBoneLengths[9] * AvatarBoneLengths[9]; // rightShoulder
            FKPositions[11] = FKPositions[12] + HumanBoneVectors[10] / HumanBoneLengths[10] * AvatarBoneLengths[10]; // rightElbow
            FKPositions[10] = FKPositions[11] + HumanBoneVectors[11] / HumanBoneLengths[11] * AvatarBoneLengths[11]; // rightWrist
            // left arm
            FKPositions[13] = FKPositions[8] + HumanBoneVectors[12] / HumanBoneLengths[12] * AvatarBoneLengths[12]; // leftShoulder
            FKPositions[14] = FKPositions[13] + HumanBoneVectors[13] / HumanBoneLengths[13] * AvatarBoneLengths[13]; // leftElbow
            FKPositions[15] = FKPositions[14] + HumanBoneVectors[14] / HumanBoneLengths[14] * AvatarBoneLengths[14]; // leftWrist

            // set the foot of the avatar on the ground if necessary
            if(SetFootOnGround)
            {
                float offsetY = 0.0f;
                if(FKPositions[0].y <= FKPositions[5].y) // check which foot(left/right) is lower in Y axis coordinate
                {
                    offsetY = Mathf.Abs(FKPositions[0].y); // get the offset from the feet to the ground(zero plane)
                    if(FKPositions[0].y <= 0.0f) // check whether the foot under ground or not and correct the offset
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
                    if(FKPositions[5].y <= 0.0f)
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
    }
}
