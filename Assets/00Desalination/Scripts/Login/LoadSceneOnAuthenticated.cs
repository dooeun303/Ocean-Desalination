using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// QrLoginGate 의 인증 완료 이벤트를 받아서 메인(MR) 씬으로 전환한다.
/// 로그인 씬(Login.unity)에 QrLoginGate 와 같이 놓고, 인스펙터에서 qrLoginGate 와
/// nextSceneName 만 연결하면 됨.
/// </summary>
public class LoadSceneOnAuthenticated : MonoBehaviour
{
    [Tooltip("같은 씬에 있는 QrLoginGate")]
    [SerializeField] private QrLoginGate qrLoginGate;

    [Tooltip("로그인 성공 후 로드할 씬 이름 (Build Settings 에 등록돼 있어야 함)")]
    [SerializeField] private string nextSceneName = "MR 1";

    private void OnEnable()
    {
        if (qrLoginGate != null)
        {
            qrLoginGate.OnAuthenticated += HandleAuthenticated;
        }
        else
        {
            Debug.LogError(nameof(LoadSceneOnAuthenticated) + ": qrLoginGate 가 비어있습니다.");
        }
    }

    private void OnDisable()
    {
        if (qrLoginGate != null)
        {
            qrLoginGate.OnAuthenticated -= HandleAuthenticated;
        }
    }

    private void HandleAuthenticated(AuthApi.UserDto user)
    {
        Debug.Log(nameof(LoadSceneOnAuthenticated) + $": {user?.user_nm} 인증됨 → '{nextSceneName}' 씬 로드");
        SceneManager.LoadScene(nextSceneName);
    }
}
