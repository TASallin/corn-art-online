using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartFanfare : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform leftText;
    [SerializeField] private RectTransform rightText;
    [SerializeField] private RectTransform centerImage;

    [Header("Text Components")]
    [SerializeField] private TextMeshProUGUI leftTextComponent;
    [SerializeField] private TextMeshProUGUI rightTextComponent;
    [SerializeField] private Image centerImageComponent;

    [Header("AudioSettings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip introSound;
    [SerializeField] private AudioClip recruitModeStartSound;
    [SerializeField] private AudioClip wheelOfDeathStartSound;
    [SerializeField] private int soundPriority = 2; // Higher priority for UI sounds

    [Header("Animation Settings")]
    [SerializeField] private float slideInDuration = 1.0f;
    [SerializeField] private float centerHoldDuration = 2.0f;
    [SerializeField] private float slideOutDuration = 1.0f;
    [SerializeField] private float delayBeforeGameStart = 1.0f;
    [SerializeField] private float horizontalOffsetDistance = 1200f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Colors")]
    [SerializeField] private Color recruitModeColor = new Color(0.2f, 0.4f, 0.8f); // Blue
    [SerializeField] private Color wheelOfDeathColor = new Color(0.8f, 0.2f, 0.2f); // Red

    [Header("References")]
    [SerializeField] private ArmyManager armyManager;

    // Store original positions
    private Vector2 leftTextOriginalPos;
    private Vector2 rightTextOriginalPos;
    private Vector2 centerImageOriginalPos;

    private void Awake()
    {
        // Store the center positions
        leftTextOriginalPos = leftText.anchoredPosition;
        rightTextOriginalPos = rightText.anchoredPosition;
        centerImageOriginalPos = centerImage.anchoredPosition;

        // Position elements off-screen initially
        leftText.anchoredPosition = new Vector2(-horizontalOffsetDistance, leftTextOriginalPos.y);
        rightText.anchoredPosition = new Vector2(horizontalOffsetDistance, rightTextOriginalPos.y);
        centerImage.anchoredPosition = new Vector2(0, centerImageOriginalPos.y);

        // Reduce centerImage's scale initially
        centerImage.localScale = Vector3.one * 0.8f;

        // Set text content
        SetTextContent();

        // Set color based on stream mode
        SetColorBasedOnStreamMode();

        // Make sure audio source has the right settings
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
        } else
        {
            Debug.LogWarning("No AudioSource assigned to StartFanfare. Sound effects will be disabled.");
        }
    }

    private void Start()
    {
        // Pause the game
        Time.timeScale = 0f;

        // Start the fanfare sequence
        StartCoroutine(PlayFanfareSequence());
    }

    private void SetTextContent()
    {
        if (leftTextComponent != null)
        {
            string armyName = MenuSettings.Instance.armyName;
            if (string.IsNullOrEmpty(armyName))
            {
                armyName = TeamClassComposition.Instance.GetCurrentArmyName();
            }
            leftTextComponent.text = !string.IsNullOrEmpty(armyName) ? armyName : "Army";
        }

        if (rightTextComponent != null)
        {
            string gameMode = MenuSettings.Instance.selectedGameMode;
            rightTextComponent.text = !string.IsNullOrEmpty(gameMode) ? gameMode : "Battle";
        }
    }

    private void SetColorBasedOnStreamMode()
    {
        Color selectedColor = MenuSettings.Instance.IsRecruitMode() ?
                             recruitModeColor : wheelOfDeathColor;

        if (leftTextComponent != null)
            leftTextComponent.color = selectedColor;

        if (rightTextComponent != null)
            rightTextComponent.color = selectedColor;

        if (centerImageComponent != null)
            centerImageComponent.color = selectedColor;
    }

    private IEnumerator PlayFanfareSequence()
    {
        // Play intro sound
        PlaySound(introSound);

        // Slide in animation
        float elapsedTime = 0f;
        yield return null;
        SetTextContent();
        while (elapsedTime < slideInDuration)
        {
            float t = slideCurve.Evaluate(elapsedTime / slideInDuration);

            // Move left text from left to center
            leftText.anchoredPosition = Vector2.Lerp(
                new Vector2(-horizontalOffsetDistance, leftTextOriginalPos.y),
                leftTextOriginalPos,
                t
            );

            // Move right text from right to center
            rightText.anchoredPosition = Vector2.Lerp(
                new Vector2(horizontalOffsetDistance, rightTextOriginalPos.y),
                rightTextOriginalPos,
                t
            );

            // Scale up center image
            centerImage.localScale = Vector3.Lerp(
                Vector3.one * 0.8f,
                Vector3.one,
                t
            );

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // Ensure final positions are exact
        leftText.anchoredPosition = leftTextOriginalPos;
        rightText.anchoredPosition = rightTextOriginalPos;
        centerImage.localScale = Vector3.one;

        // Hold in center
        yield return new WaitForSecondsRealtime(centerHoldDuration);

        // Play the appropriate start sound based on stream mode
        if (MenuSettings.Instance.IsRecruitMode())
        {
            PlaySound(recruitModeStartSound);
        } else
        {
            PlaySound(wheelOfDeathStartSound);
        }

        // Slide out animation
        elapsedTime = 0f;
        while (elapsedTime < slideOutDuration)
        {
            float t = slideCurve.Evaluate(elapsedTime / slideOutDuration);

            // Move left text from center to right (crossing over)
            leftText.anchoredPosition = Vector2.Lerp(
                leftTextOriginalPos,
                new Vector2(horizontalOffsetDistance, leftTextOriginalPos.y),
                t
            );

            // Move right text from center to left (crossing over)
            rightText.anchoredPosition = Vector2.Lerp(
                rightTextOriginalPos,
                new Vector2(-horizontalOffsetDistance, rightTextOriginalPos.y),
                t
            );

            // Scale down center image
            centerImage.localScale = Vector3.Lerp(
                Vector3.one,
                Vector3.one * 0.8f,
                t
            );

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // Wait before starting the game
        yield return new WaitForSecondsRealtime(delayBeforeGameStart);

        // Resume the game
        Time.timeScale = 1f;
        armyManager.ActivateAI();

        // Hide fanfare UI elements
        leftText.gameObject.SetActive(false);
        rightText.gameObject.SetActive(false);
        centerImage.gameObject.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null && AudioChannelManager.Instance != null)
        {
            AudioChannelManager.Instance.TryPlayAudio(
                audioSource,
                clip,
                AudioChannelManager.AudioType.SoundEffect,
                soundPriority
            );
        }
    }

    public void PlayTextAnimation(string text, Color color, bool useLeftText, float duration = 1.0f)
    {
        StartCoroutine(AnimateText(text, color, useLeftText, duration));
    }

    private IEnumerator AnimateText(string text, Color color, bool useLeftText, float duration)
    {
        // Get the appropriate text component and transform
        RectTransform textTransform = useLeftText ? leftText : rightText;
        TextMeshProUGUI textComponent = useLeftText ? leftTextComponent : rightTextComponent;

        if (textTransform == null || textComponent == null) yield break;

        // Store original position
        Vector2 originalPos = textTransform.anchoredPosition;
        originalPos.x = 0;

        // Set text and color
        textComponent.text = text;
        textComponent.color = color;

        // Make sure object is active and visible
        textTransform.gameObject.SetActive(true);

        // Start from off-screen
        textTransform.anchoredPosition = new Vector2(
            -horizontalOffsetDistance * (useLeftText ? 1 : -1),
            originalPos.y
        );

        // Slide in
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = slideCurve.Evaluate(elapsedTime / duration);
            textTransform.anchoredPosition = Vector2.Lerp(
                new Vector2(-horizontalOffsetDistance * (useLeftText ? 1 : -1), originalPos.y),
                originalPos,
                t
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Hold briefly
        yield return new WaitForSeconds(2f);

        // Slide out
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = slideCurve.Evaluate(elapsedTime / duration);
            textTransform.anchoredPosition = Vector2.Lerp(
                originalPos,
                new Vector2(horizontalOffsetDistance * (useLeftText ? 1 : -1), originalPos.y),
                t
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Hide the text
        textTransform.gameObject.SetActive(false);
    }
}
