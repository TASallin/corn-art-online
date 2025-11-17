using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ConfirmationDialog : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialogPanel;
    public TMP_Text messageText;
    public Button confirmButton;
    public Button cancelButton;
    public AudioClip cancelClip;
    public AudioClip confirmClip;

    private Action onConfirm;
    private Action onCancel;
    
    private void Awake()
    {
        // If dialogPanel is not assigned, DON'T default to gameObject
        // This is likely the issue - the component itself is inactive
        
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        // Hide dialog panel on start (not the component's gameObject)
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }

    public void Show(string message, Action confirmAction, Action cancelAction = null)
    {
        messageText.text = message;
        onConfirm = confirmAction;
        onCancel = cancelAction;
        dialogPanel.SetActive(true);
    }

    public void Hide()
    {
        dialogPanel.SetActive(false);
        onConfirm = null;
        onCancel = null;
    }

    private void OnConfirmClicked()
    {
        UISfx.Instance.PlayUIAudio(confirmClip);
        onConfirm?.Invoke();
        Hide();
    }

    private void OnCancelClicked()
    {
        UISfx.Instance.PlayUIAudio(cancelClip);
        onCancel?.Invoke();
        Hide();
    }
}