using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasOn : MonoBehaviour
{
    public GameObject Canvas;

    private void Start()
    {
        Canvas.SetActive(true);
    }
}