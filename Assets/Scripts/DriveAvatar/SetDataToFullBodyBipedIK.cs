using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// for socket
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Net.NetworkInformation;

public class SetDataToFullBodyBipedIK : MonoBehaviour
{
	// FullBodyBipedIK 脚本
    //public FullBodyBipedIK FullBodyBipedIK_Script;
	// Define the lower limb keypoints
    public Transform hip;
    public Transform leftAnkle;
    public Transform rightAnkle;
    //public Transform leftHand;
    //public Transform rightHand;
	//TODO: what;s the meaning of cacheData
	public ArrayList hipHight;
	private ModelTest cacheData;

	[System.Serializable]
	// Define the human keypoints from socket data stream
	public class ModelTest
	{
		public string frames;
		public string totalId;
		public string id;

		public string data;
		public string rtibia; //胫骨
		public string rfemur; //股骨
		public string rhipjoint; //髋关节
		public string lhipjoint;
		public string lfemur;

		public string ltibia;
		public string rwrist;
		public string rhumerus; //肱骨
		public string rclavicle; //锁骨
		public string lclavicle;

		public string lhumerus;
		public string lwrist;
		public string thorax; //胸部
		public string head;
	}

	// Socket network setting
	//TODO: what's the meaning of text
	private TextMesh text;
	private ModelTest recvMessage;
	private List<ModelTest> play0MessageList = new List<ModelTest>();
	private List<ModelTest> play1MessageList = new List<ModelTest>();

	private byte[] messTmp;
	private int currentFrame =0;

	//private Socket client;
	private string host = "127.0.0.1";
	//private string host = GetIP(ADDRESSFAM.IPv4);
	private int port = 10086;
	UdpClient client = null;
	IPEndPoint remotePoint = new IPEndPoint(IPAddress.Any, 0);

	int praseStr(string line)
	{
		int frame = 1;

		line = line.Trim();
		var tokens = line.Split(' ').Select(t => t.Trim()).ToList();
		if (tokens.Count <= 0)
		{
			print ("praseStr error! 0");
			return frame;
		}

		if (int.TryParse (tokens [1], out frame)) {
			return frame;
		} else {
			print ("praseStr error! 1");
			return frame;
		}
	}

	// Get the id in the frame
	List<int> getIdInfm(ModelTest info)
	{
		List<int> lst = new List<int>();
		lst.Add(praseStr(info.totalId));
		lst.Add(praseStr(info.id));

		return lst;
	}

	// Concat the info for robot model in the Unity
	List<string> concatInfo( ModelTest info, int debugframe)
	{
		List<string> strList = new List<string>();

		strList.Add(info.frames);
		strList.Add(info.rtibia);
		strList.Add(info.rfemur);
		strList.Add(info.rhipjoint);
		strList.Add(info.lhipjoint);
		strList.Add(info.lfemur);

		strList.Add(info.ltibia);
		strList.Add(info.rwrist);
		strList.Add(info.rhumerus);
		strList.Add(info.rclavicle);
		strList.Add(info.lclavicle);

		strList.Add(info.lhumerus);
		strList.Add(info.lwrist);
		strList.Add(info.thorax);
		strList.Add(info.head);
		//Debug.LogError (info.id + ",debugframe,=" + debugframe + ",+info.head = " + info.head);
		Debug.LogError (info.id + ",info.frames,=" + info.frames + ",+info.data = " + info.data);
		return strList;
	}

	// Set the data stream to the human robot model object
	ModelTest  ReadToObject(string json)
	{
		//DataObject deserializedUser = new DataObject();

		//split data to avoid Sticky bag ,the better way is concat data 
		string[] startStr = json.Split(new string[] { "{" }, StringSplitOptions.None);
		if ((startStr.Length<2)||(startStr[1] == ""))
			return cacheData;

		string[] endtStr = startStr[1].Split(new string[] { "}" }, StringSplitOptions.None);
		string res = "{"+endtStr[0]+"}";

		if ((endtStr.Length<2)||(endtStr[0] == ""))
			return cacheData;

		//Debug.Log("!!!!" + res);


		ModelTest obj = JsonUtility.FromJson<ModelTest>(res)as ModelTest;
		cacheData = obj;

		return obj;
	}

