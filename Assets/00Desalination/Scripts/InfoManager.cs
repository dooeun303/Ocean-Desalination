using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 정보공유 팝업 관리 스크립트
// 서버에서 파일 목록 받아서 아이템 생성
public class InfoShareManager : MonoBehaviour
{
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
        FileData[] files = null;
        yield return FileApi.List(type, data => files = data,
            e => Debug.LogError("[InfoShareManager] 파일 목록 로드 실패: " + e));
        if (files == null) yield break;

        // 기존 아이템 제거
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        // 아이템 생성
        foreach (var file in files)
        {
            var go = Instantiate(itemPrefab, itemContainer);
            var icon = go.GetComponent<FileIconItem>();
            if (icon != null)
            {
                icon.rootCanvas = rootCanvas;
                icon.SetData(file.id, file.name, file.url);

                // 이미지가 있으면 다운로드해서 표시(상대경로면 ApiClient가 서버 주소를 붙인다)
                if (!string.IsNullOrEmpty(file.url))
                    StartCoroutine(LoadImage(file.url, icon));
            }
        }

        Debug.Log($"[InfoShareManager] 파일 {files.Length}개 로드 완료");
    }

    // ─────────────────────────────────────────
    // 이미지 다운로드
    // ─────────────────────────────────────────
    private IEnumerator LoadImage(string url, FileIconItem icon)
    {
        Texture2D texture = null;
        yield return ApiClient.GetTexture(url, t => texture = t,
            e => Debug.LogWarning("[InfoShareManager] 이미지 로드 실패: " + e));
        if (texture == null) yield break;

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