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
        string currentTime = DateTime.Now.ToString("yyyy년 M월 d일 tt h시 mm분", new System.Globalization.CultureInfo("ko-KR"));
        clockText.text = currentTime;
    }
}
