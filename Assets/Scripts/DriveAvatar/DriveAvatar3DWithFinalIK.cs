﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// for M2Mqtt
using System;
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
// for avatar retarget
using AvatarRetarget.Core;
using AvatarRetarget.Retarget;


public class DriveAvatar3DWithFinalIK : MonoBehaviour
{
    // define the transform for driving avatar player and the script inspector
    public Transform pelvis;
    // public Transform leftHip;
    // public Transform rightHip;
    public Transform leftAnkle;
    public Transform rightAnkle;
    public Transform upperNeck;
    // public Transform leftShoulder;
    // public Transform rightShoulder;
    public Transform leftWrist;
    public Transform rightWrist;

    // [System.Serializable]
    // define the transform of avatar player keypoints for unity
    public class Transforms
    {
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
    public class HumanKeyPoints : Transforms
    {
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

    // [System.Serializable]
    // define the data format of human keypoints as string from the broker
    public class HumanPlayer
    {
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

    // define the bones, fixed bones, joints, bone lengths, rotations of the avatar and rotations, transform rotations of the human
    List<GameObject> avatarBones = new List<GameObject>();
    List<GameObject> avatarFixedBones = new List<GameObject>();
    List<GameObject> avatarJoints = new List<GameObject>();
    List<float> avatarBoneLengths = new List<float>();
    List<Quaternion> avatarTPoseRotations = new List<Quaternion>();
    List<Quaternion> humanTPoseRotations = new List<Quaternion>();
    List<Quaternion> humanToAvatarBoneTransformRotations = new List<Quaternion>();

