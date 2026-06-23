using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CreateRemotePopup
{
    [MenuItem("Tools/원격점검 팝업 자동 생성")]
    public static void CreatePopup()
    {
        var existing = Object.FindObjectOfType<MaintenanceRegisterPopup>(true);
        if (existing == null) {
            Debug.LogError("[CreateRemotePopup] MaintenanceRegisterPopup을 씬에서 찾을 수 없습니다. 복제 대상을 열어주세요.");
            return;
        }

        GameObject newPopup = Object.Instantiate(existing.gameObject, existing.transform.parent);
        newPopup.name = "RemoteInspectionPopup_Root";
        
        var oldComp = newPopup.GetComponent<MaintenanceRegisterPopup>();
        var newComp = newPopup.AddComponent<RemoteInspectionRegisterPopup>();

        // Unity의 Instantiate는 내부 계층의 참조를 자동으로 새 복제본으로 재매핑해 줍니다.
        // 따라서 oldComp가 참조하고 있는 객체들은 이미 복제본(newPopup) 내부의 객체들입니다.
        newComp.popupPanel = oldComp.popupPanel;
        newComp.successPanel = oldComp.successPanel;
        newComp.equipmentDropdown = oldComp.equipmentDropdown;
        newComp.technicianDropdown = oldComp.technicianDropdown;
        newComp.scheduledAtInput = oldComp.scheduledAtInput;
        newComp.registerButton = oldComp.registerButton;
        newComp.closeButton = oldComp.closeButton;
        
        // 원격점검 테이블 컨트롤러 자동 연결
        var remoteTable = Object.FindObjectOfType<RemoteInspectionTableController>(true);
        if (remoteTable != null)
        {
            newComp.tableController = remoteTable;
        }

        // 사용하지 않는 입력 필드 비활성화
        if (oldComp.workTypeDropdown != null)
        {
            oldComp.workTypeDropdown.transform.parent.gameObject.SetActive(false);
        }
        if (oldComp.manualDropdown != null)
        {
            oldComp.manualDropdown.transform.parent.gameObject.SetActive(false);
        }

        // 기존 컴포넌트 삭제
        Object.DestroyImmediate(oldComp);

        // 변경 사항 저장 표시
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[CreateRemotePopup] 원격점검 팝업이 성공적으로 복제 및 생성되었습니다! 하이라키를 확인해주세요.");
    }
}
