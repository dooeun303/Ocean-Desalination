using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// 화상통화 중 마이크 녹음 + 서버 전송 스크립트
public class AudioRecorder : MonoBehaviour
{
    public static AudioRecorder Instance;

    [Header("서버 주소")]
    public string serverUrl = "http://192.168.0.66:3000/api/summary";

    [Header("녹음 설정")]
    public int sampleRate = 16000;   // Whisper 권장 샘플레이트
    public int maxDuration = 500;    // 최대 녹음 시간 (초)

    private AudioClip _recording;
    private bool _isRecording = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 녹음 시작 (화상통화 시작 시 호출)
    public void StartRecording()
    {
        if (_isRecording) return;

        _recording = Microphone.Start(null, false, maxDuration, sampleRate);
        _isRecording = true;
        Debug.Log("[AudioRecorder] 녹음 시작");
    }

    // 녹음 종료 + 서버 전송 (화상통화 종료 시 호출)
    public void StopAndSend(string maintenanceId = "")
    {
        if (!_isRecording)
        {
            Debug.LogWarning("[AudioRecorder] 녹음 중이 아닙니다.");
            return;
        }

        int position = Microphone.GetPosition(null);
        Microphone.End(null);
        _isRecording = false;

        if (position <= 0)
        {
            Debug.LogWarning("[AudioRecorder] 녹음 데이터 없음");
            return;
        }

        // 실제 녹음된 길이만큼 자르기
        var samples = new float[position * _recording.channels];
        _recording.GetData(samples, 0);

        StartCoroutine(SendAudio(samples, maintenanceId));
    }

    // WAV 변환 + 서버 전송
    private IEnumerator SendAudio(float[] samples, string maintenanceId = "")
    {
        Debug.Log("[AudioRecorder] 서버 전송 중...");

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
                Debug.Log("[AudioRecorder] 요약 완료");
                SummaryDisplay.Instance?.ShowSummary(response.summary, response.transcript);
            }
            else
            {
                Debug.LogError("[AudioRecorder] 요약 실패: " + response.message);
                SummaryDisplay.Instance?.ShowError("요약에 실패했습니다.");
            }
        }
        else
        {
            Debug.LogError("[AudioRecorder] 전송 실패: " + req.error);
            SummaryDisplay.Instance?.ShowError("서버 연결에 실패했습니다.");
        }
    }

    // float[] → WAV 변환
    private byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        int sampleCount = samples.Length;
        int byteCount = sampleCount * 2; // 16bit = 2bytes

        // WAV 헤더
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

        // 샘플 데이터
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

