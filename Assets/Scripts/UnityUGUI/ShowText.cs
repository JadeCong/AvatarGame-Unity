using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowText : MonoBehaviour
{
    // define the UGUI variables
    public Rect area;
    public GUIContent content;
    Camera camera;
    Vector3 screenPos;

    // define the show text functions
    void OnGUI()
    {
        // get the position of the gameObject in current camera
        screenPos = camera.WorldToScreenPoint(this.transform.position);

        // set the position of the text in the view window
        area.x = screenPos.x;
        area.y = camera.scaledPixelHeight - screenPos.y;
        GUI.Label(area, content);
    }

    // Start is called before the first frame update
    void Start()
    {
        // get and initialize the camera
        // camera = Camera.main;
        camera = GameObject.Find("Camera_Third_Perspective_Front").GetComponent<Camera>();

        // set the area settings
        area.width = 80;
        area.height = 40;

        // set the content settings
        content.text = "<size=30><color=pink>" + content.text + "</color></size>";
    }

    // Update is called once per frame
    void Update()
    {

    }
}
