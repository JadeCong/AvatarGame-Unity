using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // for M2Mqtt
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using System.Runtime.Serialization;
using System.Threading;
using AvatarRetarget.Core; // for avatar retarget
using AvatarRetarget.Retarget;
using AvatarRetarget.Filter;
using MathematicUtility.Definition; // for data filter calculation

public class DriveAvatar3DWithPoses4 : MonoBehaviour
{
    // define the retargeting contents
    public bool ShowHumanSkeleton = true; // for skeleton
    public bool UseFinalIK = false; // for retarget
    public bool RetargetRootLocation = true;
    public bool RetargetPose = true;
    public bool UseLocalRotation = false;
    public bool UseFilter = true; // for filter
    public bool SetKalmanFilter = true;
    public static float[,] stateVector = new float[,] {{1.0f}, {1.0f}, {1.0f}};
    // public static float[,] estimateUncertainty = new float[,] {{10000f, 12000f, 20000f}, {12000f, 13000f, 22000f}, {20000f, 22000f, 40000f}};
    public static float[,] estimateUncertainty = new float[,] {{10000f, 2f, 3f}, {1f, 60000f, 3f}, {1f, 2f, 13000f}};
    public static float[,] KFParamF = new float[,] {{1.0f, 0.0f, 0.0f}, {0.0f, 1.0f, 0.0f}, {0.0f, 0.0f, 1.0f}};
    public static float[,] KFParamG = new float[,] {{1.0f, 0.0f, 0.0f}, {0.0f, 1.0f, 0.0f}, {0.0f, 0.0f, 1.0f}};
    public static float[,] KFParamu = new float[,] {{0.0f}, {0.0f}, {0.0f}};
    public static float[,] KFParamw = new float[,] {{0.03f}, {0.07f}, {0.04f}}; // set the different process noise variance Q in X,Y,Z coordinate axis(for debug)
    public static float[,] KFParamH = new float[,] {{1.0f, 0.0f, 0.0f}, {0.0f, 1.0f, 0.0f}, {0.0f, 0.0f, 1.0f}};
    public static float[,] KFParamv = new float[,] {{0.03f}, {0.08f}, {0.05f}}; // set the different process noise variance Q in X,Y,Z coordinate axis(for debug)
    public bool SetLowpassFilter = true;
    public Vector3 LowpassParamAlpha = new Vector3(0.9f, 0.9f, 0.9f); // set the different lowpass filter parameter in X,Y,Z coordinate axis(for debug)

    // define the joint keypoint transform class of human markers for driving avatar player
    [System.Serializable]
    public class HumanMarkers {
        public Transform headTop; // 9
        public Transform upperNeck; // 8
        public Transform leftShoulder; // 13
        public Transform rightShoulder; // 12
        public Transform leftElbow; // 14
        public Transform rightElbow; // 11
        public Transform leftWrist; // 15
        public Transform rightWrist; // 10
        public Transform thorax; // 7
        public Transform pelvis; // 6
        public Transform leftHip; // 3
        public Transform rightHip; // 2
        public Transform leftKnee; // 4
        public Transform rightKnee; // 1
        public Transform leftAnkle; // 5
        public Transform rightAnkle; // 0
    }
    public HumanMarkers humanMarkers;

    // define the human skeleton class
    public class HumanSkeleton {
        public GameObject SkeletonObject;
        public LineRenderer Skeleton;

        public Transform Start;
        public Transform End;
    }
    public List<HumanSkeleton> humanSkeletons = new List<HumanSkeleton>();

    // define the transform of avatar player keypoints for unity
    public class Transforms {
        public Transform root; // reference:self
        public Transform parent; // reference:self
        public int childCount; // reference:self

        public Vector3 position; // reference:world
        public Vector3 localPosition; // reference:parent
        public Vector3 eulerAngles; // reference:world(degree)
        public Vector3 localEulerAngles; // reference:parent(degree)
        public Quaternion rotation; // reference:world(quaternion)
        public Quaternion localRotation; // reference:parent(quaternion)
        public Vector3 lossyScale; // reference:world
        public Vector3 localScale; // reference:parent
        public Vector3 right; // reference:world(x)
        public Vector3 up; // reference:world(y)
        public Vector3 forward; // reference:world(z)

        public Matrix4x4 LocalToWorldMatrix; // reference:self
        public Matrix4x4 worldToLocalMatrix; // reference:world
    }

    // define the class of human keypoint transforms for driving the avatar
    public class HumanKeyPoints : Transforms {
        // public int frame;

        public Transforms rightAnkle;
        public Transforms rightKnee;
        public Transforms rightHip;
        public Transforms leftHip;
        public Transforms leftKnee;
        public Transforms leftAnkle;

        public Transforms pelvis;
        public Transforms thorax;
        public Transforms upperNeck;
        public Transforms headTop;

        public Transforms rightWrist;
        public Transforms rightElbow;
        public Transforms rightShoulder;
        public Transforms leftShoulder;
        public Transforms leftElbow;
        public Transforms leftWrist;
    }
    HumanKeyPoints humanKPs = new HumanKeyPoints(){
        // frame = 0,
        rightAnkle=new Transforms(), rightKnee=new Transforms(), rightHip=new Transforms(), leftHip=new Transforms(), leftKnee=new Transforms(), leftAnkle=new Transforms(), 
        pelvis=new Transforms(), thorax=new Transforms(), upperNeck=new Transforms(), headTop=new Transforms(), 
        rightWrist=new Transforms(), rightElbow=new Transforms(), rightShoulder=new Transforms(), leftShoulder=new Transforms(), leftElbow=new Transforms(), leftWrist=new Transforms()
    };

    // define the data format of human keypoints as string from the broker
    public class HumanPlayer {
        public string frame;
		public string totalId;
		public string id;

		public string rightAnkle;
		public string rightKnee;
		public string rightHip;
		public string leftHip;
		public string leftKnee;
		public string leftAnkle;

        public string pelvis;
        public string thorax;
		public string upperNeck;
        public string headTop;

