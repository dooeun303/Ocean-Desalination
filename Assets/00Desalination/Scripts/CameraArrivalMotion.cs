using System.Collections;
using UnityEngine;

// 지원요청 수락 → 설비 앞 텔레포트 → 페이드 인 직후, 카메라가 설비 앞에 "이쁘게 자리잡는" 짧은 모션.
// XR Rig(플레이어 위치)는 그대로 두고 카메라의 로컬 트랜스폼만 살짝 뒤/위에서 시작해 제자리로 이즈인.
//
// ※ Main Camera 자체엔 보통 TrackedPoseDriver가 있어서(XRI 스타터셋) 매 프레임 로컬 포즈를
//   덮어쓴다 - 여기에 붙이면 모션이 안 보인다.
//   → XRI 기준: "XR Origin (XR Rig) / Camera Offset" (Main Camera의 부모) 에 붙일 것.
//     Camera Offset은 트래킹이 안 걸려서 안전하고, 헤드 트래킹과 자연스럽게 합성된다.
//   SupportCallList는 Camera.main.GetComponentInParent<CameraArrivalMotion>() 로 찾으므로
//   부모(Camera Offset)에 있어도 자동 연결된다.
public class CameraArrivalMotion : MonoBehaviour
{
    [SerializeField] float duration = 3.8f;
    [SerializeField] float pullBack = 0.9f;      // 시작 시 시선의 뒤쪽으로
    [SerializeField] float riseUp = 0.3f;        // 시작 시 위(+up)로
    // (startLookDownDeg 제거 - Camera Offset을 숙이면 고개 돌린 상태에서 사선/기울기로 보였다)
    // ⚠ 0 이 아니면 Camera Offset 에 상시 pitch 가 남아 헤드트래킹과 합성될 때 멀미를 유발한다.
    //   "끝나면 설비를 내려다보는 느낌"은 텔레포트 회전(targetPoint)을 설비 쪽으로 살짝 숙여 잡는 게 안전.
    [SerializeField] float endLookDownDeg = 0f;   // 모션 종료 후 유지할 아래 각도(권장 0)

    Vector3 _restPos;
    Quaternion _restRot;
    bool _captured;
    Coroutine _co;

    // 처음 Play 시점의 로컬 포즈를 "제자리"로 기억하고, 이후엔 항상 그 자리로 복귀한다.
    public void Play()
    {
        if (!_captured)
        {
            _restPos = transform.localPosition;
            _restRot = transform.localRotation;
            _captured = true;
        }
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Settle());
    }

    IEnumerator Settle()
    {
        // 2026-10-20: "회전하니까 사선으로 움직여" - 예전엔 XR Rig의 앞뒤로 당기고 Camera Offset을 숙였는데,
        // 헤드셋 사용자가 고개를 조금만 돌려도 그 축이 시선과 어긋나 비스듬히 미끄러지고 기울어 보였다.
        // 이제 회전은 하지 않고, 지금 실제 시선(수평)의 정반대 방향으로 물러났다가 똑바로 다가온다.
        Quaternion endRot = _restRot * Quaternion.Euler(endLookDownDeg, 0f, 0f);
        Vector3 startPos = _restPos + LocalBackAndUp();
        transform.localRotation = endRot;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            k = 1f - Mathf.Pow(1f - k, 3f); // ease-out cubic
            transform.localPosition = Vector3.LerpUnclamped(startPos, _restPos, k);
            yield return null;
        }
        transform.localPosition = _restPos;
        _co = null;
    }

    // 시선의 수평 뒤쪽 pullBack + 월드 위쪽 riseUp 을 이 오브젝트 부모의 로컬 좌표로.
    Vector3 LocalBackAndUp()
    {
        var cam = Camera.main;
        Vector3 fwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : Vector3.zero;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 worldOffset = -fwd.normalized * pullBack + Vector3.up * riseUp;
        return transform.parent != null ? transform.parent.InverseTransformVector(worldOffset) : worldOffset;
    }
}
