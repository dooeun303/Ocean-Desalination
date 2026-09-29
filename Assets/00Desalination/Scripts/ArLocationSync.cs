using UnityEngine;

// AR의 위치를 디지털 트윈에 반영한다. 두 신호를 함께 쓴다:
//  - ar_location (QR 스캔 시점, 드물게): "그 순간 AR 로컬 좌표 = 이 설비의 트윈 좌표"라는 보정점.
//    이 순간에 맞춰 해당 설비에 링+사람모형을 스냅시키고(EquipmentMarker.ShowArLocation),
//    이후 ar_pose를 트윈 좌표로 변환할 오프셋을 갱신한다.
//  - ar_pose (0.3초 간격, AR 6DoF 카메라 로컬 좌표): 보정 오프셋을 더해 트윈 좌표로 바꾼 뒤,
//    별도의 "실시간 위치" 사람모형을 그 자리로 계속 옮긴다. QR을 또 스캔할 때마다 오프셋이
//    재계산되므로 SLAM 드리프트도 자연히 재보정된다.
//
// MR 1.unity 처럼 EquipmentMarker들이 있는 씬에 아무 오브젝트에나 붙여두면 됨.
public class ArLocationSync : MonoBehaviour
{
    [Tooltip("보정 전(ar_location 한 번도 안 옴) 상태에서 실시간 위치 표시를 만들지 여부")]
    [SerializeField] private Color liveFigureColor = new Color(1f, 0.55f, 0.1f, 1f);

    private bool _calibrated;
    private Vector3 _offset; // 트윈좌표 = ar_pose 로컬좌표 + _offset
    private Vector3 _lastRawPose;
    private bool _hasRawPose;

    private Transform _liveFigure;

    private void OnEnable()
    {
        VideoCallSignalingMR.OnArLocationReceived += HandleArLocation;
        VideoCallSignalingMR.OnArPoseReceived += HandleArPose;
        Debug.Log(nameof(ArLocationSync) + ": 구독 시작됨");
    }

    private void OnDisable()
    {
        VideoCallSignalingMR.OnArLocationReceived -= HandleArLocation;
        VideoCallSignalingMR.OnArPoseReceived -= HandleArPose;

        if (_liveFigure != null) { Destroy(_liveFigure.gameObject); _liveFigure = null; }
        _calibrated = false;
        _hasRawPose = false;
    }

    private void HandleArLocation(string equipmentId)
    {
        var marker = EquipmentMarker.Find(equipmentId);
        if (marker == null)
        {
            Debug.LogWarning(nameof(ArLocationSync) + $": equipmentId={equipmentId} 에 해당하는 EquipmentMarker를 씬에서 못 찾음");
            return;
        }

        marker.ShowArLocation();
        Debug.Log(nameof(ArLocationSync) + $": {marker.name} 에 AR 위치 표시 켬");

        // 보정: 가장 최근 ar_pose 로컬 좌표가 있어야 오프셋을 계산할 수 있다.
        // 아직 ar_pose를 한 번도 못 받았으면(예: 6DoF 아직 준비 안 됨) 이번 스캔으론 보정 불가 -
        // 다음 ar_pose가 오면 그때 값으로 재시도하지 않고, 다음 QR 스캔을 기다린다(단순함 우선).
        if (_hasRawPose)
        {
            _offset = marker.transform.position - _lastRawPose;
            _calibrated = true;
            Debug.Log(nameof(ArLocationSync) + $": 보정 완료. offset={_offset}");
        }
        else
        {
            Debug.LogWarning(nameof(ArLocationSync) + ": 아직 ar_pose를 한 번도 못 받아서 보정 불가 (6DoF 준비 전일 수 있음)");
        }
    }

    private void HandleArPose(Vector3 rawLocalPos)
    {
        _lastRawPose = rawLocalPos;
        _hasRawPose = true;

        if (!_calibrated) return; // 아직 한 번도 QR을 안 찍어서 트윈 좌표로 바꿀 기준이 없음

        Vector3 twinPos = rawLocalPos + _offset;

        if (_liveFigure == null)
        {
            _liveFigure = EquipmentMarker.BuildSimpleFigure(liveFigureColor).transform;
            _liveFigure.gameObject.name = "ArLiveWorkerFigure";
        }

        _liveFigure.position = twinPos;
    }
}