    // define the pose of the avatar(for joint rotation/position control)
    // public Poses2 avatarPose = new Poses2(new List<Vector3>(), new List<Quaternion>());
    public Poses2 avatarPose = new Poses2(new List<Vector3>(), new List<Quaternion>(), new List<float>()){
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
        HumanToAvatarTransformRotations=new List<Quaternion>(){
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
    // define the T-Pose of the human(for joint rotation/position control)
    // public Poses2 humanTPose = new Poses2(new List<Vector3>(), new List<Quaternion>());
    public Poses2 humanTPose = new Poses2(new List<Vector3>(), new List<Quaternion>(), new List<float>()){
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
        HumanToAvatarTransformRotations=new List<Quaternion>(){
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
        receivedMsgs = new byte[2048];

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

        vectorMsgs.Add(string2Vector(stringMsgs.rightAnkle)); //vectorHumanKPs[0]=rightAnkle
        vectorMsgs.Add(string2Vector(stringMsgs.rightKnee)); //vectorHumanKPs[1]=rightKnee
        vectorMsgs.Add(string2Vector(stringMsgs.rightHip)); //vectorHumanKPs[2]=rightHip
        vectorMsgs.Add(string2Vector(stringMsgs.leftHip)); //vectorHumanKPs[3]=leftHip
        vectorMsgs.Add(string2Vector(stringMsgs.leftKnee)); //vectorHumanKPs[4]=leftKnee
        vectorMsgs.Add(string2Vector(stringMsgs.leftAnkle)); //vectorHumanKPs[5]=leftAnkle

        vectorMsgs.Add(string2Vector(stringMsgs.pelvis)); //vectorHumanKPs[6]=pelvis
        vectorMsgs.Add(string2Vector(stringMsgs.thorax)); //vectorHumanKPs[7]=thorax
        vectorMsgs.Add(string2Vector(stringMsgs.upperNeck)); //vectorHumanKPs[8]=upperNeck
        vectorMsgs.Add(string2Vector(stringMsgs.headTop)); //vectorHumanKPs[9]=headTop

        vectorMsgs.Add(string2Vector(stringMsgs.rightWrist)); //vectorHumanKPs[10]=rightWrist
        vectorMsgs.Add(string2Vector(stringMsgs.rightElbow)); //vectorHumanKPs[11]=rightElbow
        vectorMsgs.Add(string2Vector(stringMsgs.rightShoulder)); //vectorHumanKPs[12]=rightShoulder
        vectorMsgs.Add(string2Vector(stringMsgs.leftShoulder)); //vectorHumanKPs[13]=leftShoulder
        vectorMsgs.Add(string2Vector(stringMsgs.leftElbow)); //vectorHumanKPs[14]=leftElbow
        vectorMsgs.Add(string2Vector(stringMsgs.leftWrist)); //vectorHumanKPs[15]=leftWrist

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
				// python端的输入顺序是xyz
				lastPos = new Vector3(x, y, z);
			}
		}

		return lastPos;
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

        // get the key bones of the avatar
        avatarBones.AddRange(new GameObject[] {
            // TODO: need to be check the hip bone
            pelvis, // 0
            rightThigh,
            rightCalf,
            
            pelvis, // 3
            leftThigh,
            leftCalf,

            pelvis, // 6
            spine2, // 7
            neck,

            rightClavicle, // 9
            rightUpperArm,
            rightForearm,
            
            leftClavicle, // 12
            leftUpperArm,
            leftForearm,
        });

        // get the fixed bones of the avatar
        avatarFixedBones.AddRange(new GameObject[] {
            spine,
            spine1,
            spine3,
            rightClavicle,
            leftClavicle,

            rightFoot,
            leftFoot,
            rightHand,
            leftHand,
        });

        // get the key joints of the avatar
        avatarJoints.AddRange(new GameObject[] {
            rightFoot,
            rightCalf,
            rightThigh,
            leftThigh,
            leftCalf,
            leftFoot,

            pelvis,
            spine2,
            neck,
            head,

            rightHand,
            rightForearm,
            rightUpperArm,
            leftUpperArm,
            leftForearm,
            leftHand,
        });

        // get the key bone lengths of the avatar
        avatarBoneLengths.Add((rightThigh.transform.position - pelvis.transform.position).magnitude); // rightHipBone
        avatarBoneLengths.Add((rightCalf.transform.position - rightThigh.transform.position).magnitude); // rightThigh
        avatarBoneLengths.Add((rightFoot.transform.position - rightCalf.transform.position).magnitude); // rightCalf

        avatarBoneLengths.Add((leftThigh.transform.position - pelvis.transform.position).magnitude); // leftHipBone
        avatarBoneLengths.Add((leftCalf.transform.position - leftThigh.transform.position).magnitude); // leftThigh
        avatarBoneLengths.Add((leftFoot.transform.position - leftCalf.transform.position).magnitude); // leftCalf

        avatarBoneLengths.Add((spine2.transform.position - pelvis.transform.position).magnitude); // waist
        avatarBoneLengths.Add((neck.transform.position - spine2.transform.position).magnitude); // chest
        avatarBoneLengths.Add((head.transform.position - neck.transform.position).magnitude); // neck

        avatarBoneLengths.Add((rightUpperArm.transform.position - neck.transform.position).magnitude); // rightClavicle
        avatarBoneLengths.Add((rightForearm.transform.position - rightUpperArm.transform.position).magnitude); // rightUpperArm
        avatarBoneLengths.Add((rightHand.transform.position - rightForearm.transform.position).magnitude); // rightForearm

        avatarBoneLengths.Add((leftUpperArm.transform.position - neck.transform.position).magnitude); // leftClavicle
        avatarBoneLengths.Add((leftForearm.transform.position - leftUpperArm.transform.position).magnitude); // leftUpperArm
        avatarBoneLengths.Add((leftHand.transform.position - leftForearm.transform.position).magnitude); // leftForearm

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
        List<Quaternion> avatarTPose = new List<Quaternion>();

        for(int i = 0; i < bones.Count; i++)
        {
            avatarTPose.Add(bones[i].transform.rotation);
        }

        Debug.Log("Got the initial T-Pose rotations of the avatar successfully......");
        return avatarTPose;
    }

    // CheckHumanTPose is called for checking whether the human joint keypoints are set to T-Pose
    void CheckHumanTPose()
    {
        // define the human pose sample and the human T-Pose conditions
        List<Vector3> humanPoseSample = new List<Vector3>();
        float torsoDiff, rightArmDiff, leftArmDiff, rightLegDiff, leftLegDiff, meanSquareDiff;
        float humanTPoseCheckDiff = 1.5f; // custom setting for human T-Pose check

        // check whether the playerMsgList is empty
        if(playerMsgList.Count != 0)
        {
            // check whether the human joint keypoints are set to T-Pose
            while(true)
            {
                // get the human joint keypoints vector
                humanPoseSample = MakeHumanKPsVector(playerMsgList[playerMsgList.Count-1]);

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
                    humanTPoseRotations = GetHumanTPoseRotations(humanPoseSample);
                    
                    break;
                }

                // clear the buffer of playerMsgList and humanPoseSample
                playerMsgList.RemoveAll(it => true);
                humanPoseSample.Clear();
            }

            Debug.Log("Got the initial T-Pose rotations of the human successfully......");
            return;
        }
        else
        {
            Debug.Log("Failed to receive the messages from the broker......");
            return;
        }
    }

    // GetHumanTPoseRotations is called for getting the initial T-Pose rotations(world) of the human
    List<Quaternion> GetHumanTPoseRotations(List<Vector3> vectorHumanTPose)
    {
        // define the human T-Pose rotations
        List<Quaternion> humanTPoseRots = new List<Quaternion>();
        List<Quaternion> humanToAvatarTPoseTransformRots = new List<Quaternion>(); // no rotations for human pose

        for(int i =0; i < 15; i++)
        {
            humanToAvatarTPoseTransformRots.Add(new Quaternion(0f, 0f, 0f, 1f));
        }

        // construct the Poses2 class for calculating the human T-Pose rotations
        humanTPose = new Poses2(vectorHumanTPose, humanToAvatarTPoseTransformRots, avatarBoneLengths);
        humanTPoseRots = humanTPose.Rotations;

        Debug.Log("Calculated the bone rotations of the human from the human joint keypoints successfully......");
        return humanTPoseRots;
    }

