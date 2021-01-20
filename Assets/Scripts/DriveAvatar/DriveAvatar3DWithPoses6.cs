using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // for M2Mqtt
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Security;
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
using RootMotion.FinalIK; // for configuring the Final IK

public class DriveAvatar3DWithPoses6 : MonoBehaviour
{
    // define the retargeting contents in inspector
    public string brokerIP = "127.0.0.1";
    public string subscribeTopic = "/pacific/avatar/human_keypoints_3d";
    public string publishTopic = "/pacific/avatar/avatar_pose_3d";
    public int liveHumanPoseFrame = 0;
    public int humanID = 0;
    public bool ShowHumanSkeleton = true; // for showing human skeleton
    public Vector3 OffsetsFromSkeletonToAvatar = new Vector3(-1.2f, 0.0f, 0.0f);
    [System.Serializable]
    public class HumanMarkers { // define the joint keypoint transform class of human markers for showing human markers
        public Transform headTop; // 10
        public Transform upperNeck; // 9
        public Transform thorax; // 8
        public Transform leftShoulder; // 14
        public Transform rightShoulder; // 13
        public Transform leftElbow; // 15
        public Transform rightElbow; // 12
        public Transform leftWrist; // 16
        public Transform rightWrist; // 11
        public Transform spine; // 7
        public Transform pelvis; // 6
        public Transform leftHip; // 3
        public Transform rightHip; // 2
        public Transform leftKnee; // 4
        public Transform rightKnee; // 1
        public Transform leftAnkle; // 5
        public Transform rightAnkle; // 0
    }
    public HumanMarkers humanMarkers; // for human markers
    public class HumanSkeleton { // define the human skeleton class for showing human skeletons
        public GameObject SkeletonObject;
        public LineRenderer Skeleton;
        public Transform Start;
        public Transform End;
    }
    public List<HumanSkeleton> humanSkeletons; // for human skeletons
    public bool UseFilter = true; // for filters
    public bool SetKalmanFilter = true; // for kalman filter
    public static float[,] stateVector = new float[,] {{1.0f}, {1.0f}, {1.0f}};
    public static float[,] estimateUncertainty = new float[,] {{10000f, 2f, 3f}, {1f, 60000f, 3f}, {1f, 2f, 13000f}};
    public static float[,] KFParamF = new float[,] {{1.0f, 0.0f, 0.0f}, {0.0f, 1.0f, 0.0f}, {0.0f, 0.0f, 1.0f}};
    public static float[,] KFParamG = new float[,] {{1.0f, 0.0f, 0.0f}, {0.0f, 1.0f, 0.0f}, {0.0f, 0.0f, 1.0f}};
    public static float[,] KFParamu = new float[,] {{0.0f}, {0.0f}, {0.0f}};
    public static float[,] KFParamw = new float[,] {{0.03f}, {0.07f}, {0.04f}}; // set the different process noise variance Q in X,Y,Z coordinate axis(for debug)
    public static float[,] KFParamH = new float[,] {{1.0f, 0.0f, 0.0f}, {0.0f, 1.0f, 0.0f}, {0.0f, 0.0f, 1.0f}};
    public static float[,] KFParamv = new float[,] {{0.03f}, {0.08f}, {0.05f}}; // set the different process noise variance Q in X,Y,Z coordinate axis(for debug)
    public bool SetLowpassFilter = true; // for lowpass filter
    public int LowpassFilterOrder = 2; // custom order: second order(higher order means more complex computation)
    public Vector3 LowpassParamAlpha = new Vector3(0.9f, 0.9f, 0.9f); // set the different lowpass filter parameter in X,Y,Z coordinate axis
    public bool UseFinalIK = false; // for Final IK retarget
    [System.Serializable]
    public class FinalIKMarkers { // define the joint keypoint transform class of Final IK markers for driving avatar player
        public Transform body;
        public Transform leftHand;
        public Transform leftShoulder;
        public Transform rightHand;
        public Transform rightShoulder;
        public Transform leftFoot;
        public Transform leftThigh;
        public Transform rightFoot;
        public Transform rightThigh;
    }
    public FinalIKMarkers finalIKMarkers; // for Final IK markers
    public bool UseAbsoluteCoordinate = true; // for absolute coordinate
    public bool UseLocalRotation = false; // for local rotation
    public bool RetargetRootLocation = true; // for bone rotation retarget
    public bool RetargetPose = true; 
    public bool UseForwardKinematics = true; // for forward kinematics
    public bool SetFootOnGround = true; // for setting avatar's foot on ground

    // define the settings of MQTT client for data transmission
    private MqttClient mqttClient;
    private string brokerAddress;
    private int brokerPort;
    private string clientID;
    private string userName;
    private string userPassword;
    private bool willRetain;
    private byte willQosLevel;
    private bool willFlag;
    private string willTopic;
    private string willMessage;
    private bool cleanSession;
    private ushort keepAlivePeriod;
    private string topicSub;
    private string topicPub;
    private byte[] receivedMsgs;

    // define the data format of human joint keypoints as string from the broker
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
        public string spine;
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
    public HumanPlayer cacheData;
    public List<HumanPlayer> playerMsgList;

    // define the transform of avatar player joint keypoints for unity
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

    // define the class of human joint keypoint transforms for driving the avatar
    public class HumanKeyPoints : Transforms {
        public int frame;

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
    HumanKeyPoints humanKPs = new HumanKeyPoints() {
        frame = 0,
        rightAnkle=new Transforms(), rightKnee=new Transforms(), rightHip=new Transforms(), leftHip=new Transforms(), leftKnee=new Transforms(), leftAnkle=new Transforms(), 
        pelvis=new Transforms(), thorax=new Transforms(), upperNeck=new Transforms(), headTop=new Transforms(), 
        rightWrist=new Transforms(), rightElbow=new Transforms(), rightShoulder=new Transforms(), leftShoulder=new Transforms(), leftElbow=new Transforms(), leftWrist=new Transforms()
    };