		public string rightWrist;
		public string rightElbow;
		public string rightShoulder;
		public string leftShoulder;
		public string leftElbow;
		public string leftWrist;
    }
    private HumanPlayer cacheData;
    private List<HumanPlayer> playerMsgList = new List<HumanPlayer>();

    // define the data format of human keypoints as Vector3 from the broker
    List<Vector3> vectorHumanKPs = new List<Vector3>();

    // define the class of kalman filter and lowpass filter
    public static Matrixf initStateVector = new Matrixf(stateVector);
    public static Matrixf initEstimateUncertainty = new Matrixf(estimateUncertainty);
    public Matrixf F = new Matrixf(KFParamF);
    public Matrixf G = new Matrixf(KFParamG);
    public Matrixf u = new Matrixf(KFParamu);
    public Matrixf w = new Matrixf(KFParamw);
    public Matrixf H = new Matrixf(KFParamH);
    public Matrixf v = new Matrixf(KFParamv);
    // public KalmanFilterf[] kalmanFilters; // kalmanFilter
    // public LowpassFilter[] lowpassFilters; // lowpassFilter
    public List<KalmanFilterf> kalmanFilters = new List<KalmanFilterf>(); // kalmanFilter
    public List<LowpassFilter> lowpassFilters = new List<LowpassFilter>(); // lowpassFilter

    // define the settings of MQTT client for data transmission
    private MqttClient mqttClient;
    private string brokerAddress;
    private string clientID;
    private string topicSub;
    private string topicPub;
    private byte[] receivedMsgs;
    // TODO: whether define the broker port
    private int port = 1883;
    IPEndPoint brokerPort = new IPEndPoint(IPAddress.Any, 0);

    // define the bones, fixed bones, joints, bone lengths, T-Pose rotations and pose rotations of the avatar
    List<GameObject> avatarBones = new List<GameObject>();
    List<GameObject> avatarFixedBones = new List<GameObject>();
    List<GameObject> avatarJoints = new List<GameObject>();
    List<float> avatarBoneLengths = new List<float>();
    List<Quaternion> avatarTPoseRotations = new List<Quaternion>();
    List<Transforms> avatarPose = new List<Transforms>(){
        // 16 transform in total(first 15 rotation/localRotation for joint rotation control and 16 position for joint position control in order)
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
        new Transforms(),
    };

