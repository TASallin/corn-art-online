using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

public class SaveLoadPanelUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panelRoot;
    public Button closeButton;

    [Header("Mode")]
    public bool isLoadMode = false;

    [Header("Save Slots")]
    public SaveSlotUI[] normalSlots = new SaveSlotUI[5];
    public SaveSlotUI autoSaveSlot;

    [Header("UI References")]
    public TMP_Text panelTitleText;
    public Button autoSaveToggleButton;
    public RectTransform slotsContainer; // The container for all slots

    [Header("Layout Settings")]
    public float defaultSlotHeight = 100f;
    public float slotSpacing = 10f;
    public float defaultContainerHeight = 550f; // Set this to your normal container height
    public float topPadding = 10f; // Space from the top of container

    [Header("Confirmation")]
    public ConfirmationDialog confirmationDialog;

    public AudioClip saveClip;
    public AudioClip cancelClip;
    public AudioClip closeClip;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseButtonClick);

        if (autoSaveToggleButton != null)
            autoSaveToggleButton.onClick.AddListener(ToggleAutoSaveVisibility);

        InitializePanel();
    }

    public void OpenAsSavePanel()
    {
        isLoadMode = false;
        OpenPanel();
    }

    public void OpenAsLoadPanel()
    {
        isLoadMode = true;
        OpenPanel();
    }

    private void OpenPanel()
    {
        panelRoot.SetActive(true);
        RefreshPanel();
    }

    public void ClosePanel()
    {
        panelRoot.SetActive(false);
    }

    public void CloseButtonClick()
    {
        UISfx.Instance.PlayUIAudio(closeClip);
        ClosePanel();
    }

    private void InitializePanel()
    {
        // Set up each normal slot
        for (int i = 0; i < normalSlots.Length; i++)
        {
            if (normalSlots[i] != null)
            {
                int slotIndex = i; // Local variable for closure
                normalSlots[i].SetSlotIndex(i);
                normalSlots[i].onSlotClicked.AddListener(() => OnSlotClicked(slotIndex, false));
                normalSlots[i].onDeleteClicked.AddListener(() => OnDeleteClicked(slotIndex, false));
            }
        }

        // Set up autosave slot
        if (autoSaveSlot != null)
        {
            autoSaveSlot.SetSlotIndex(-1); // Special index for autosave
            autoSaveSlot.onSlotClicked.AddListener(() => OnSlotClicked(0, true));
            autoSaveSlot.onDeleteClicked.AddListener(() => OnDeleteClicked(0, true));
        }

        ClosePanel(); // Start with panel closed
    }

    private void RefreshPanel()
    {
        List<SaveSlotUI> visibleSlots = new List<SaveSlotUI>();
        bool hasAnySaveFile = false;

        // Process normal slots
        for (int i = 0; i < normalSlots.Length; i++)
        {
            if (normalSlots[i] != null)
            {
                SaveLoadManager.GameSaveData saveData = SaveLoadManager.Instance.GetSaveInfo(i);

                // In save mode, always show slots. In load mode, only show slots with data
                bool showSlot = !isLoadMode || saveData != null;
                normalSlots[i].gameObject.SetActive(showSlot);

                if (showSlot)
                {
                    normalSlots[i].UpdateUI(saveData, isLoadMode);
                    visibleSlots.Add(normalSlots[i]);

                    if (saveData != null)
                        hasAnySaveFile = true;
                }
            }
        }

        // Handle autosave slot visibility
        if (autoSaveSlot != null)
        {
            SaveLoadManager.GameSaveData autoSaveData = SaveLoadManager.Instance.GetSaveInfo(0, true);
            bool showAutoSave = isLoadMode && autoSaveData != null;
            autoSaveSlot.gameObject.SetActive(showAutoSave);

            if (showAutoSave)
            {
                autoSaveSlot.UpdateUI(autoSaveData, true);
                visibleSlots.Add(autoSaveSlot);
                hasAnySaveFile = true;
            }
        }

        // Update panel title
        if (panelTitleText != null)
        {
            if (isLoadMode && !hasAnySaveFile)
            {
                panelTitleText.text = "No Save Data Available";
            } else
            {
                panelTitleText.text = isLoadMode ? "Load Game" : "Save Game";
            }
        }

        // Show/hide autosave toggle button
        if (autoSaveToggleButton != null)
            autoSaveToggleButton.gameObject.SetActive(isLoadMode && SaveLoadManager.Instance.AutoSaveExists());

        // Rearrange visible slots
        RearrangeVisibleSlots(visibleSlots);
    }

    private void RearrangeVisibleSlots(List<SaveSlotUI> visibleSlots)
    {
        if (slotsContainer == null)
            return;

        // Always reset the container to its default height first
        slotsContainer.sizeDelta = new Vector2(slotsContainer.sizeDelta.x, defaultContainerHeight);

        if (visibleSlots.Count == 0)
            return;

        // Sort slots by their index (autosave should be at the TOP now)
        visibleSlots = visibleSlots.OrderBy(slot => {
            if (slot == autoSaveSlot) return -1; // Changed from int.MaxValue to -1
            int index = Array.IndexOf(normalSlots, slot);
            return index >= 0 ? index : int.MaxValue;
        }).ToList();

        // The rest of the method remains the same...
        float slotHeight = visibleSlots[0].GetComponent<RectTransform>().rect.height;
        if (slotHeight < 10f) slotHeight = defaultSlotHeight;

        // Position slots from top to bottom
        for (int i = 0; i < visibleSlots.Count; i++)
        {
            RectTransform rt = visibleSlots[i].GetComponent<RectTransform>();

            // Position from top of container + padding
            float topPosition = topPadding + i * (slotHeight + slotSpacing);

            // Anchor to top-center of container and position
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -topPosition);

            // Ensure size is correct
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, slotHeight);
        }

        // The container resizing logic remains the same...
        if (isLoadMode && visibleSlots.Count < normalSlots.Length / 2)
        {
            float totalNeededHeight = visibleSlots.Count * (slotHeight + slotSpacing) + topPadding;
            float minHeight = 200f;

            float newHeight = Mathf.Max(totalNeededHeight, minHeight);
            newHeight = Mathf.Min(newHeight, defaultContainerHeight);

            slotsContainer.sizeDelta = new Vector2(slotsContainer.sizeDelta.x, newHeight);
        } else
        {
            slotsContainer.sizeDelta = new Vector2(slotsContainer.sizeDelta.x, defaultContainerHeight);
        }
    }

    private void OnSlotClicked(int slotIndex, bool isAutoSave)
    {
        if (isLoadMode)
        {
            UISfx.Instance.PlayUIAudio(saveClip);
            LoadFromSlot(slotIndex, isAutoSave);
        } else
        {
            UISfx.Instance.PlayUIAudio(saveClip);
            SaveToSlot(slotIndex);
        }
    }

    private void OnDeleteClicked(int slotIndex, bool isAutoSave)
    {
        string saveType = isAutoSave ? "autosave" : $"save slot {slotIndex + 1}";
        UISfx.Instance.PlayUIAudio(cancelClip);

        if (confirmationDialog != null)
        {
            confirmationDialog.Show(
                $"Really delete {saveType}?",
                () => {
                    SaveLoadManager.Instance.DeleteSave(slotIndex, isAutoSave);
                    RefreshPanel();
                }
            );
        } else
        {
            // Fallback if no dialog is assigned
            SaveLoadManager.Instance.DeleteSave(slotIndex, isAutoSave);
            RefreshPanel();
        }
    }

    private void SaveToSlot(int slotIndex)
    {
        SaveLoadManager.Instance.SaveGame(slotIndex);
        RefreshPanel();
    }

    private void LoadFromSlot(int slotIndex, bool isAutoSave)
    {
        bool success = SaveLoadManager.Instance.LoadGame(slotIndex, isAutoSave);
        if (success)
        {
            ClosePanel();
            // Broadcast an event to update all UI elements
            RefreshAllUIElements();
        }
    }

    private void ToggleAutoSaveVisibility()
    {
        if (autoSaveSlot != null && isLoadMode)
        {
            autoSaveSlot.gameObject.SetActive(!autoSaveSlot.gameObject.activeSelf);
            RefreshPanel(); // Refresh to update the layout
        }
    }

    private void RefreshAllUIElements()
    {
        Debug.Log($"[SaveLoadPanelUI] RefreshAllUIElements - Current MenuSettings.numberOfWinners: {MenuSettings.Instance.numberOfWinners}");
        Debug.Log($"[SaveLoadPanelUI] RefreshAllUIElements - Player count: {MenuSettings.Instance.playerNames.Length}");

        // Find and refresh the level dropdown
        LevelSelectUI levelSelectUI = FindObjectOfType<LevelSelectUI>();
        if (levelSelectUI != null && !string.IsNullOrEmpty(MenuSettings.Instance.selectedLevel))
        {
            levelSelectUI.SelectLevel(MenuSettings.Instance.selectedLevel);
        }

        // FIRST: Load the names to update the maximum winners limit
        NameEntryManager nameEntryManager = FindObjectOfType<NameEntryManager>();
        if (nameEntryManager != null)
        {
            nameEntryManager.RefreshFromMenuSettings();
        }

        // THEN: Refresh the number of winners UI (after the max has been updated)
        NumberOfWinnersUI winnersUI = FindObjectOfType<NumberOfWinnersUI>();
        if (winnersUI != null)
        {
            if (MenuSettings.Instance.IsWheelOfDeathMode())
            {
                Debug.Log($"[SaveLoadPanelUI] Found NumberOfWinnersUI, calling SetValue with: {MenuSettings.Instance.numberOfDeaths}");
                winnersUI.SetValue(MenuSettings.Instance.numberOfDeaths);
            } else
            {
                Debug.Log($"[SaveLoadPanelUI] Found NumberOfWinnersUI, calling SetValue with: {MenuSettings.Instance.numberOfWinners}");
                winnersUI.SetValue(MenuSettings.Instance.numberOfWinners);
            }
        } else
        {
            Debug.LogWarning("[SaveLoadPanelUI] NumberOfWinnersUI not found!");
        }

        MusicSettingsUI musicUI = FindObjectOfType<MusicSettingsUI>();
        if (musicUI != null)
        {
            musicUI.RefreshDropdowns();
            Debug.Log("Music dropdowns refreshed");
        }

        MiscRecruitMeSettingsUI miscUI = FindObjectOfType<MiscRecruitMeSettingsUI>();
        if (miscUI != null)
        {
            miscUI.RefreshToggles();
        }
    }

    public void SetSlotActive(SaveSlotUI slot, bool active)
    {
        if (slot != null && slot.gameObject != null)
        {
            slot.gameObject.SetActive(active);
        }
    }

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(SaveLoadPanelUI))]
public class SaveLoadPanelUIEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        UnityEditor.EditorGUILayout.LabelField("Save/Load Panel Settings", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif
}