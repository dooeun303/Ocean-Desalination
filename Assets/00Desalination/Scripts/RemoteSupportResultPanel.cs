using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

// 원격지원 결과 저장 패널 (MR).
// 화상통화(원격지원) 종료 시(AR/MR 무관) 표시 → 결과 선택 + STT 음성기록 → 서버 저장.
// * 신규 파일. 기존 스크립트/씬은 건드리지 않는다. 모든 참조는 Inspector에서 배선.
// * 이 컴포넌트는 "항상 활성" GameObject에 붙이고, panelRoot 에 "원격지원 결과 저장" 패널을 넣는다.
public class RemoteSupportResultPanel : MonoBehaviour
{
    [Header("연결된 유지보수 건 표시 (선택)")]
    [SerializeField] TMP_Text contextText;   // 예: "펌프 P-101 · 베어링 마모 의심 점검"

    string _maintenanceLogId;

    [Header("패널")]
    [SerializeField] GameObject panelRoot;       // "원격지원 결과 저장" (통화 종료 시 켜짐)
    [SerializeField] GameObject itemContainer;   // "아이템 컨테이너" (입력 폼)
    [SerializeField] GameObject resultHistory;   // "결과내역" (저장 후 표시)
    [SerializeField] float placeDistance = 1.5f; // 통화 종료 시 카메라 앞 몇 m 에 띄울지

    [Header("결과 버튼 선택 표시 (라디오)")]
    [SerializeField] Image[] resultButtonGraphics = new Image[4]; // 해결완료·취소·재지원예정·기타 버튼의 Image (순서대로)
    [SerializeField] Color selectedColor = new Color(0.20f, 0.55f, 0.95f); // 선택된 버튼 색
    [SerializeField] Color normalColor = Color.white;                     // 안 선택된 버튼 색
    [SerializeField] GameObject[] selectedMarks = new GameObject[0];          // (선택) 버튼별 체크 아이콘 등. 버튼 본체 넣지 말 것

    [Header("음성 기록")]
    [SerializeField] TMP_Text recButtonLabel;    // "음성 기록 시작" 버튼 라벨 (선택)
    [SerializeField] Image progressBar;       // Image Type=Filled, Fill Method=Horizontal (VU 미터)
    [SerializeField] TMP_Text statusText;        // "음성 기록 상태"
    [SerializeField] TMP_Text exampleText;       // "음성기록 예시"
    [SerializeField] int maxRecordSeconds = 60;
    [SerializeField] int sampleRate = 16000;
    [SerializeField] float micGain = 8f;      // 음량 미터 감도 (소리 작으면 15~20 으로)

    [Header("결과내역 채우기 (선택)")]
    [SerializeField] TMP_Text historyResultText;
    [SerializeField] TMP_Text historyNoteText;

    [Header("서버")]
    [SerializeField] string serverBaseUrl = "http://192.168.0.66:3000";

    static readonly string[] CODES = { "RESOLVED", "CANCELED", "RESUPPORT", "ETC" };
    static readonly string[] LABELS = { "해결 완료", "취소", "재지원 예정", "기타" };

    int _sel = -1;
    string _supportCallId;
    bool _rec;
    AudioClip _clip;
    Coroutine _progCo;

    void Awake()
    {
        if (panelRoot) panelRoot.SetActive(false);
        ResetForm();
    }

    void OnEnable()
    {
        VideoCallSignalingMR.OnCallEnded += HandleCallEnded;
        Debug.Log("[RemoteSupport] OnCallEnded 구독됨 (GO: " + gameObject.name +
                  " / activeInHierarchy: " + gameObject.activeInHierarchy + ")");
    }

    void OnDisable()
    {
        VideoCallSignalingMR.OnCallEnded -= HandleCallEnded;
    }

