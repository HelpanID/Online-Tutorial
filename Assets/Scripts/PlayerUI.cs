using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Text PlayerText;

    public void SetPlayer(string name)
    {
        PlayerText.text = name;
    }
}