    // define the parameters for kalman filter and list of kalman and lowpass filter
    public Matrixf initStateVector; // kalmanFilter: initStateVector
    public Matrixf initEstimateUncertainty; // kalmanFilter: initEstimateUncertainty
    public Matrixf F;
    public Matrixf G;
    public Matrixf u;
    public Matrixf w;
    public Matrixf H;
    public Matrixf v;
    public List<KalmanFilterf> kalmanFilters; // kalmanFilter
    public List<LowpassFilter> lowpassFilters; // lowpassFilter

    // define the data format of human joint keypoints as int(frame) and Vector3 list(joint keypoints) from the broker
    private int humanPoseFrame; // for picked frame of human poses for driving avatar
    List<Vector3> humanKPsVector;
    // define the T-Pose rotations base reference and pose class of human for driving avatar(joint rotation/position control)
    List<Quaternion> humanTPoseRotations;
    public Poses8 humanPose;
    
    // define the bones, fixed bones, joints and bone lengths of avatar
    List<GameObject> avatarBones;
    List<GameObject> avatarFixedBones;
    List<GameObject> avatarJoints;
    List<float> avatarBoneLengths;
    // define the T-Pose rotations base reference and pose transforms list of avatar for driving avatar(joint rotation/position control)
    List<Quaternion> avatarTPoseRotations;
    public List<Transforms> avatarPose;

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

        // get the key bones of the avatar(16 bones in total)
        avatarBones.AddRange(new GameObject[] {
            pelvis, // 0(rightHipBone)
            rightThigh, // 1(rightThigh)
            rightCalf, // 2(rightCalf)
            
            pelvis, // 3(leftHipBone)
            leftThigh, // 4(leftThigh)
            leftCalf, // 5(leftCalf)

            pelvis, // 6(waist)
            spine1, // 7(chest)
            neck, // 8(neck)
            head, // 9(head)

            rightClavicle, // 10(rightClavicle)
            rightUpperArm, // 11(rightUpperArm)
            rightForearm, // 12(rightForearm)
            
            leftClavicle, // 13(leftClavicle)
            leftUpperArm, // 14(leftUpperArm)
            leftForearm, // 15(leftForearm)
        });

