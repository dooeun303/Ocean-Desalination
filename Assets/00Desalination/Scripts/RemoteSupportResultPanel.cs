using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

// �������� ��� ���� �г� (MR).
// ȭ����ȭ(��������) ���� ��(AR/MR ����) ǥ�� �� ��� ���� + STT ������� �� ���� ����.
// * �ű� ����. ���� ��ũ��Ʈ/���� �ǵ帮�� �ʴ´�. ��� ������ Inspector���� �輱.
// * �� ������Ʈ�� "�׻� Ȱ��" GameObject�� ���̰�, panelRoot �� "�������� ��� ����" �г��� �ִ´�.
public class RemoteSupportResultPanel : MonoBehaviour
{
    [Header("����� �������� �� ǥ�� (����)")]
    [SerializeField] TMP_Text contextText;   // ��: "���� P-101 �� ��� ���� �ǽ� ����"

    string _maintenanceLogId;

    [Header("�г�")]
    [SerializeField] GameObject panelRoot;       // "�������� ��� ����" (��ȭ ���� �� ����)
    [SerializeField] GameObject itemContainer;   // "������ �����̳�" (�Է� ��)
    [SerializeField] GameObject resultHistory;   // "�������" (���� �� ǥ��)
    [SerializeField] float placeDistance = 1.5f; // ��ȭ ���� �� ī�޶� �� �� m �� �����

    [Header("��� ��ư ���� ǥ�� (����)")]
    [SerializeField] Image[] resultButtonGraphics = new Image[4]; // �ذ�Ϸᡤ��ҡ���������������Ÿ ��ư�� Image (�������)
    [SerializeField] Color selectedColor = new Color(0.20f, 0.55f, 0.95f); // ���õ� ��ư ��
    [SerializeField] Color normalColor = Color.white;                     // �� ���õ� ��ư ��
    [SerializeField] GameObject[] selectedMarks = new GameObject[0];          // (����) ��ư�� üũ ������ ��. ��ư ��ü ���� �� ��

    [Header("���� ���")]
    [SerializeField] TMP_Text recButtonLabel;    // "���� ��� ����" ��ư �� (����)
    [SerializeField] Image progressBar;       // Image Type=Filled, Fill Method=Horizontal (VU ����)
    [SerializeField] TMP_Text statusText;        // "���� ��� ����"
    [SerializeField] TMP_Text exampleText;       // "������� ����"
    [SerializeField] int maxRecordSeconds = 60;
    [SerializeField] int sampleRate = 16000;
    [SerializeField] float micGain = 8f;      // ���� ���� ���� (�Ҹ� ������ 15~20 ����)

    [Header("������� ä��� (����)")]
    [SerializeField] TMP_Text historyResultText;
    [SerializeField] TMP_Text historyNoteText;

    string serverBaseUrl => ServerConfig.BaseUrl;

