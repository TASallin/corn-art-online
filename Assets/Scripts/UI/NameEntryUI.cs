using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NameEntryUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_InputField editInput;
    public Button editButton;
    public Button deleteButton;
    public Image[] backgroundImages; // Array of images to turn yellow when duplicate
    public AudioClip deleteClip;
    public AudioClip editClip;
    public AudioClip confirmClip;
    public AudioClip errorClip;

    [Header("Duplicate Settings")]
    public Color normalColor = Color.white;
    public Color duplicateColor = Color.yellow;

    private NameEntryManager manager;
    private string currentName;
    private bool isEditing = false;

    public void Initialize(string name, NameEntryManager entryManager)
    {
        currentName = name;
        manager = entryManager;

        nameText.text = currentName;
        editInput.text = currentName;
        editInput.gameObject.SetActive(false);

        editButton.onClick.AddListener(ToggleEdit);
        deleteButton.onClick.AddListener(OnDelete);
        editInput.onSubmit.AddListener(OnEditSubmit);

        // Set initial colors
        SetBackgroundColor(normalColor);
    }

    public void ToggleEdit()
    {
        isEditing = !isEditing;

        if (isEditing)
        {
            nameText.gameObject.SetActive(false);
            editInput.gameObject.SetActive(true);
            editInput.text = currentName;
            editInput.ActivateInputField();
            UISfx.Instance.PlayUIAudio(editClip);
        } else
        {
            SaveEdit(editInput.text);
        }
    }

    public void OnEditSubmit(string newName)
    {
        SaveEdit(newName);
    }

    private void SaveEdit(string newName)
    {
        if (!string.IsNullOrEmpty(newName))
        {
            currentName = newName;
            nameText.text = currentName;
            manager.UpdateEntryName(this, currentName);
            UISfx.Instance.PlayUIAudio(confirmClip);
        } else
        {
            UISfx.Instance.PlayUIAudio(errorClip);
        }

        isEditing = false;
        nameText.gameObject.SetActive(true);
        editInput.gameObject.SetActive(false);
        if (editButton.GetComponentInChildren<TMP_Text>() != null)
        {
            //editButton.GetComponentInChildren<TMP_Text>().text = "Edit";
        }
    }

    public void OnDelete()
    {
        UISfx.Instance.PlayUIAudio(deleteClip);
        manager.RemoveEntry(this);
    }

    public void SetDuplicateStatus(bool isDuplicate)
    {
        SetBackgroundColor(isDuplicate ? duplicateColor : normalColor);
    }

    private void SetBackgroundColor(Color color)
    {
        foreach (var image in backgroundImages)
        {
            if (image != null)
            {
                image.color = color;
            }
        }
    }
}