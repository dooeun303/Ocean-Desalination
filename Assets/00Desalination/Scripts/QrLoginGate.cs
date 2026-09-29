using System;
using System.Collections;
using Meta.XR;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// QR 로그인 게이트. 씬 진입 시 패널을 띄우고 패스스루 카메라로 작업자 배지 QR을 스캔,
/// 서버(auth.js) 인증이 성공하면 패널을 닫고 <see cref="OnAuthenticated"/> 를 호출한다.
/// 이미 유효한 토큰이 있으면(자동 로그인) 스캔 없이 바로 통과 시도.
///
/// [씬 설정 - 인스펙터에서 연결]
/// 1) Meta.XR.PassthroughCameraAccess 컴포넌트를 가진 GameObject 준비
///    (이 오브젝트에 같이 붙여도 됨, 처음엔 체크박스 꺼둘 것 - 이 스크립트가 켜고 끔) → passthroughCamera
/// 2) "QR을 바라보세요" 안내 패널(Canvas 하위 오브젝트) → panelRoot
/// 3) 상태 표시용 TMP_Text → statusText
/// 4) (선택) 헤드셋 없이 테스트할 때 쓸 TMP_InputField + Button → debugQrInput / debugLoginButton
/// 5) AndroidManifest.xml 에 horizonos.permission.HEADSET_CAMERA 권한 필수 (없으면 카메라 자체가 안 켜짐)
/// </summary>
public class QrLoginGate : MonoBehaviour
{
    [Header("카메라")]
    [SerializeField] private PassthroughCameraAccess passthroughCamera;
    [SerializeField] private float scanIntervalSeconds = 0.4f;
    [Tooltip("인증 성공 메시지를 보여주고 패널을 닫기까지 대기하는 시간(초)")]
    [SerializeField] private float successDisplaySeconds = 3f;
    [Tooltip("스캔이 안 되면 반대로 바꿔서 테스트 (기기별 이미지 방향 차이 보정용)")]
    [SerializeField] private bool flipVertical = true;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text statusText;
    [Tooltip("지금 카메라가 뭘 보고 있는지 화면에 띄워주는 미리보기 (선택, 안 쓰면 비워둬도 됨)")]
    [SerializeField] private UnityEngine.UI.RawImage cameraPreview;

    [Header("디버그/테스트용 (안 쓰면 비워둬도 됨)")]
    [Tooltip("헤드셋 없이 에디터에서 QR 없이 텍스트로 로그인 테스트")]
    [SerializeField] private TMP_InputField debugQrInput;
    [SerializeField] private UnityEngine.UI.Button debugLoginButton;
    [Tooltip("저장된 로그인 세션을 지우고 QR 패널을 다시 띄운다. 앱 재설치 없이 반복 테스트할 때 사용 - " +
             "패널 밖(로그인 완료 후에도 보이는 곳)에 배치해서 눌러야 함")]
    [SerializeField] private UnityEngine.UI.Button debugLogoutButton;

    /// <summary>인증 완료 시 1회 호출. 그 뒤 메인 씬/기능을 열어주는 쪽에서 구독.</summary>
    public event Action<AuthApi.UserDto> OnAuthenticated;

    private bool _authenticated;
    private bool _loginInFlight;
    private bool _readbackInFlight;
    private Coroutine _scanRoutine;
    private int _scanAttemptCount; // 진단용 - 스캔이 실제로 돌고는 있는지 확인하려고 주기적으로 로그를 남긴다

    private void Awake()
    {
        if (debugLoginButton != null)
        {
            debugLoginButton.onClick.AddListener(() =>
            {
                if (debugQrInput != null && !string.IsNullOrEmpty(debugQrInput.text))
                {
                    TryLogin(debugQrInput.text);
                }
            });
        }

        if (debugLogoutButton != null)
        {
            debugLogoutButton.onClick.AddListener(Logout);
        }
    }

    /// <summary>
    /// (테스트용) 저장된 로그인 세션을 지우고 QR 패널을 다시 띄운다.
    /// 앱을 지우지 않고도 로그인 흐름을 반복 테스트할 수 있게 하는 용도.
    /// </summary>
    public void Logout()
    {
        if (debugLogoutButton != null)
        {
            debugLogoutButton.interactable = false;
        }

        AuthApi.Instance.Logout(() =>
        {
            if (debugLogoutButton != null)
            {
                debugLogoutButton.interactable = true;
            }

            // OnDisable → OnEnable 을 다시 태워서 QR 스캔 흐름을 처음부터 재시작한다.
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        });
    }

    private void OnEnable()
    {
        _authenticated = false;
        ShowPanel(true);
        SetStatus("로그인 확인 중...");
        StartCoroutine(CoWaitForAuthApiThenStart());
    }

    // AuthApi 싱글톤의 Awake() 가 스크립트 실행 순서상 이 오브젝트의 OnEnable() 보다 늦게 돌 수 있어서
    // (특히 AuthApi 오브젝트가 씬 로드 시점에 다른 오브젝트 자식이었다가 옮겨지는 등 순서가 꼬이면)
    // Instance 가 준비될 때까지 한두 프레임 기다렸다가 진행한다. NullReferenceException 방지용.
    private IEnumerator CoWaitForAuthApiThenStart()
    {
        while (AuthApi.Instance == null)
        {
            yield return null;
        }

        if (AuthSession.IsLoggedIn)
        {
            AuthApi.Instance.VerifyToken(valid =>
            {
                if (valid)
                {
                    Complete(new AuthApi.UserDto
                    {
                        user_id = AuthSession.UserId,
                        user_nm = AuthSession.UserName,
                        company = AuthSession.Company,
                    });
                }
                else
                {
                    BeginScanning();
                }
            });
        }
        else
        {
            BeginScanning();
        }
    }

