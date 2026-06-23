using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TMPThreeColorGradient : MonoBehaviour
{
    [SerializeField] private Color colorLeft = new Color32(0x73, 0xCF, 0x79, 0xFF); // #73CF79
    [SerializeField] private Color colorMiddle = new Color32(0x50, 0xB8, 0xB8, 0xFF); // #50B8B8
    [SerializeField] private Color colorRight = new Color32(0x39, 0x78, 0xB8, 0xFF); // #3978B8

    private TMP_Text tmpText;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        tmpText.ForceMeshUpdate();
        ApplyGradient();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();
        if (tmpText == null) return;

        // Edit 모드에서 즉시 반영되도록 한 프레임 미뤄서 호출
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || tmpText == null) return; // 오브젝트가 삭제됐을 경우 대비
            tmpText.ForceMeshUpdate();
            ApplyGradient();
        };
    }
#endif

    // 텍스트가 런타임에 바뀌는 경우(타이틀이 동적으로 세팅될 때) 이 함수를 호출해주세요.
    public void ApplyGradient()
    {
        tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmpText.textInfo;

        int charCount = textInfo.characterCount;
        if (charCount == 0) return;

        // 공백 등 보이지 않는 문자를 제외한 "보이는 글자 수" 기준으로 비율 계산
        int visibleCount = 0;
        for (int i = 0; i < charCount; i++)
            if (textInfo.characterInfo[i].isVisible) visibleCount++;

        if (visibleCount <= 1) visibleCount = 1; // 0으로 나누기 방지

        int visibleIndex = 0;

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            // 0~1 사이 비율 (글자가 가로상 어느 지점에 있는지)
            float t = visibleCount == 1 ? 0f : (float)visibleIndex / (visibleCount - 1);
            visibleIndex++;

            Color charColor = EvaluateThreeColor(t);

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            // 글자 사각형의 네 꼭짓점 모두 같은 색 (가로 그라데이션이므로 글자 내부는 단색)
            vertexColors[vertexIndex + 0] = charColor;
            vertexColors[vertexIndex + 1] = charColor;
            vertexColors[vertexIndex + 2] = charColor;
            vertexColors[vertexIndex + 3] = charColor;
        }

        // 계산한 정점 색을 실제 메쉬에 반영
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    private Color EvaluateThreeColor(float t)
    {
        if (t <= 0.5f)
            return Color.Lerp(colorLeft, colorMiddle, t / 0.5f);
        else
            return Color.Lerp(colorMiddle, colorRight, (t - 0.5f) / 0.5f);
    }
}