	void GetMessage()
	{
		while(true){

			messTmp = client.Receive(ref remotePoint); 

			ModelTest message = ReadToObject(Encoding.UTF8.GetString(messTmp, 1, messTmp.Length - 2));

			List<int> idInfo = getIdInfm (message);
			int playId = idInfo [1];
			if(playId == 0){ 
				play0MessageList.Add (message);
			}
			if(playId == 1){ 
				play1MessageList.Add (message);
			}
			Array.Clear (messTmp, 0, messTmp.Length);
		}
	}

	Vector3 string2Vector(string tmpTrans)
	{
		Vector3 lastPos = new Vector3(0,0,0);

		string line = tmpTrans.Trim();
		var tokens = line.Split(' ').Select(t => t.Trim()).ToList();
		if (tokens.Count <= 0)
		{
			return lastPos;
		}


		if (tokens.Count == 4)
		{
			float x, y, z;
			if (float.TryParse(tokens[1], out x) &&
				float.TryParse(tokens[2], out y) &&
				float.TryParse(tokens[3], out z))
			{
				// 注意 python端的输入顺序是 x z y
				lastPos = new Vector3(x, z, 0.0f);
				//Debug.Log("calHipTrans %f  %f  %f  "+x+"  "+y+"  "+z);
			}
		}

		return lastPos;
	}

	List<Vector3> calcIKInfomation(ModelTest message)
	{

		List<Vector3> vecList = new List<Vector3>();

		vecList.Add(string2Vector(message.data));
		//2d 输入是 位置在 3 6 才是左右脚
		vecList.Add(string2Vector(message.ltibia));
		vecList.Add(string2Vector(message.rhipjoint));

		vecList.Add(string2Vector(message.head));
		vecList.Add(string2Vector(message.thorax));


		return vecList;
	}

	public Vector3 calHipTrans(ModelTest message)
	{

		string tmpTrans = message.data;			
		return string2Vector(tmpTrans);
	}

	private int debugPlay0Frame;
	private int debugPlay1Frame;
	// Update the data to humanoid player
	private void UpdatePlayer(List<ModelTest> list,int playId){
		if(list.Count!=0){
			int frameId = 0;
			if(playId == 0){
				debugPlay0Frame++;
				frameId = debugPlay0Frame;
			}
			if(playId == 1){
				debugPlay1Frame++;
				frameId = debugPlay1Frame;
			}

			//get last data
			ModelTest message  = list [list.Count-1]; 
			//Debug.LogError (message.id + ",info.frames,=" + message.frames + ",+info.data = " + message.data);

			List<Vector3> praseInfo = calcIKInfomation(message);
			//hip.position = praseInfo[0];
			hip.position = new Vector3(praseInfo[0].x, praseInfo[0].y+0.68f, praseInfo[0].z); // modified 0.4 to 0.68(by Jade)

			rightAnkle.position = praseInfo[1];
			leftAnkle.position = praseInfo[2];

			
			//rightHand.position = praseInfo[3];
			//leftHand.position = praseInfo[4];

			list.Remove (message);
			if(list.Count > 100){
				list.RemoveRange(0,100);
			}

			return;
		}
	}

    // Start is called before the first frame update
    void Start()
    {	
		// Create the buffer for storing the data stream
		//recvMessage = new ModelTest ();
		messTmp = new byte[2048];

		// Connect the socket data stream from the Neural Network
		try
		{
			//client.Connect(new IPEndPoint(IPAddress.Parse(host), port));
			//client.Client.ReceiveTimeout = 5000;
			client = new UdpClient(port);
			Debug.Log("connect success!!");
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
			return;
		}

		// Create new thread for getting date stream
		//StartCoroutine (GetMessage());
		Thread socThread = new Thread(new ThreadStart(GetMessage));
		socThread.Start();

		// get the limb length of humanoid
		hipHight = GetLimbLength();
		
    }

	public ArrayList GetLimbLength()
	{
		ArrayList limblength = new ArrayList();

		limblength.Add((hip.position.y - leftAnkle.position.y));

		return limblength;
	}

    // Update is called once per frame
    // void Update()
    // {
    //     UpdatePlayer(play0MessageList,0);
    // }

	void FixedUpdate()
	{
		UpdatePlayer(play0MessageList,0); //modified the update to fixedupdate(by Jade)
	}
}
