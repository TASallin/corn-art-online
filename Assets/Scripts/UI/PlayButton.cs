using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayButton : MonoBehaviour
{
    
    [Header("Button References")]
    public Button playButton;

    [Header("Settings")]
    public string gameSceneName = "GameScene"; // The scene to load when Play is clicked
    public bool createAutosaveOnPlay = true;
    public GameObject loadingScreen;
    public GameObject menuPanel;
    public Image cyanImage;
    public Image bossImage;
    public Image leftCorrinHair;
    public Image leftCorrinDetail;
    public Image rightCorrinHair;
    public Image rightCorrinDetail;
    public float loadingDisplacement = 1000f;
    public float displacementTime = 2f;
    public float loadingAudioBuffer = 1f;
    public AudioClip errorClip;
    public AudioClip confirmClip;

    [Header("Validation")]
    public bool requirePlayerNames = true;
    public bool requireLevelSelection = true;
    public ConfirmationDialog errorDialog;

    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
    }

    public void OnPlayButtonClicked()
    {
        // Validate game settings before playing
        if (!ValidateGameSettings())
        {
            UISfx.Instance.PlayUIAudio(errorClip);
            return;
        }

        playButton.interactable = false;

        // Create autosave
        if (createAutosaveOnPlay)
        {
            SaveLoadManager.Instance.AutoSave();
            Debug.Log("Created autosave before starting game");
        }
        UISfx.Instance.PlayUIAudio(confirmClip);
        // Load the game scene
        StartGame();
    }

    private bool ValidateGameSettings()
    {
        MenuSettings settings = MenuSettings.Instance;
        string errorMessage = null;

        // Check if a level is selected
        if (requireLevelSelection && string.IsNullOrEmpty(settings.selectedLevel))
        {
            errorMessage = "Please select a level before playing.";
        }
        // Check if there are enough player names
        else if (requirePlayerNames &&
                (settings.playerNames == null || settings.playerNames.Length < 2))
        {
            errorMessage = "Please add at least two player names.";
        }
        // Check if number of winners is valid
        else if (settings.numberOfWinners < 1 ||
                (settings.playerNames != null && settings.numberOfWinners >= settings.playerNames.Length))
        {
            errorMessage = "Number of winners must be less than the number of players.";
        }

        // Show error dialog if validation failed
        if (errorMessage != null)
        {
            if (errorDialog != null)
            {
                errorDialog.Show(errorMessage, null);
            } else
            {
                Debug.LogWarning(errorMessage);
            }

            return false;
        }

        return true;
    }

    private void StartGame()
    {
        Debug.Log($"Starting game with {MenuSettings.Instance.playerNames.Length} players...");

        // Load the game scene
        MenuSettings.Instance.NormalizeWinnersToDeaths();
        MenuSettings.Instance.CheckRandomWinCondition();
        StartCoroutine("LoadingSequence");
    }

    public IEnumerator LoadingSequence()
    {
        loadingScreen.SetActive(true);
        menuPanel.transform.localScale = new Vector3(0, 0, 0);
        string leftCharacterName;
        string rightCharacterName;
        float elapsedTime = 0f;
        float startingPosition = bossImage.transform.localPosition.x;
        string critAudioName;
        Sprite portraitSprite;
        if (MenuSettings.Instance.streamMode == "Recruit")
        {
            leftCharacterName = "Cyan " + (GameManager.GetInstance().rng.Next(7) + 1);
            rightCharacterName = "Takumi";
            TeamClassComposition.Instance.LoadCompositionByName(MenuSettings.Instance.selectedLevel);
            TeamClassComposition.TeamComposition enemyComp = TeamClassComposition.Instance.GetTeamComposition(2, 20);
            rightCharacterName = enemyComp.classDistribution[0].unitData[0].UnitName;

            var cyanCharacterData = CharacterAssetLoader.Instance.GetCharacterData(leftCharacterName);
            cyanImage.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(cyanCharacterData.portraitPrefix + " Neutral");
            var bossCharacterData = CharacterAssetLoader.Instance.GetCharacterData(rightCharacterName);
            bossImage.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(bossCharacterData.portraitPrefix + " Neutral");
            while (elapsedTime < displacementTime)
            {
                float currentPosition = startingPosition - loadingDisplacement * (elapsedTime / displacementTime);
                bossImage.transform.localPosition = new Vector3(currentPosition, bossImage.transform.localPosition.y, 0);
                cyanImage.transform.localPosition = new Vector3(currentPosition * -1, cyanImage.transform.localPosition.y, 0);
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            bossImage.transform.localPosition = new Vector3(startingPosition - loadingDisplacement, bossImage.transform.localPosition.y, 0);
            cyanImage.transform.localPosition = new Vector3(loadingDisplacement - startingPosition, cyanImage.transform.localPosition.y, 0);
            yield return new WaitForSeconds(loadingAudioBuffer);
            if (GameManager.GetInstance().rng.Next(2) == 1)
            {
                critAudioName = cyanCharacterData.audioPrefix + " Crit";
                portraitSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(cyanCharacterData.portraitPrefix + " Crit");
                // Fallback to neutral if the specific portrait isn't available
                if (portraitSprite == null)
                {
                    portraitSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(cyanCharacterData.portraitPrefix + " Neutral");
                }
                cyanImage.sprite = portraitSprite;
            } else
            {
                critAudioName = bossCharacterData.audioPrefix + " Crit";
                portraitSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(bossCharacterData.portraitPrefix + " Crit");
                // Fallback to neutral if the specific portrait isn't available
                if (portraitSprite == null)
                {
                    portraitSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(bossCharacterData.portraitPrefix + " Neutral");
                }
                bossImage.sprite = portraitSprite;
            }
            AudioClip critClip = CharacterAssetLoader.Instance.GetOrLoadAudio(critAudioName);
            UISfx.Instance.PlayUIAudio(critClip);
            float waitTime = Mathf.Max(critClip.length, loadingAudioBuffer);
            yield return new WaitForSeconds(waitTime);
        } else
        {
            leftCharacterName = "Corrin";
            rightCharacterName = "Corrin";
            bool leftIsMale = false;
            if (GameManager.GetInstance().rng.Next(2) == 1)
            {
                leftIsMale = true;
            }
            int bodyType = GameManager.GetInstance().rng.Next(2) + 1;
            int faceIndex = GameManager.GetInstance().rng.Next(7) + 1;
            int hairIndex = GameManager.GetInstance().rng.Next(12) + 1;
            int detailIndex = GameManager.GetInstance().rng.Next(24) + 1;
            if (detailIndex > 12)
            {
                detailIndex = 0; //No facial detail
            }
            Color hairColor = new Color((float)GameManager.GetInstance().rng.NextDouble(), (float)GameManager.GetInstance().rng.NextDouble(), (float)GameManager.GetInstance().rng.NextDouble(), 1f);
            string leftFacePath = CharacterAssetLoader.Instance.GetCorrinFacePrefix(leftIsMale, bodyType, faceIndex);
            string hairPath = CharacterAssetLoader.Instance.GetCorrinHairPrefix(leftIsMale, bodyType, hairIndex);
            string detailPath = CharacterAssetLoader.Instance.GetCorrinDetailPrefix(leftIsMale, bodyType, detailIndex);

            cyanImage.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(leftFacePath + " Neutral");
            leftCorrinHair.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(hairPath);
            leftCorrinHair.color = hairColor;
            leftCorrinHair.gameObject.SetActive(true);
            if (detailIndex > 0)
            {
                leftCorrinDetail.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(detailPath);
                leftCorrinDetail.gameObject.SetActive(true);
            }

            bool rightIsMale = false;
            if (GameManager.GetInstance().rng.Next(2) == 1)
            {
                rightIsMale = true;
            }
            bodyType = GameManager.GetInstance().rng.Next(2) + 1;
            faceIndex = GameManager.GetInstance().rng.Next(7) + 1;
            hairIndex = GameManager.GetInstance().rng.Next(12) + 1;
            detailIndex = GameManager.GetInstance().rng.Next(24) + 1;
            if (detailIndex > 12)
            {
                detailIndex = 0; //No facial detail
            }
            hairColor = new Color((float)GameManager.GetInstance().rng.NextDouble(), (float)GameManager.GetInstance().rng.NextDouble(), (float)GameManager.GetInstance().rng.NextDouble(), 1f);
            string rightFacePath = CharacterAssetLoader.Instance.GetCorrinFacePrefix(rightIsMale, bodyType, faceIndex);
            hairPath = CharacterAssetLoader.Instance.GetCorrinHairPrefix(rightIsMale, bodyType, hairIndex);
            detailPath = CharacterAssetLoader.Instance.GetCorrinDetailPrefix(rightIsMale, bodyType, detailIndex);

            bossImage.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(rightFacePath + " Neutral");
            rightCorrinHair.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(hairPath);
            rightCorrinHair.color = hairColor;
            rightCorrinHair.gameObject.SetActive(true);
            if (detailIndex > 0)
            {
                rightCorrinDetail.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(detailPath);
                rightCorrinDetail.gameObject.SetActive(true);
            }
            while (elapsedTime < displacementTime)
            {
                float currentPosition = startingPosition - loadingDisplacement * (elapsedTime / displacementTime);
                bossImage.transform.localPosition = new Vector3(currentPosition, bossImage.transform.localPosition.y, 0);
                cyanImage.transform.localPosition = new Vector3(currentPosition * -1, cyanImage.transform.localPosition.y, 0);
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            bossImage.transform.localPosition = new Vector3(startingPosition - loadingDisplacement, bossImage.transform.localPosition.y, 0);
            cyanImage.transform.localPosition = new Vector3(loadingDisplacement - startingPosition, cyanImage.transform.localPosition.y, 0);
            yield return new WaitForSeconds(loadingAudioBuffer);
            if (GameManager.GetInstance().rng.Next(2) == 1)
            {
                if (leftIsMale)
                {
                    critAudioName = "CorrinM Crit";
                } else
                {
                    critAudioName = "CorrinF Crit";
                }
                portraitSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(leftFacePath + " Crit");
                cyanImage.sprite = portraitSprite;
            }
            else
            {
                if (rightIsMale)
                {
                    critAudioName = "CorrinM Crit";
                }
                else
                {
                    critAudioName = "CorrinF Crit";
                }
                portraitSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(rightFacePath + " Crit");
                bossImage.sprite = portraitSprite;
            }
            AudioClip critClip = CharacterAssetLoader.Instance.GetOrLoadAudio(critAudioName);
            UISfx.Instance.PlayUIAudio(critClip);
            float waitTime = Mathf.Max(critClip.length, loadingAudioBuffer);
            yield return new WaitForSeconds(waitTime);
        }
        SceneManager.LoadScene(gameSceneName);
    }
}