using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// 정보공유 팝업 관리 스크립트
// 서버에서 파일 목록 받아서 아이템 생성
public class InfoShareManager : MonoBehaviour
{
    public string serverUrl => ServerConfig.BaseUrl + "/api/files";

    [Header("UI 연결")]
    public Transform itemContainer;   // 아이템 컨테이너
    public GameObject itemPrefab;      // 정보공유아이템 프리팹
    public Canvas rootCanvas;      // 루트 캔버스 (드래그용)

    private string _currentType = "general"; // 현재 선택된 타입

    void OnEnable()
    {
        LoadByType(_currentType);
    }

    // 탭 버튼에서 호출
    public void OnTabGeneral() => LoadByType("general");
    public void OnTabScreenshot() => LoadByType("screenshot");
    public void OnTabPump() => LoadByType("pump");
    public void OnTabValve() => LoadByType("valve");

    private void LoadByType(string type)
    {
        _currentType = type;
        StartCoroutine(LoadFiles(type));
    }

    // ─────────────────────────────────────────
    // 파일 목록 로드
    // ─────────────────────────────────────────
    private IEnumerator LoadFiles(string type = "general")
    {
        string url = string.IsNullOrEmpty(type) ? serverUrl : $"{serverUrl}?type={type}";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[InfoShareManager] 파일 목록 로드 실패: " + req.error);
            yield break;
        }

        var response = JsonUtility.FromJson<FileListResponse>(req.downloadHandler.text);
        if (!response.success || response.data == null)
        {
            Debug.LogWarning("[InfoShareManager] 파일 데이터 없음");
            yield break;
        }

        // 기존 아이템 제거
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        // 아이템 생성
        foreach (var file in response.data)
        {
            var go = Instantiate(itemPrefab, itemContainer);
            var icon = go.GetComponent<FileIconItem>();
            if (icon != null)
            {
                icon.rootCanvas = rootCanvas;
                icon.SetData(file.id, file.name, file.url);

                // 이미지가 있으면 다운로드해서 표시
                if (!string.IsNullOrEmpty(file.url))
                {
                    // 상대경로면 서버 주소 붙이기
                    string imageUrl = file.url.StartsWith("http")
                        ? file.url
                        : serverUrl.Replace("/api/files", "") + file.url;
                    StartCoroutine(LoadImage(imageUrl, icon));
                }
            }
        }

        Debug.Log($"[InfoShareManager] 파일 {response.data.Length}개 로드 완료");
    }

    // ─────────────────────────────────────────
    // 이미지 다운로드
    // ─────────────────────────────────────────
    private IEnumerator LoadImage(string url, FileIconItem icon)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("[InfoShareManager] 이미지 로드 실패: " + req.error);
            yield break;
        }

        var texture = DownloadHandlerTexture.GetContent(req);
        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        icon.SetData(icon.FileId, icon.FileName, icon.FileUrl, sprite);
    }
}

// ─────────────────────────────────────────
// 데이터 모델
// ─────────────────────────────────────────
[System.Serializable]
public class FileData
{
    public string id;
    public string name;
    public string url;
}

[System.Serializable]
public class FileListResponse
{
    public bool success;
    public FileData[] data;
}