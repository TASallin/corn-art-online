using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NumberOfWinnersUI : MonoBehaviour
{
    [Header("UI References")]
    public Button textButton;
    public TMP_Text displayText;
    public TMP_InputField inputField;
    public Button incrementButton;
    public Button decrementButton;

    [Header("Settings")]
    public int minValue = 1;
    public int maxValue = 10;
    public AudioClip buttonClip;
    public AudioClip openClip;
    public AudioClip confirmClip;
    public AudioClip errorClip;

    private int currentValue = 1;
    private float lastClickTime = 0f;
    private float doubleClickTime = 0.3f;

    private void Start()
    {
        // Initialize UI
        inputField.gameObject.SetActive(false);

        // In WheelOfDeath mode, use the numberOfDeaths directly (don't recalculate)
        if (MenuSettings.Instance.IsWheelOfDeathMode())
        {
            currentValue = MenuSettings.Instance.numberOfDeaths; // Use the value directly!
        } else
        {
            currentValue = MenuSettings.Instance.numberOfWinners;
        }

        UpdateDisplay();

        // Set up button listeners
        incrementButton.onClick.AddListener(IncrementValue);
        decrementButton.onClick.AddListener(DecrementValue);
        textButton.onClick.AddListener(OnTextButtonClick);

        // Set up input field listeners
        inputField.onSubmit.AddListener(OnInputSubmit);
        inputField.onEndEdit.AddListener(OnInputEndEdit);
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;

        // Update max value based on player count
        UpdateMaxBasedOnPlayerCount();
    }

    private void UpdateMaxBasedOnPlayerCount()
    {
        if (MenuSettings.Instance.playerNames.Length > 0)
        {
            // For Wheel of Death, max deaths is total players - 1 (leave at least 1 survivor)
            if (MenuSettings.Instance.IsWheelOfDeathMode())
            {
                maxValue = Mathf.Max(1, MenuSettings.Instance.playerNames.Length - 1);
            } else
            {
                // For Recruit mode, max winners is total players
                maxValue = MenuSettings.Instance.playerNames.Length;
            }

            // Ensure current value is within bounds
            currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
            UpdateDisplay();
        }
    }

    public void IncrementValue()
    {
        if (currentValue < maxValue)
        {
            currentValue++;
            UISfx.Instance.PlayUIAudio(buttonClip);
            UpdateDisplay();
            UpdateMenuSettings();
        }
    }

    public void DecrementValue()
    {
        if (currentValue > minValue)
        {
            currentValue--;
            UISfx.Instance.PlayUIAudio(buttonClip);
            UpdateDisplay();
            UpdateMenuSettings();
        }
    }

    private void UpdateDisplay()
    {
        displayText.text = currentValue.ToString();

        // Disable decrement button if at minimum value
        decrementButton.interactable = currentValue > minValue;

        // Disable increment button if at maximum value
        incrementButton.interactable = currentValue < maxValue;
    }

    private void OnTextButtonClick()
    {
        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickTime)
        {
            ActivateInputField();
        }

        lastClickTime = Time.time;
    }

    private void ActivateInputField()
    {
        UISfx.Instance.PlayUIAudio(openClip);
        displayText.gameObject.SetActive(false);
        inputField.gameObject.SetActive(true);
        inputField.text = currentValue.ToString();
        inputField.ActivateInputField();
        inputField.Select();
    }

    private void DeactivateInputField()
    {
        displayText.gameObject.SetActive(true);
        inputField.gameObject.SetActive(false);
    }

    public void OnInputSubmit(string value)
    {
        ProcessInput(value);
    }

    public void OnInputEndEdit(string value)
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ProcessInput(value);
        }
    }

    private void ProcessInput(string value)
    {
        int newValue;

        if (int.TryParse(value, out newValue))
        {
            newValue = Mathf.Clamp(newValue, minValue, maxValue);
            currentValue = newValue;
            UISfx.Instance.PlayUIAudio(confirmClip);
        } else
        {
            UISfx.Instance.PlayUIAudio(errorClip);
        }

        UpdateDisplay();
        UpdateMenuSettings();
        DeactivateInputField();
    }

    private void UpdateMenuSettings()
    {
        MenuSettings.Instance.numberOfWinners = currentValue;
        Debug.Log($"UI set winners to {currentValue}");
        if (MenuSettings.Instance.IsWheelOfDeathMode())
        {
            // Store the deaths and calculate winners
            MenuSettings.Instance.UpdateWheelOfDeathWinners(currentValue);
            Debug.Log($"UI set deaths to {currentValue}, winners calculated as {MenuSettings.Instance.numberOfWinners}");
        } else
        {
            // Regular Recruit mode
            MenuSettings.Instance.numberOfWinners = currentValue;
            Debug.Log($"UI set winners to {currentValue}");
        }
    }

    public void SetMaxValue(int max)
    {
        maxValue = max;
        if (currentValue > maxValue)
        {
            currentValue = maxValue;
            UpdateDisplay();
            UpdateMenuSettings();
        } else
        {
            UpdateDisplay();
        }
    }

    public void SetMinValue(int min)
    {
        minValue = min;
        if (currentValue < minValue)
        {
            currentValue = minValue;
            UpdateDisplay();
            UpdateMenuSettings();
        } else
        {
            UpdateDisplay();
        }
    }

    public void SetValue(int value)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        UpdateDisplay();
        UpdateMenuSettings();
    }

    // Call this when player names are updated
    public void OnPlayerNamesUpdated()
    {
        UpdateMaxBasedOnPlayerCount();
    }
}