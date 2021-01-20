
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class BVHExAll : BVHBase {

	void AddChild(Transform t,NodeInfo parent)
	{
		NodeInfo i = new NodeInfo(t.name);
		i.Trans = t;
		i.IsExist = true;
		if (parent!=null)
		{
			i.ParentInfo = parent;
			parent.InfoChildren.Add(i);
		}else{
			i.ParentInfo = i;
		}
		nodeInfos.Add(i);
		
		for(int idChild=0;idChild<t.childCount;++idChild)
		{
			AddChild(t.GetChild(idChild),i);
		}
	}
	
	// set first pose
	void SetFirstPose()
	{
		AddChild(this.transform,null);
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
