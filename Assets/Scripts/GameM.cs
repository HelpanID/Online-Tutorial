using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class GameM : MonoBehaviour
{
    public CinemachineVirtualCamera Camera;
    [HideInInspector] public Transform Follow;

    private void Update()
    {
        if(Camera.Follow == null)
        {
            Camera.Follow = Follow;
        }
    }
}