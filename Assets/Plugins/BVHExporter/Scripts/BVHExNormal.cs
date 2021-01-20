
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class BVHExNormal : BVHBase {
	
	public GameObject Hips;
	public GameObject Spine1;
	public GameObject Spine2;
	public GameObject Spine3;
	public GameObject Neck;
	public GameObject Head;
	public GameObject LeftShoulder;
	public GameObject LeftArm;
	public GameObject LeftForeArm;
	public GameObject LeftHand;
	public GameObject RightShoulder;
	public GameObject RightArm;
	public GameObject RightForeArm;
	public GameObject RightHand;
	public GameObject LeftUpLeg;
	public GameObject LeftLeg;
	public GameObject LeftFoot;
	public GameObject LeftToe;
	public GameObject RightUpLeg;
	public GameObject RightLeg;
	public GameObject RightFoot;
	public GameObject RightToe;
		
	class HrcPair
	{
		public string Name;
		public string Parent;
		
		public HrcPair(string name,string parent)
		{
			Name = name;
			Parent = parent;
		}
	}
	HrcPair[] hrcPair = new HrcPair[]
	{
		new HrcPair("Hips","Hips"),
		new HrcPair("Spine1","Hips"),
		new HrcPair("Spine2","Spine1"),
		new HrcPair("Spine3","Spine2"),
		new HrcPair("Neck","Spine3"),
		new HrcPair("Head","Neck"),
		new HrcPair("LeftShoulder","Spine3"),
		new HrcPair("LeftArm","LeftShoulder"),
		new HrcPair("LeftForeArm","LeftArm"),
		new HrcPair("LeftHand","LeftForeArm"),
		new HrcPair("RightShoulder","Spine3"),
		new HrcPair("RightArm","RightShoulder"),
		new HrcPair("RightForeArm","RightArm"),
		new HrcPair("RightHand","RightForeArm"),
		new HrcPair("LeftUpLeg","Hips"),
		new HrcPair("LeftLeg","LeftUpLeg"),
		new HrcPair("LeftFoot","LeftLeg"),
		new HrcPair("LeftToe","LeftFoot"),
		new HrcPair("RightUpLeg","Hips"),
		new HrcPair("RightLeg","RightUpLeg"),
		new HrcPair("RightFoot","RightLeg"),
		new HrcPair("RightToe","RightFoot"),
	};
	NodeInfo FindNodeInfoParent(string b)
	{
		NodeInfo info = FindNodeInfo(b);
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
	
	GameObject GetGameObject(string name)
	{
		switch(name)
		{
		case "Hips":
			return Hips;
		case "Spine1":
			return Spine1;
		case "Spine2":
			return Spine2;
		case "Spine3":
			return Spine3;
		case "Neck":
			return Neck;
		case "Head":
			return Head;
		case "LeftShoulder":
			return LeftShoulder;
		case "LeftArm":
			return LeftArm;
		case "LeftForeArm":
			return LeftForeArm;
		case "LeftHand":
			return LeftHand;
		case "RightShoulder":
			return RightShoulder;
		case "RightArm":
			return RightArm;
		case "RightForeArm":
			return RightForeArm;
		case "RightHand":
			return RightHand;
		case "LeftUpLeg":
			return LeftUpLeg;
		case "LeftLeg":
			return LeftLeg;
		case "LeftFoot":
			return LeftFoot;
		case "LeftToe":
			return LeftToe;
		case "RightUpLeg":
			return RightUpLeg;
		case "RightLeg":
			return RightLeg;
		case "RightFoot":
			return RightFoot;
		case "RightToe":
			return RightToe;
		}
		return null;
	}
	
	
	// set first pose
	void SetFirstPose()
	{
		foreach(HrcPair p in hrcPair)
		{
			NodeInfo i = new NodeInfo(p.Name);
			i.Tag = p;
			GameObject o = GetGameObject(p.Name);
			if (o!=null)
			{
				i.Trans = o.transform;
				i.IsExist = true;
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

}
