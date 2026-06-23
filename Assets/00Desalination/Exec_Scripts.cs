using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Exec_Scripts : MonoBehaviour
{
    private float timer = 0f;
    public TMP_Text tt;
    public TMP_Text tt2;

    private void Awake()
    {
        Debug.Log("나 깨어났다!");
    }
    private void Start()
    {
        
        Debug.Log("나 준비됐다!");
    }

    private void Update()
    {
            timer += Time.deltaTime;
            if (timer >= 3f)
            {
                Debug.Log("나 살아있다!");
                timer = 0f;
            }
       
    }


}