    // ── 통화 종료 → 패널 표시 ──
    void HandleCallEnded(string channelName)
    {
        Debug.Log("[RemoteSupport] 통화 종료 수신 → 패널 켜기  channel=" + channelName);
        _supportCallId = VideoCallSignalingMR.CurrentSupportCallId; // null 가능
        if (contextText) contextText.text = "";
        if (!string.IsNullOrEmpty(_supportCallId)) StartCoroutine(FetchContext());
        ResetForm();
        if (panelRoot)
        {
            panelRoot.SetActive(true);
            PlaceInFront();
        }
    }

    void PlaceInFront()
    {
        var cam = Camera.main;
        if (cam == null || panelRoot == null) return;
        var t = panelRoot.transform;
        t.position = cam.transform.position + cam.transform.forward * placeDistance;
        Vector3 fwd = cam.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        t.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
    }

    void ResetForm()
    {
        _sel = -1;
        if (itemContainer) itemContainer.SetActive(true);
        if (resultHistory) resultHistory.SetActive(false);
        if (progressBar) progressBar.fillAmount = 0f;
        if (statusText) statusText.text = "";
        if (exampleText) exampleText.text = "";
        if (recButtonLabel) recButtonLabel.text = "음성 기록 시작";
        foreach (var g in resultButtonGraphics) if (g) g.color = normalColor;
        foreach (var m in selectedMarks) if (m) m.SetActive(false);
        StopMic(false);
    }

    // ── 버튼 OnClick: 결과 선택 (해결완료→0, 취소→1, 재지원예정→2, 기타→3). 라디오: 1개만 유지 ──
    public void OnResultButton(int code)
    {
        _sel = Mathf.Clamp(code, 0, 3);

        for (int i = 0; i < resultButtonGraphics.Length; i++)
            if (resultButtonGraphics[i])
                resultButtonGraphics[i].color = (i == _sel) ? selectedColor : normalColor;

        for (int i = 0; i < selectedMarks.Length; i++)
            if (selectedMarks[i]) selectedMarks[i].SetActive(i == _sel);
    }

    // ── 버튼 OnClick: 음성 기록 시작/중지 토글 ──
    public void ToggleRecording()
    {
        if (_rec) StopMic(true);
        else StartMic();
    }

    void StartMic()
    {
        if (Microphone.devices.Length == 0) { Status("마이크 없음"); return; }
        _clip = Microphone.Start(null, false, maxRecordSeconds, sampleRate);
        _rec = true;
        if (recButtonLabel) recButtonLabel.text = "녹음 중지";
        Status("녹음 중…");
        _progCo = StartCoroutine(Progress());
    }

    // 실시간 마이크 음량(RMS) → progressBar.fillAmount (VU 미터). 시간 상한 도달 시 자동 종료.
    IEnumerator Progress()
    {
        const int win = 256;
        var buf = new float[win];
        float elapsed = 0f;

        while (_rec)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= maxRecordSeconds) { StopMic(true); yield break; }