        // get the fixed bones of the avatar
        avatarFixedBones.AddRange(new GameObject[] {
            avatarRoot,
            spine,
            spine2,
            spine3,

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

        // get the key joint points of the avatar(17 kps in total)
        avatarJoints.AddRange(new GameObject[] {
            rightFoot, // 0(rightAnkle)
            rightCalf, // 1(rightKnee)
            rightThigh, // 2(rightHip)
            leftThigh, // 3(leftHip)
            leftCalf, // 4(leftKnee)
            leftFoot, // 5(leftAnkle)

            pelvis, // 6(pelvis)
            spine1, // 7(spine)
            neck, // 8(thorax)
            head, // 9(upperNeck)
            head, // 10(headTop)

            rightHand, // 11(rightWrist)
            rightForearm, // 12(rightElbow)
            rightUpperArm, // 13(rightShoulder)
            leftUpperArm, // 14(leftShoulder)
            leftForearm, // 15(leftElbow)
            leftHand, // 16(leftWrist)
        });

        // get the key bone lengths of the avatar
        avatarBoneLengths.AddRange(new float[] {
            (rightThigh.transform.position - pelvis.transform.position).magnitude, // rightHipBone # 0
            (rightCalf.transform.position - rightThigh.transform.position).magnitude, // rightThigh # 1
            (rightFoot.transform.position - rightCalf.transform.position).magnitude, // rightCalf # 2

            (leftThigh.transform.position - pelvis.transform.position).magnitude, // leftHipBone # 3
            (leftCalf.transform.position - leftThigh.transform.position).magnitude, // leftThigh # 4
            (leftFoot.transform.position - leftCalf.transform.position).magnitude, // leftCalf # 5

            (spine1.transform.position - pelvis.transform.position).magnitude, // waist # 6
            (neck.transform.position - spine1.transform.position).magnitude, // chest # 7
            (head.transform.position - neck.transform.position).magnitude, // neck # 8
            (head.transform.position - neck.transform.position).magnitude, // head(almost equal to neck) # 9

            (rightUpperArm.transform.position - neck.transform.position).magnitude, // rightClavicle # 10
            (rightForearm.transform.position - rightUpperArm.transform.position).magnitude, // rightUpperArm # 11
            (rightHand.transform.position - rightForearm.transform.position).magnitude, // rightForearm # 12

            (leftUpperArm.transform.position - neck.transform.position).magnitude, // leftClavicle # 13
            (leftForearm.transform.position - leftUpperArm.transform.position).magnitude, // leftUpperArm # 14
            (leftHand.transform.position - leftForearm.transform.position).magnitude, // leftForearm # 15
        });

        Debug.Log("Got all the bones, fixedbones, joints and bone lengths of the avatar in unity successfully......");
    }

    // SetAvatarTPose is called for setting the avator to T-Pose
    void SetAvatarTPose(List<GameObject> bones)
    {
        // define the bones to be set zero local rotations for T-Pose
        int[] bonesNeedToBeSetZero = new int[]
        {
            // 0, // rightHipBone(pelvis)
            // 1, // rightThigh
            2, // rightCalf

            // 3, // leftHipBone(pelvis)
            // 4, // leftThigh
            5, // leftCalf

            // 6, // waist(pelvis)
            7, // chest(spine1)
            8, // neck(neck)
            9, // head(head)

            // 10, // rightClavicle
            11, // rightUpperArm
            12, // rightForearm

            // 13, // leftClavicle
            14, // leftUpperArm
            15, // leftForearm
        };

        // set the part of all avatar bones to the zero local rotations for T-Pose
        foreach(var idx in bonesNeedToBeSetZero)
        {
            bones[idx].transform.localRotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
        }

        // set the pelvis, rightThigh, leftThigh, rightClavicle and leftClavicle to specific local rotations for T-Pose
        bones[0].transform.localRotation = new Quaternion(-0.5f, 0.5f, 0.5f, 0.5f); // pelvis
        bones[1].transform.localRotation = new Quaternion(0.0f, -1.0f, 0.0f, 0.0f); // rightThigh
        bones[4].transform.localRotation = new Quaternion(0.0f, -1.0f, 0.0f, 0.0f); // leftThigh
        bones[10].transform.localRotation = new Quaternion(-0.707f, 0.0f, -0.707f, 0.0f); // rightClavicle
        bones[13].transform.localRotation = new Quaternion(-0.707f, 0.0f, 0.707f, 0.0f); // leftClavicle

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
            humanMrks.spine,
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
                // 16 human bones in total and 6 means pelvis(hip)
                6, 2, // rightHipBone
                2, 1, // rightThigh
                1, 0, // rightCalf

                6, 3, // leftHipBone
                3, 4, // leftThigh
                4, 5, // leftCalf

                6, 7, // waist
                7, 8, // chest
                8, 9, // neck
                9, 10, // head
                
                8, 13, // rightClavicle
                13, 12, // rightUpperArm
                12, 11, // rightForearm

                8, 14, // leftClavicle
                14, 15, // leftUpperArm
                15, 16, // leftForearm
            };
        
        // construct the humanSkeletons list
        for(int i = 0; i < 16; i++)
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

    // ConnectBroker is called for creating the client and connecting the broker
    void ConnectBroker()
    {
        // initialize the settings of MQTT client for receiving messages
        brokerAddress = brokerIP; // the address of the broker(localhost/remote IP)
        // brokerAddress = "PacificFuture-Broker"; // domain name of the broker
        brokerPort = 1883;
        clientID = Guid.NewGuid().ToString(); // or "Client_Unity"
        userName = "Client_Unity";
        userPassword = "pacificfuture666";
        willRetain = false;
        willQosLevel = 1; // for debug
        willFlag = true;
        willTopic = subscribeTopic; // for debug
        willMessage = "OMG, Client_Unity goes offline.";
        cleanSession = true;
        keepAlivePeriod = 120;
        topicSub = subscribeTopic;
        topicPub = publishTopic;
        receivedMsgs = new byte[10240];
        playerMsgList = new List<HumanPlayer>();

        // create the MQTT client instance
        // mqttClient = new MqttClient(IPAddress.Parse(brokerAddress));
        mqttClient = new MqttClient(brokerAddress, brokerPort, false, null);
        Debug.Log("Creating the MQTT client......");

        // register to the events of connect/subscribe/unsubscribe/publish/disconnect
        // mqttClient.MqttMsgConnected += MqttMsgConnected;
        // mqttClient.MqttMsgDisconnected += MqttMsgDisconnected;
        // mqttClient.MqttMsgSubscribed += MqttMsgSubscribed;
        // mqttClient.MqttMsgUnsubscribed += MqttMsgUnsubscribed;
        // mqttClient.MqttMsgSubscribeReceived += MqttMsgSubscribeReceived;
        // mqttClient.MqttMsgUnsubscribeReceived += MqttMsgUnsubscribeReceived;
        // mqttClient.MqttMsgPublished += MqttMsgPublished;
        mqttClient.MqttMsgPublishReceived += MqttMsgPublishReceived;

        // connect the broker
        try
        {
            // mqttClient.Connect(clientID);
            mqttClient.Connect(clientID, userName, userPassword, willRetain, willQosLevel, willFlag, willTopic, willMessage, cleanSession, keepAlivePeriod);
            Debug.Log("Connecting the broker......");
        }
        catch(Exception error)
        {
            Console.WriteLine(error.Message);
            throw new ArgumentException("Failed to connect the broker.", nameof(mqttClient));
            return;
        }

        // subscribe the specified topic from the broker
        mqttClient.Subscribe(new string[] {topicSub}, new byte[] {MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE});
        Debug.Log("Subscribing the topic from the broker......");
    }

    // MqttMsgPublishReceived is called for event of receiving messages from broker when topic published
    void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs messageReceived)
    {
        // call the event for receiving messages
        // Debug.Log("Calling the event for receiving messages from the broker......"); // for debug
        byte[] msgs = new byte[5120];
        msgs = messageReceived.Message;
        ReceiveMessage(msgs);
    }

    // ReceiveMessage is called for receiving messages from the broker
    void ReceiveMessage(byte[] messages)
    {
        // receive the messages from the broker
        // Debug.Log("Receiving the messages from the broker......"); // for debug
        receivedMsgs = messages;
        // Debug.Log("receivedMsgs: " + Encoding.UTF8.GetString(receivedMsgs, 1, receivedMsgs.Length-2)); // for debug
        HumanPlayer humanPlayer = ParseMessage(Encoding.UTF8.GetString(receivedMsgs, 1, receivedMsgs.Length-2));
        playerMsgList.Add(humanPlayer);
        Array.Clear(receivedMsgs, 0, receivedMsgs.Length);
    }

