// #define ENABLE_DEBUG_POSE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//socket
//using Assets.Scripts;
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


namespace JustWithJoints
{
    public class CMUMotionPlayer : MonoBehaviour
    {
        public string DataPath = "Assets/FAvatarRetarget/Resources/sample.txt";
        public Core.CoordinateSystemType CoodinateSystem = Core.CoordinateSystemType.RightHanded;

        public float Timer = 0.0f;
        public float FPS = 120.0f;
		public int modelNum = 1;

        public bool Loop = true;
        public bool Play = true;

#if ENABLE_DEBUG_POSE
        public bool DebugFixedPoseMode = false;
        const float DebugFixedPoseScale = 0.5f;
        public Vector3[] DebugFixedPose = new Vector3[] {
            new Vector3(-1, 0, 0) * DebugFixedPoseScale,
            new Vector3(-1, 1, 0) * DebugFixedPoseScale,
            new Vector3(-1, 2, 0) * DebugFixedPoseScale,
            new Vector3(1, 2, 0) * DebugFixedPoseScale,
            new Vector3(1, 1, 0) * DebugFixedPoseScale,
            new Vector3(1, 0, 0) * DebugFixedPoseScale,
            new Vector3(-3, 4, 0) * DebugFixedPoseScale,
            new Vector3(-2, 4, 0) * DebugFixedPoseScale,
            new Vector3(-1, 4, 0) * DebugFixedPoseScale,
            new Vector3(1, 4, 0) * DebugFixedPoseScale,
            new Vector3(2, 4, 0) * DebugFixedPoseScale,
            new Vector3(3, 4, 0) * DebugFixedPoseScale,
            new Vector3(0, 4, 0) * DebugFixedPoseScale,
            new Vector3(0, 5, 0) * DebugFixedPoseScale,
        };
#endif
		private List<JustWithJoints.Core.Motion> motion_ = new List<JustWithJoints.Core.Motion>();
		private List<Vector3> hipPos = new List<Vector3>();

        private int frame_ = 0;
		bool isRecieve = false;
        private ModelTest cacheData ;

		[System.Serializable]
		public class ModelTest
		{
			public string frames;
			public string totalId;
			public string id;


			public string data;
			public string rtibia;
			public string rfemur;
			public string rhipjoint;
			public string lhipjoint;

			public string lfemur;
			public string ltibia;
			public string rwrist;
			public string rhumerus;
			public string rclavicle;

			public string lclavicle;
			public string lhumerus;
			public string lwrist;
			public string thorax;
			public string head;
		}




