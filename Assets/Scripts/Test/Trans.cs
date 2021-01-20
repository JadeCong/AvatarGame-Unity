using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class Trans : MonoBehaviour
{
    public Transform avatar;
    public Transform human;
    public Quaternion humanToAvatar;
    public Quaternion calcAvatar;

    public Quaternion rotAngle;
    public Quaternion newHuman;
    public Quaternion newAvatar;
    public Quaternion newCalcAvatar;

    // public Quaternion lookRot;

    // Start is called before the first frame update
    void Start()
    {
        human = GameObject.Find("Human").transform;
        Debug.Log("human.position: " + human.position);
        Debug.Log("human.localPosition: " + human.localPosition);
        Debug.Log("human.rotation: " + human.rotation); //(30,0,0)or(0.3,0,0,1)

        avatar = GameObject.Find("Avatar").transform;
        Debug.Log("avatar.position: " + avatar.position);
        Debug.Log("avatar.localPosition: " + avatar.localPosition);
        Debug.Log("avatar.rotation: " + avatar.rotation); //(0,0,45)or(0,0,0.4,0.9)

        humanToAvatar = avatar.rotation * Quaternion.Inverse(human.rotation);
        // humanToAvatar = human.rotation * Quaternion.Inverse(avatar.rotation);
        // humanToAvatar = Quaternion.Inverse(human.rotation) * avatar.rotation;
        // humanToAvatar = Quaternion.Inverse(avatar.rotation) * human.rotation;
        Debug.Log("humanToAvatar.rotation: " + humanToAvatar);

        calcAvatar = humanToAvatar * human.rotation;
        Debug.Log("WTF, calcAvatar.rotation: " + calcAvatar);




        Thread.Sleep(2000);
        rotAngle = Quaternion.Euler(10, 30, 50);
        // Debug.Log("rotAngle.rotation: " + rotAngle.ToString("F3"));
        Debug.Log("rotAngle.rotation: " + rotAngle);

        newHuman = rotAngle * human.rotation;
        Debug.Log("newHuman.rotation: " + newHuman);
        newAvatar = rotAngle * avatar.rotation;
        Debug.Log("newAvatar.rotation: " + newAvatar);

        newCalcAvatar = newHuman * Quaternion.Inverse(human.rotation) * avatar.rotation;
        Debug.Log("WTF, newCalcAvatar.rotation: " + newCalcAvatar);




        // lookRot = Quaternion.LookRotation(Vector3.forward, Vector3.right);
        // Debug.Log("lookRot.rotation: " + lookRot);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