    // ParseMessage is called for parsing messages from the broker
    HumanPlayer ParseMessage(string jsonMsgs)
    {
        // parse the jason format message to specified format
        // Debug.Log("Parsing the messages received from the broker......"); // for debug
        string[] subStr = jsonMsgs.Split(new string[] { "->" }, StringSplitOptions.None);
        if((subStr.Length < 2) || (subStr[1] == ""))
            return cacheData;
        string startStr = "\": \"id: ";
        string endStr = "\", ";
        string commonStr = subStr[0] + startStr + humanID.ToString() + endStr;
        
        string pickedStr = "";
        foreach(var sub in subStr)
        {
            if(sub[0].ToString() == humanID.ToString())
            {
                pickedStr = sub;
            }
        }
        string[] contentStr = pickedStr.Split(new string[] { "{", "}" }, StringSplitOptions.None);
        string res = commonStr + contentStr[1] + "}";

        HumanPlayer obj = JsonUtility.FromJson<HumanPlayer>(res) as HumanPlayer;
        cacheData = obj;
        
        return obj;
    }

    // EncodeHumanKPs is called for encodeing the HumanPlayer string class into int frame and human joint keypoints vectors
    (int, List<Vector3>) EncodeHumanKPs(HumanPlayer stringMsgs)
    {
        // get the picked frame of human joint keypoints
        int pickedFrame = string2Int(stringMsgs.frame);

        // convert the human joint keypoints into the vector in the order(HumanKeyPoints class)
        List<Vector3> humanKpsVec = new List<Vector3>();
        humanKpsVec.AddRange(new Vector3[] {
            string2Vector(stringMsgs.rightAnkle), // humanKPsVector[0]=rightAnkle
            string2Vector(stringMsgs.rightKnee), // humanKPsVector[1]=rightKnee
            string2Vector(stringMsgs.rightHip), // humanKPsVector[2]=rightHip
            string2Vector(stringMsgs.leftHip), // humanKPsVector[3]=leftHip
            string2Vector(stringMsgs.leftKnee), // humanKPsVector[4]=leftKnee
            string2Vector(stringMsgs.leftAnkle), // humanKPsVector[5]=leftAnkle

            string2Vector(stringMsgs.pelvis), // humanKPsVector[6]=pelvis
            string2Vector(stringMsgs.spine), // humanKPsVector[7]=spine
            string2Vector(stringMsgs.thorax), // humanKPsVector[8]=thorax
            string2Vector(stringMsgs.upperNeck), // humanKPsVector[9]=upperNeck
            string2Vector(stringMsgs.headTop), // humanKPsVector[10]=headTop

            string2Vector(stringMsgs.rightWrist), // humanKPsVector[11]=rightWrist
            string2Vector(stringMsgs.rightElbow), // humanKPsVector[12]=rightElbow
            string2Vector(stringMsgs.rightShoulder), // humanKPsVector[13]=rightShoulder
            string2Vector(stringMsgs.leftShoulder), // humanKPsVector[14]=leftShoulder
            string2Vector(stringMsgs.leftElbow), // humanKPsVector[15]=leftElbow
            string2Vector(stringMsgs.leftWrist), // humanKPsVector[16]=leftWrist
        });

        return (pickedFrame, humanKpsVec);
    }

    // string2Int is called for converting the string to int
    int string2Int(string tmpTrans)
    {
        int data = 0;

        string line = tmpTrans.Trim();
        var tokens = line.Split(' ').Select(t => t.Trim()).ToList();
        if(tokens.Count <= 0)
        {
            throw new ArgumentException("Empty frame or id data for HumanPlayer messages.", nameof(tokens));
			return data;
        }
        if(tokens.Count == 2)
        {
            data = int.Parse(tokens[1]);
        }
        
        return data;
    }

    // string2Vector is called for converting the string to vector
    Vector3 string2Vector(string tmpTrans)
	{
		Vector3 lastPos = new Vector3(0.0f, 0.0f, 0.0f);

		string line = tmpTrans.Trim();
		var tokens = line.Split(' ').Select(t => t.Trim()).ToList();
		if(tokens.Count <= 0)
		{
            throw new ArgumentException("Empty joint keypoint data for HumanPlayer messages.", nameof(tokens));
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
                (humanPoseFrame, humanPoseSample) = EncodeHumanKPs(playerMsgList[playerMsgList.Count-1]);
                liveHumanPoseFrame = humanPoseFrame;
                
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
                            if(LowpassFilterOrder == 1)
                            {
                                humanPoseSample[i] = lowpassFilters[i].FirstOrderLowpassFilter(humanPoseSample[i], LowpassParamAlpha); // first order
                            }
                            else if(LowpassFilterOrder == 2)
                            {
                                humanPoseSample[i] = lowpassFilters[i].SecondOrderLowpassFilter(humanPoseSample[i], LowpassParamAlpha); // second order
                            }
                            else if(LowpassFilterOrder == 3)
                            {
                                humanPoseSample[i] = lowpassFilters[i].ThirdOrderLowpassFilter(humanPoseSample[i], LowpassParamAlpha); // third order
                            }
                            else
                            {
                                throw new ArgumentException("Wrong lowpass filter order and please set it to int 1, 2 or 3 order.", nameof(LowpassFilterOrder));
                            }
                        }
                    }
                }
                
                // align the coordinate system from camera to Unity
                for(int idx = 0; idx < humanPoseSample.Count; idx++)
                {
                    humanPoseSample[idx] = AlignCoordinate(humanPoseSample[idx]);
                }
                
