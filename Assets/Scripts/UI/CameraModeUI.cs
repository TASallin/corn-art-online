using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CameraModeUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown cameraModeDropdown;
    [SerializeField] private GameObject manualControlsGraphic;
    public AudioClip selectClip;

    [Header("Camera Reference")]
    [SerializeField] private CameraController cameraController;

    private CameraMode lastKnownMode = CameraMode.Fixed;

    private void Start()
    {
        // Try to find camera controller if not assigned
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
            if (cameraController == null)
            {
                Debug.LogError("CameraController not found! Please assign it in the inspector.");
                return;
            }
        }

        SetupDropdown();

        if (cameraModeDropdown != null)
        {
            cameraModeDropdown.onValueChanged.AddListener(OnCameraModeChanged);
        }

        // Apply saved preference
        ApplySavedCameraMode();
    }

    private void SetupDropdown()
    {
        if (cameraModeDropdown == null) return;

        cameraModeDropdown.ClearOptions();

        // Add camera mode options
        List<string> options = new List<string>
        {
            "Fixed",
            "Dynamic",
            "Manual"
        };

        cameraModeDropdown.AddOptions(options);
    }

    private void ApplySavedCameraMode()
    {
        if (cameraController == null || cameraModeDropdown == null) return;

        // Get saved preference
        int savedMode = PlayerPrefs.GetInt("PreferredCameraMode", 0);

        // Apply the saved mode
        CameraMode mode = (CameraMode)savedMode;
        cameraController.SetCameraMode(mode);

        // Update dropdown
        cameraModeDropdown.value = savedMode;

        // Update last known mode
        lastKnownMode = mode;

        Debug.Log($"Applied saved camera mode: {mode}");
    }

    private void OnCameraModeChanged(int index)
    {
        if (cameraController == null) return;

        CameraMode newMode = (CameraMode)index;
        cameraController.SetCameraMode(newMode);

        // Save preference
        PlayerPrefs.SetInt("PreferredCameraMode", index);
        PlayerPrefs.Save();

        // Update last known mode
        lastKnownMode = newMode;

        // Save to global preferences if SaveLoadManager exists
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGlobalPreferences();
        }
        //UISfx.Instance.PlayUIAudio(selectClip);
        Debug.Log($"Camera mode changed to: {newMode}");
    }

    private void Update()
    {
        // Monitor camera mode changes
        if (cameraController != null && cameraController.currentMode != lastKnownMode)
        {
            lastKnownMode = cameraController.currentMode;
            UpdateControlsDisplay();

            // Update dropdown to match
            int modeIndex = (int)lastKnownMode;
            if (cameraModeDropdown != null && cameraModeDropdown.value != modeIndex)
            {
                cameraModeDropdown.value = modeIndex;
            }
        }

        // Always update controls display based on current mode
        UpdateControlsDisplay();
    }

    private void UpdateControlsDisplay()
    {
        if (manualControlsGraphic == null || cameraController == null) return;

        bool shouldShowControls = cameraController.currentMode == CameraMode.Manual;

        if (manualControlsGraphic.activeSelf != shouldShowControls)
        {
            manualControlsGraphic.SetActive(shouldShowControls);

            if (shouldShowControls)
            {
                Debug.Log("Manual camera controls displayed");
            }
        }
    }

    public void RefreshUI()
    {
        if (cameraController != null && cameraModeDropdown != null)
        {
            int currentModeIndex = (int)cameraController.currentMode;
            if (cameraModeDropdown.value != currentModeIndex)
            {
                cameraModeDropdown.value = currentModeIndex;
            }

            UpdateControlsDisplay();
        }
    }

    private void OnDestroy()
    {
        if (cameraModeDropdown != null)
        {
            cameraModeDropdown.onValueChanged.RemoveListener(OnCameraModeChanged);
        }
    }
}