    // GetHumanToAvatarBoneTransformRotations is called for getting all the bone transform rotations from humna pose to avatar pose
    List<Quaternion> GetHumanToAvatarBoneTransformRotations()
    {
        // define the bone transform rotations from human pose to avatar pose
        List<Quaternion> humanToAvatarBoneTransformRots = new List<Quaternion>();

        // TODO: make sure the Inverse on the left or the right
        // calculate the humanToAvatarBoneTransformRots(H->A = WC->A - WC->H)
        for(int i = 0; i < humanTPoseRotations.Count; i++)
        {
            humanToAvatarBoneTransformRots.Add(avatarTPoseRotations[i] * Quaternion.Inverse(humanTPoseRotations[i]));
            // humanToAvatarBoneTransformRots.Add(Quaternion.Inverse(humanTPoseRotations[i]) * avatarTPoseRotations[i]);
        }
        
        Debug.Log("Got all the bone transform rotations from human pose to avatar pose successfully......");
        return humanToAvatarBoneTransformRots;
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
    void MappingScale()
    {
        // get the torso length of avatar
        float avatarTorso = avatarBoneLengths[6] + avatarBoneLengths[7];
        Debug.Log("avatarTorso:" + avatarTorso);

        // get the torso length of human
        float humanTorso = Vector3.Distance(vectorHumanKPs[6], vectorHumanKPs[8]);
        Debug.Log("humanTorso:" + humanTorso);

        // get the mapping scale
        float mappingScale = avatarTorso / humanTorso;

        // mapping the scale to the avatar player
        for(int idx = 0; idx < vectorHumanKPs.Count; idx++)
        {
            vectorHumanKPs[idx] = vectorHumanKPs[idx] * mappingScale;
        }
    }

    // RetargetTransform is called for retargeting the transfrom of avatar player from camera to unity
    void RetargetTransform(HumanKeyPoints humanKps)
    {
        // get the keypoints of existing player from the player message list
        HumanPlayer playerMsgs = playerMsgList[playerMsgList.Count-1]; // get the last data
        
        // get the keypoints vector from the keypoints string messages
        vectorHumanKPs = MakeHumanKPsVector(playerMsgs);
        
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
        // MappingScale();

        // get the pose of the human(reference:world) for avatar's pose
        avatarPose = new Poses2(vectorHumanKPs, humanToAvatarBoneTransformRotations, avatarBoneLengths);

        // update the humanKPS with avatarPose data(FKPositions)
        humanKps.pelvis.position = avatarPose.FKPositions[6];
        // humanKps.leftHip.position = avatarPose.FKPositions[3];
        // humanKps.rightHip.position = avatarPose.FKPositions[2];
        humanKps.leftAnkle.position = avatarPose.FKPositions[5];
        humanKps.rightAnkle.position = avatarPose.FKPositions[0];

        humanKps.upperNeck.position = avatarPose.FKPositions[8];
        // humanKps.leftShoulder.position = avatarPose.FKPositions[13];
        // humanKps.rightShoulder.position = avatarPose.FKPositions[12];
        humanKps.leftWrist.position = avatarPose.FKPositions[15];
        humanKps.rightWrist.position = avatarPose.FKPositions[10];
    }

    // SetTransform is called for updating the transform to humanoid player(for joint position control)
    void SetTransform(HumanKeyPoints humanKps)
    {
        // update the lower limb(legs)
        pelvis.position = humanKps.pelvis.position;
        // leftHip.position = humanKps.leftHip.position;
        // rightHip.position = humanKps.rightHip.position;
        leftAnkle.position = humanKps.leftAnkle.position;
        rightAnkle.position = humanKps.rightAnkle.position;

        // update the upper limb(arms)
        upperNeck.position = humanKps.upperNeck.position;
        // leftShoulder.position = humanKps.leftShoulder.position;
        // rightShoulder.position = humanKps.rightShoulder.position;
        leftWrist.position = humanKps.leftWrist.position;
        rightWrist.position = humanKps.rightWrist.position;
    }

    // DrivePlayer is called for driving the avatar player
    void DrivePlayer()
    {
        // check whether the playerMsgList is empty
        if(playerMsgList.Count != 0) // playerMsgList.Count=0 while executed for the first cycle
        {
            // retarget the transform for avatar player
            RetargetTransform(humanKPs);

            // set the transform for avatar player
            SetTransform(humanKPs); // for joint position control

            // clean the buffer(received messages, update messages and avatarPose)
            playerMsgList.RemoveAll(it => true);
            vectorHumanKPs.Clear();
            // avatarPose.Clear(); // for debug
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

        // check whether the human joint keypoints are set to T-Pose and get the initial T-Pose rotations(world) of the human
        Thread.Sleep(100); // delay for receiving data(for debug)
        CheckHumanTPose();
        // humanTPose.Clear(); // for debug

        // get all the bone transform rotations from human initial T-Pose to avatar initial T-Pose
        humanToAvatarBoneTransformRotations = GetHumanToAvatarBoneTransformRotations();
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