using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigBone {
	public GameObject gameObject;
	public HumanBodyBones bone;
	public bool isValid;

	Animator animator;
	public Quaternion savedValue;
	public Vector3	wPos;

	public RigBone(GameObject g, HumanBodyBones b) 
	{
		gameObject = g;
		bone = b;
		isValid = false;
		animator = gameObject.GetComponent<Animator>();
		if (animator == null) {
		Debug.Log("no Animator Component");
		return;
		}

		Avatar avatar = animator.avatar;
		if (avatar == null || !avatar.isHuman || !avatar.isValid) {
		Debug.Log("Avatar is not Humanoid or it is not valid");
		return;
		}

		isValid = true;
		savedValue = animator.GetBoneTransform(bone).localRotation;
		wPos = animator.GetBoneTransform(bone).position;
	}


	public void set(float a, float x, float y, float z) {

		set(Quaternion.AngleAxis(a, new Vector3(x,y,z)));
	}

	public void set(Quaternion q) {
		animator.GetBoneTransform(bone).localRotation = q;
		savedValue = q;
	}


	public void setWordRot(Quaternion q) {
		animator.GetBoneTransform(bone).rotation = q;
		savedValue = q;
	}
	public void setEuler(Vector3 rot) {
		animator.GetBoneTransform(bone).localRotation =  Quaternion.Euler(rot);
		savedValue = Quaternion.Euler(rot);
	}


	public void setWordPos(Vector3 pos) {
		animator.GetBoneTransform(bone).position =  pos;
		wPos = pos;
	}

	public Vector3 getNowWordPos() {
		return animator.GetBoneTransform(bone).position ;
	}


	public void mul(float a, float x, float y, float z) {
		mul(Quaternion.AngleAxis(a, new Vector3(x,y,z)));
	}

	public void mul(Quaternion q) {
		Transform tr = animator.GetBoneTransform(bone);
		tr.localRotation = q * tr.localRotation;
	}
	public void offset(float a, float x, float y, float z) {
		offset(Quaternion.AngleAxis(a, new Vector3(x,y,z)));
	}
	public void offset(Quaternion q) {
		animator.GetBoneTransform(bone).localRotation = q * savedValue;
	}
	public void changeBone(HumanBodyBones b) {
		bone = b;
		savedValue = animator.GetBoneTransform(bone).localRotation;
	}


}