		public enum ADDRESSFAM
		{
		    IPv4, IPv6
		}
		public static string GetIP(ADDRESSFAM Addfam)
		{
			//Return null if ADDRESSFAM is Ipv6 but Os does not support it
			if (Addfam == ADDRESSFAM.IPv6 && !Socket.OSSupportsIPv6)
			{
			    return null;
			}

			string output = "";

			foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
			{
			#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			    NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
			    NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

			    if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) && item.OperationalStatus == OperationalStatus.Up)
			#endif 
			    {
				foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
				{
				    //IPv4
				    if (Addfam == ADDRESSFAM.IPv4)
				    {
					if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
					{
					    output = ip.Address.ToString();
						Debug.Log("ip : " + output);
					}
				    }

				    //IPv6
				    else if (Addfam == ADDRESSFAM.IPv6)
				    {
					if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
					{
					    output = ip.Address.ToString();
					}
				    }
				}
			    }
			}
			//Debug.Log("ip : " + output);
			return output;
		}




		//ip
		private TextMesh text;
		private ModelTest recvMessage;
		private List<ModelTest> play0MessageList = new List<ModelTest>();
		private List<ModelTest> play1MessageList = new List<ModelTest>();

		private byte[] messTmp;
		private int currentFrame =0 ;

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


		List<int> getIdInfm(ModelTest info)
		{

			List<int> lst = new List<int>();
			lst.Add(praseStr(info.totalId));
			lst.Add(praseStr(info.id));
 
			return lst;


		}
	

		List<string> concatInfo( ModelTest info,int debugframe)
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
			//Debug.LogError (info.id + ",info.frames,=" + info.frames + ",+info.head = " + info.head);
			return strList;

		}




        // Use this for initialization
        void Start()
        {
			//file
            //motion_ = CMUMotionLoader.Load(DataPath, CoodinateSystem);

			motion_.Add ( new Core.Motion () );
			motion_.Add ( new Core.Motion () );
			hipPos.Add (new Vector3(0,0,0));
			hipPos.Add (new Vector3(0,0,0));

			//recvMessage = new ModelTest ();
			messTmp = new byte[2048];

			// 构建一个Socket实例，并连接指定的服务端。这里需要使用IPEndPoint类(ip和端口号的封装)
			//client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


			try
			{
				//client.Connect(new IPEndPoint(IPAddress.Parse(host), port));
				client = new UdpClient(port);
				//client.Client.ReceiveTimeout = 5000;

				Debug.Log("connect success!!");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return;
			}

			//StartCoroutine (GetMessage());
			Thread socThread = new Thread(new ThreadStart(GetMessage));
			socThread.Start ();
        }



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
			/*
			var count = client.Receive(messTmp);

	

			if (count != 0)
			{
				
				//Debug.Log (Encoding.UTF8.GetString(messTmp, 1, count - 2));
	
				recvMessage = ReadToObject(Encoding.UTF8.GetString(messTmp, 1, count - 2));
				Array.Clear(messTmp, 0, count);
			}
			*/
			while(true){

				messTmp = client.Receive(ref remotePoint); 


				//recvMessage = ReadToObject(Encoding.UTF8.GetString(messTmp, 1, count - 2));
				//recvMessage = ReadToObject(Encoding.UTF8.GetString(messTmp, 1, messTmp.Length - 2));
				//Array.Clear (messTmp, 0, messTmp.Length);

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
		private int debugPlay0Frame;
		private int debugPlay1Frame;
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

				ModelTest message  = list [list.Count-1]; 
				motion_ [playId] = CMUMotionLoader.LoadSocketData (concatInfo ( message,frameId), frameId, CoodinateSystem);
				hipPos [playId] = calHipTrans ( message);
				list.Remove (message);
				if(list.Count > 100){
					list.RemoveRange(0,100);
				}

				return;
			}
		}

        // Update is called once per frame
        void Update()
		{
			//Debug.LogError ("play0MessageList = "+play0MessageList.Count);
			//Debug.LogError ("play0MessageList = "+play0MessageList.Count);

			UpdatePlayer (play0MessageList,0);
			UpdatePlayer (play1MessageList,1);
			 
			//List<int> idInfo = getIdInfm (recvMessage);

			//modelNum = idInfo [0];
			//if (idInfo [0] > motion_.Count ()) {
			//	for (int i = 0; i < (idInfo [0] - motion_.Count ()); i++) {

			//		motion_.Add (CMUMotionLoader.LoadSocketData (concatInfo (recvMessage), currentFrame, CoodinateSystem));
			//		hipPos.Add (calHipTrans ());

					//motion_.Add (new Core.Motion ());
					//hipPos.Add (new Vector3 (-1, 0, 0));
			//	}
			//}






			//motion_ [idInfo [1]] = CMUMotionLoader.LoadSocketData (concatInfo ( recvMessage), currentFrame, CoodinateSystem);
			//hipPos [idInfo [1]] = calHipTrans ( );
			 

			//Debug.Log (idInfo [1]);
			//Debug.Log ( recvMessage.rwrist);
			//Debug.Log ( motion_.ToString());


			//currentFrame++;


			if (Play) {
				Timer += Time.deltaTime;
			}

			frame_ = (int)(Timer * FPS);
			/*
			if (frame_ >= motion_[idInfo [1]].Poses.Count)
            {
                Timer = 0.0f;
            }
            */

  
        }

		public Core.Pose GetCurrentPose(int id)
        {
#if ENABLE_DEBUG_POSE
            if (DebugFixedPoseMode)
            {
                return new Core.Pose(0, DebugFixedPose.ToList());
            }
#endif
			if (frame_ < 0 || motion_[id].Poses.Count <= frame_)
            {
                return null;
            }
			var pose = motion_[id].Poses[frame_];
            return pose;
        }



		public Core.Pose GetSocketPose(int id)
		{
			if (id + 1 > motion_.Count ()) {
				return null;

			}
			if (motion_[id].Poses.Count <= 0)
			{
				return null;
			}
			var pose = motion_[id].Poses[motion_[id].Poses.Count - 1];
			//var pose = motion_[1].Poses[0];
			return pose;
		}


		public Vector3 calHipTrans(ModelTest message)
		{



			Vector3 lastPos = new Vector3(0,0,0);

			string tmpTrans = message.data;
		
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
					lastPos = new Vector3(x, y, z);
				}
			}


			return lastPos;
		}


		public Vector3 GetHipTrans(int id)
		{
			if (id + 1 > motion_.Count ()) {
				return  new Vector3(0,0,0);

			}

			if (motion_[id].Poses.Count <= 0)
			{
				return  new Vector3(0,0,0);
			}

			return hipPos[id];
		}

    }
}
