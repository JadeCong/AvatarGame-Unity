using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JustWithJoints.Core
{
    public class Pose
    {
        public int Frame;

        public List<Vector3> Positions { get; set; }
        public List<Vector3> RawPositions { get; set; }
        public List<float> BoneLengths { get; set; }
        public List<Quaternion> LocalRotations { get; set; }
        public List<Quaternion> Rotations { get; set; }

        public Pose(int frame, List<Vector3> positions)
        {
            Frame = frame;
            Positions = positions.ToList();
            Refresh();
        }
        
        List<Vector3> asRightHanded(List<Vector3> positions)
        {
            int[] inds = new[]
            {
                5, 4, 3,
                2, 1, 0,
                11, 10, 9,
                8, 7, 6,
                12, 13
            };
            List<Vector3> newPositions = new List<Vector3>();
            foreach (int i in inds)
            {
                newPositions.Add(positions[i]);
            }
            return newPositions;
        }

        public void Refresh()
        {
            BoneLengths = new List<float>();
            LocalRotations = new List<Quaternion>();
            Rotations = new List<Quaternion>();

            // Input = Right-Handed Joint Locations
            // Output = Left-Handed Joint Orientations
            //   Note: input is RH just for historical reasons. 
            var positionsRightHanded = asRightHanded(Positions);
            calculateBoneLengths(positionsRightHanded);
            calculateBoneRotations(positionsRightHanded);
        }

        void calculateBoneLengths(List<Vector3> positionsRightHanded)
        {
            // Positions are in the LSP order.
            int[] boneJoints = new int[]
            {
                // 14 means hip
                2, 3,
                3, 4,
                4, 5,
                2, 1,
                1, 0,
                14, 12,
                12, 9,
                9, 10,
                10, 11,
                12, 8,
                8, 7,
                7, 6,
                12, 13,
            };

            var positions = positionsRightHanded.ToList();
            var hip = (positions[2] + positions[3]) * 0.5f;
            positions.Add(hip);

            for (int i = 0; i < 13; i++)
            {
                int src = boneJoints[2 * i];
                int dst = boneJoints[2 * i + 1];
                var translate = positions[src] - positions[dst];
                var boneLength = translate.magnitude;
                BoneLengths.Add(boneLength);
            }
           
        }
        
        void calculateBoneRotations(List<Vector3> positionsRightHanded)
        {
            var positions = positionsRightHanded.ToList();

            // Global Rotations
            for (int i = 0; i < 13; i++)
            {
                Rotations.Add(Quaternion.identity);
            }
            var rotLeftForward = Quaternion.LookRotation(Vector3.left, Vector3.forward);
            var rotLeftBack = Quaternion.LookRotation(Vector3.left, Vector3.back);
            var rotLeftDown = Quaternion.LookRotation(Vector3.left, Vector3.down);
            var rotLeftUp = Quaternion.LookRotation(Vector3.left, Vector3.up);
            var hip = (positions[2] + positions[3]) * 0.5f;
            Rotations[0] = calculateGlobalRotation(Positions[3] - Positions[2], hip - Positions[12], rotLeftDown);
            Rotations[1] = calculateGlobalRotation(Positions[1] - Positions[2], Positions[0] - Positions[1], rotLeftForward);
            Rotations[2] = calculateGlobalRotation(Positions[0] - Positions[1], Positions[2] - Positions[1], rotLeftForward); //
            Rotations[3] = calculateGlobalRotation(Positions[4] - Positions[3], Positions[5] - Positions[4], rotLeftForward);
            Rotations[4] = calculateGlobalRotation(Positions[5] - Positions[4], Positions[3] - Positions[4], rotLeftForward); //
            Rotations[5] = calculateGlobalRotation(Positions[12] - hip, -Positions[3] + Positions[2], rotLeftDown);
            Rotations[6] = calculateGlobalRotation(Positions[8] - Positions[12], Positions[9] - Positions[8], rotLeftUp); //
            Rotations[7] = calculateGlobalRotation(Positions[7] - Positions[8], Positions[6] - Positions[7], rotLeftBack);
            Rotations[8] = calculateGlobalRotation(Positions[6] - Positions[7], Positions[8] - Positions[7], rotLeftBack);
            Rotations[9] = calculateGlobalRotation(Positions[9] - Positions[12], Positions[9] - Positions[8], rotLeftUp);
            Rotations[10] = calculateGlobalRotation(Positions[10] - Positions[9], Positions[11] - Positions[10], rotLeftBack);
            Rotations[11] = calculateGlobalRotation(Positions[11] - Positions[10], Positions[9] - Positions[10], rotLeftBack);
            Rotations[12] = calculateGlobalRotation(Positions[13] - Positions[12], -Positions[9] + Positions[8], rotLeftDown);
/*
            // Local Rotations
            var invRoot = Quaternion.Inverse(Rotations[0]);
            var invRULeg = Quaternion.Inverse(Rotations[1]);
            var invLULeg = Quaternion.Inverse(Rotations[3]);
            var invSpine = Quaternion.Inverse(Rotations[5]);
            var invRShoulder = Quaternion.Inverse(Rotations[6]);
            var invRUArm = Quaternion.Inverse(Rotations[7]);
            var invLShoulder = Quaternion.Inverse(Rotations[9]);
            var invLUArm = Quaternion.Inverse(Rotations[10]);
            LocalRotations = Rotations.ToList();
            LocalRotations[1] = invRoot * LocalRotations[1];
            LocalRotations[2] = invRULeg * LocalRotations[2];
            LocalRotations[3] = invRoot * LocalRotations[3];
            LocalRotations[4] = invLULeg * LocalRotations[4];
            LocalRotations[6] = invSpine * LocalRotations[6];
            LocalRotations[7] = invRShoulder * LocalRotations[7];
            LocalRotations[8] = invRUArm * LocalRotations[8];
            LocalRotations[9] = invSpine * LocalRotations[9];
            LocalRotations[10] = invLShoulder * LocalRotations[10];
            LocalRotations[11] = invLUArm * LocalRotations[11];
            LocalRotations[12] = invSpine * LocalRotations[12];
*/
	    // fix 
	    Vector3 v1=Rotations[1].eulerAngles;
            Vector3 v3=Rotations[3].eulerAngles;


	    if(v1.z<0)
	    {
		v1.z = 0;
	    }

	    if(v3.z<0)
	    {
		v3.z = 0;
	    }



            //Rotations[5] = Quaternion.Euler(new Vector3(0,0,0));//hip

            Rotations[1] = Quaternion.Euler(new Vector3(v1.x,v1.y,v1.z));//right hip
            //Rotations[2] = Quaternion.Euler(new Vector3(0,0,0));//right foot ankle
            Rotations[3] = Quaternion.Euler(new Vector3(v3.x,v3.y,v3.z));//left hip

            //Rotations[4] = Quaternion.Euler(new Vector3(0,0,0)); // left foot ankle
	    
        }

        Quaternion calculateGlobalRotation(Vector3 boneDirection, Vector3 left, Quaternion init)
        {
            var d = boneDirection;
            var up = Vector3.Cross(d, left);
            d.Normalize();
            up.Normalize();
            var target = Quaternion.LookRotation(d, up);
            var rot = target * Quaternion.Inverse(init);
            return rot;
        }

        public List<Vector3> FK()
        {
            var positions = Positions.ToList();
            var hip = (positions[2] + positions[3]) * 0.5f;
            positions.Add(hip);

            var lengths = BoneLengths.ToList();
            var hipLength = lengths[0] * 0.5f;
            lengths[0] = hipLength;
            lengths.Insert(0, -hipLength);
            var rots = LocalRotations.ToList();
            rots.Insert(0, rots[0]);

            int[] joints = new int[] { 2, 3, 1, 0, 4, 5, 12, 8, 7, 6, 9, 10, 11, 13, };

            // 14 means hip
            int[] parentJoints = new int[] { 14, 14, 2, 1, 3, 4, 14, 12, 8, 7, 12, 9, 10, 12, };

            Vector3[] fkPositions = new Vector3[15];
            fkPositions[14] = hip;
            Quaternion[] globalRotations = new Quaternion[15];
            globalRotations[14] = Quaternion.identity;

            for (int i = 0; i < joints.Length; i++)
            {
                float len = lengths[i];
                Quaternion localRot = rots[i];
                int j = joints[i];
                int p = parentJoints[i];
                Vector3 pPos = fkPositions[p];
                Quaternion parentRot = globalRotations[p];
                Quaternion globalRot = parentRot * localRot;
                Vector3 translate = globalRot * Vector3.left * len;
                Vector3 pos = pPos + translate;
                fkPositions[j] = pos;
                globalRotations[j] = globalRot;
            }

            return fkPositions.Take(14).ToList();
        }

    }
}
