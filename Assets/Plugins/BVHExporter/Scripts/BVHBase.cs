using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BVHBase : MonoBehaviour {
		
	public enum FolderType
	{
		MyDocuments,
		ApplicationData,
		Desktop,
		Other,
	};
	public FolderType SelectFolder = FolderType.MyDocuments;
	public string FolderName = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
	public bool RightHandCoordinate = true;
	public bool OriginalName = false;
	public bool FromOrigin = false;
	public float Scale = 1f;
	
	public enum Mode
	{
		FromStartToFinish,
		RecordButton,
		ShortcutKey,
	};
	public Mode CaptureMode = Mode.FromStartToFinish; 
	public KeyCode ShortcutKey = KeyCode.P;
	
	protected Quaternion invertPose = Quaternion.AngleAxis(0f,Vector3.up);
	protected bool IsCapturing = false;
	protected Vector3 captureStartPos = Vector3.zero;
	
	protected class NodeInfo
	{		
		public bool IsExist = false;
		public Transform Trans;
		public string BoneName;
		public Vector3 firstPosition;
		public Quaternion firstRotation;
		public List<Vector3> pos = new List<Vector3>();
		public List<Quaternion> rot = new List<Quaternion>();
		public Vector3 prevEuler = Vector3.zero;
		public object Tag = null;
		public NodeInfo ParentInfo;
		public List<NodeInfo> InfoChildren = new List<NodeInfo>();
		
		public NodeInfo(string bone)
		{
			BoneName = bone;
		}
		
		public Vector3 FirstLocalPosition
		{
			get
			{
				if (this!=ParentInfo)
				{
					return firstPosition - ParentInfo.firstPosition;
				}
				return firstPosition;
			}
		}
		
		public bool HasTranslate = false;
		public void Check()
		{
			int num = pos.Count;
			if (num>0)
			{
				Vector3 first = pos[0];
				for(int i=0;i<num;++i)
				{
					if ((pos[i]-first).magnitude > 0.1f)
					{
						HasTranslate = true;
						break;
					}
				}
			}
			foreach(NodeInfo info in InfoChildren)
			{
				info.Check();
			}
		}
	}
	protected List<NodeInfo> nodeInfos = new List<NodeInfo>();
	
	protected NodeInfo FindNodeInfo(string b)
	{
		foreach(NodeInfo i in nodeInfos)
		{
			if (i.BoneName == b)
			{
				return i;
			}
		}
		return null;
	}
	
	protected NodeInfo GetRootInfo()
	{
		if (nodeInfos.Count>0)
		{
			return nodeInfos[0];
		}
		return null;
	}
	
	protected void WriteTab(int tab,StreamWriter sw)
	{
		for(int i=0;i<tab;++i)
		{
			sw.Write("    ");
		}
	}
	
	protected Vector3 PosConvert(Vector3 pos)
	{
		pos *= Scale;
		if (RightHandCoordinate)
		{
			pos.z = -pos.z;
		}
		return pos;
	}
	
	static public Quaternion InvertRot(Quaternion q)
	{
		q.x = -q.x;
		q.y = -q.y;
		q.z = -q.z;
		return q;
	}
	
	protected float DifAngle(float a,float b)
	{
		float abs = Mathf.Abs(b-a);
		if (abs>180f)
		{
			if (a<b)
			{
				int count = (int)((b-a+180f)/360f);
				a += 360f * count;
			}
			else
			{
				int count = (int)((a-b+180f)/360f);
				a -= 360f * count;
			}
		}
		return b-a;
	}
	
	protected Vector3 RotationGimbalAvoid(Vector3 prev,Vector3 cur)
	{
		Vector3 dif = Vector3.zero;
		dif.x = DifAngle(prev.x,cur.x);
		dif.y = DifAngle(prev.y,cur.y);
		dif.z = DifAngle(prev.z,cur.z);
		return prev + dif;
	}

	// add pose
	protected void AddPose()
	{
		if (IsCapturing)
		{
			NodeInfo rootInfo = GetRootInfo();
			if ((rootInfo!=null) && rootInfo.IsExist)
			{
				foreach(NodeInfo i in nodeInfos)
				{
					Transform t = i.Trans;
					if (t!=null)
					{
						Vector3 pos = t.position;
						if (i != i.ParentInfo)
						{
							pos = Matrix4x4.Inverse(i.ParentInfo.Trans.localToWorldMatrix).MultiplyPoint(pos);
						}
//						i.pos.Add(invertPose * t.position);
//						i.rot.Add(invertPose * t.rotation);
						
						if (FromOrigin==false)
						{
							pos -= captureStartPos;
						}
						i.pos.Add(pos);
						i.rot.Add(t.rotation);
					}
				}
			}
		}
	}
	
	
	protected void WriteChildren(NodeInfo i,StreamWriter sw,int tab)
	{
		if (i == GetRootInfo())
		{
			// root
			sw.WriteLine("ROOT " + (OriginalName?i.Trans.name:i.BoneName));
		}else{
			WriteTab(tab,sw);
			sw.WriteLine("JOINT " + (OriginalName?i.Trans.name:i.BoneName));
		}
		// {
		WriteTab(tab,sw);
		sw.WriteLine("{");
		// OFFSET
		WriteTab(tab+1,sw);
		Vector3 ofs = PosConvert(i.FirstLocalPosition);
		sw.WriteLine("OFFSET " + ofs.x.ToString () + " " + ofs.y.ToString() + " " + ofs.z.ToString());
		// CHANNELS
		if ((i == GetRootInfo()) || i.HasTranslate)
		{
			WriteTab(tab+1,sw);
			sw.WriteLine("CHANNELS 6 Xposition Yposition Zposition Yrotation Xrotation Zrotation");
		}else{
			WriteTab(tab+1,sw);
			sw.WriteLine("CHANNELS 3 Yrotation Xrotation Zrotation");
		}
		// children
		foreach(NodeInfo c in i.InfoChildren)
		{
			WriteChildren(c,sw,tab+1);
		}
		// site
		if (i.InfoChildren.Count==0)
		{
			WriteTab(tab+1,sw);
			sw.WriteLine("End Site");
			WriteTab(tab+1,sw);
			sw.WriteLine("{");
			WriteTab(tab+1,sw);
			Vector3 site = PosConvert((i.firstPosition - i.ParentInfo.firstPosition) * 0.2f);
			sw.WriteLine("    OFFSET " + site.x.ToString() + " " + site.y.ToString() + " " + site.z.ToString());
			WriteTab(tab+1,sw);
			sw.WriteLine("}");
		}
		// }
		WriteTab(tab,sw);
		sw.WriteLine("}");
	}

	
	void WriteValue(StreamWriter sw,NodeInfo i,int frame,Quaternion qParent)
	{
		if ((i == GetRootInfo()) || (i.HasTranslate))
		{
			if (i != GetRootInfo())
			{
				sw.Write(" ");
			}
			Vector3 p = PosConvert(i.pos[frame]);
			sw.Write(p.x.ToString() + " " + p.y.ToString() + " " + p.z.ToString());
		}
		Quaternion qInv = InvertRot(qParent);
		Quaternion qGlobal = i.rot[frame] * InvertRot(i.firstRotation);
		Quaternion qRot = qInv * qGlobal;
		Vector3 r = qRot.eulerAngles;
		if (RightHandCoordinate)
		{
			r.x = -r.x;
			r.y = -r.y;
		}
		r = RotationGimbalAvoid(i.prevEuler,r);
		sw.Write(" " + r.y.ToString() + " " + r.x.ToString() + " " + r.z.ToString());
		i.prevEuler = r;
		
		foreach(NodeInfo c in i.InfoChildren)
		{
			WriteValue(sw,c,frame,qGlobal);
		}
	}
	
	protected void WriteValues(StreamWriter sw)
	{
		NodeInfo rootInfo = GetRootInfo();
		for(int i=0;i<rootInfo.pos.Count;++i)
		{
			WriteValue(sw,rootInfo,i,Quaternion.identity);
			sw.WriteLine("");
		}
	}
	
	public void CaptureInit()
	{
		switch(SelectFolder)
		{
		case FolderType.MyDocuments:
			FolderName = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
			break;
		case FolderType.ApplicationData:
			FolderName = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
			break;
		case FolderType.Desktop:
			FolderName = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
			break;
		}
		
		NodeInfo footR = FindNodeInfo("RightFoot");
		NodeInfo footL = FindNodeInfo("LeftFoot");
		if ((footR!=null) && (footL!=null))
		{
			if (footR.IsExist && footL.IsExist)
			{
				Vector3 front = Vector3.Cross(footL.Trans.position - footR.Trans.position,Vector3.up);
				front.y = 0f;
				invertPose = Quaternion.Inverse(Quaternion.LookRotation(front.normalized,Vector3.up));
			}
		}
		
		foreach(NodeInfo i in nodeInfos)
		{
			if (i.IsExist)
			{
				i.firstPosition = invertPose * i.Trans.position;
				i.firstRotation = invertPose * i.Trans.rotation;

				i.firstPosition -= transform.position;
			}
		}
		if (CaptureMode == Mode.FromStartToFinish)
		{
			CaptureStart();
		}
		
	}
	
	public void CaptureStart()
	{
		IsCapturing = true;
		captureStartPos = transform.position;
	}
	
	public void CaptureEnd()
	{
		if (enabled==false)
		{
			return;
		}
		if (IsCapturing==false)
		{
			return;
		}
		
		string fileName = "";
		NodeInfo rootInfo = GetRootInfo();
		if ((rootInfo!=null) && rootInfo.IsExist)
		{
			int fileNo = 1;
			do
			{
				string fn = FolderName+"/Take"+fileNo.ToString()+".bvh";
				if (File.Exists(fn)==false)
				{
					fileName = fn;
					break;
				}
				fileNo++;
			}while(true);
			
			rootInfo.Check();
			
			using(StreamWriter sw = new StreamWriter(fileName))
			{				
				// HIERARCHY
				sw.WriteLine("HIERARCHY");
				WriteChildren(rootInfo,sw,0);
				
				// MOTION
				sw.WriteLine("MOTION");
				// Frames
				sw.WriteLine("Frames:    " + rootInfo.pos.Count.ToString());
				// Frame Time:
				sw.WriteLine("Frame Time:    " + Time.fixedDeltaTime.ToString());
				// Values
				WriteValues(sw);
				
				sw.Close ();
			}
		}else{
			Debug.Log("Hips not exist!");
		}
		
		foreach(NodeInfo i in nodeInfos)
		{
			i.pos.Clear();
			i.rot.Clear();
		}
		
		IsCapturing = false;
	}
		
	bool keyState = false;
	protected void ModifyCaptureState()
	{
		if (CaptureMode == Mode.ShortcutKey)
		{
			bool s = Input.GetKeyDown(ShortcutKey);
			if (s && (keyState != s))
			{
				if (IsCapturing)
				{
					CaptureEnd();
				}else{
					CaptureStart();
				}
			}
			keyState = s;
		}
	}
	
	Rect rectButton = new Rect(50,50,300,60);
	public void OnGUI()
	{
		if (CaptureMode == Mode.RecordButton)
		{
			if (IsCapturing==false)
			{
				if (GUI.Button(rectButton,"Start Recording"))
				{
					CaptureStart();
				}
			}else{
				if (GUI.Button(rectButton,"End Recording"))
				{
					CaptureEnd();
				}
			}
		}
	}
	
}
