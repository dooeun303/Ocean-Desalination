using UnityEngine;

/// <summary>
/// 자식 큐브들을 사인파를 따라 위아래로 움직여 물결(웨이브) 효과를 만듭니다.
/// [ExecuteAlways] 덕분에 Play 모드뿐 아니라 에디터에서도 동작합니다.
/// </summary>
public class CubeWave : MonoBehaviour
{
    [Tooltip("파동의 높이(진폭)")]
    public float amplitude = 1.5f;

    [Tooltip("파동의 속도")]
    public float speed = 2f;

    [Tooltip("큐브 간 위상 차이 (값이 클수록 물결이 촘촘해짐)")]
    public float phaseStep = 0.6f;

    [Tooltip("큐브가 함께 회전하도록 할지 여부")]
    public bool spin = true;

    public float spinSpeed = 45f;

    // 각 자식의 기준(원래) 높이를 저장
    private float[] _baseY;
    private Transform[] _cubes;

    void OnEnable()
    {
        CacheChildren();
    }

    void CacheChildren()
    {
        int n = transform.childCount;
        _cubes = new Transform[n];
        _baseY = new float[n];
        for (int i = 0; i < n; i++)
        {
            _cubes[i] = transform.GetChild(i);
            _baseY[i] = _cubes[i].localPosition.y;
        }
    }

    void Update()
    {
        // 에디터에서 자식 수가 바뀌었을 때 대비
        if (_cubes == null || _cubes.Length != transform.childCount)
            CacheChildren();

#if UNITY_EDITOR
        float t = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
        float t = Time.time;
#endif

        for (int i = 0; i < _cubes.Length; i++)
        {
            if (_cubes[i] == null) continue;

            Vector3 p = _cubes[i].localPosition;
            p.y = _baseY[i] + Mathf.Sin(t * speed + i * phaseStep) * amplitude;
            _cubes[i].localPosition = p;

            if (spin)
                _cubes[i].Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        }
    }
}
