using System.Collections;
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

public class DriveAvatar2D : MonoBehaviour
{
    // define the transform for driving avata player and the script inspector
    public Transform pelvis;
    public Transform leftHip;
    public Transform rightHip;
    public Transform leftAnkle;
    public Transform rightAnkle;

    public Transform leftShoulder;
    public Transform rightShoulder;
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
    HumanKeyPoints humanKPs = new HumanKeyPoints()
    {
        rightAnkle=new Transforms(), rightKnee=new Transforms(), rightHip=new Transforms(), leftHip=new Transforms(), leftKnee=new Transforms(), leftAnkle=new Transforms(), 
        pelvis=new Transforms(), thorax=new Transforms(), upperNeck=new Transforms(), headTop=new Transforms(), 
        rightWrist=new Transforms(), rightElbow=new Transforms(), rightShoulder=new Transforms(), leftShoulder=new Transforms(), leftElbow=new Transforms(), leftWrist=new Transforms()
    };

    // define the limb length of avatar player for mapping scale
    public class LimbLength
    {
        public float rightTibia;
        public float rightThigh;
        public float leftTibia;
        public float leftThigh;
        public float hip;

        public float waist;
        public float chest;

        public float rightUpperArm;
        public float rightForearm;
        public float leftUpperArm;
        public float leftForearm;
        public float shoulder;

