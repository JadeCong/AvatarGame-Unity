using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;


namespace JustWithJoints.Avatars
{
    public class UnityChanAvatar : MonoBehaviour
    {
        public GameObject MotionProvider;
	    public GameObject humanoid;


        public bool RetargetPose = true;
        public bool RetargetRootLocation = false ;

		public int modelId = 1;

        List<GameObject> joints_ = new List<GameObject>();
        List<RigBone> bones_ = new List<RigBone>();
		List<RigBone> fixBones_ = new List<RigBone>();

        
        // Use this for initialization
        void Start()
        {
			
			RigBone	hips = new RigBone(humanoid, HumanBodyBones.Hips);
			RigBone	rightUpLeg = new RigBone(humanoid, HumanBodyBones.RightUpperLeg);
			RigBone rightLeg = new RigBone(humanoid, HumanBodyBones.RightLowerLeg);

			RigBone leftUpLeg = new RigBone(humanoid, HumanBodyBones.LeftUpperLeg);
			RigBone leftLeg = new RigBone(humanoid, HumanBodyBones.LeftLowerLeg);




			RigBone	spine = new RigBone(humanoid, HumanBodyBones.Spine);
			RigBone	rightShoulder = new RigBone(humanoid, HumanBodyBones.RightShoulder);
			RigBone rightArm = new RigBone(humanoid, HumanBodyBones.RightUpperArm);
			RigBone rightForeArm = new RigBone(humanoid, HumanBodyBones.RightLowerArm);

			RigBone leftShoulder = new RigBone(humanoid, HumanBodyBones.LeftShoulder);



			RigBone	leftArm = new RigBone(humanoid, HumanBodyBones.LeftUpperArm);
			RigBone	leftForeArm = new RigBone(humanoid, HumanBodyBones.LeftLowerArm);
			RigBone neck = new RigBone(humanoid, HumanBodyBones.Neck);


			RigBone	leftHand_ = new RigBone(humanoid, HumanBodyBones.LeftHand);
			RigBone	rightHand_ = new RigBone(humanoid, HumanBodyBones.RightHand);
			RigBone leftToes_ = new RigBone(humanoid, HumanBodyBones.LeftToes);
			RigBone rightToes_ = new RigBone(humanoid, HumanBodyBones.RightToes);

			RigBone leftFoot = new RigBone(humanoid, HumanBodyBones.LeftFoot);
			RigBone rightFoot = new RigBone(humanoid, HumanBodyBones.RightFoot);

            bones_.AddRange(new RigBone[]
            {
                hips,
                rightUpLeg,
                rightLeg,
                leftUpLeg,
                leftLeg,

                spine,
                rightShoulder,
                rightArm,
                rightForeArm,
                leftShoulder,

                leftArm,
                leftForeArm,
                neck,
            });

			fixBones_.AddRange(new RigBone[]
			{
				leftHand_,
				rightHand_,
				leftToes_,
				rightToes_,
				leftFoot,
				rightFoot,

			});



        }

        void Update()
        {
			 
            Core.Pose pose = null;
			var component = MotionProvider.GetComponent<CMUMotionPlayer>();
            if (MotionProvider)
            {
                
                if (component)
                {
					//file
                    //pose = component.GetCurrentPose();

					//socket
					pose = component.GetSocketPose(modelId);

					//Debug.LogError ("modelId."+modelId);
                }
            }

            if (pose != null)
            {
                if (RetargetRootLocation)
                {
                    bones_[0].setWordPos ( (pose.Positions[2] + pose.Positions[3]) * 0.5f + gameObject.transform.position	);
                }

                if (RetargetPose)
                {
                    for (int i = 0; i < 13; i++)
                    {
                        int boneIndex = i;
                        if (i == (int)Core.BoneType.Trans)
                        {
                            // Use spine of Core.Pose as root of avatar
                            boneIndex = (int)Core.BoneType.Spine;
                        }
                        if (i == (int)Core.BoneType.Spine)
                        {
                            // Skip spine
                            continue;
                        }
			
			/*
			if (0 ==i)
			{
				Quaternion rotation = Quaternion.Euler(-90,0,0);
				bones_[i].setWordRot(	 rotation*pose.Rotations[boneIndex]	);
			}
			else
			{

				bones_[i].setWordRot(	 pose.Rotations[boneIndex]	);

			}
			*/

			//为了适应ｈｍｒ，总体旋转了９０
			//Quaternion rotation = Quaternion.Euler(90,0,0);
			//bones_[i].setWordRot(	 rotation*pose.Rotations[boneIndex]	);


			bones_[i].setWordRot(	 pose.Rotations[boneIndex]	);

                    }


					for (int i = 0; i < 6; i++) {
						fixBones_ [i].set (fixBones_ [i].savedValue);			
					}


                }

				Vector3 hipPos = bones_ [(int)Core.BoneType.Trans].getNowWordPos ();
				Vector3 rightLegPos = fixBones_ [5].getNowWordPos ();
				Vector3 leftLegPos = fixBones_ [4].getNowWordPos ();

				// keep foot on the ground
				var hipHeight = Math.Max (hipPos.y-rightLegPos.y,hipPos.y-leftLegPos.y);

				Vector3 tmpLeft = fixBones_ [2].getNowWordPos ();
				fixBones_ [2].setWordPos (new Vector3(tmpLeft.x,leftLegPos.y,tmpLeft.z));

				Vector3 tmpRight = fixBones_ [3].getNowWordPos ();
				fixBones_ [3].setWordPos (new Vector3(tmpRight.x,rightLegPos.y,tmpRight.z));


				//Debug.LogError ("modelId."+modelId +",tmpRight "+new Vector3(tmpRight.x,rightLegPos.y,tmpRight.z));
				//print("!!! "+(hipHeight));
				//var hipHeight = hips.position.y - foot.position.y;


				float scale = 0.5f;
				Vector3 transT = component.GetHipTrans (modelId);

				//add by lightSnail
				//start
				if (modelId == 0) {
					transT = new Vector3 (1,0,0);
				} else {
					transT = new Vector3 (-1,0,0);
				}
				//end

				//Vector3 trans = new Vector3 (transT.x,-transT.y,-transT.z*scale);
				Vector3 trans = new Vector3 (transT.x,hipHeight,-transT.z*scale);
				bones_ [0].setWordPos( trans	);
				//print (trans.x);

				
            }
        }
    }
}
