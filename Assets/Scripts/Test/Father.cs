using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Father : MonoBehaviour
{
    public Transform cube_father;
    public Transform cylinder_son;
    public Transform capsule_grandson;
    
    public Quaternion parent_to_son;
    public Quaternion son_to_grandson;
    public Quaternion parent_to_grandson;

    // Start is called before the first frame update
    void Start()
    {
        cube_father = GameObject.Find("Cube").transform;
        Debug.Log("Cube.position: " + cube_father.position);
        Debug.Log("Cube.localPosition: " + cube_father.localPosition);
        Debug.Log("Cube.rotation: " + cube_father.rotation);
        Debug.Log("Cube.localRotation: " + cube_father.localRotation);

        cylinder_son = GameObject.Find("Cylinder").transform;
        Debug.Log("Cylinder.position: " + cylinder_son.position);
        Debug.Log("Cylinder.localPosition: " + cylinder_son.localPosition);
        Debug.Log("Cylinder.rotation: " + cylinder_son.rotation);
        Debug.Log("Cylinder.localRotation: " + cylinder_son.localRotation);

        capsule_grandson = GameObject.Find("Capsule").transform;
        Debug.Log("Capsule.position: " + capsule_grandson.position);
        Debug.Log("Capsule.localPosition: " + capsule_grandson.localPosition);
        Debug.Log("Capsule.rotation: " + capsule_grandson.rotation);
        Debug.Log("Capsule.localRotation: " + capsule_grandson.localRotation);

        parent_to_son = cylinder_son.rotation * Quaternion.Inverse(cube_father.rotation);
        Debug.Log("parent_to_son: " + parent_to_son);
        Debug.Log("calculate_son_from_father.rotation: " + parent_to_son * cube_father.rotation);
        // Debug.Log("calculate_son_from_father.position: " + (cube_father.position + Quaternion.Inverse(cube_father.localRotation) * cylinder_son.localPosition));
        Debug.Log("calculate_son_from_father.position: " + (cube_father.position + cube_father.localRotation * cylinder_son.localPosition));
        // Debug.Log("calculate_son_from_father.rotation: " + cube_father.rotation * parent_to_son);

        son_to_grandson = capsule_grandson.rotation * Quaternion.Inverse(cylinder_son.rotation);
        Debug.Log("son_to_grandson: " + son_to_grandson);
        Debug.Log("calculate_grandson_from_son.rotation: " + son_to_grandson * cylinder_son.rotation);
        // Debug.Log("calculate_grandson_from_son.rotation: " + cylinder_son.rotation * son_to_grandson);

        parent_to_grandson = capsule_grandson.rotation * Quaternion.Inverse(cube_father.rotation);
        Debug.Log("parent_to_grandson: " + parent_to_grandson);
        Debug.Log("calculate_grandson_from_father.rotation: " + parent_to_grandson * cube_father.rotation);

        Debug.Log("calculate_grandson_from_father_and_son.rotation: " + son_to_grandson * parent_to_son * cube_father.rotation);
        // Debug.Log("calculate_grandson_from_father_and_son.rotation: " + parent_to_son * son_to_grandson * cube_father.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