        public float neck;
        public float head;
    }
    LimbLength limbLength = new LimbLength()
    {
        rightTibia=0.0f, rightThigh=0.0f, leftTibia=0.0f, leftThigh=0.0f, hip=0.0f, 
        waist=0.0f, chest=0.0f, 
        rightUpperArm=0.0f, rightForearm=0.0f, leftUpperArm=0.0f, leftForearm=0.0f, shoulder=0.0f, 
        neck=0.0f, head=0.0f
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

    // define the settings for client connetion with broker
    //TODO:whether define the broker port
    private int port = 10086;
    IPEndPoint brokerPort = new IPEndPoint(IPAddress.Any, 0);

    // define the settings of MQTT client for data transmission
    private MqttClient mqttClient;
    private string brokerAddress;
    private string clientID;
    private string topicSub;
    private string topicPub;
    private byte[] receivedMsgs;

    // ConnectBroker is called for creating the client and connecting the broker
    void ConnectBroker()
    {
        // initialize the settings of MQTT client for receiving messages
        // brokerAddress = "127.0.0.1"; // the address of the broker(localhost/remote IP)
        brokerAddress = "192.168.20.62";
        // brokerAddress = "PacificFuture-Broker"; // domain name of the broker
        topicSub = "/pacific/avatar/human_keypoints_2d";
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

    // AlignCoordinate is called for aligning the Unity coordinate with camera(image) coordinate
    Vector3 AlignCoordinate(Vector3 coordinate)
    {
        // convert the camera coordinate(uv) to the unity coordinate(xyz)
        Vector3 newCoordinate = new Vector3();

        newCoordinate[0] = 640-coordinate[0];
        newCoordinate[1] = 480-coordinate[1];
        newCoordinate[2] = coordinate[2];

        return newCoordinate;
    }

    // OffsetOrigin is called for offsetting the origin of human keypoints in camera to the origin in Unity
    Vector3 OffsetOrigin(Vector3 coordinate, float coordOriginX, float coordOriginY) 
    {
        // define the return coordinate
        Vector3 newCoordinate = new Vector3();

        // calculate the new coordinate
        newCoordinate[0] = coordinate[0] - coordOriginX;
        newCoordinate[1] = coordinate[1] - coordOriginY;
        newCoordinate[2] = coordinate[2];

        return newCoordinate;
    }

    // GetMappingScale is called for calculating mapping scale from camera to unity
    void GetMappingScale()
    {
        //TODO: get the mapping scale from the camera to unity
        // get the leg length of avatar player
        limbLength.rightTibia = GameObject.Find("Bip002 R Calf").transform.position.y - GameObject.Find("Bip002 R Foot").transform.position.y;
        limbLength.rightThigh = GameObject.Find("Bip002 R Thigh").transform.position.y - GameObject.Find("Bip002 R Calf").transform.position.y;
        limbLength.leftTibia = GameObject.Find("Bip002 L Calf").transform.position.y - GameObject.Find("Bip002 L Foot").transform.position.y;
        limbLength.leftThigh = GameObject.Find("Bip002 L Thigh").transform.position.y - GameObject.Find("Bip002 L Calf").transform.position.y;
        limbLength.hip = GameObject.Find("Bip002 R Thigh").transform.position.x - GameObject.Find("Bip002 L Thigh").transform.position.x;

        // get the limb length of avatar player
        limbLength.waist = GameObject.Find("Bip002 Spine1").transform.position.y - GameObject.Find("Bip002 Pelvis").transform.position.y;
        limbLength.chest = GameObject.Find("Bip002 Neck").transform.position.y - GameObject.Find("Bip002 Spine1").transform.position.y;

        // get the arm length of avatar player
        limbLength.rightUpperArm = GameObject.Find("Bip002 R UpperArm").transform.position.x - GameObject.Find("Bip002 R Forearm").transform.position.x;
        limbLength.rightForearm = GameObject.Find("Bip002 R Forearm").transform.position.x - GameObject.Find("Bip002 R Hand").transform.position.x;
        limbLength.leftUpperArm = GameObject.Find("Bip002 L UpperArm").transform.position.x - GameObject.Find("Bip002 L Forearm").transform.position.x;
        limbLength.leftForearm = GameObject.Find("Bip002 L Forearm").transform.position.x - GameObject.Find("Bip002 L Hand").transform.position.x;
        limbLength.shoulder = GameObject.Find("Bip002 R UpperArm").transform.position.x - GameObject.Find("Bip002 L UpperArm").transform.position.x;

        // get the neck and head length
        limbLength.neck = GameObject.Find("Bip002 Head").transform.position.y - GameObject.Find("Bip002 Neck").transform.position.y;
        limbLength.head = 0.2f;
    }

    // MappingScale is called for mapping the size of human to the size of avatar
    void MappingScale()
    {
        // get the limb length of avatar
        float avatarLimb = limbLength.waist + limbLength.chest;
        Debug.Log("avatarLimb:" + avatarLimb); // for debug

        // get the limb length of human
        float humanLimb = Vector3.Distance(vectorHumanKPs[6], vectorHumanKPs[8]);
        Debug.Log("humanLimb:" + humanLimb); // for debug

        // get the mapping scale
        float mappingScale = avatarLimb / humanLimb;

        // mapping the scale to the avatar player
        for(int idx=0; idx<vectorHumanKPs.Count; idx++)
        {
            vectorHumanKPs[idx] = vectorHumanKPs[idx] * mappingScale * 1.3f;
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
        for(int idx=0; idx<vectorHumanKPs.Count; idx++)
        {
            vectorHumanKPs[idx] = AlignCoordinate(vectorHumanKPs[idx]);
        }

        // offset the origin from camera to Unity
        float originX, originY;
        originX = vectorHumanKPs[6].x;
        if(vectorHumanKPs[0].y <= vectorHumanKPs[5].y)
        {
            originY = vectorHumanKPs[0].y;
        }
        else
        {
            originY = vectorHumanKPs[5].y;
        }
        for(int idx=0; idx<vectorHumanKPs.Count; idx++)
        {
            vectorHumanKPs[idx] = OffsetOrigin(vectorHumanKPs[idx], originX, originY);
        }

        // mapping the size between human and avatar
        MappingScale();

        // update the HumanKeyPoints class instance(humanKPs)
        humanKps.pelvis.position = vectorHumanKPs[6];
        humanKps.leftHip.position = vectorHumanKPs[3];
        humanKps.rightHip.position = vectorHumanKPs[2];
        humanKps.leftAnkle.position = vectorHumanKPs[5];
        humanKps.rightAnkle.position = vectorHumanKPs[0];

        humanKps.leftShoulder.position = vectorHumanKPs[13];
        humanKps.rightShoulder.position = vectorHumanKPs[12];
        humanKps.leftWrist.position = vectorHumanKPs[15];
        humanKps.rightWrist.position = vectorHumanKPs[10];
    }

    // SetTransfrom is called for updating the transform to humanoid player
    void SetTransfrom(HumanKeyPoints humanKps)
    {
        // update the lower limb(legs)
        pelvis.position = humanKps.pelvis.position;
        leftHip.position = humanKps.leftHip.position;
        rightHip.position = humanKps.rightHip.position;
        leftAnkle.position = humanKps.leftAnkle.position;
        rightAnkle.position = humanKps.rightAnkle.position;

        // update the upper limb(arms)
        leftShoulder.position = humanKps.leftShoulder.position;
        rightShoulder.position = humanKps.rightShoulder.position;
        leftWrist.position = humanKps.leftWrist.position;
        rightWrist.position = humanKps.rightWrist.position;
    }

    // DrivePlayer is called for driving the avatar player
    void DrivePlayer()
    {
        if(playerMsgList.Count != 0)
        {
            // retarget the transform for avatar player
            RetargetTransform(humanKPs);

            // set the transform for avatar player
            SetTransfrom(humanKPs);

            // clean the buffer(received messages and update messages)
            playerMsgList.RemoveAll(it => true);
            // humanKPs.Clear();

            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // connect the borker for receiving messages
        ConnectBroker();

        // get the mapping scale from camera to unity
        GetMappingScale();
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
