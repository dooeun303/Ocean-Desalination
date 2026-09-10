using UnityEditor;
using UnityEngine;
using GLTFast.Export;

// 씬(MR.unity)에 이미 배치돼있는 실제 설비 모델(3dmodelfbx.fbx에서 온 pump1/pump2 등)을 glTFast의
// GLB 익스포트 기능으로 파일로 뽑아내는 일회성 에디터 도구. 모델 회전 테스트 패널에서 ToyCar 샘플
// 대신 실제 펌프 모델을 쓰기 위해 필요 - glTFast는 GLB만 URL로 불러올 수 있고 FBX는 못 읽으므로,
// 씬에 있는 FBX 인스턴스를 GLB로 한 번 구워서 서버에 올려두는 방식.
// 프로젝트 루트에 pump_export.glb로 저장한다(Assets 밖 - Unity가 애셋으로 잡지 않게).
//
// GameObject.Find("pump1")로 이름 검색했더니 동명의 엉뚱한 오브젝트("펌프1 패널 가이드"라는 2D
// 가이드 UI를 자식으로 물고 있는, 실제 설비 메시가 아닌 것)를 잘못 찾아왔다(실기 확인 - 메시 4개만
// 나옴, 원래는 30개 이상). Find는 동명 오브젝트 중 아무거나 하나를 잡을 수 있어 신뢰할 수 없으므로,
// 하이러키에서 직접 선택한 오브젝트만 내보내도록 바꿨다 - Tools 메뉴 실행 전에 Hierarchy에서
// 실제 pump1/pump2(설비 메시가 있는 쪽)를 직접 선택해야 한다.
static class PumpGltfExporter
{
    [MenuItem("Tools/Export Pump To GLB")]
    static async void ExportPump()
    {
        var roots = Selection.gameObjects;

        if (roots == null || roots.Length == 0)
        {
            Debug.LogError("[PumpGltfExporter] Hierarchy에서 내보낼 오브젝트를 먼저 선택하세요 (실제 설비 메시가 있는 pump1/pump2 등).");
            return;
        }

        Debug.Log("[PumpGltfExporter] 선택된 오브젝트: " + string.Join(", ", System.Array.ConvertAll(roots, g => g.name)));

        // ExportSettings.Format 기본값이 Json이라, .glb 확장자로 저장해도 실제 내용은 JSON+외부 .bin
        // 파일로 나가서(진짜 바이너리 GLB가 아님 - magic 헤더가 "glTF"가 아니라 "{"로 시작) 로딩 쪽
        // glTFast가 바이너리로 파싱을 시도하다 실패했다(실기 확인). Binary로 명시해야 진짜 .glb가 됨.
        var exportSettings = new ExportSettings { Format = GltfFormat.Binary };
        var export = new GameObjectExport(exportSettings);
        export.AddScene(roots, "Pump");

        string path = System.IO.Path.Combine(Application.dataPath, "..", "pump_export.glb");
        bool success = await export.SaveToFileAndDispose(path);

        if (success)
            Debug.Log($"[PumpGltfExporter] 익스포트 완료: {path}");
        else
            Debug.LogError("[PumpGltfExporter] 익스포트 실패");
    }
}