    // define the T-Pose class, T-Pose rotations and the normal pose class of the avatar(for joint rotation/position control)
    List<Quaternion> humanTPoseRotations = new List<Quaternion>();
    public Poses6 humanTPose = new Poses6(new List<Vector3>(), new List<float>()){
        Positions=new List<Vector3>(){
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),},
        AvatarBoneLengths=new List<float>(){
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,},
        HumanBoneVectors=new List<Vector3>(){
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),},
        HumanBoneLengths=new List<float>(){
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,},
        Rotations=new List<Quaternion>(){
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),},
        LocalRotations=new List<Quaternion>(){
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),},
        FKPositions=new List<Vector3>(){
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),},
    };
    public Poses6 humanPose = new Poses6(new List<Vector3>(), new List<float>()){
        Positions=new List<Vector3>(){
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),},
        AvatarBoneLengths=new List<float>(){
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,},
        HumanBoneVectors=new List<Vector3>(){
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),},
        HumanBoneLengths=new List<float>(){
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,},
        Rotations=new List<Quaternion>(){
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),},
        LocalRotations=new List<Quaternion>(){
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),
            new Quaternion(0f, 0f, 0f, 1f),},
        FKPositions=new List<Vector3>(){
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 0f),},
    };

    // ConnectBroker is called for creating the client and connecting the broker
    void ConnectBroker()
    {
        // initialize the settings of MQTT client for receiving messages
        // brokerAddress = "127.0.0.1"; // the address of the broker(localhost/remote IP)
        brokerAddress = "192.168.20.62";
        // brokerAddress = "PacificFuture-Broker"; // domain name of the broker
        topicSub = "/pacific/avatar/human_keypoints_3d";
        receivedMsgs = new byte[10240];

        // create the MQTT client instance
        mqttClient = new MqttClient(IPAddress.Parse(brokerAddress));
        Debug.Log("Creating the MQTT client......");

        // register to the events of connect/subscribe/publish/disconnect
        // mqttClient.MqttMsgConnected += MqttMsgConnected;
        // mqttClient.MqttMsgDisconnected += MqttMsgDisconnected;
        // mqttClient.MqttMsgSubscribed += MqttMsgSubscribed;
        // mqttClient.MqttMsgUnsubscribed += MqttMsgUnsubscribed;
        // mqttClient.MqttMsgSubscribeReceived += MqttMsgSubscribeReceived;
        // mqttClient.MqttMsgUnsubscribeReceived += MqttMsgUnsubscribeReceived;
        // mqttClient.MqttMsgPublished += MqttMsgPublished;
        mqttClient.MqttMsgPublishReceived += MqttMsgPublishReceived;

        // make the client ID and connect the broker
        clientID = Guid.NewGuid().ToString(); // or "Client_Unity"
        try
        {
            mqttClient.Connect(clientID);
            Debug.Log("Connecting the broker......");
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
            Debug.Log("Failed to connect the broker.");
            return;
        }

        // subscribe the specified topic from the broker
        mqttClient.Subscribe(new string[] {topicSub}, new byte[] {MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE});
        Debug.Log("Subscribing the topic from the broker......");
    }

    // MqttMsgPublishReceived is called for event of receiving messages from broker when topic published
    void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        // call the event for receiving messages
        Debug.Log("Calling the event for receiving messages from the broker......");
        byte[] msgs = new byte[2048];
        msgs = e.Message;
        ReceiveMessage(msgs);
    }

    // ReceiveMessage is called for receiving messages from the broker
    void ReceiveMessage(byte[] messages)
    {
        // receive the messages from the broker
        Debug.Log("Receiving the messages from the broker......");
        receivedMsgs = messages;
        // Debug.Log("receivedMsgs:" + Encoding.UTF8.GetString(receivedMsgs, 1, receivedMsgs.Length-2)); // for debug
        HumanPlayer humanPlayer = ParseMessage(Encoding.UTF8.GetString(receivedMsgs, 1, receivedMsgs.Length-2));
        playerMsgList.Add(humanPlayer);
        Array.Clear(receivedMsgs, 0, receivedMsgs.Length);
    }

    // ParseMessage is called for parsing messages from the broker
    HumanPlayer ParseMessage(string jsonMsgs)
    {
        // parse the jason format message to specified format
        Debug.Log("Parsing the messages received from the broker......");
        string[] startStr = jsonMsgs.Split(new string[] { "{" }, StringSplitOptions.None);
        if((startStr.Length<2)||(startStr[1] == ""))
			return cacheData;
		string[] endtStr = startStr[1].Split(new string[] { "}" }, StringSplitOptions.None);
		if((endtStr.Length<2)||(endtStr[0] == ""))
			return cacheData;

        string res = "{"+endtStr[0]+"}";
        HumanPlayer obj = JsonUtility.FromJson<HumanPlayer>(res) as HumanPlayer;
        cacheData = obj;

        return obj;
    }

    // MakeHumanKPsVector is called for converting the human keypoints string into the human keypoints vector
    List<Vector3> MakeHumanKPsVector(HumanPlayer stringMsgs)
    {
        // put the human keypoints into the vector in the order(HumanKeyPoints class)
        List<Vector3> vectorMsgs = new List<Vector3>();

        vectorMsgs.AddRange(new Vector3[] {
            string2Vector(stringMsgs.rightAnkle), // vectorHumanKPs[0]=rightAnkle
            string2Vector(stringMsgs.rightKnee), // vectorHumanKPs[1]=rightKnee
            string2Vector(stringMsgs.rightHip), // vectorHumanKPs[2]=rightHip
            string2Vector(stringMsgs.leftHip), // vectorHumanKPs[3]=leftHip
            string2Vector(stringMsgs.leftKnee), // vectorHumanKPs[4]=leftKnee
            string2Vector(stringMsgs.leftAnkle), // vectorHumanKPs[5]=leftAnkle

            string2Vector(stringMsgs.pelvis), // vectorHumanKPs[6]=pelvis
            string2Vector(stringMsgs.thorax), // vectorHumanKPs[7]=thorax
            string2Vector(stringMsgs.upperNeck), // vectorHumanKPs[8]=upperNeck
            string2Vector(stringMsgs.headTop), // vectorHumanKPs[9]=headTop

            string2Vector(stringMsgs.rightWrist), // vectorHumanKPs[10]=rightWrist
            string2Vector(stringMsgs.rightElbow), // vectorHumanKPs[11]=rightElbow
            string2Vector(stringMsgs.rightShoulder), // vectorHumanKPs[12]=rightShoulder
            string2Vector(stringMsgs.leftShoulder), // vectorHumanKPs[13]=leftShoulder
            string2Vector(stringMsgs.leftElbow), // vectorHumanKPs[14]=leftElbow
            string2Vector(stringMsgs.leftWrist), // vectorHumanKPs[15]=leftWrist
        });

        return vectorMsgs;
    }

    // string2Vector is called for transforming the string to vector
    Vector3 string2Vector(string tmpTrans)
	{
		Vector3 lastPos = new Vector3(0,0,0);

		string line = tmpTrans.Trim();
		var tokens = line.Split(' ').Select(t => t.Trim()).ToList();
		if(tokens.Count <= 0)
		{
			return lastPos;
		}
		if(tokens.Count == 4)
		{
			float x, y, z;
			if(float.TryParse(tokens[1], out x) && float.TryParse(tokens[2], out y) && float.TryParse(tokens[3], out z))
			{
				lastPos = new Vector3(x, y, z); // python端的输入顺序是xyz
			}
		}

		return lastPos;
	}

    // ConstructHumanSkeletons is called for constructing the humanSkeletons list
    void ConstructHumanSkeletons(HumanMarkers humanMrks)
    {
        // parse the HumanMarkers class to Transform list
        List<Transform> humanJointMarkers = new List<Transform>();
        humanJointMarkers.AddRange(new Transform[] {
            humanMrks.rightAnkle,
            humanMrks.rightKnee,
            humanMrks.rightHip,
            humanMrks.leftHip,
            humanMrks.leftKnee,
            humanMrks.leftAnkle,
            humanMrks.pelvis,
            humanMrks.thorax,
            humanMrks.upperNeck,
            humanMrks.headTop,
            humanMrks.rightWrist,
            humanMrks.rightElbow,
            humanMrks.rightShoulder,
            humanMrks.leftShoulder,
            humanMrks.leftElbow,
            humanMrks.leftWrist,
        });

        // define the skeleton links for showing human skeletons
        int[] skeletonJoints = new int[]{
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
        
        // construct the humanSkeletons list
        for(int i = 0; i < 15; i++)
        {
            int src = skeletonJoints[2 * i];
            int dst = skeletonJoints[2 * i + 1];
            AddSkeleton(humanJointMarkers[src], humanJointMarkers[dst]);
        }

        Debug.Log("Construct the human skeletons based on the joint keypoint markers successfully......");
    }

    // AddSkeleton is called for adding human skeleton to the human skeleton list
    void AddSkeleton(Transform startPos, Transform endPos)
    {
        // define the human bone skeleton
        var sk = new HumanSkeleton(){
            SkeletonObject = new GameObject("HumanSkeleton"),
            Start = startPos,
            End = endPos,
        };

        // add the LineRenderer object
        sk.Skeleton = sk.SkeletonObject.AddComponent<LineRenderer>();

        // configure the LineRenderer settings
        sk.Skeleton.positionCount = 2;
        sk.Skeleton.startWidth = 0.04f;
        sk.Skeleton.endWidth = 0.01f;
        sk.Skeleton.material.color = Color.green;

        // add the human bone skeleton into the human skeleton list
        humanSkeletons.Add(sk);
    }

    // GetAvatar is called for getting the bones and joints of avatar   
    void GetAvatar()
    {
        // get all the components of the avatar
        var avatarRoot = gameObject.transform.Find("BipDummy").gameObject;
        var pelvis = avatarRoot.transform.Find("Bip002 Pelvis").gameObject;
        var spine = pelvis.transform.Find("Bip002 Spine").gameObject;

        var leftThigh = spine.transform.Find("Bip002 L Thigh").gameObject;
        var leftCalf =  leftThigh.transform.Find("Bip002 L Calf").gameObject;
        var leftFoot =  leftCalf.transform.Find("Bip002 L Foot").gameObject;
        var leftToe0 = leftFoot.transform.Find("Bip002 L Toe0").gameObject;
        var leftToe1 = leftToe0.transform.Find("Bip002 L Toe01").gameObject;
        var leftToe2 = leftToe1.transform.Find("Bip002 L Toe02").gameObject;

        var rightThigh = spine.transform.Find("Bip002 R Thigh").gameObject;
        var rightCalf =  rightThigh.transform.Find("Bip002 R Calf").gameObject;
        var rightFoot =  rightCalf.transform.Find("Bip002 R Foot").gameObject;
        var rightToe0 = rightFoot.transform.Find("Bip002 R Toe0").gameObject;
        var rightToe1 = rightToe0.transform.Find("Bip002 R Toe01").gameObject;
        var rightToe2 = rightToe1.transform.Find("Bip002 R Toe02").gameObject;

        var spine1 = spine.transform.Find("Bip002 Spine1").gameObject;
        var spine2 = spine1.transform.Find("Bip002 Spine2").gameObject;
        var spine3 = spine2.transform.Find("Bip002 Spine3").gameObject;
        var neck = spine3.transform.Find("Bip002 Neck").gameObject;

        var head = neck.transform.Find("Bip002 Head").gameObject;
        
        var leftClavicle = neck.transform.Find("Bip002 L Clavicle").gameObject;
        var leftUpperArm = leftClavicle.transform.Find("Bip002 L UpperArm").gameObject;
        var leftForearm = leftUpperArm.transform.Find("Bip002 L Forearm").gameObject;
        var leftHand = leftForearm.transform.Find("Bip002 L Hand").gameObject;

        var leftFinger0 = leftHand.transform.Find("Bip002 L Finger0").gameObject;
        var leftFinger01 = leftFinger0.transform.Find("BipDummy L Finger01").gameObject;
        var leftFinger02 = leftFinger01.transform.Find("BipDummy L Finger02").gameObject;

        var leftFinger1 = leftHand.transform.Find("BipDummy L Finger1").gameObject;
        var leftFinger11 = leftFinger1.transform.Find("BipDummy L Finger11").gameObject;
        var leftFinger12 = leftFinger11.transform.Find("BipDummy L Finger12").gameObject;

        var leftFinger2 = leftHand.transform.Find("BipDummy L Finger2").gameObject;
        var leftFinger21 = leftFinger2.transform.Find("BipDummy L Finger21").gameObject;
        var leftFinger22 = leftFinger21.transform.Find("BipDummy L Finger22").gameObject;

        var leftFinger3 = leftHand.transform.Find("BipDummy L Finger3").gameObject;
        var leftFinger31 = leftFinger3.transform.Find("BipDummy L Finger31").gameObject;
        var leftFinger32 = leftFinger31.transform.Find("BipDummy L Finger32").gameObject;

        var leftFinger4 = leftHand.transform.Find("BipDummy L Finger4").gameObject;
        var leftFinger41 = leftFinger4.transform.Find("BipDummy L Finger41").gameObject;
        var leftFinger42 = leftFinger41.transform.Find("BipDummy L Finger42").gameObject;

        var rightClavicle = neck.transform.Find("Bip002 R Clavicle").gameObject;
        var rightUpperArm = rightClavicle.transform.Find("Bip002 R UpperArm").gameObject;
        var rightForearm = rightUpperArm.transform.Find("Bip002 R Forearm").gameObject;
        var rightHand = rightForearm.transform.Find("Bip002 R Hand").gameObject;

        var rightFinger0 = rightHand.transform.Find("Bip002 R Finger0").gameObject;
        var rightFinger01 = rightFinger0.transform.Find("BipDummy R Finger01").gameObject;
        var rightFinger02 = rightFinger01.transform.Find("BipDummy R Finger02").gameObject;

        var rightFinger1 = rightHand.transform.Find("BipDummy R Finger1").gameObject;
        var rightFinger11 = rightFinger1.transform.Find("BipDummy R Finger11").gameObject;
        var rightFinger12 = rightFinger11.transform.Find("BipDummy R Finger12").gameObject;

        var rightFinger2 = rightHand.transform.Find("BipDummy R Finger2").gameObject;
        var rightFinger21 = rightFinger2.transform.Find("BipDummy R Finger21").gameObject;
        var rightFinger22 = rightFinger21.transform.Find("BipDummy R Finger22").gameObject;

        var rightFinger3 = rightHand.transform.Find("BipDummy R Finger3").gameObject;
        var rightFinger31 = rightFinger3.transform.Find("BipDummy R Finger31").gameObject;
        var rightFinger32 = rightFinger31.transform.Find("BipDummy R Finger32").gameObject;

        var rightFinger4 = rightHand.transform.Find("BipDummy R Finger4").gameObject;
        var rightFinger41 = rightFinger4.transform.Find("BipDummy R Finger41").gameObject;
        var rightFinger42 = rightFinger41.transform.Find("BipDummy R Finger42").gameObject;

        // get the key bones of the avatar(15 bones in total)
        avatarBones.AddRange(new GameObject[] {
            pelvis, // 0(rightHipBone)
            rightThigh, // 1(rightThigh)
            rightCalf, // 2(rightCalf)
            
            pelvis, // 3(leftHipBone)
            leftThigh, // 4(leftThigh)
            leftCalf, // 5(leftCalf)

            pelvis, // 6(waist)
            spine2, // 7(chest)
            neck, // 8(neck)

            rightClavicle, // 9(rightClavicle)
            rightUpperArm, // 10(rightUpperArm)
            rightForearm, // 11(rightForearm)
            
            leftClavicle, // 12(leftClavicle)
            leftUpperArm, // 13(leftUpperArm)
            leftForearm, // 14(leftForearm)
        });

        // get the fixed bones of the avatar
        avatarFixedBones.AddRange(new GameObject[] {
            avatarRoot,
            spine,
            spine1,
            spine3,
            head,

            rightClavicle,
            rightHand,
            rightFinger0,
            rightFinger01,
            rightFinger02,
            rightFinger1,
            rightFinger11,
            rightFinger12,
            rightFinger2,
            rightFinger21,
            rightFinger22,
            rightFinger3,
            rightFinger31,
            rightFinger32,
            rightFinger4,
            rightFinger41,
            rightFinger42,

            leftClavicle,
            leftHand,
            leftFinger0,
            leftFinger01,
            leftFinger02,
            leftFinger1,
            leftFinger11,
            leftFinger12,
            leftFinger2,
            leftFinger21,
            leftFinger22,
            leftFinger3,
            leftFinger31,
            leftFinger32,
            leftFinger4,
            leftFinger41,
            leftFinger42,

            rightFoot,
            rightToe0,
            rightToe1,
            rightToe2,

            leftFoot,
            leftToe0,
            leftToe1,
            leftToe2,   
        });

        // get the key joint points of the avatar(16 kps in total)
        avatarJoints.AddRange(new GameObject[] {
            rightFoot, // 0(rightAnkle)
            rightCalf, // 1(rightKnee)
            rightThigh, // 2(rightHip)
            leftThigh, // 3(leftHip)
            leftCalf, // 4(leftKnee)
            leftFoot, // 5(leftAnkle)

            pelvis, // 6(pelvis)
            spine2, // 7(thorax)
            neck, // 8(upperNeck)
            head, // 9(headTop)

            rightHand, // 10(rightWrist)
            rightForearm, // 11(rightElbow)
            rightUpperArm, // 12(rightShoulder)
            leftUpperArm, // 13(leftShoulder)
            leftForearm, // 14(leftElbow)
            leftHand, // 15(leftWrist)
        });

        // get the key bone lengths of the avatar
        avatarBoneLengths.AddRange(new float[] {
            (rightThigh.transform.position - pelvis.transform.position).magnitude, // rightHipBone
            (rightCalf.transform.position - rightThigh.transform.position).magnitude, // rightThigh
            (rightFoot.transform.position - rightCalf.transform.position).magnitude, // rightCalf

            (leftThigh.transform.position - pelvis.transform.position).magnitude, // leftHipBone
            (leftCalf.transform.position - leftThigh.transform.position).magnitude, // leftThigh
            (leftFoot.transform.position - leftCalf.transform.position).magnitude, // leftCalf

            (spine2.transform.position - pelvis.transform.position).magnitude, // waist
            (neck.transform.position - spine2.transform.position).magnitude, // chest
            (head.transform.position - neck.transform.position).magnitude, // neck

            (rightUpperArm.transform.position - neck.transform.position).magnitude, // rightClavicle
            (rightForearm.transform.position - rightUpperArm.transform.position).magnitude, // rightUpperArm
            (rightHand.transform.position - rightForearm.transform.position).magnitude, // rightForearm

            (leftUpperArm.transform.position - neck.transform.position).magnitude, // leftClavicle
            (leftForearm.transform.position - leftUpperArm.transform.position).magnitude, // leftUpperArm
            (leftHand.transform.position - leftForearm.transform.position).magnitude, // leftForearm
        });

        Debug.Log("Got all the bones, fixedbones, joints and bone lengths of the avatar in unity successfully......");
    }

    // SetAvatarTPose is called for setting the avator to T-Pose
    void SetAvatarTPose(List<GameObject> bones)
    {
        // define the bones to be set zero local rotations for T-Pose
        int[] bonesNeedToBeSetZero = new int[]
        {
            // 1, // rightThigh
            2, // rightCalf

            // 4, // leftThigh
            5, // leftCalf

            7, // spine2
            8, // neck

            10, // rightUpperArm
            11, // rightForearm

            13, // leftUpperArm
            14, // leftForearm
        };

        // set the part of all avatar bones to the zero local rotations for T-Pose
        for(int i = 0; i < bonesNeedToBeSetZero.Length; i++)
        {
            bones[bonesNeedToBeSetZero[i]].transform.localRotation = new Quaternion(0f, 0f, 0f, 1f);
        }

        Debug.Log("Set the avatar to T-Pose successfully......");
    }

    // GetAvatarTPoseRotations is called for getting the initial T-Pose rotations(wold) of the avatar
    List<Quaternion> GetAvatarTPoseRotations(List<GameObject> bones)
    {
        // get the initial T-Pose rotations(wold) of all avatar bones(15 used according to human joint keypoints)
        List<Quaternion> avatarTPoseRots = new List<Quaternion>();

        for(int i = 0; i < bones.Count; i++)
        {
            avatarTPoseRots.Add(bones[i].transform.rotation);
        }

        Debug.Log("Got the initial T-Pose rotations of the avatar successfully......");
        return avatarTPoseRots;
    }

    // CheckHumanTPose is called for checking whether the human joint keypoints are set to T-Pose
    List<Quaternion> CheckHumanTPose()
    {
        // define the human T-Pose rotations, the human pose sample and the human T-Pose conditions
        List<Quaternion> humanTPoseRots = new List<Quaternion>();
        List<Vector3> humanPoseSample = new List<Vector3>();
        float torsoDiff, rightArmDiff, leftArmDiff, rightLegDiff, leftLegDiff, meanSquareDiff;
        float humanTPoseCheckDiff = 1.5f; // custom setting for human T-Pose check
        Matrixf measurements;
        float[,] meas = new float[3,1];

        // check whether the playerMsgList is empty
        if(playerMsgList.Count != 0)
        {
            // check whether the human joint keypoints are set to T-Pose
            while(true)
            {
                // get the human joint keypoints vector
                humanPoseSample = MakeHumanKPsVector(playerMsgList[playerMsgList.Count-1]);

                // filter the human joint keypoints using the kalman filter and lowpass filter if the option picked
                if(UseFilter)
                {
                    // for kalman filter if the option picked
                    if(SetKalmanFilter)
                    {
                        for(int i = 0; i < humanPoseSample.Count; i++)
                        {
                            // construct the Matrixf
                            meas[0,0] = humanPoseSample[i].x;
                            meas[1,0] = humanPoseSample[i].y;
                            meas[2,0] = humanPoseSample[i].z;
                            measurements = new Matrixf(meas);
                            // predict the state vector
                            kalmanFilters[i].KalmanCorrectf(measurements);
                            kalmanFilters[i].KalmanPredictf();
                            // assign the humanPoseSample with state vector
                            humanPoseSample[i].Set(kalmanFilters[i].xf.matrixf[0,0], kalmanFilters[i].xf.matrixf[1,0], kalmanFilters[i].xf.matrixf[2,0]); // update the humanPoseSample
                        }
                    }
                    
                    // for lowpass filter if the option picked
                    if(SetLowpassFilter)
                    {
                        for(int i = 0; i < humanPoseSample.Count; i++)
                        {
                            // humanPoseSample[i] = lowpassFilters[i].FirstOrderLowpassFilter(humanPoseSample[i], LowpassParamAlpha); // first order
                            humanPoseSample[i] = lowpassFilters[i].SecondOrderLowpassFilter(humanPoseSample[i], LowpassParamAlpha); // second order(for debug)
                            // humanPoseSample[i] = lowpassFilters[i].ThirdOrderLowpassFilter(humanPoseSample[i], LowpassParamAlpha); // third order(for debug)
                        }
                    }
                }

                // align the coordinate system from camera to Unity
                for(int idx = 0; idx < humanPoseSample.Count; idx++)
                {
                    humanPoseSample[idx] = AlignCoordinate(humanPoseSample[idx]);
                }

                // calculate the human T-Pose difference
                torsoDiff = Math.Abs(humanPoseSample[6].x - humanPoseSample[7].x) + Math.Abs(humanPoseSample[6].y - humanPoseSample[7].y) + 
                            Math.Abs(humanPoseSample[6].x - humanPoseSample[8].x) + Math.Abs(humanPoseSample[6].y - humanPoseSample[8].y);
                rightArmDiff = Math.Abs(humanPoseSample[8].y - humanPoseSample[12].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[12].z) + 
                               Math.Abs(humanPoseSample[8].y - humanPoseSample[11].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[11].z) + 
                               Math.Abs(humanPoseSample[8].y - humanPoseSample[10].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[10].z);
                leftArmDiff = Math.Abs(humanPoseSample[8].y - humanPoseSample[13].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[13].z) + 
                               Math.Abs(humanPoseSample[8].y - humanPoseSample[14].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[14].z) + 
                               Math.Abs(humanPoseSample[8].y - humanPoseSample[15].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[15].z);
                rightLegDiff = Math.Abs(humanPoseSample[6].y - humanPoseSample[2].y) + Math.Abs(humanPoseSample[6].z - humanPoseSample[2].z) + 
                               Math.Abs(humanPoseSample[2].x - humanPoseSample[1].x) + Math.Abs(humanPoseSample[2].z - humanPoseSample[1].z) + 
                               Math.Abs(humanPoseSample[2].x - humanPoseSample[0].x) + Math.Abs(humanPoseSample[2].z - humanPoseSample[0].z);
                leftLegDiff = Math.Abs(humanPoseSample[6].y - humanPoseSample[3].y) + Math.Abs(humanPoseSample[6].z - humanPoseSample[3].z) + 
                              Math.Abs(humanPoseSample[3].x - humanPoseSample[4].x) + Math.Abs(humanPoseSample[3].z - humanPoseSample[4].z) + 
                              Math.Abs(humanPoseSample[3].x - humanPoseSample[5].x) + Math.Abs(humanPoseSample[3].z - humanPoseSample[5].z);
                meanSquareDiff = (float)Math.Sqrt(Math.Pow(torsoDiff, 2.0) + Math.Pow(rightArmDiff, 2.0) + Math.Pow(leftArmDiff, 2.0) + Math.Pow(rightLegDiff, 2.0) + Math.Pow(leftLegDiff, 2.0));
                Debug.Log(meanSquareDiff);

                // get the initial T-Pose rotations(world) of the human
                if(meanSquareDiff < humanTPoseCheckDiff)
                {
                    Debug.Log("Bingo, the human joint keypoints match the T-Pose......");
                    humanTPoseRots = GetHumanTPoseRotations(humanPoseSample);
                    
                    break;
                }

                // clear the buffer of playerMsgList and humanPoseSample
                playerMsgList.RemoveAll(it => true);
                humanPoseSample.Clear();
            }

            Debug.Log("Got the initial T-Pose rotations of the human successfully......");
            return humanTPoseRots;
        }
        else
        {
            Debug.Log("Failed to receive the messages from the broker......");
            return null;
        }
    }

    // GetHumanTPoseRotations is called for getting the initial T-Pose rotations(world) of the human
    List<Quaternion> GetHumanTPoseRotations(List<Vector3> vectorHumanTPose)
    {
        // define the human T-Pose rotations
        List<Quaternion> humanTPoseRots = new List<Quaternion>();

        // construct the Poses6 class for calculating the human T-Pose rotations
        humanTPose = new Poses6(vectorHumanTPose, avatarBoneLengths);
        humanTPoseRots = humanTPose.Rotations;

        Debug.Log("Calculated the bone rotations of the human from the human joint keypoints successfully......");
        return humanTPoseRots;
    }

    // AlignCoordinate is called for aligning the Unity coordinate with camera(image) coordinate
    Vector3 AlignCoordinate(Vector3 coordinate)
    {
        // convert the camera coordinate(xyz)(right-hand) to the unity coordinate(xyz)(left-hand): mapping(x:x, z:y, y:z)
        Vector3 newCoordinate = new Vector3();

        newCoordinate[0] = coordinate[0];
        newCoordinate[1] = coordinate[2];
        newCoordinate[2] = coordinate[1];

        return newCoordinate;
    }

    // OffsetOrigin is called for offsetting the origin of human keypoints in camera to the origin in Unity
    Vector3 OffsetOrigin(Vector3 coordinate, float coordOriginX, float coordOriginY, float coordOriginZ) 
    {
        // define the return coordinate
        Vector3 newCoordinate = new Vector3();

        // calculate the new coordinate
        newCoordinate[0] = coordinate[0] - coordOriginX;
        newCoordinate[1] = coordinate[1] - coordOriginY;
        newCoordinate[2] = coordinate[2] - coordOriginZ;

        return newCoordinate;
    }

    // MappingScale is called for mapping the size of human to the size of avatar
    void MappingScale(List<Vector3> vecHumanKPs)
    {
        // get the torso length of avatar
        float avatarTorso = avatarBoneLengths[6] + avatarBoneLengths[7];
        Debug.Log("avatarTorso:" + avatarTorso);

        // get the torso length of human
        float humanTorso = Vector3.Distance(vecHumanKPs[6], vecHumanKPs[8]);
        Debug.Log("humanTorso:" + humanTorso);

        // get the mapping scale
        float mappingScale = avatarTorso / humanTorso;

        // mapping the scale to the avatar player
        for(int idx = 0; idx < vecHumanKPs.Count; idx++)
        {
            vecHumanKPs[idx] = vecHumanKPs[idx] * mappingScale;
        }
    }

    // RefreshHumanMarkers is called for calculating the positions of human joint markers
    void RefreshHumanMarkers(List<Vector3> markersPos, Vector3 offsetPos)
    {
        humanMarkers.rightAnkle.position = markersPos[0] + offsetPos;
        humanMarkers.rightKnee.position = markersPos[1] + offsetPos;
        humanMarkers.rightHip.position = markersPos[2] + offsetPos;
        humanMarkers.leftHip.position = markersPos[3] + offsetPos;
        humanMarkers.leftKnee.position = markersPos[4] + offsetPos;
        humanMarkers.leftAnkle.position = markersPos[5] + offsetPos;
        humanMarkers.pelvis.position = markersPos[6] + offsetPos;
        humanMarkers.thorax.position = markersPos[7] + offsetPos;
        humanMarkers.upperNeck.position = markersPos[8] + offsetPos;
        humanMarkers.headTop.position = markersPos[9] + offsetPos;
        humanMarkers.rightWrist.position = markersPos[10] + offsetPos;
        humanMarkers.rightElbow.position = markersPos[11] + offsetPos;
        humanMarkers.rightShoulder.position = markersPos[12] + offsetPos;
        humanMarkers.leftShoulder.position = markersPos[13] + offsetPos;
        humanMarkers.leftElbow.position = markersPos[14] + offsetPos;
        humanMarkers.leftWrist.position = markersPos[15] + offsetPos;
    }

    // RetargetTransform is called for retargeting the transfrom of avatar player from camera to unity
    // void RetargetTransform(HumanKeyPoints humanKps)
    void RetargetTransform()
    {
        // get the keypoints of existing player from the player message list
        HumanPlayer playerMsgs = playerMsgList[playerMsgList.Count-1]; // get the last data from the buffer
        Matrixf measurements;
        float[,] meas = new float[3,1];
        
        // get the keypoints vector from the keypoints string messages
        vectorHumanKPs = MakeHumanKPsVector(playerMsgs);

        // filter the human keypoints using the kalman filter and lowpass filter if the option picked
        if(UseFilter)
        {
            // for kalman filter if the option picked
            if(SetKalmanFilter)
            {
                for(int i = 0; i < vectorHumanKPs.Count; i++)
                {
                    // construct the Matrixf
                    meas[0,0] = vectorHumanKPs[i].x;
                    meas[1,0] = vectorHumanKPs[i].y;
                    meas[2,0] = vectorHumanKPs[i].z;
                    measurements = new Matrixf(meas);
                    // predict the state vector
                    kalmanFilters[i].KalmanCorrectf(measurements);
                    kalmanFilters[i].KalmanPredictf();
                    // assign the vectorHumanKPs with state vector
                    vectorHumanKPs[i].Set(kalmanFilters[i].xf.matrixf[0,0], kalmanFilters[i].xf.matrixf[1,0], kalmanFilters[i].xf.matrixf[2,0]); // update the vectorHumanKPs
                }
            }

            // for lowpass filter if the option picked
            if(SetLowpassFilter)
            {
                for(int i = 0; i < vectorHumanKPs.Count; i++)
                {
                    // vectorHumanKPs[i] = lowpassFilters[i].FirstOrderLowpassFilter(vectorHumanKPs[i], LowpassParamAlpha); // first order
                    vectorHumanKPs[i] = lowpassFilters[i].SecondOrderLowpassFilter(vectorHumanKPs[i], LowpassParamAlpha); // second order(for debug)
                    // vectorHumanKPs[i] = lowpassFilters[i].ThirdOrderLowpassFilter(vectorHumanKPs[i], LowpassParamAlpha); // third order(for debug)
                }
            }
        }
        
        // align the coordinate system from camera to Unity
        for(int idx = 0; idx < vectorHumanKPs.Count; idx++)
        {
            vectorHumanKPs[idx] = AlignCoordinate(vectorHumanKPs[idx]);
        }

        // offset the origin from camera to Unity
        // float originX, originY, originZ;
        // originX = vectorHumanKPs[6].x;
        // originZ = vectorHumanKPs[6].z;
        // if(vectorHumanKPs[0].y <= vectorHumanKPs[5].y)
        // {
        //     originY = vectorHumanKPs[0].y;
        // }
        // else
        // {
        //     originY = vectorHumanKPs[5].y;
        // }
        // for(int idx = 0; idx < vectorHumanKPs.Count; idx++)
        // {
        //     vectorHumanKPs[idx] = OffsetOrigin(vectorHumanKPs[idx], originX, originY, originZ);
        // }

        // mapping the size between human and avatar
        // MappingScale(vectorHumanKPs);

        // get the pose of the human(reference:world) for retargeting avatar's pose
        humanPose = new Poses6(vectorHumanKPs, avatarBoneLengths);

        // calculate the pose of the avatar based on bone rotations(reference:world/local)
        for(int i = 0; i < avatarBones.Count; i++)
        {
            avatarPose[i].rotation = humanPose.Rotations[i] * Quaternion.Inverse(humanTPoseRotations[i]) * avatarTPoseRotations[i]; // world reference
            avatarPose[i].localRotation = humanPose.LocalRotations[i]; // local reference
        }

        // calculate the pose of the avatar based on joint positions(reference:world)
        for(int i = 0; i < avatarJoints.Count; i++)
        {
            avatarPose[i].position = humanPose.FKPositions[i]; // world reference
        }
        
        // calculate the positions of human joint markers
        Vector3 offsets = new Vector3(-1.2f, 0f, 0f);
        RefreshHumanMarkers(humanPose.FKPositions, offsets);
    }

    // SetTransform is called for updating the transform to avatar player(for joint rotation/position control)
    // void SetTransform(HumanKeyPoints humanKps)
    void SetTransform()
    {
        // define the fixed bones(rotation control) and set joints(position control) for driving avatar
        // int[] fixedBones = new int[] {0, 3, 6, 7, 9, 12}; // for debug
        int[] fixedBones = new int[] {7};
        // int[] setJoints = new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15}; // for debug
        int[] setJoints = new int[] {0, 5, 6, 10, 15};

        // choose the method for driving the avatar(joint rotation/position control)
        if(UseFinalIK)
        {
            // update the pose(joint position) of the avatar using FinalIK
            for(int i = 0; i < avatarJoints.Count; i++)
            {
                // pick the setJoints to set
                if(setJoints.Contains(i))
                {
                    // Debug.Log("avatarPose[i].position: " + avatarPose[i].position); // for debug
                    avatarJoints[i].transform.position = avatarPose[i].position; // world reference
                }
            }
        }
        else
        {
            // update the pelvis(hip) position of the avatar if the option picked
            if(RetargetRootLocation)
            {
                // Debug.Log("avatarPose[6].position: " + avatarPose[6].position); // for debug
                avatarBones[6].transform.position = avatarPose[6].position; // 6 stand for pelvis
            }

            // update the pose(joint rotation) of the avatar if the option picked
            if(RetargetPose)
            {
                // check the rotation type(world/local)
                if(UseLocalRotation)
                {
                    for(int i = 0; i < avatarBones.Count; i++)
                    {
                        // pass the fixed bones
                        if(!fixedBones.Contains(i))
                        {
                            // Debug.Log("avatarPose[i].localRotation: " + avatarPose[i].localRotation); // for debug
                            avatarBones[i].transform.localRotation = avatarPose[i].localRotation; // local reference
                        }
                    }
                }
                else
                {
                    for(int i = 0; i < avatarBones.Count; i++)
                    {
                        // pass the fixed bones
                        if(!fixedBones.Contains(i))
                        {
                            // Debug.Log("avatarPose[i].rotation: " + avatarPose[i].rotation); // for debug
                            avatarBones[i].transform.rotation = avatarPose[i].rotation; // world reference
                        }
                    }
                }
            }
        }
    }
    
    // PlotHumanSkeletons is called for plotting the human skeletons
    void PlotHumanSkeletons()
    {
        // link the start and end for human skeleton
        foreach(var sk in humanSkeletons)
        {
            sk.Skeleton.SetPosition(0,sk.Start.position);
            sk.Skeleton.SetPosition(1,sk.End.position);
        }
    }

    // DrivePlayer is called for driving the avatar player
    void DrivePlayer()
    {
        // check whether the playerMsgList is empty
        if(playerMsgList.Count != 0) // playerMsgList.Count=0 while executed for the first cycle
        {
            // retarget the transform for avatar player
            // RetargetTransform(humanKPs);
            RetargetTransform();

            // set the transform for avatar player
            // SetTransform(humanKPs);
            SetTransform();

            // plot the human skeletons if the option picked
            if(ShowHumanSkeleton)
            {
                PlotHumanSkeletons();
            }
            
            // clean the buffer(received messages, update messages and humanPose)
            playerMsgList.RemoveAll(it => true);
            vectorHumanKPs.Clear();
            // humanPose.AllClear(); // for debug
            // humanKPs.Clear(); // for debug

            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // get the joints and bones of the avatar
        GetAvatar(); // for joint rotation/position control

        // set the avator to T-pose and get the initial T-pose rotations(world) of the avatar
        SetAvatarTPose(avatarBones);
        avatarTPoseRotations = GetAvatarTPoseRotations(avatarBones);

        // connect the borker for receiving messages
        ConnectBroker();

        // initialize the kalman and lowpass filters
        for(int i = 0; i < 16; i++)
        {
            // kalmanFilters.Append(new KalmanFilterf(3, 3, 3, initStateVector, initEstimateUncertainty));
            // lowpassFilters.Append(new LowpassFilter());
            kalmanFilters.Add(new KalmanFilterf(3, 3, 3, initStateVector, initEstimateUncertainty));
            lowpassFilters.Add(new LowpassFilter());
        }

        // assign the custom kalman filter
        for(int i = 0; i < 16; i++)
        {
            kalmanFilters[i].KalmanInitializef(F, G, u, w, H, v);
        }

        // check whether the human joint keypoints are set to T-Pose and get the initial T-Pose rotations(world) of the human
        Thread.Sleep(100); // delay for receiving data(for debug)
        humanTPoseRotations = CheckHumanTPose();
        // humanTPose.AllClear(); // for debug

        // construct the humanSkeletons list for showing the human skeletons
        ConstructHumanSkeletons(humanMarkers);

        // reassign the custom kalman filter for updating the unity frame(for debug)
        for(int i = 0; i < 16; i++)
        {
            kalmanFilters[i].xf = initStateVector;
            kalmanFilters[i].Pf = initEstimateUncertainty;
            kalmanFilters[i].KalmanInitializef(F, G, u, w, H, v);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // call DrivePlayer for driving the avatar player per frame
        DrivePlayer();
    }

    // FixedUpdate is called once per frame after Update with fixed FPS(0.02s)
    void FixedUpdate()
    {
        // DrivePlayer();
    }

    // LateUpdate is called once per frame after FixedUpdate
    void LateUpdate()
    {

    }
}
