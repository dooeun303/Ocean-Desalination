using UnityEngine;
using System;

public class ExitGame : MonoBehaviour
{

    public void OnExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
