using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarGameStart : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // OnStartGame is called for starting the avatar game
    public void OnStartGame(string sceneName)
    {
        Application.LoadLevel(sceneName);
    }
}
