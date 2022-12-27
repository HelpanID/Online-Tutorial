using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Connection : MonoBehaviour
{
    public NetworkManager networkManager;

    private void Start()
    {
        if(!Application.isBatchMode)
        {
            networkManager.StartClient();
        }
    }

    public void JoinClient()
    {
        networkManager.networkAddress = "localhost";
        networkManager.StartClient();
    }
}