    static readonly string[] CODES = { "RSUP01", "RSUP02", "RSUP03", "RSUP04" }; // system_code RESP11
    static readonly string[] LABELS = { "�ذ� �Ϸ�", "���", "������ ����", "��Ÿ" };

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
        Debug.Log("[RemoteSupport] OnCallEnded ������ (GO: " + gameObject.name +
                  " / activeInHierarchy: " + gameObject.activeInHierarchy + ")");
    }

    void OnDisable()
    {
        VideoCallSignalingMR.OnCallEnded -= HandleCallEnded;
    }

    // ���� ��ȭ ���� �� �г� ǥ�� ����
    void HandleCallEnded(string channelName)
    {
        Debug.Log("[RemoteSupport] ��ȭ ���� ���� �� �г� �ѱ�  channel=" + channelName);
        _supportCallId = VideoCallSignalingMR.CurrentSupportCallId; // null ����
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
        if (recButtonLabel) recButtonLabel.text = "���� ��� ����";
        foreach (var g in resultButtonGraphics) if (g) g.color = normalColor;
        foreach (var m in selectedMarks) if (m) m.SetActive(false);
        StopMic(false);
    }

    // ���� ��ư OnClick: ��� ���� (�ذ�Ϸ��0, ��ҡ�1, ������������2, ��Ÿ��3). ����: 1���� ���� ����
    public void OnResultButton(int code)
    {
        _sel = Mathf.Clamp(code, 0, 3);

        for (int i = 0; i < resultButtonGraphics.Length; i++)
            if (resultButtonGraphics[i])
                resultButtonGraphics[i].color = (i == _sel) ? selectedColor : normalColor;

        for (int i = 0; i < selectedMarks.Length; i++)
            if (selectedMarks[i]) selectedMarks[i].SetActive(i == _sel);
    }

    // ���� ��ư OnClick: ���� ��� ����/���� ��� ����
    public void ToggleRecording()
    {
        if (_rec) StopMic(true);
        else StartMic();
    }

    void StartMic()
    {
        if (Microphone.devices.Length == 0) { Status("����ũ ����"); return; }
        _clip = Microphone.Start(null, false, maxRecordSeconds, sampleRate);
        _rec = true;
        if (recButtonLabel) recButtonLabel.text = "���� ����";
        Status("���� �ߡ�");
        _progCo = StartCoroutine(Progress());
    }

    // �ǽð� ����ũ ����(RMS) �� progressBar.fillAmount (VU ����). �ð� ���� ���� �� �ڵ� ����.
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
            Status("���� �ߡ�");
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
        if (recButtonLabel) recButtonLabel.text = "���� ��� ����";
        if (progressBar) progressBar.fillAmount = 0f;

        if (!toStt || _clip == null || pos <= 0) { Status(""); return; }

        var samples = new float[pos * _clip.channels];
        _clip.GetData(samples, 0);
        Status("STT ��ȯ �ߡ�");
        StartCoroutine(Stt(ToWav(samples, _clip.channels, sampleRate)));
    }

    IEnumerator Stt(byte[] wav)
    {
        var form = new WWWForm();
        form.AddBinaryData("file", wav, "record.wav", "audio/wav");
        using var req = UnityWebRequest.Post(serverBaseUrl.TrimEnd('/') + "/api/ai/transcribe", form);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            SttResp r = null;
            try { r = JsonUtility.FromJson<SttResp>(req.downloadHandler.text); } catch { }
            if (r != null && r.success && !string.IsNullOrEmpty(r.text))
            {
                if (exampleText) exampleText.text = r.text;
                Status("�Ϸ�");
            }
            else Status("STT ��� ����");
        }
        else Status("STT ����: " + req.error);
    }

    // ���� ��ư OnClick: ���� ����
    public void OnSave()
    {
        if (_sel < 0) { Status("����� �����ϼ���"); return; }
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
            if (req.result != UnityWebRequest.Result.Success) { Status("���� ����: " + req.error); yield break; }
        }
        else Debug.LogWarning("[RemoteSupport] support call id ���� - ���� ���� �ǳʶ�");

        if (itemContainer) itemContainer.SetActive(false);
        if (resultHistory) resultHistory.SetActive(true);
        if (historyResultText) historyResultText.text = LABELS[_sel];
        if (historyNoteText) historyNoteText.text = string.IsNullOrEmpty(note) ? "����" : note;
        Status("�����");
    }

    // ���� ��ư OnClick: �ݱ� ����
    public void OnClose()
    {
        StopMic(false);
        if (panelRoot) panelRoot.SetActive(false);
    }

    void Status(string s) { if (statusText) statusText.text = s; }

    // float[] �� 16bit PCM WAV (AudioRecorder.cs�� ���� ���)
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
        string nm = string.IsNullOrEmpty(r.data.equipment_name) ? "���� ������" : r.data.equipment_name;
        string ds = !string.IsNullOrEmpty(r.data.maintenance_description) ? r.data.maintenance_description
                  : (!string.IsNullOrEmpty(r.data.work_type) ? r.data.work_type : "���� ����");
        if (contextText) contextText.text = nm + " �� " + ds;
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