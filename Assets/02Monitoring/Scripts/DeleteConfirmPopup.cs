using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DeleteConfirmPopup : MonoBehaviour
{
    public GameObject panelRoot;
    public TMP_Text titleText;
    public TMP_Text messageText;
    
    public Button cancelButton;
    public Button confirmButton;

    private Action _onConfirm;
    private Action _onCancel;

    private void Awake()
    {
        // 팝업 패널에 Fade-Up 애니메이션 자동 부착
        UIFadeUp.ApplyTo(panelRoot);
    }

    private void Start()
    {
        cancelButton?.onClick.AddListener(OnCancelClicked);
        confirmButton?.onClick.AddListener(OnConfirmClicked);
        Hide();
    }

    public void Show(string title, string message, Action onConfirm, Action onCancel = null)
    {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;

        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (panelRoot != null) panelRoot.SetActive(true);
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // 최상단 노출
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnCancelClicked()
    {
        _onCancel?.Invoke();
        Hide();
    }

    private void OnConfirmClicked()
    {
        _onConfirm?.Invoke();
        Hide();
    }
}
