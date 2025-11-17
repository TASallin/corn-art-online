using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text slotNumberText;
    public TMP_Text saveDateText;
    public TMP_Text levelNameText;
    public TMP_Text playersCountText;
    public TMP_Text firstPlayerNameText; // NEW: Add this field for showing first player name
    public GameObject emptyStateObject;
    public GameObject dataStateObject;
    public Button mainButton;
    public Button deleteButton;

    [HideInInspector]
    public UnityEvent onSlotClicked = new UnityEvent();

    [HideInInspector]
    public UnityEvent onDeleteClicked = new UnityEvent();

    private int slotIndex;
    private bool isAutoSave;

    private void Start()
    {
        if (mainButton != null)
            mainButton.onClick.AddListener(OnMainButtonClicked);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
    }

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
        isAutoSave = (index == -1);

        if (slotNumberText != null)
        {
            if (isAutoSave)
            {
                slotNumberText.text = "Auto";
            } else
            {
                // Format as 2-digit number: 01, 02, 03, etc.
                slotNumberText.text = (slotIndex + 1).ToString("00");
            }
        }
    }

    public void UpdateUI(SaveLoadManager.GameSaveData saveData, bool isLoadMode)
    {
        bool hasData = (saveData != null);

        if (emptyStateObject != null)
            emptyStateObject.SetActive(!hasData);

        if (dataStateObject != null)
            dataStateObject.SetActive(hasData);

        if (deleteButton != null)
            deleteButton.gameObject.SetActive(hasData);

        if (!hasData)
        {
            if (slotNumberText != null)
            {
                if (isAutoSave)
                {
                    slotNumberText.text = "Auto";
                } else
                {
                    slotNumberText.text = (slotIndex + 1).ToString("00");
                }
            }

            if (firstPlayerNameText != null)
                firstPlayerNameText.text = ""; // Clear first player name when empty

            if (mainButton != null)
                mainButton.interactable = !isLoadMode;
            return;
        }

        // Update with save data
        if (saveDateText != null)
            saveDateText.text = saveData.saveDateTime;

        if (levelNameText != null)
            levelNameText.text = saveData.selectedLevel;

        if (playersCountText != null)
            playersCountText.text = $"{saveData.playerNames.Length} players";

        // NEW: Show first player name if available
        if (firstPlayerNameText != null)
        {
            if (saveData.playerNames != null && saveData.playerNames.Length > 0)
            {
                firstPlayerNameText.text = saveData.playerNames[0];
            } else
            {
                firstPlayerNameText.text = "";
            }
        }

        if (mainButton != null)
            mainButton.interactable = true;
    }

    private void OnMainButtonClicked()
    {
        onSlotClicked.Invoke();
    }

    private void OnDeleteButtonClicked()
    {
        onDeleteClicked.Invoke();
    }
}