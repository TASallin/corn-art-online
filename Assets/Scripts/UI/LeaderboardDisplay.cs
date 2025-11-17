// LeaderboardDisplay.cs
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using SFB;

public class LeaderboardDisplay : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform _panel; // The panel where entries will be spawned
    [SerializeField] private GameObject _entryPrefab; // Reference to your leaderboard entry prefab

    [Header("Layout Settings")]
    [SerializeField] private Vector2 _startPosition = new Vector2(-265, 190);
    [SerializeField] private Vector2 _entrySize = new Vector2(265, 30);
    [SerializeField] private int _entriesPerColumn = 12;
    [SerializeField] private float _columnSpacing = 265f;
    [SerializeField] private float _verticalSpacing = 30f;

    [Header("Pagination")]
    [SerializeField] private Button _prevPageButton;
    [SerializeField] private Button _nextPageButton;
    [SerializeField] private int _entriesPerPage = 36; // 3 columns × 12 rows

    [Header("Navigation")]
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private Button _setupGameButton;
    [SerializeField] private Button _saveImageButton;
    [SerializeField] private Button _saveNamesButton;
    [SerializeField] private GameObject _downloadConfirmation;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";
    [SerializeField] private string _gameSetupSceneName = "GameSetup";

    [Header("Animation")]
    [SerializeField] private bool _animateFirstPage = true;
    [SerializeField] private float _totalAnimationDuration = 4f; // 3-5 seconds for full leaderboard

    [Header("Colors")]
    [SerializeField] private Color _winningColor = Color.green;
    [SerializeField] private Color _normalColor = Color.red;
    //[SerializeField] private int _numWinningPlayers = 1; // Default: winner is first place only

    [Header("Voice Settings")]
    [SerializeField] private AudioSource _voiceAudioSource;
    [SerializeField] private float _voiceLineDelay = 0.75f; // Delay between voice lines
    public AudioClip setupClip;
    public AudioClip buttonClip;
    public AudioClip downloadClip;
    public AudioClip saveClip;
    public AudioClip exitClip;
    public AudioClip errorClip;

    private Coroutine _voicePlaybackCoroutine;
    private LeaderboardEntry _currentlySpeakingEntry;

    private List<LeaderboardEntry> _spawnedEntries = new List<LeaderboardEntry>();
    private int _currentPage = 0;
    private int _totalPages = 0;
    private bool _isFirstLoad = true;

    void Start()
    {
        // Setup navigation button listeners
        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (_saveNamesButton != null)
            _saveNamesButton.onClick.AddListener(SaveNames);

        if (_setupGameButton != null)
        {
            _setupGameButton.onClick.AddListener(GoToGameSetup);
            if (MenuSettings.Instance != null)
            {
                if (MenuSettings.Instance.streamMode == "Recruit")
                {
                    _setupGameButton.GetComponentInChildren<TMP_Text>().text = "Recruitme Setup";
                }
                else
                {
                    _setupGameButton.GetComponentInChildren<TMP_Text>().text = "Wheel of Death Setup";
                    _saveNamesButton.gameObject.SetActive(false);
                }
            }
        }

        if (_prevPageButton != null)
            _prevPageButton.onClick.AddListener(PreviousPage);

        if (_nextPageButton != null)
            _nextPageButton.onClick.AddListener(NextPage);

        if (_saveImageButton != null)
            _saveImageButton.onClick.AddListener(SaveImage);

        // If we have leaderboard data, display it
        if (LeaderboardData.Instance != null && LeaderboardData.Instance.HasData)
        {
            if (MenuSettings.Instance != null)
            {
                MenuSettings.Instance.winners.Clear();
                //MenuSettings.Instance.eliminated.Clear();
                for (int i = 0; i < LeaderboardData.Instance.NumberOfWinners; i++)
                {
                    MenuSettings.Instance.winners.Add(LeaderboardData.Instance.RankedUnits[i].playerName);
                }
            }
            DisplayLeaderboard();
            _isFirstLoad = false;
        }
    }

    void OnDestroy()
    {
        StopVoicePlayback();
        // Clean up button listeners
        if (_mainMenuButton != null)
            _mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        if (_setupGameButton != null)
            _setupGameButton.onClick.RemoveListener(GoToGameSetup);

        if (_prevPageButton != null)
            _prevPageButton.onClick.RemoveListener(PreviousPage);

        if (_nextPageButton != null)
            _nextPageButton.onClick.RemoveListener(NextPage);
    }

    public void DisplayLeaderboard()
    {
        // Stop any ongoing voice playback when changing pages
        StopVoicePlayback();
        // Clear existing entries
        ClearEntries();

        // Get leaderboard data
        var rankedUnits = LeaderboardData.Instance.RankedUnits;
        var scores = LeaderboardData.Instance.Scores;
        var eliminationTracker = LeaderboardData.Instance.EliminatorTracker;
        var defeatedEnemiesTracker = LeaderboardData.Instance.DefeatedEnemiesTracker; // Add this

        // Get the number of winners from LeaderboardData
        int numberOfWinners = LeaderboardData.Instance.NumberOfWinners;

        // Calculate total pages
        _totalPages = Mathf.CeilToInt((float)rankedUnits.Count / _entriesPerPage);

        // Update pagination buttons
        UpdatePaginationButtons();

        // Calculate the start and end index for the current page
        int startIndex = _currentPage * _entriesPerPage;
        int endIndex = Mathf.Min(startIndex + _entriesPerPage, rankedUnits.Count);

        // Should we animate this page?
        bool shouldAnimate = _animateFirstPage && _isFirstLoad && _currentPage == 0;

        // Calculate delay between animations if animating
        float delayBetweenEntries = 0;
        if (shouldAnimate && endIndex > startIndex)
        {
            delayBetweenEntries = _totalAnimationDuration / (endIndex - startIndex);
        }

        // Create entries for the current page
        for (int i = startIndex; i < endIndex; i++)
        {
            var unit = rankedUnits[i];
            int rank = i + 1;

            // Use playerName if it exists, otherwise use unitName
            string name = !string.IsNullOrEmpty(unit.playerName) ? unit.playerName : unit.unitName;
            string characterName = unit.unitName; // To get the appropriate sprites

            // Get unit's ID using unitID!!
            int unitId = unit.unitID;

            // Check for elimination - a unit is only considered eliminated if it has elimination data
            bool isActuallyEliminated = false;
            EliminatorData eliminatorData = null;

            if (eliminationTracker != null && eliminationTracker.ContainsKey(unitId) && !unit.GetAlive())
            {
                // Only eliminated if there's actual eliminator data
                isActuallyEliminated = true;
                eliminatorData = eliminationTracker[unitId];
            }

            // Log for debugging
            if (!unit.GetAlive() && !isActuallyEliminated)
            {
                Debug.Log($"Unit {unit.unitName} is dead but has no eliminator data - treating as NOT eliminated");
            }

            // Get score using unitID
            float score = scores.ContainsKey(unitId) ? scores[unitId].TotalScore : 0f;
            Debug.Log($"Unit {unit.unitName} (ID: {unitId}) has score: {score}");

            // Calculate position on the page
            int localIndex = i - startIndex;
            int column = localIndex / _entriesPerColumn;
            int row = localIndex % _entriesPerColumn;
            float xPos = _startPosition.x + (column * _columnSpacing);
            float yPos = _startPosition.y - (row * _verticalSpacing);

            // Create entry
            GameObject entryObj = Instantiate(_entryPrefab, _panel);
            entryObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);
            entryObj.GetComponent<RectTransform>().sizeDelta = _entrySize;

            LeaderboardEntry entry = entryObj.GetComponent<LeaderboardEntry>();

            // Get the appropriate character portrait
            Sprite portraitSprite = null;

            // Get character data
            var characterData = CharacterAssetLoader.Instance.GetCharacterData(characterName);
            if (characterData != null)
            {
                string portraitSuffix;

                // Choose portrait:
                // 1. Winners always get crit portraits (even if eliminated)
                // 2. Eliminated units get damage portraits
                // 3. Anyone else gets neutral portraits
                if (rank <= numberOfWinners)
                {
                    portraitSuffix = " Crit";
                } else if (isActuallyEliminated)
                {
                    portraitSuffix = " Damage";
                } else
                {
                    portraitSuffix = " Neutral";
                }

                if (characterName == "Corrin")
                {
                    portraitSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(CharacterAssetLoader.Instance.GetCorrinFacePrefix(unit.corrinIsMale, unit.corrinBodyType, unit.corrinFace) + portraitSuffix);
                } else
                {
                    portraitSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(characterData.portraitPrefix + portraitSuffix);

                    // Fallback to neutral if the specific portrait isn't available
                    if (portraitSprite == null)
                    {
                        portraitSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(characterData.portraitPrefix + " Neutral");
                    }
                }

            }

            // Get eliminator sprite if the unit was actually eliminated
            Sprite eliminatorSprite = null;

            if (isActuallyEliminated && eliminatorData != null)
            {
                var eliminatorCharData = CharacterAssetLoader.Instance.GetCharacterData(eliminatorData.CharacterName);
                if (eliminatorCharData != null)
                {
                    if (eliminatorData.CharacterName == "Corrin")
                    {
                        eliminatorSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(CharacterAssetLoader.Instance.GetCorrinFacePrefix(eliminatorData.CorrinIsMale, eliminatorData.CorrinBodyType, eliminatorData.CorrinFace) + " Crit");
                    } else
                    {
                        // Eliminator always gets their crit portrait
                        eliminatorSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(eliminatorCharData.portraitPrefix + " Crit");

                        // Fallback to neutral if crit isn't available
                        if (eliminatorSprite == null)
                        {
                            eliminatorSprite = CharacterAssetLoader.Instance.GetOrLoadSprite(eliminatorCharData.portraitPrefix + " Neutral");
                        }
                    }
                }
            }

            List<EnemyDefeatData> defeatedEnemies = null;
            if (defeatedEnemiesTracker != null && defeatedEnemiesTracker.ContainsKey(unitId))
            {
                defeatedEnemies = defeatedEnemiesTracker[unitId];
            }

            bool isCorrin = false;
            bool corrinIsMale = false;
            int corrinBodyType = 1;
            int corrinHair = 1;
            int corrinDetail = 1;
            Color corrinHairColor = Color.white;

            if (characterName == "Corrin")
            {
                isCorrin = true;
                corrinIsMale = unit.corrinIsMale;
                corrinBodyType = unit.corrinBodyType;
                corrinHair = unit.corrinHair;
                corrinDetail = unit.corrinDetail;
                corrinHairColor = unit.corrinHairColor;
            }

            // Initialize entry with all the data
            entry.Initialize(rank, name, score, portraitSprite, isActuallyEliminated,
                        eliminatorSprite, eliminatorData, defeatedEnemies, shouldAnimate, isCorrin, corrinIsMale, corrinBodyType, corrinHair, corrinDetail, corrinHairColor);

            // Set color based on rank
            if (rank <= numberOfWinners)
            {
                entry.SetColor(_winningColor);
            } else
            {
                entry.SetColor(_normalColor);
            }

            // Start animation if needed
            if (shouldAnimate)
            {
                entry.AnimateEntry(localIndex * delayBetweenEntries);
            }

            _spawnedEntries.Add(entry);
        }

        CharacterAssetLoader.Instance.LogAudioCacheStatistics();
        if (_currentPage == 0 && shouldAnimate) // Only play voice lines on the first page
        {
            // Start voice playback after all animations complete
            float totalAnimationTime = _totalAnimationDuration;

            if (_voicePlaybackCoroutine != null)
            {
                StopCoroutine(_voicePlaybackCoroutine);
            }

            _voicePlaybackCoroutine = StartCoroutine(PlayWinnerVoiceLines(totalAnimationTime));
        }
    }

    private void ClearEntries()
    {
        foreach (var entry in _spawnedEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }
        _spawnedEntries.Clear();
    }

    private void UpdatePaginationButtons()
    {
        // Enable/disable prev button based on current page
        if (_prevPageButton != null)
            _prevPageButton.interactable = (_currentPage > 0);

        // Enable/disable next button based on whether there are more pages
        if (_nextPageButton != null)
            _nextPageButton.interactable = (_currentPage < _totalPages - 1);
    }

    private void StopVoicePlayback()
    {
        if (_voicePlaybackCoroutine != null)
        {
            StopCoroutine(_voicePlaybackCoroutine);
            _voicePlaybackCoroutine = null;
        }

        if (_voiceAudioSource != null && _voiceAudioSource.isPlaying)
        {
            _voiceAudioSource.Stop();
        }

        if (_currentlySpeakingEntry != null)
        {
            _currentlySpeakingEntry.SetSpeaking(false);
            _currentlySpeakingEntry = null;
        }
    }

    private IEnumerator PlayWinnerVoiceLines(float initialDelay)
    {
        // Wait for animations to complete
        yield return new WaitForSeconds(initialDelay);

        int numberOfWinners = LeaderboardData.Instance.NumberOfWinners;

        // Play voice lines for winners only
        for (int i = 0; i < Mathf.Min(numberOfWinners, _spawnedEntries.Count); i++)
        {
            var entry = _spawnedEntries[i];
            var unit = LeaderboardData.Instance.RankedUnits[i];

            // Get character data for voice line
            var characterData = CharacterAssetLoader.Instance.GetCharacterData(unit.unitName);
            if (characterData != null)
            {
                // Try to get victory audio
                string victoryAudioName = characterData.audioPrefix + " Win";
                if (unit.unitName == "Corrin")
                {
                    if (unit.corrinIsMale)
                    {
                        victoryAudioName = "CorrinM Win";
                    } else
                    {
                        victoryAudioName = "CorrinF Win";
                    }
                }
                AudioClip victoryClip = CharacterAssetLoader.Instance.GetOrLoadAudio(victoryAudioName);

                if (victoryClip != null && _voiceAudioSource != null)
                {
                    // Mark entry as speaking
                    if (_currentlySpeakingEntry != null)
                    {
                        _currentlySpeakingEntry.SetSpeaking(false);
                    }

                    _currentlySpeakingEntry = entry;
                    entry.SetSpeaking(true);

                    // Play the voice line
                    _voiceAudioSource.clip = victoryClip;
                    _voiceAudioSource.Play();

                    // Wait for the clip to finish or the delay, whichever is longer
                    float waitTime = Mathf.Max(victoryClip.length, _voiceLineDelay);
                    yield return new WaitForSeconds(waitTime);

                    // Stop speaking indicator
                    entry.SetSpeaking(false);
                } else
                {
                    Debug.LogWarning($"No victory voice line found for {unit.unitName}");

                    // Still wait the delay even if no voice line
                    yield return new WaitForSeconds(_voiceLineDelay);
                }
            } else
            {
                // Still wait the delay even if no character data
                yield return new WaitForSeconds(_voiceLineDelay);
            }
        }

        // Clear the speaking entry reference
        _currentlySpeakingEntry = null;
    }

    // Update navigation methods to stop voice playback
    public void PreviousPage()
    {
        if (_currentPage > 0)
        {
            StopVoicePlayback();
            _currentPage--;
            UISfx.Instance.PlayUIAudio(buttonClip);
            DisplayLeaderboard();
        }
    }

    public void NextPage()
    {
        if (_currentPage < _totalPages - 1)
        {
            StopVoicePlayback();
            _currentPage++;
            UISfx.Instance.PlayUIAudio(buttonClip);
            DisplayLeaderboard();
        }
    }

    public void ReturnToMainMenu()
    {
        // Clean up leaderboard data
        LeaderboardData.Instance.Reset();
        UISfx.Instance.PlayUIAudio(exitClip);
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    public void GoToGameSetup()
    {
        // Clean up leaderboard data
        LeaderboardData.Instance.Reset();
        UISfx.Instance.PlayUIAudio(setupClip);
        SceneManager.LoadScene(_gameSetupSceneName);
    }

    public void SaveImage()
    {
        string defaultFileName = "recruits";
        if (MenuSettings.Instance != null)
        {
            if (MenuSettings.Instance.streamMode != "Recruit")
            {
                defaultFileName = "deaths";
            }
            defaultFileName = MenuSettings.Instance.selectedLevel + " " + defaultFileName;
        }
        UISfx.Instance.PlayUIAudio(downloadClip);

        var path = StandaloneFileBrowser.SaveFilePanel("Save Results Image", "", defaultFileName, "png");

        if (!string.IsNullOrEmpty(path))
        {
            ScreenCapture.CaptureScreenshot(path);
            UISfx.Instance.PlayUIAudio(saveClip);
            StartCoroutine("DisplayDownloadConfirmation");
        } else
        {
            UISfx.Instance.PlayUIAudio(errorClip);
        }
    }

    public void SaveNames()
    {
        string names = "";
        if (MenuSettings.Instance != null)
        {
            foreach (string entrant in MenuSettings.Instance.playerNames)
            {
                if (!MenuSettings.Instance.winners.Contains(entrant))
                {
                    names = names + entrant + "\n";
                }
            }
        }
        UISfx.Instance.PlayUIAudio(downloadClip);

        var path = StandaloneFileBrowser.SaveFilePanel("Save Names Excluding Winners", "", "corrinquest_entrants", "txt");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, names);
            UISfx.Instance.PlayUIAudio(saveClip);
            StartCoroutine("DisplayDownloadConfirmation");
        } else
        {
            UISfx.Instance.PlayUIAudio(errorClip);
        }
    }

    public IEnumerator DisplayDownloadConfirmation()
    {
        yield return new WaitForSeconds(0.1f);
        _downloadConfirmation.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        _downloadConfirmation.SetActive(false);
        yield return null;
    }

    private int GetUnitId(Unit unit)
    {
        return unit.unitID;
    }

    // Method for game rules to set number of winners (for future use)
    public void SetNumWinningPlayers(int numWinners)
    {
        // Update the LeaderboardData
        LeaderboardData.Instance.NumberOfWinners = Mathf.Max(1, numWinners);

        // If leaderboard is already displayed, update colors
        if (_spawnedEntries.Count > 0)
        {
            int numberOfWinners = LeaderboardData.Instance.NumberOfWinners;

            for (int i = 0; i < _spawnedEntries.Count; i++)
            {
                int rank = i + 1 + (_currentPage * _entriesPerPage);
                if (rank <= numberOfWinners)
                {
                    _spawnedEntries[i].SetColor(_winningColor);
                } else
                {
                    _spawnedEntries[i].SetColor(_normalColor);
                }
            }
        }
    }
}