    private void OnDisable()
    {
        if (_scanRoutine != null)
        {
            StopCoroutine(_scanRoutine);
            _scanRoutine = null;
        }

        if (cameraPreview != null)
        {
            cameraPreview.texture = null;
        }

        _readbackInFlight = false;
    }

    private void Update()
    {
        // 카메라가 지금 뭘 찍고 있는지 매 프레임 미리보기에 반영 (텍스처 참조만 갱신하는 거라 가벼움)
        if (cameraPreview == null || passthroughCamera == null || !passthroughCamera.IsPlaying)
        {
            return;
        }

        var tex = passthroughCamera.GetTexture();
        if (cameraPreview.texture != tex)
        {
            cameraPreview.texture = tex;
        }
    }

    private void BeginScanning()
    {
        if (passthroughCamera == null)
        {
            SetStatus("카메라 미연결 - 인스펙터에서 passthroughCamera 를 확인하세요.");
            Debug.LogError(nameof(QrLoginGate) + ": passthroughCamera 가 비어있습니다.");
            return;
        }

        if (!PassthroughCameraAccess.IsSupported)
        {
            SetStatus("이 기기는 패스스루 카메라 QR 스캔을 지원하지 않습니다.\n(아래 디버그 입력으로 테스트하세요)");
            Debug.LogWarning(nameof(QrLoginGate) + ": PassthroughCameraAccess.IsSupported == false");
            return;
        }

        SetStatus("QR을 바라보세요");
        passthroughCamera.enabled = true;
        _scanRoutine = StartCoroutine(ScanLoop());
    }

    // 스캔 루프: Meta SDK 의 PassthroughCameraAccess.GetColors() 는 내부적으로
    //   AsyncGPUReadback.RequestIntoNativeArray(...).WaitForCompletion() 를 써서 메인 스레드를 블로킹한다.
    // 카메라가 계속 새 프레임을 GPU 에 밀어넣는 와중에 이 블로킹 호출을 반복하면 GPU 커맨드 큐가
    // 밀리기 시작해서("PCA: previous command buffer is still executing") 프레임레이트가 영구적으로
    // 떨어지는 문제가 실기기에서 확인됨. 그래서 GetColors() 대신 GetTexture() + 논블로킹
    // AsyncGPUReadback.Request() 를 직접 써서 GPU 스톨을 피한다. 요청은 한 번에 하나만 나가게 가드.
    private IEnumerator ScanLoop()
    {
        int notPlayingLogCount = 0;

        while (!_authenticated)
        {
            yield return new WaitForSeconds(scanIntervalSeconds);

            if (_loginInFlight || _readbackInFlight)
            {
                continue;
            }

            if (passthroughCamera == null || !passthroughCamera.IsPlaying)
            {
                // 진단용: 카메라가 아직도 안 돌고 있으면(권한 대기 중이거나 Play 실패) 대략 4초마다 한 번씩 로그.
                notPlayingLogCount++;
                if (notPlayingLogCount % 10 == 1)
                {
                    Debug.Log(nameof(QrLoginGate) + $": 카메라 대기 중... IsPlaying={passthroughCamera?.IsPlaying}");
                }
                continue;
            }

            var tex = passthroughCamera.GetTexture();
            if (tex == null)
            {
                continue;
            }

            _readbackInFlight = true;
            AsyncGPUReadback.Request(tex, 0, TextureFormat.RGBA32, OnReadbackComplete);
        }
    }

    private void OnReadbackComplete(AsyncGPUReadbackRequest request)
    {
        _readbackInFlight = false;

        if (_authenticated || _loginInFlight)
        {
            return;
        }

        if (request.hasError)
        {
            Debug.LogWarning(nameof(QrLoginGate) + ": AsyncGPUReadback 실패");
            return;
        }

        // 진단용: 스캔이 실제로 돌고 있는지, 어떤 해상도로 프레임을 받는지 대략 4초마다 한 번씩 로그.
        _scanAttemptCount++;
        if (_scanAttemptCount % 10 == 1)
        {
            Debug.Log(nameof(QrLoginGate) + $": 스캔 중... {request.width}x{request.height} (시도 {_scanAttemptCount}회, QR 못 찾음)");
        }

        var colors = request.GetData<Color32>();
        if (QrDecoder.TryDecode(colors, request.width, request.height, flipVertical, out string qrText))
        {
            TryLogin(qrText);
        }
    }

    private void TryLogin(string qrText)
    {
        if (_loginInFlight || _authenticated)
        {
            return;
        }

        _loginInFlight = true;
        SetStatus($"인증 중... ({qrText})");

        AuthApi.Instance.QrLogin(qrText,
            onSuccess: user =>
            {
                _loginInFlight = false;
                Complete(user);
            },
            onError: msg =>
            {
                _loginInFlight = false;
                SetStatus($"로그인 실패: {msg}\n다시 QR을 바라보세요");
            });
    }

    private void Complete(AuthApi.UserDto user)
    {
        _authenticated = true;

        if (passthroughCamera != null)
        {
            passthroughCamera.enabled = false; // 로그인 후엔 불필요 - 배터리/성능 절약
        }

        SetStatus($"인증되었습니다 ({user?.user_nm})");
        StartCoroutine(CoFinishAfterDelay(user));
    }

    // 인증 메시지를 successDisplaySeconds 초 동안 보여준 뒤 패널을 닫고 완료 이벤트를 쏜다.
    private IEnumerator CoFinishAfterDelay(AuthApi.UserDto user)
    {
        yield return new WaitForSeconds(successDisplaySeconds);
        ShowPanel(false);
        OnAuthenticated?.Invoke(user);
    }

    private void ShowPanel(bool show)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(show);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(nameof(QrLoginGate) + ": " + message);
    }
}
