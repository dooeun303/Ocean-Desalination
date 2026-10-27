using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class Clock : MonoBehaviour
{
    public TMP_Text clockText;
    // Update is called once per frame
    void Update()
    {
        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        clockText.text = currentTime;
    }
}