            int pos = Microphone.GetPosition(null);
            if (_clip != null && pos > win)
            {
                _clip.GetData(buf, pos - win);
                float sum = 0f;
                for (int i = 0; i < win; i++) sum += buf[i] * buf[i];
                float level = Mathf.Clamp01(Mathf.Sqrt(sum / win) * micGain);
                if (progressBar)
                    progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, level, 0.35f);
            }
            Status("녹음 중…");
            yield return null;
        }
    }

    void StopMic(bool toStt)
    {
        if (_progCo != null) { StopCoroutine(_progCo); _progCo = null; }
        if (!_rec) return;

        int pos = Microphone.GetPosition(null);
        Microphone.End(null);
        _rec = false;
        if (recButtonLabel) recButtonLabel.text = "음성 기록 시작";
        if (progressBar) progressBar.fillAmount = 0f;

        if (!toStt || _clip == null || pos <= 0) { Status(""); return; }

        var samples = new float[pos * _clip.channels];
        _clip.GetData(samples, 0);
        Status("STT 변환 중…");
        StartCoroutine(Stt(ToWav(samples, _clip.channels, sampleRate)));
    }

    IEnumerator Stt(byte[] wav)
    {
        var form = new WWWForm();
        form.AddBinaryData("file", wav, "record.wav", "audio/wav");
        using var req = UnityWebRequest.Post(serverBaseUrl.TrimEnd('/') + "/api/stt", form);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            SttResp r = null;
            try { r = JsonUtility.FromJson<SttResp>(req.downloadHandler.text); } catch { }
            if (r != null && r.success && !string.IsNullOrEmpty(r.text))
            {
                if (exampleText) exampleText.text = r.text;
                Status("완료");
            }
            else Status("STT 결과 없음");
        }
        else Status("STT 실패: " + req.error);
    }

    // ── 버튼 OnClick: 저장 ──
    public void OnSave()
    {
        if (_sel < 0) { Status("결과를 선택하세요"); return; }
        StartCoroutine(SaveCo());
    }

    IEnumerator SaveCo()
    {
        string code = CODES[_sel];
        string note = exampleText ? exampleText.text : "";

        if (!string.IsNullOrEmpty(_supportCallId))
        {
            string url = serverBaseUrl.TrimEnd('/') + "/api/support-calls/" + _supportCallId + "/result";
            string json = JsonUtility.ToJson(new Body { support_result = code, support_note = note });
            using var req = new UnityWebRequest(url, "PATCH");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { Status("저장 실패: " + req.error); yield break; }
        }
        else Debug.LogWarning("[RemoteSupport] support call id 없음 - 서버 저장 건너뜀");

        if (itemContainer) itemContainer.SetActive(false);
        if (resultHistory) resultHistory.SetActive(true);
        if (historyResultText) historyResultText.text = LABELS[_sel];
        if (historyNoteText) historyNoteText.text = string.IsNullOrEmpty(note) ? "없음" : note;
        Status("저장됨");
    }

    // ── 버튼 OnClick: 닫기 ──
    public void OnClose()
    {
        StopMic(false);
        if (panelRoot) panelRoot.SetActive(false);
    }

    void Status(string s) { if (statusText) statusText.text = s; }

    // float[] → 16bit PCM WAV (AudioRecorder.cs와 동일 방식)
    static byte[] ToWav(float[] s, int ch, int rate)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        int b = s.Length * 2;
        w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + b);
        w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE")); w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(rate);
        w.Write(rate * ch * 2); w.Write((short)(ch * 2)); w.Write((short)16);
        w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(b);
        foreach (var x in s) w.Write((short)(Mathf.Clamp(x, -1f, 1f) * short.MaxValue));
        return ms.ToArray();
    }

    IEnumerator FetchContext()
    {
        using var req = UnityWebRequest.Get(serverBaseUrl.TrimEnd('/') + "/api/support-calls/" + _supportCallId);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) yield break;

        CtxResp r = null;
        try { r = JsonUtility.FromJson<CtxResp>(req.downloadHandler.text); } catch { }
        if (r == null || !r.success || r.data == null) yield break;

        _maintenanceLogId = r.data.maintenance_log_id;
        string nm = string.IsNullOrEmpty(r.data.equipment_name) ? "설비 미지정" : r.data.equipment_name;
        string ds = !string.IsNullOrEmpty(r.data.maintenance_description) ? r.data.maintenance_description
                  : (!string.IsNullOrEmpty(r.data.work_type) ? r.data.work_type : "원격 지원");
        if (contextText) contextText.text = nm + " · " + ds;
    }

    [Serializable] class CtxResp { public bool success; public CtxData data; }
    [Serializable]
    class CtxData
    {
        public string equipment_name;
        public string maintenance_description;
        public string work_type;
        public string maintenance_log_id;
    }

    [Serializable] class SttResp { public bool success; public string text; public double duration; }
    [Serializable] class Body { public string support_result; public string support_note; }
}