                // calculate the human T-Pose difference
                torsoDiff = Math.Abs(humanPoseSample[6].x - humanPoseSample[7].x) + Math.Abs(humanPoseSample[6].z - humanPoseSample[7].z) + 
                            Math.Abs(humanPoseSample[6].x - humanPoseSample[8].x) + Math.Abs(humanPoseSample[6].z - humanPoseSample[8].z) + 
                            Math.Abs(humanPoseSample[6].x - humanPoseSample[9].x) + Math.Abs(humanPoseSample[6].z - humanPoseSample[9].z) + 
                            Math.Abs(humanPoseSample[6].x - humanPoseSample[10].x);
                rightArmDiff = Math.Abs(humanPoseSample[8].y - humanPoseSample[13].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[13].z) + 
                               Math.Abs(humanPoseSample[8].y - humanPoseSample[12].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[12].z) + 
                               Math.Abs(humanPoseSample[8].y - humanPoseSample[11].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[11].z);
                leftArmDiff = Math.Abs(humanPoseSample[8].y - humanPoseSample[14].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[14].z) + 
                               Math.Abs(humanPoseSample[8].y - humanPoseSample[15].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[15].z) + 
                               Math.Abs(humanPoseSample[8].y - humanPoseSample[16].y) + Math.Abs(humanPoseSample[8].z - humanPoseSample[16].z);
                rightLegDiff = Math.Abs(humanPoseSample[6].y - humanPoseSample[2].y) + Math.Abs(humanPoseSample[6].z - humanPoseSample[2].z) + 
                               Math.Abs(humanPoseSample[2].x - humanPoseSample[1].x) + Math.Abs(humanPoseSample[2].z - humanPoseSample[1].z) + 
                               Math.Abs(humanPoseSample[2].x - humanPoseSample[0].x) + Math.Abs(humanPoseSample[2].z - humanPoseSample[0].z);
                leftLegDiff = Math.Abs(humanPoseSample[6].y - humanPoseSample[3].y) + Math.Abs(humanPoseSample[6].z - humanPoseSample[3].z) + 
                              Math.Abs(humanPoseSample[3].x - humanPoseSample[4].x) + Math.Abs(humanPoseSample[3].z - humanPoseSample[4].z) + 
                              Math.Abs(humanPoseSample[3].x - humanPoseSample[5].x) + Math.Abs(humanPoseSample[3].z - humanPoseSample[5].z);
                meanSquareDiff = (float)Math.Sqrt(Math.Pow(torsoDiff, 2.0) + Math.Pow(rightArmDiff, 2.0) + Math.Pow(leftArmDiff, 2.0) + Math.Pow(rightLegDiff, 2.0) + Math.Pow(leftLegDiff, 2.0));
                Debug.Log("Human T-Pose mean square difference: " + meanSquareDiff);

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
        
        // construct the Poses8 class for calculating the human T-Pose rotations
        Poses8 humanTPose = new Poses8(humanPoseFrame, vectorHumanTPose, avatarBoneLengths, UseAbsoluteCoordinate, UseLocalRotation, UseForwardKinematics, SetFootOnGround);
        humanTPoseRots = humanTPose.Rotations;
        
