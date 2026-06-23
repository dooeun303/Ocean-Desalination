using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class text : MonoBehaviour
{
    public TMP_Text survivor_text;
    public TMP_Text timer_text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        survivor_text.text = "생존자: 4명";
        timer_text.text = "3";

        StartCoroutine(OnTimer());
    }


    public IEnumerator OnTimer()
    {
        yield return new WaitForSeconds(1f);

        timer_text.text = "2";
        yield return new WaitForSeconds(1f);
        timer_text.text = "1";
        yield return new WaitForSeconds(1f);
        timer_text.text = "시작!";

        //SceneManager.LoadScene("MainScene");
    }



}
