using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class BVHExMecanim : BVHBase {
	
#if UNITY_EDITOR

	private Animator animator;
	
	class HrcPair
	{
		public HumanBodyBones Name;
		public HumanBodyBones Parent;
		
		public HrcPair(HumanBodyBones name,HumanBodyBones parent)
		{
			Name = name;
			Parent = parent;
		}
	}
	HrcPair[] hrcPair = new HrcPair[]
	{
		new HrcPair(HumanBodyBones.Hips,HumanBodyBones.Hips),
		new HrcPair(HumanBodyBones.Spine,HumanBodyBones.Hips),
		new HrcPair(HumanBodyBones.Chest,HumanBodyBones.Spine),
		new HrcPair(HumanBodyBones.Neck,HumanBodyBones.Chest),
		new HrcPair(HumanBodyBones.Head,HumanBodyBones.Neck),
//		new HrcPair(HumanBodyBones.LeftEye,HumanBodyBones.Head),
//		new HrcPair(HumanBodyBones.RightEye,HumanBodyBones.Head),
//		new HrcPair(HumanBodyBones.Jaw,HumanBodyBones.Head),
		new HrcPair(HumanBodyBones.LeftShoulder,HumanBodyBones.Chest),
		new HrcPair(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftShoulder),
		new HrcPair(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftUpperArm),
		new HrcPair(HumanBodyBones.LeftHand,HumanBodyBones.LeftLowerArm),
		new HrcPair(HumanBodyBones.RightShoulder,HumanBodyBones.Chest),
		new HrcPair(HumanBodyBones.RightUpperArm,HumanBodyBones.RightShoulder),
		new HrcPair(HumanBodyBones.RightLowerArm,HumanBodyBones.RightUpperArm),
		new HrcPair(HumanBodyBones.RightHand,HumanBodyBones.RightLowerArm),
		new HrcPair(HumanBodyBones.LeftUpperLeg,HumanBodyBones.Hips),
		new HrcPair(HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftUpperLeg),
		new HrcPair(HumanBodyBones.LeftFoot,HumanBodyBones.LeftLowerLeg),
		new HrcPair(HumanBodyBones.LeftToes,HumanBodyBones.LeftFoot),
		new HrcPair(HumanBodyBones.RightUpperLeg,HumanBodyBones.Hips),
		new HrcPair(HumanBodyBones.RightLowerLeg,HumanBodyBones.RightUpperLeg),
		new HrcPair(HumanBodyBones.RightFoot,HumanBodyBones.RightLowerLeg),
		new HrcPair(HumanBodyBones.RightToes,HumanBodyBones.RightFoot),
	};
	NodeInfo FindNodeInfoParent(HumanBodyBones b)
	{
		NodeInfo info = FindNodeInfo(b.ToString());
		if ((info!=null) && (info.IsExist))
		{
			return info;
		}
		foreach(HrcPair p in hrcPair)
		{
			if (p.Name == b)
			{
				if (p.Name != p.Parent)
				{
					return FindNodeInfoParent(p.Parent);
				}
			}
		}
		return null;
	}
	
	// set first pose
	void SetFirstPose()
	{
		foreach(HrcPair p in hrcPair)
		{
			NodeInfo i = new NodeInfo(p.Name.ToString());
			i.Tag = p;
			try
			{
				Transform o = animator.GetBoneTransform(p.Name);// GetGameObject(p.Name);
				if (o!=null)
				{
					i.Trans = o;
					i.IsExist = true;
				}
			}catch(Exception)
			{
			}
			nodeInfos.Add(i);
		}
		foreach(NodeInfo i in nodeInfos)
		{
			if (i.IsExist)
			{
				HrcPair p = i.Tag as HrcPair;
				i.ParentInfo = FindNodeInfoParent(p.Parent);
				if (i.ParentInfo != i)
				{
					i.ParentInfo.InfoChildren.Add(i);
				}
			}
		}
		CaptureInit();
	}
	
	// Use this for initialization
	void Start () {
		animator = GetComponent<Animator>();
		
		SetFirstPose();
	}
	
	
	void OnDestroy()
	{
		CaptureEnd();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		AddPose();
	}
	void Update()
	{
		ModifyCaptureState();
	}
#endif
}