        Debug.Log("Calculated the bone rotations of the human from the human joint keypoints successfully......");
        return humanTPoseRots;
    }

    // DrivePlayer is called for driving the avatar player
    void DrivePlayer()
    {
        // check whether the playerMsgList is empty
        if(playerMsgList.Count != 0) // playerMsgList.Count=0 while executed for the first cycle
        {
            // retarget the transform for avatar player
            RetargetTransform();

            // set the transform for avatar player
            SetTransform();

            // plot the human skeletons if the option picked
            if(ShowHumanSkeleton)
            {
                PlotHumanSkeletons();
            }
            
            // clean the buffer(received messages, update messages and humanPose)
            playerMsgList.RemoveAll(it => true);
            humanKPsVector.Clear();
            // humanPose.AllClear(); // for debug

            return;
        }
    }

    // RetargetTransform is called for retargeting the transfrom of avatar player from camera to unity
    void RetargetTransform()
    {
        // get the keypoints of existing player from the player message list
        HumanPlayer playerMsgs = playerMsgList[playerMsgList.Count-1]; // get the last data from the buffer
        Matrixf measurements;
        float[,] meas = new float[3,1];
        
        // get the keypoints vector from the keypoints string messages
        (humanPoseFrame, humanKPsVector) = EncodeHumanKPs(playerMsgs);
        liveHumanPoseFrame = humanPoseFrame;

        // filter the human keypoints using the kalman filter and lowpass filter if the option picked
        if(UseFilter)
        {
            // for kalman filter if the option picked
            if(SetKalmanFilter)
            {
                for(int i = 0; i < humanKPsVector.Count; i++)
                {
                    // construct the Matrixf
                    meas[0,0] = humanKPsVector[i].x;
                    meas[1,0] = humanKPsVector[i].y;
                    meas[2,0] = humanKPsVector[i].z;
                    measurements = new Matrixf(meas);
                    // predict the state vector
                    kalmanFilters[i].KalmanCorrectf(measurements);
                    kalmanFilters[i].KalmanPredictf();
                    // assign the humanKPsVector with state vector
                    humanKPsVector[i].Set(kalmanFilters[i].xf.matrixf[0,0], kalmanFilters[i].xf.matrixf[1,0], kalmanFilters[i].xf.matrixf[2,0]); // update the humanKPsVector
                }
            }

            // for lowpass filter if the option picked
            if(SetLowpassFilter)
            {
                for(int j = 0; j < humanKPsVector.Count; j++)
                {
                    if(LowpassFilterOrder == 1)
                    {
                        humanKPsVector[j] = lowpassFilters[j].FirstOrderLowpassFilter(humanKPsVector[j], LowpassParamAlpha); // first order
                    }
                    else if(LowpassFilterOrder == 2)
                    {
                        humanKPsVector[j] = lowpassFilters[j].SecondOrderLowpassFilter(humanKPsVector[j], LowpassParamAlpha); // second order
                    }
                    else if(LowpassFilterOrder == 3)
                    {
                        humanKPsVector[j] = lowpassFilters[j].ThirdOrderLowpassFilter(humanKPsVector[j], LowpassParamAlpha); // third order
                    }
                    else
                    {
                        throw new ArgumentException("Wrong lowpass filter order and please set it to int 1, 2 or 3 order.", nameof(LowpassFilterOrder));
                    }
                }
            }
        }
        
        // align the coordinate system from camera to Unity
        for(int idx = 0; idx < humanKPsVector.Count; idx++)
        {
            humanKPsVector[idx] = AlignCoordinate(humanKPsVector[idx]);
        }

        // check whether using Final IK control and get ready for configuring the Final IK control
        if(UseFinalIK)
        {
            // configure the settings for Final IK control
            UseAbsoluteCoordinate = true; // cause the bone's rotation is not that accurate using relative coordinate system
            UseLocalRotation = false; // cause Final IK control for bone's rotation instead of localRotation
        }

        // get the pose of the human(reference:world) for retargeting avatar's pose
        humanPose = new Poses8(humanPoseFrame, humanKPsVector, avatarBoneLengths, UseAbsoluteCoordinate, UseLocalRotation, UseForwardKinematics, SetFootOnGround);

        // check whether using the absolute coordinate system for updating the bone rotations of avatar
        if(UseAbsoluteCoordinate)
        {
            // calculate the pose of the avatar based on bone rotations(reference:world/local) using absolute coordinate system
            for(int i = 0; i < avatarBones.Count; i++) // 16 rotations for avatarPose in avatarBones order
            {
                avatarPose[i].rotation = humanPose.Rotations[i]; // world reference
                avatarPose[i].localRotation = humanPose.LocalRotations[i]; // local reference
            }
        }
        else
        {
            // calculate the pose of the avatar based on bone rotations(reference:world/local) using relative coordinate system
            int[] localParentIndex = new int[] {6, 0, 1, 6, 3, 4, 6, 6, 7, 8, 8, 10, 11, 8, 13, 14};
            for(int i = 0; i < avatarBones.Count; i++) // 16 rotations for avatarPose in avatarBones order
            {
                avatarPose[i].rotation = humanPose.Rotations[i] * Quaternion.Inverse(humanTPoseRotations[i]) * avatarTPoseRotations[i]; // world reference
                // avatarPose[i].localRotation = humanPose.LocalRotations[i]; // local reference(for debug)
                avatarPose[i].localRotation = Quaternion.Inverse(avatarPose[localParentIndex[i]].rotation) * avatarPose[i].rotation; // local reference
            }
            // correct the pelvis localRotation(relative rotation of pelvis to the world coordinate system)
            avatarPose[6].localRotation = Quaternion.Inverse(new Quaternion(-0.5f, 0.5f, 0.5f, 0.5f)) * avatarPose[6].rotation; // local reference
        }
        
        // check whether updating the joint keypoint positions of avatar
        if(UseFinalIK || UseForwardKinematics || ShowHumanSkeleton)
        {
            // calculate the pose of the avatar based on joint positions(reference:world)
            for(int i = 0; i < avatarJoints.Count; i++) // 17 positions for avatarPose in avatarJoints order
            {
                avatarPose[i].position = humanPose.FKPositions[i]; // world reference
            }
        }
        
        // calculate the positions of human joint markers
        if(ShowHumanSkeleton)
        {
            RefreshHumanMarkers(humanPose.FKPositions, OffsetsFromSkeletonToAvatar, humanMarkers);
        }
    }

    // SetTransform is called for updating the transform to avatar player(for joint rotation/position control)
    void SetTransform()
    {
        // define the setJoints/setJointsParentIndex/finalIKMemberIndex(Final IK control) and fixedBones(bone rotation control) for driving avatar
        string[] finalIKMemberIndex = new string[] {"body", "leftHand", "leftShoulder", "rightHand", "rightShoulder", "leftFoot", "leftThigh", "rightFoot", "rightThigh"}; 
        int[] setJoints = new int[] {7, 16, 14, 11, 13, 5, 3, 0, 2}; // for Final IK control(in FinalIKMarkers order)
        int[] setJointsParentIndex = new int[] {6, 15, 13, 12, 10, 5, 3, 2, 0}; // for joint rotation parent index of Final IK control(in avatarBones and FinalIKMarkers order)
        int[] fixedBones = new int[] {7}; // for bone rotation control({0, 3, 6, 7, 9, 12} for debug)
        
        // choose the method for driving the avatar(bone rotation/Final IK control)
        if(UseFinalIK)
        {
            // set the Final IK plugin enabled
            // GameObject.Find("Dummy_1").GetComponent<FullBodyBipedIK>().enabled = true;
            GameObject.Find(gameObject.name).GetComponent<FullBodyBipedIK>().enabled = true;

            // update the pose(joint position/rotation) of the avatar using Final IK
            RefreshFinalIKMarkers(setJoints, setJointsParentIndex, avatarPose, finalIKMemberIndex, finalIKMarkers);
        }
        else
        {
            // set the Final IK plugin disabled
            // GameObject.Find("Dummy_1").GetComponent<FullBodyBipedIK>().enabled = false;
            GameObject.Find(gameObject.name).GetComponent<FullBodyBipedIK>().enabled = false;

            // update the pelvis(hip) position of the avatar if the option picked
            if(RetargetRootLocation)
            {
                // Debug.Log("avatarPose[6].position: " + avatarPose[6].position);
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
                            // Debug.Log("avatarPose[i].localRotation: " + avatarPose[i].localRotation);
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
                            // Debug.Log("avatarPose[i].rotation: " + avatarPose[i].rotation);
                            avatarBones[i].transform.rotation = avatarPose[i].rotation; // world reference
                        }
                    }
                }
            }
        }
    }

    // RefreshFinalIKMarkers is called for calculating the position and rotation of Final IK markers
    void RefreshFinalIKMarkers(int[] jointsIndex, int[] parentsIndex, List<Transforms> avatarData, string[] markersMemberIndex, FinalIKMarkers finalIKMrks)
    {
        // define and initialize the dictionary for storing temp Final IK markers
        Dictionary<string, Transforms> tempMarkers = new Dictionary<string, Transforms>();
        foreach(var index in markersMemberIndex)
        {
            tempMarkers.Add(index, new Transforms());
        }
        
        // calculating the position and rotation of the Final IK markers
        foreach(var key in tempMarkers.Keys)
        {
            // set the position
            tempMarkers[key].position = avatarData[jointsIndex[Array.IndexOf(markersMemberIndex, key)]].position; // world reference

            // set the rotation
            if(key == "rightFoot" || key == "leftFoot" || key == "body") // for parent of 2/5/6
            {
                tempMarkers[key].rotation = avatarPose[parentsIndex[Array.IndexOf(markersMemberIndex, key)]].rotation * new Quaternion(0.0f, 0.0f, 0.0f, 1.0f); // world reference
            }
            else if(key == "rightHand") // for parent of 12
            {
                tempMarkers[key].rotation = avatarPose[parentsIndex[Array.IndexOf(markersMemberIndex, key)]].rotation * new Quaternion(0.707f, 0.0f, 0.0f, 0.707f); // world reference
            }
            else if(key == "leftHand") // for parent of 15
            {
                tempMarkers[key].rotation = avatarPose[parentsIndex[Array.IndexOf(markersMemberIndex, key)]].rotation * new Quaternion(-0.707f, 0.0f, 0.0f, 0.707f); // world reference
            }
            else if(key == "rightThigh") // for parent of 0(for debug)
            {
                tempMarkers[key].rotation = avatarPose[parentsIndex[Array.IndexOf(markersMemberIndex, key)]].rotation * new Quaternion(0.0f, 0.0f, 0.0f, 1.0f); // world reference
            }
            else if(key == "leftThigh") // for parent of 3(for debug)
            {
                tempMarkers[key].rotation = avatarPose[parentsIndex[Array.IndexOf(markersMemberIndex, key)]].rotation * new Quaternion(0.0f, 0.0f, 0.0f, 1.0f); // world reference
            }
            else if(key == "rightShoulder") // for parent of 10(for debug)
            {
                tempMarkers[key].rotation = avatarPose[parentsIndex[Array.IndexOf(markersMemberIndex, key)]].rotation * new Quaternion(0.0f, 0.0f, 0.0f, 1.0f); // world reference
            }
            else if(key == "leftShoulder") // for parent of 13(for debug)
            {
                tempMarkers[key].rotation = avatarPose[parentsIndex[Array.IndexOf(markersMemberIndex, key)]].rotation * new Quaternion(0.0f, 0.0f, 0.0f, 1.0f); // world reference
            }
        }

        // assign the values of tempMarkers to finalIKMrks
        finalIKMrks.body.position = tempMarkers["body"].position; // 7(spine)
        finalIKMrks.body.rotation = tempMarkers["body"].rotation;
        finalIKMrks.leftHand.position = tempMarkers["leftHand"].position; // 16(leftWrist)
        finalIKMrks.leftHand.rotation = tempMarkers["leftHand"].rotation;
        finalIKMrks.leftShoulder.position = tempMarkers["leftShoulder"].position; // 14(leftShoulder)
        finalIKMrks.leftShoulder.rotation = tempMarkers["leftShoulder"].rotation;
        finalIKMrks.rightHand.position = tempMarkers["rightHand"].position; // 11(rightWrist)
        finalIKMrks.rightHand.rotation = tempMarkers["rightHand"].rotation;
        finalIKMrks.rightShoulder.position = tempMarkers["rightShoulder"].position; // 13(rightShoulder)
        finalIKMrks.rightShoulder.rotation = tempMarkers["rightShoulder"].rotation;
        finalIKMrks.leftFoot.position = tempMarkers["leftFoot"].position; // 5(leftAnkle)
        finalIKMrks.leftFoot.rotation = tempMarkers["leftFoot"].rotation;
        finalIKMrks.leftThigh.position = tempMarkers["leftThigh"].position; // 3(leftHip)
        finalIKMrks.leftThigh.rotation = tempMarkers["leftThigh"].rotation;
        finalIKMrks.rightFoot.position = tempMarkers["rightFoot"].position; // 0(rightAnkle)
        finalIKMrks.rightFoot.rotation = tempMarkers["rightFoot"].rotation;
        finalIKMrks.rightThigh.position = tempMarkers["rightThigh"].position; // 2(rightHip)
        finalIKMrks.rightThigh.rotation = tempMarkers["rightThigh"].rotation;
    }

    // RefreshHumanMarkers is called for calculating the positions of human joint markers
    void RefreshHumanMarkers(List<Vector3> markersPos, Vector3 offsetPos, HumanMarkers humanMrks)
    {
        // calculating the positions of human joint markers
        humanMrks.rightAnkle.position = markersPos[0] + offsetPos;
        humanMrks.rightKnee.position = markersPos[1] + offsetPos;
        humanMrks.rightHip.position = markersPos[2] + offsetPos;
        humanMrks.leftHip.position = markersPos[3] + offsetPos;
        humanMrks.leftKnee.position = markersPos[4] + offsetPos;
        humanMrks.leftAnkle.position = markersPos[5] + offsetPos;
        humanMrks.pelvis.position = markersPos[6] + offsetPos;
        humanMrks.spine.position = markersPos[7] + offsetPos;
        humanMrks.thorax.position = markersPos[8] + offsetPos;
        humanMrks.upperNeck.position = markersPos[9] + offsetPos;
        humanMrks.headTop.position = markersPos[10] + offsetPos;
        humanMrks.rightWrist.position = markersPos[11] + offsetPos;
        humanMrks.rightElbow.position = markersPos[12] + offsetPos;
        humanMrks.rightShoulder.position = markersPos[13] + offsetPos;
        humanMrks.leftShoulder.position = markersPos[14] + offsetPos;
        humanMrks.leftElbow.position = markersPos[15] + offsetPos;
        humanMrks.leftWrist.position = markersPos[16] + offsetPos;
    }
    
    // PlotHumanSkeletons is called for plotting the human skeletons
    void PlotHumanSkeletons()
    {
        // link the start and end for human skeleton
        foreach(var sk in humanSkeletons)
        {
            sk.Skeleton.SetPosition(0, sk.Start.position);
            sk.Skeleton.SetPosition(1, sk.End.position);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // initialize the human skeleton list for showing human skeletons
        humanSkeletons = new List<HumanSkeleton>();

        // initialize the parameters for kalman filter and list of kalman and lowpass filter
        initStateVector = new Matrixf(stateVector);
        initEstimateUncertainty = new Matrixf(estimateUncertainty);
        F = new Matrixf(KFParamF);
        G = new Matrixf(KFParamG);
        u = new Matrixf(KFParamu);
        w = new Matrixf(KFParamw);
        H = new Matrixf(KFParamH);
        v = new Matrixf(KFParamv);
        kalmanFilters = new List<KalmanFilterf>(); // kalmanFilter
        lowpassFilters = new List<LowpassFilter>(); // lowpassFilter

        // initialize the frame and joint keypoints vector list of human pose
        humanPoseFrame = 0;
        humanKPsVector = new List<Vector3>();
        // initialize the T-Pose rotations base reference of human
        humanTPoseRotations = new List<Quaternion>();

        // initialize the bones, fixed bones, joints and bone lengths of avatar
        avatarBones = new List<GameObject>();
        avatarFixedBones = new List<GameObject>();
        avatarJoints = new List<GameObject>();
        avatarBoneLengths = new List<float>();
        // initialize the T-Pose rotations base reference and pose transforms list of avatar
        avatarTPoseRotations = new List<Quaternion>();
        avatarPose = new List<Transforms>();
        for(int i = 0; i < 17; i++)
        {
            // 17 transforms in total(first 16 rotation/localRotation for joint rotation control in avatarBones order and 16 position for joint position control in avatarJoints order)
            avatarPose.Add(new Transforms()); 
        }

        // get the joints and bones of the avatar
        GetAvatar(); // for joint rotation/position control

        // check whether using the absolute coordinate system for driving avatar
        if(!UseAbsoluteCoordinate)
        {
            // set the avator to T-pose and get the initial T-pose rotations(world) of the avatar
            SetAvatarTPose(avatarBones);
            avatarTPoseRotations = GetAvatarTPoseRotations(avatarBones);
        }

        // check whether showing the human skeletons
        if(ShowHumanSkeleton)
        {
            // construct the humanSkeletons list for showing the human skeletons
            ConstructHumanSkeletons(humanMarkers);
        }
        
        // connect the broker for receiving messages
        ConnectBroker();

        // check whether using the filters for more accurate human joint keypoints 
        if(UseFilter)
        {
            // initialize the kalman and lowpass filters
            for(int i = 0; i < 17; i++)
            {
                kalmanFilters.Add(new KalmanFilterf(3, 3, 3, initStateVector, initEstimateUncertainty));
                lowpassFilters.Add(new LowpassFilter());
            }

            // assign the custom kalman filter
            for(int j = 0; j < 17; j++)
            {
                kalmanFilters[j].KalmanInitializef(F, G, u, w, H, v);
            }

            // reassign the custom kalman filter for updating the unity frame(for debug)
            for(int k = 0; k < 17; k++)
            {
                kalmanFilters[k].xf = initStateVector;
                kalmanFilters[k].Pf = initEstimateUncertainty;
                kalmanFilters[k].KalmanInitializef(F, G, u, w, H, v);
            }
        }

        // check whether using the absolute coordinate system for driving avatar
        if(!UseAbsoluteCoordinate)
        {
            // check whether the human joint keypoints are set to T-Pose and get the initial T-Pose rotations(world) of the human
            Thread.Sleep(100); // delay for receiving data(for debug)
            humanTPoseRotations = CheckHumanTPose();
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
        // DrivePlayer(); // for debug
    }

    // LateUpdate is called once per frame after FixedUpdate
    void LateUpdate()
    {

    }
} // end class
