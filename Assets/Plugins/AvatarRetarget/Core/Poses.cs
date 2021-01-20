using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AvatarRetarget.Core
{
    public class Poses
    {
        // public int Frame;
        public List<Vector3> Positions { get; set; }
        public List<Quaternion> HumanToAvatarTransformRotations { get; set; }
        public List<float> BoneLengths { get; set; }
        public List<Quaternion> LocalRotations { get; set; }
        public List<Quaternion> Rotations { get; set; }

        public Poses(List<Vector3> positions, List<Quaternion> boneTransformRots)
        {
            // Frame = frame;
            Positions = new List<Vector3>();
            HumanToAvatarTransformRotations = new List<Quaternion>();
            if(positions.Count == 0 || boneTransformRots.Count == 0) // positions.Count=0 while executed for the first cycle
            {
                Debug.Log("Bad data: Positions.Count=0 or HumanToAvatarTransformRotations.Count=0......"); // for debug
                return;
            }
            Positions = positions; // world positions
            HumanToAvatarTransformRotations = boneTransformRots;
            Refresh();
        }

        public void Refresh()
        {
            BoneLengths = new List<float>();
            LocalRotations = new List<Quaternion>(); // local rotations relative to parents
            Rotations = new List<Quaternion>(); // world rotations 
            CalculateBoneLengths();
            CalculateBoneRotations();
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
                var boneLength = translate.magnitude;
                BoneLengths.Add(boneLength);
            }
        }

        public void CalculateBoneRotations()
        {
            // Global Rotations
            for(int i = 0; i < 15; i++)
            {
                Rotations.Add(Quaternion.identity);
            }

            // TODO: make sure that the coordinate system will be the same(fixed to the bones) for every frame based on the human joint keypoints
            Rotations[0] = CalculateGlobalRotation(Positions[2]-Positions[6], Positions[7]-Positions[6], HumanToAvatarTransformRotations[0]);
            Rotations[1] = CalculateGlobalRotation(Positions[1]-Positions[2], Positions[0]-Positions[1], HumanToAvatarTransformRotations[1]);
            Rotations[2] = CalculateGlobalRotation(Positions[0]-Positions[1], Positions[2]-Positions[1], HumanToAvatarTransformRotations[2]);

            Rotations[3] = CalculateGlobalRotation(Positions[3]-Positions[6], Positions[6]-Positions[7], HumanToAvatarTransformRotations[3]);
            Rotations[4] = CalculateGlobalRotation(Positions[4]-Positions[3], Positions[5]-Positions[4], HumanToAvatarTransformRotations[4]);
            Rotations[5] = CalculateGlobalRotation(Positions[5]-Positions[4], Positions[3]-Positions[4], HumanToAvatarTransformRotations[5]);

            Rotations[6] = CalculateGlobalRotation(Positions[7]-Positions[6], Positions[3]-Positions[2], HumanToAvatarTransformRotations[6]);
            Rotations[7] = CalculateGlobalRotation(Positions[8]-Positions[7], Positions[6]-Positions[7], HumanToAvatarTransformRotations[7]);
            Rotations[8] = CalculateGlobalRotation(Positions[9]-Positions[8], Positions[13]-Positions[12], HumanToAvatarTransformRotations[8]);

            Rotations[9] = CalculateGlobalRotation(Positions[12]-Positions[8], Positions[8]-Positions[7], HumanToAvatarTransformRotations[9]);
            Rotations[10] = CalculateGlobalRotation(Positions[11]-Positions[12], Positions[10]-Positions[11], HumanToAvatarTransformRotations[10]);
            Rotations[11] = CalculateGlobalRotation(Positions[10]-Positions[11], Positions[12]-Positions[11], HumanToAvatarTransformRotations[11]);

            Rotations[12] = CalculateGlobalRotation(Positions[13]-Positions[8], Positions[7]-Positions[8], HumanToAvatarTransformRotations[12]);
            Rotations[13] = CalculateGlobalRotation(Positions[14]-Positions[13], Positions[15]-Positions[14], HumanToAvatarTransformRotations[13]);
            Rotations[14] = CalculateGlobalRotation(Positions[15]-Positions[14], Positions[13]-Positions[14], HumanToAvatarTransformRotations[14]);

            // Debug.Log("WTF, Rotations[0]: " + Rotations[0]); // for debug
            // Debug.Log("WTF, Rotations[1]: " + Rotations[1]); // for debug
            // Debug.Log("WTF, Rotations[2]: " + Rotations[2]); // for debug

            // Local Rotations
            LocalRotations = Rotations.ToList();

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
            // TODO: make sure the inv on the left or the right
            LocalRotations[0] = LocalRotations[0] * invWaist;
            LocalRotations[1] = LocalRotations[1] * invRightHipBine;
            LocalRotations[2] = LocalRotations[2] * invRightThigh;

            LocalRotations[3] = LocalRotations[3] * invWaist;
            LocalRotations[4] = LocalRotations[4] * invLeftHipBone;
            LocalRotations[5] = LocalRotations[5] * invLeftThigh;

            // LocalRotations[6] = LocalRotations[6] * invWaist;
            LocalRotations[7] = LocalRotations[7] * invWaist;
            LocalRotations[8] = LocalRotations[8] * invCheset;

            LocalRotations[9] = LocalRotations[9] * invNeck;
            LocalRotations[10] = LocalRotations[10] * invRightClavicle;
            LocalRotations[11] = LocalRotations[11] * invRigthUpperArm;

            LocalRotations[12] = LocalRotations[12] * invNeck;
            LocalRotations[13] = LocalRotations[13] * invLeftClavicle;
            LocalRotations[14] = LocalRotations[14] * invLeftUpperArm;
            // LocalRotations[0] = invWaist * LocalRotations[0];
            // LocalRotations[1] = invRightHipBine * LocalRotations[1];
            // LocalRotations[2] = invRightThigh * LocalRotations[2];

            // LocalRotations[3] = invWaist * LocalRotations[3];
            // LocalRotations[4] = invLeftHipBone * LocalRotations[4];
            // LocalRotations[5] = invLeftThigh * LocalRotations[5];

            // // LocalRotations[6] = invWaist * LocalRotations[6];
            // LocalRotations[7] = invWaist * LocalRotations[7];
            // LocalRotations[8] = invCheset * LocalRotations[8];

            // LocalRotations[9] = invNeck * LocalRotations[9];
            // LocalRotations[10] = invRightClavicle * LocalRotations[10];
            // LocalRotations[11] = invRigthUpperArm * LocalRotations[11];

            // LocalRotations[12] = invNeck * LocalRotations[12];
            // LocalRotations[13] = invLeftClavicle * LocalRotations[13];
            // LocalRotations[14] = invLeftUpperArm * LocalRotations[14];  
        }

        public Quaternion CalculateGlobalRotation(Vector3 boneDirection, Vector3 right, Quaternion init)
        {
            var d = boneDirection;
            var up = Vector3.Cross(d, right); // construct the up direction(left-hand coordinate)
            d.Normalize();
            up.Normalize();
            var target = Quaternion.LookRotation(d, up); // get the current rotation of the specified coordinate

            // TODO: make sure the init on the left or on the right
            var rot = init * target;
            // var rot = target * init;

            return rot;
        }

        // Get the joint positions of the avatar in Unity by the forward kinematics
        public List<Vector3> ForwardKinematics()
        {
            // get the joint positions, bone lengths and the joint local rotations
            var positions = Positions.ToList();
            var lengths = BoneLengths.ToList();
            var rots = LocalRotations.ToList();

            // get the joint order and the parent joint order(MPII order)
            int[] joints = new int[] {6, 2, 1, 0, 3, 4, 5, 7, 8, 9, 12, 11, 10, 13, 14, 15};
            int[] parentJoints = new int[] {6, 6, 2, 1, 6, 3, 4, 6, 7, 8, 8, 12, 11, 8, 13, 14}; // pelvis's parent is self(6)

            // get the new joint positions of the avatar by FK
            Vector3[] fkPositions = new Vector3[16];
            Quaternion[] globalRotations = new Quaternion[16];
            for(int i = 0; i < joints.Length; i++)
            {
                float len = lengths[i];
                Quaternion localRot = rots[i];

                int j = joints[i];
                int pj = parentJoints[i];

                Vector3 pPos = fkPositions[pj];
                Quaternion parentRot = globalRotations[pj];
                Quaternion globalRot = parentRot * localRot;
                Vector3 translate = globalRot * Vector3.left * len;
                Vector3 pos = pPos + translate;
                fkPositions[j] = pos;
                globalRotations[j] = globalRot;
            }

            // return the new joint positions of the avatar
            return fkPositions.Take(16).ToList();
        }

        // Clear the class buffer for BoneLengths/HumanToAvatarTransformRotations/Positions/Rotations/LocalRotations
        public void Clear()
        {
            Positions.Clear();
            HumanToAvatarTransformRotations.Clear();
            BoneLengths.Clear();
            Rotations.Clear();
            LocalRotations.Clear();
        }
    }
}
