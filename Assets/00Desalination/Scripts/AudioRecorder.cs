using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// ȭ����ȭ �� ����ũ ���� + ���� ���� ��ũ��Ʈ
public class AudioRecorder : MonoBehaviour
{
    public static AudioRecorder Instance;

    public string serverUrl => ServerConfig.BaseUrl + "/api/ai/voice-summary";

    [Header("���� ����")]
    public int sampleRate = 16000;   // Whisper ���� ���÷���Ʈ
    public int maxDuration = 500;    // �ִ� ���� �ð� (��)

    private AudioClip _recording;
    private bool _isRecording = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ���� ���� (ȭ����ȭ ���� �� ȣ��)
    public void StartRecording()
    {
        if (_isRecording) return;

        _recording = Microphone.Start(null, false, maxDuration, sampleRate);
        _isRecording = true;
        Debug.Log("[AudioRecorder] ���� ����");
    }

    // ���� ���� + ���� ���� (ȭ����ȭ ���� �� ȣ��)
    public void StopAndSend(string maintenanceId = "")
    {
        if (!_isRecording)
        {
            Debug.LogWarning("[AudioRecorder] ���� ���� �ƴմϴ�.");
            return;
        }

        int position = Microphone.GetPosition(null);
        Microphone.End(null);
        _isRecording = false;

        if (position <= 0)
        {
            Debug.LogWarning("[AudioRecorder] ���� ������ ����");
            return;
        }

        // ���� ������ ���̸�ŭ �ڸ���
        var samples = new float[position * _recording.channels];
        _recording.GetData(samples, 0);

        StartCoroutine(SendAudio(samples, maintenanceId));
    }

    // WAV ��ȯ + ���� ����
    private IEnumerator SendAudio(float[] samples, string maintenanceId = "")
    {
        Debug.Log("[AudioRecorder] ���� ���� ��...");

        byte[] wavData = ConvertToWav(samples, _recording.channels, sampleRate);

        var form = new WWWForm();
        form.AddBinaryData("audio", wavData, "audio.wav", "audio/wav");
        if (!string.IsNullOrEmpty(maintenanceId))
            form.AddField("maintenance_id", maintenanceId);

        using var req = UnityWebRequest.Post(serverUrl, form);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<SummaryResponse>(req.downloadHandler.text);
            if (response.success)
            {
                Debug.Log("[AudioRecorder] ��� �Ϸ�");
                SummaryDisplay.Instance?.ShowSummary(response.summary, response.transcript);
            }
            else
            {
                Debug.LogError("[AudioRecorder] ��� ����: " + response.message);
                SummaryDisplay.Instance?.ShowError("��࿡ �����߽��ϴ�.");
            }
        }
        else
        {
            Debug.LogError("[AudioRecorder] ���� ����: " + req.error);
            SummaryDisplay.Instance?.ShowError("���� ���ῡ �����߽��ϴ�.");
        }
    }

    // float[] �� WAV ��ȯ
    private byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        int sampleCount = samples.Length;
        int byteCount = sampleCount * 2; // 16bit = 2bytes

        // WAV ���
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + byteCount);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);          // PCM
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);         // 16bit
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(byteCount);

        // ���� ������
        foreach (var sample in samples)
        {
            short s = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
            writer.Write(s);
        }

        return stream.ToArray();
    }
}

[System.Serializable]
public class SummaryResponse
{
    public bool success;
    public string transcript;
    public string summary;
    public string message;
}

