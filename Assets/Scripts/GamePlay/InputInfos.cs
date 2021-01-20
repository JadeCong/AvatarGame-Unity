using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputInfos : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        transform.GetComponent<InputField>().onValueChanged.AddListener(ChangeValue);
        transform.GetComponent<InputField>().onEndEdit.AddListener(EndEdit);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // ChangeValue is called for changing the content in input field
    public void ChangeValue(string inputText)
    {
        print("Manual Inputting Content: " + inputText);
    }

    // EndEdit is called for getting the finished content in input field
    public void EndEdit(string inputText)
    {
        print("Manual Input Content: " + inputText);
    }
}
