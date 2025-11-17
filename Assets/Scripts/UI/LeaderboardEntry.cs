// LeaderboardEntry.cs (updated)
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardEntry : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _rankText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _characterPortrait; // Character portrait
    [SerializeField] private Image _characterCorrinHair;
    [SerializeField] private Image _characterCorrinDetail;
    [SerializeField] private GameObject _eliminatedIndicator; // Indicator for eliminated units
    [SerializeField] private Image _eliminatorPortrait; // Portrait of who eliminated this unit
    [SerializeField] private Image _eliminatorCorrinHair;
    [SerializeField] private Image _eliminatorCorrinDetail;
    [SerializeField] private Image _eliminatorPortraitRing; // Ring around eliminator portrait

    [Header("Speaking Indicator")]
    [SerializeField] private GameObject _speakingIndicator; // Visual indicator when character is speaking

    [Header("Animation Settings")]
    [SerializeField] private float _animationDistance = 50f;
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private AnimationCurve _fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Enemy Defeat Display")]
    [SerializeField] private TextMeshProUGUI _enemiesDefeatedText;

    [Header("Enemy Portraits")]
    [SerializeField] private GameObject _enemyPortraitPrefab; // Your portrait prefab
    [SerializeField] private Transform _bossPortraitContainer; // Where boss portraits go
    [SerializeField] private Transform _elitePortraitContainer; // Where elite portraits go  
    [SerializeField] private Transform _normalPortraitContainer; // Where normal enemy portraits go

    [Header("Enemy Portraits Layout")]
    [SerializeField] private RectTransform _totalPortraitArea; // Total area for all portraits
    [SerializeField] private float _portraitOverlapRatio = 0.25f; // How much portraits overlap (0-1)
    [SerializeField] private float _portraitSpacingX = 5f; // Horizontal spacing between portraits
    [SerializeField] private float _portraitSpacingY = 5f; // Vertical spacing between portraits
    [SerializeField] private int _maxPortraitsPerRow = 4; // Max portraits in a row
    [SerializeField] private bool _prioritizeBossPortraits = true; // Give bosses more prominent positions

    [Header("Portrait Sizing")]
    [SerializeField] private float _bossPortraitScale = 2.0f;
    [SerializeField] private float _elitePortraitScale = 1.0f;
    [SerializeField] private float _normalPortraitScale = 0.5f;

    private List<GameObject> _spawnedEnemyPortraits = new List<GameObject>();

    private Vector2 _targetPosition;
    private CanvasGroup _canvasGroup;
    private bool _isAnimating = false;

    private void Awake()
    {
        // Get or add CanvasGroup for fading
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize(int rank, string name, float score, Sprite characterSprite, bool isEliminated,
                          Sprite eliminatorSprite, EliminatorData eliminatorData,
                          List<EnemyDefeatData> defeatedEnemies, bool animate, bool isCorrin, bool corrinIsMale, int corrinBodyType,
                          int corrinHair, int corrinDetail, Color corrinHairColor)
    {
        if (_rankText != null)
            _rankText.text = $"{rank}";

        if (_nameText != null)
            _nameText.text = name;

        if (_scoreText != null)
            _scoreText.text = score.ToString("F0");

        // Set character portrait if available
        if (_characterPortrait != null && characterSprite != null)
        {
            _characterPortrait.sprite = characterSprite;
            _characterPortrait.gameObject.SetActive(true);
            if (isCorrin)
            {
                _characterCorrinHair.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(CharacterAssetLoader.Instance.GetCorrinHairPrefix(corrinIsMale, corrinBodyType, corrinHair));
                _characterCorrinHair.color = corrinHairColor;
                _characterCorrinHair.gameObject.SetActive(true);
                if (corrinDetail > 0)
                {
                    _characterCorrinDetail.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(CharacterAssetLoader.Instance.GetCorrinDetailPrefix(corrinIsMale, corrinBodyType, corrinDetail));
                    _characterCorrinDetail.gameObject.SetActive(true);
                }
            }
        } else if (_characterPortrait != null)
        {
            _characterPortrait.gameObject.SetActive(false);
        }

        // Handle elimination indicator and eliminator portrait
        if (_eliminatedIndicator != null)
        {
            _eliminatedIndicator.SetActive(isEliminated);

            // Set eliminator portrait if unit was eliminated
            if (isEliminated && _eliminatorPortrait != null)
            {
                if (eliminatorSprite != null)
                {
                    _eliminatorPortrait.sprite = eliminatorSprite;
                    _eliminatorPortrait.gameObject.SetActive(true);
                    if (eliminatorData.CharacterName == "Corrin")
                    {
                        _eliminatorCorrinHair.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(CharacterAssetLoader.Instance.GetCorrinHairPrefix(eliminatorData.CorrinIsMale, eliminatorData.CorrinBodyType, eliminatorData.CorrinHair));
                        _eliminatorCorrinHair.color = eliminatorData.CorrinHairColor;
                        _eliminatorCorrinHair.gameObject.SetActive(true);
                        if (eliminatorData.CorrinDetail > 0)
                        {
                            _eliminatorCorrinDetail.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(CharacterAssetLoader.Instance.GetCorrinDetailPrefix(eliminatorData.CorrinIsMale, eliminatorData.CorrinBodyType, eliminatorData.CorrinDetail));
                            _eliminatorCorrinDetail.gameObject.SetActive(true);
                        }
                    }
                    Debug.Log($"Activated eliminator portrait for {name}");

                    // Set ring color based on eliminator data
                    if (_eliminatorPortraitRing != null && eliminatorData != null)
                    {
                        // Ensure the ring is active
                        _eliminatorPortraitRing.gameObject.SetActive(true);

                        
                        Color ringColor = Color.white; // default
                        if (eliminatorData.IsBossUnit) 
                        {
                            ringColor = Color.yellow; // gold
                        } 
                        else if (eliminatorData.IsEliteUnit) 
                        {
                            ringColor = new Color(0.75f, 0.75f, 0.75f); // silver
                        }
                        else 
                        {
                            ringColor = new Color(0.8f, 0.5f, 0.2f); // bronze
                        }
                        _eliminatorPortraitRing.color = ringColor;
                        
                    }
                } else
                {
                    _eliminatorPortrait.gameObject.SetActive(false);
                    if (_eliminatorPortraitRing != null)
                    {
                        _eliminatorPortraitRing.gameObject.SetActive(false);
                    }
                    Debug.Log($"No eliminator sprite for {name}, disabling eliminator portrait");
                }
            } else if (_eliminatorPortrait != null)
            {
                _eliminatorPortrait.gameObject.SetActive(false);
                if (_eliminatorPortraitRing != null)
                {
                    _eliminatorPortraitRing.gameObject.SetActive(false);
                }
            }

            if (_enemiesDefeatedText != null && defeatedEnemies != null)
            {
                _enemiesDefeatedText.text = $"{defeatedEnemies.Count}";
            }

            // Create enemy portraits
            if (defeatedEnemies != null && _enemyPortraitPrefab != null)
            {
                CreateEnemyPortraits(defeatedEnemies);
            }
        }

        if (animate)
        {
            // Set up for animation
            _targetPosition = GetComponent<RectTransform>().anchoredPosition;
            GetComponent<RectTransform>().anchoredPosition = _targetPosition - new Vector2(0, _animationDistance);
            _canvasGroup.alpha = 0;
        } else
        {
            // No animation, just show directly
            _canvasGroup.alpha = 1;
        }
    }

    public void SetColor(Color color)
    {
        if (_backgroundImage != null)
        {
            _backgroundImage.color = color;
        }
    }

    public void AnimateEntry(float delay)
    {
        if (!_isAnimating)
            StartCoroutine(AnimateEntryRoutine(delay));
    }

    private IEnumerator AnimateEntryRoutine(float delay)
    {
        _isAnimating = true;

        // Wait for delay
        yield return new WaitForSeconds(delay);

        float startTime = Time.time;
        Vector2 startPosition = GetComponent<RectTransform>().anchoredPosition;

        while (Time.time - startTime < _animationDuration)
        {
            float progress = (Time.time - startTime) / _animationDuration;

            // Fade in
            _canvasGroup.alpha = _fadeInCurve.Evaluate(progress);

            // Move up
            float moveFactor = _moveCurve.Evaluate(progress);
            GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(
                startPosition,
                _targetPosition,
                moveFactor);

            yield return null;
        }

        // Ensure final state is correct
        _canvasGroup.alpha = 1;
        GetComponent<RectTransform>().anchoredPosition = _targetPosition;

        _isAnimating = false;
    }

    public void SetSpeaking(bool isSpeaking)
    {
        if (_speakingIndicator != null)
        {
            _speakingIndicator.SetActive(isSpeaking);
        }
    }

    private void CreateEnemyPortraits(List<EnemyDefeatData> defeatedEnemies)
    {
        // Clear any existing portraits
        ClearEnemyPortraits();

        if (defeatedEnemies == null || defeatedEnemies.Count == 0 || _totalPortraitArea == null)
            return;

        // Group enemies by type
        var bossEnemies = defeatedEnemies.Where(e => e.IsBossUnit).ToList();
        var eliteEnemies = defeatedEnemies.Where(e => e.IsEliteUnit && !e.IsBossUnit).ToList();
        var normalEnemies = defeatedEnemies.Where(e => !e.IsBossUnit && !e.IsEliteUnit).ToList();

        // Determine which enemy types exist
        bool hasBosses = bossEnemies.Count > 0;
        bool hasElites = eliteEnemies.Count > 0;
        bool hasNormals = normalEnemies.Count > 0;
        int typesPresent = (hasBosses ? 1 : 0) + (hasElites ? 1 : 0) + (hasNormals ? 1 : 0);

        if (typesPresent == 0) return;

        // Get total area's dimensions and position
        RectTransform totalRect = _totalPortraitArea;
        Vector2 totalPos = totalRect.anchoredPosition;
        float totalHeight = totalRect.rect.height;
        float totalWidth = totalRect.rect.width;

        // If only one type exists, use the full area position
        if (typesPresent == 1)
        {
            if (hasBosses)
            {
                ResizeContainer(_bossPortraitContainer, totalPos.x, totalPos.y, totalWidth, totalHeight);
                PlacePortraitsInContainer(bossEnemies, _bossPortraitScale, _bossPortraitContainer);
            } else if (hasElites)
            {
                ResizeContainer(_elitePortraitContainer, totalPos.x, totalPos.y, totalWidth, totalHeight);
                PlacePortraitsInContainer(eliteEnemies, _elitePortraitScale, _elitePortraitContainer);
            } else if (hasNormals)
            {
                ResizeContainer(_normalPortraitContainer, totalPos.x, totalPos.y, totalWidth, totalHeight);
                PlacePortraitsInContainer(normalEnemies, _normalPortraitScale, _normalPortraitContainer);
            }
        } else if (typesPresent == 2)
        {
            if (hasBosses && (hasElites || hasNormals))
            {
                // Bosses on left, other type on right
                float leftX = totalPos.x - totalWidth / 4;
                float rightX = totalPos.x + totalWidth / 4;

                ResizeContainer(_bossPortraitContainer, leftX, totalPos.y, totalWidth / 2, totalHeight);

                if (hasElites)
                {
                    ResizeContainer(_elitePortraitContainer, rightX, totalPos.y, totalWidth / 2, totalHeight);
                    PlacePortraitsInContainer(eliteEnemies, _elitePortraitScale, _elitePortraitContainer);
                } else
                {
                    ResizeContainer(_normalPortraitContainer, rightX, totalPos.y, totalWidth / 2, totalHeight);
                    PlacePortraitsInContainer(normalEnemies, _normalPortraitScale, _normalPortraitContainer);
                }
                PlacePortraitsInContainer(bossEnemies, _bossPortraitScale, _bossPortraitContainer);
            } else if (hasElites && hasNormals)
            {
                // No bosses - elites and normals share full width, split vertically
                float topY = totalPos.y + totalHeight / 4;
                float bottomY = totalPos.y - totalHeight / 4;

                ResizeContainer(_elitePortraitContainer, totalPos.x, topY, totalWidth, totalHeight / 2);
                ResizeContainer(_normalPortraitContainer, totalPos.x, bottomY, totalWidth, totalHeight / 2);

                PlacePortraitsInContainer(eliteEnemies, _elitePortraitScale, _elitePortraitContainer);
                PlacePortraitsInContainer(normalEnemies, _normalPortraitScale, _normalPortraitContainer);
            }
        } else // typesPresent == 3
        {
            // All three types exist - bosses on left, elites and normals stacked on right
            float leftX = totalPos.x - totalWidth / 4;
            float rightX = totalPos.x + totalWidth / 4;
            float topY = totalPos.y + totalHeight / 4;
            float bottomY = totalPos.y - totalHeight / 4;

            ResizeContainer(_bossPortraitContainer, leftX, totalPos.y, totalWidth / 2, totalHeight);
            ResizeContainer(_elitePortraitContainer, rightX, topY, totalWidth / 2, totalHeight / 2);
            ResizeContainer(_normalPortraitContainer, rightX, bottomY, totalWidth / 2, totalHeight / 2);

            PlacePortraitsInContainer(bossEnemies, _bossPortraitScale, _bossPortraitContainer);
            PlacePortraitsInContainer(eliteEnemies, _elitePortraitScale, _elitePortraitContainer);
            PlacePortraitsInContainer(normalEnemies, _normalPortraitScale, _normalPortraitContainer);
        }
    }

    private void ResizeContainer(Transform container, float centerX, float centerY, float width, float height)
    {
        RectTransform rect = container as RectTransform;
        if (rect != null)
        {
            // Only modify the container if its size is changing
            bool needsResize = Mathf.Abs(rect.rect.width - width) > 0.01f ||
                              Mathf.Abs(rect.rect.height - height) > 0.01f;

            if (needsResize)
            {
                rect.anchoredPosition = new Vector2(centerX, centerY);
                rect.sizeDelta = new Vector2(width, height);
            }
        }
    }

    private void PlacePortraitsInContainer(List<EnemyDefeatData> enemies, float scale, Transform container)
    {
        if (enemies.Count == 0)
            return;

        RectTransform containerRect = container as RectTransform;
        if (containerRect == null)
            return;

        // Calculate base portrait size accounting for scale
        float basePortraitSize = 17.5f * scale;
        int portraitCount = enemies.Count;

        // Special case for single portrait - just center it
        if (portraitCount == 1)
        {
            GameObject portraitObj = Instantiate(_enemyPortraitPrefab, container);
            RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();

            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);

            portraitRect.anchoredPosition = Vector2.zero;
            portraitRect.localScale = Vector3.one * scale;

            SetupPortraitVisuals(portraitObj, enemies[0]);
            _spawnedEnemyPortraits.Add(portraitObj);
            return;
        }

        // Container bounds in local space
        float halfPortraitSize = basePortraitSize / 2;
        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;

        // Maximum allowed spacing (1.5 times the portrait size)
        float maxSpacing = basePortraitSize * 1.5f;

        // Calculate optimal grid dimensions
        int cols = Mathf.CeilToInt(Mathf.Sqrt(portraitCount));
        int rows = Mathf.CeilToInt((float)portraitCount / cols);

        // Check if portraits fit vertically with current grid
        float maxVerticalPortraits = Mathf.Floor(containerHeight / basePortraitSize);

        while (rows > maxVerticalPortraits && cols < portraitCount)
        {
            cols++;
            rows = Mathf.CeilToInt((float)portraitCount / cols);
        }

        if (rows > maxVerticalPortraits)
        {
            rows = (int)maxVerticalPortraits;
            cols = Mathf.CeilToInt((float)portraitCount / rows);
        }

        // Position portraits
        for (int i = 0; i < portraitCount; i++)
        {
            int row = i / cols;
            int col = i % cols;

            // Calculate how many portraits are actually in this row
            int portraitsInThisRow = Mathf.Min(cols, portraitCount - row * cols);

            // Calculate horizontal position
            float xPos;
            if (portraitsInThisRow == 1)
            {
                xPos = 0; // Center single portrait
            } else
            {
                // Calculate the ideal spacing to fill the container
                float idealHorizontalStep = (containerWidth - basePortraitSize) / (portraitsInThisRow - 1);

                // If spacing exceeds maximum, use max spacing and center the group
                if (idealHorizontalStep > maxSpacing)
                {
                    float groupWidth = (portraitsInThisRow - 1) * maxSpacing;
                    float startX = -groupWidth / 2;
                    xPos = startX + col * maxSpacing;
                } else
                {
                    float startX = -(containerWidth - basePortraitSize) / 2;
                    xPos = startX + col * idealHorizontalStep;
                }
            }

            // Calculate vertical position
            float yPos;
            if (rows == 1)
            {
                yPos = 0; // Center single row
            } else
            {
                // Calculate the ideal spacing to fill the container
                float idealVerticalStep = (containerHeight - basePortraitSize) / (rows - 1);

                // If spacing exceeds maximum, use max spacing and center the group
                if (idealVerticalStep > maxSpacing)
                {
                    float groupHeight = (rows - 1) * maxSpacing;
                    float startY = groupHeight / 2;
                    yPos = startY - row * maxSpacing;
                } else
                {
                    float startY = (containerHeight - basePortraitSize) / 2;
                    yPos = startY - row * idealVerticalStep;
                }
            }

            // Clamp positions to ensure portraits stay within bounds
            float maxX = (containerWidth / 2) - halfPortraitSize;
            float minX = -(containerWidth / 2) + halfPortraitSize;
            float maxY = (containerHeight / 2) - halfPortraitSize;
            float minY = -(containerHeight / 2) + halfPortraitSize;

            xPos = Mathf.Clamp(xPos, minX, maxX);
            yPos = Mathf.Clamp(yPos, minY, maxY);

            // Add slight random offset for visual interest (but keep within bounds)
            float offsetX = Random.Range(-_portraitSpacingX, _portraitSpacingX) * 0.1f;
            float offsetY = Random.Range(-_portraitSpacingY, _portraitSpacingY) * 0.1f;

            // Ensure offset doesn't push portrait out of bounds
            xPos = Mathf.Clamp(xPos + offsetX, minX, maxX);
            yPos = Mathf.Clamp(yPos + offsetY, minY, maxY);

            // Create and position portrait
            GameObject portraitObj = Instantiate(_enemyPortraitPrefab, container);
            RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();

            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);

            portraitRect.anchoredPosition = new Vector2(xPos, yPos);
            portraitRect.localScale = Vector3.one * scale;

            SetupPortraitVisuals(portraitObj, enemies[i]);
            _spawnedEnemyPortraits.Add(portraitObj);
        }
    }

    private void SetupPortraitVisuals(GameObject portraitObj, EnemyDefeatData enemy)
    {
        // Get the portrait sprite for the defeated enemy
        Sprite enemySprite = null;
        var characterData = CharacterAssetLoader.Instance.GetCharacterData(enemy.CharacterName);

        if (characterData != null)
        {
            if (enemy.CharacterName == "Corrin")
            {
                enemySprite = CharacterAssetLoader.Instance.GetOrLoadSprite(CharacterAssetLoader.Instance.GetCorrinFacePrefix(enemy.CorrinIsMale, enemy.CorrinBodyType, enemy.CorrinFace) + " Damage");
                Image hairImage = portraitObj.transform.Find("Corrin Hair")?.GetComponent<Image>();
                hairImage.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(CharacterAssetLoader.Instance.GetCorrinHairPrefix(enemy.CorrinIsMale, enemy.CorrinBodyType, enemy.CorrinHair));
                hairImage.color = enemy.CorrinHairColor;
                hairImage.gameObject.SetActive(true);
                if (enemy.CorrinDetail > 0)
                {
                    Image detailImage = portraitObj.transform.Find("Corrin Detail")?.GetComponent<Image>();
                    detailImage.sprite = CharacterAssetLoader.Instance.GetOrLoadSprite(CharacterAssetLoader.Instance.GetCorrinDetailPrefix(enemy.CorrinIsMale, enemy.CorrinBodyType, enemy.CorrinDetail));
                    detailImage.gameObject.SetActive(true);
                }
            } else
            {
                // Defeated enemies use damage portrait
                enemySprite = CharacterAssetLoader.Instance.GetOrLoadSprite(characterData.portraitPrefix + " Damage");

                // Fallback to neutral if damage isn't available
                if (enemySprite == null)
                {
                    enemySprite = CharacterAssetLoader.Instance.GetOrLoadSprite(characterData.portraitPrefix + " Neutral");
                }
            }
        }

        // Find components in portrait prefab
        Image portraitImage = portraitObj.transform.Find("Kill Character Portrait")?.GetComponent<Image>();
        Image ringImage = portraitObj.transform.Find("Kill Outline")?.GetComponent<Image>();

        // Set portrait image
        if (portraitImage != null && enemySprite != null)
        {
            portraitImage.sprite = enemySprite;
        }

        // Set ring color based on enemy type
        if (ringImage != null)
        {
            Color ringColor = Color.white; // default
            if (enemy.IsBossUnit)
            {
                ringColor = Color.yellow; // gold
            } else if (enemy.IsEliteUnit)
            {
                ringColor = new Color(0.75f, 0.75f, 0.75f); // silver
            } else
            {
                ringColor = new Color(0.8f, 0.5f, 0.2f); // bronze
            }
            ringImage.color = ringColor;
        }

        // Add depth sorting based on enemy type
        Canvas canvas = portraitObj.GetComponent<Canvas>();
        if (canvas != null)
        {
            int sortingOrder = enemy.IsBossUnit ? 30 : enemy.IsEliteUnit ? 20 : 10;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }
    }

    private void CreatePortraitsForEnemyType(List<EnemyDefeatData> enemies, Transform container, float scale)
    {
        if (container == null || enemies.Count == 0) return;

        foreach (var enemy in enemies)
        {
            GameObject portraitObj = Instantiate(_enemyPortraitPrefab, container);

            // Get the portrait sprite for the defeated enemy
            Sprite enemySprite = null;
            var characterData = CharacterAssetLoader.Instance.GetCharacterData(enemy.CharacterName);
            if (characterData != null)
            {
                // Defeated enemies use damage portrait
                enemySprite = CharacterAssetLoader.Instance.GetOrLoadSprite(characterData.portraitPrefix + " Damage");

                // Fallback to neutral if damage isn't available
                if (enemySprite == null)
                {
                    enemySprite = CharacterAssetLoader.Instance.GetOrLoadSprite(characterData.portraitPrefix + " Neutral");
                }
            }

            // Set up the portrait components
            var portraitImage = portraitObj.transform.Find("Kill Character Portrait")?.GetComponent<Image>();
            if (portraitImage != null && enemySprite != null)
            {
                portraitImage.sprite = enemySprite;
            }

            // Set ring color based on enemy type
            var ringImage = portraitObj.transform.Find("Kill Outline")?.GetComponent<Image>(); // Adjust path as needed
            if (ringImage != null)
            {
                Color ringColor = Color.white; // default
                if (enemy.IsBossUnit)
                {
                    ringColor = Color.yellow; // gold
                } else if (enemy.IsEliteUnit)
                {
                    ringColor = new Color(0.75f, 0.75f, 0.75f); // silver
                } else
                {
                    ringColor = new Color(0.8f, 0.5f, 0.2f); // bronze
                }
                ringImage.color = ringColor;
            }

            // Scale the entire portrait
            portraitObj.GetComponent<RectTransform>().localScale = Vector3.one * scale;

            _spawnedEnemyPortraits.Add(portraitObj);
        }
    }

    private void ClearEnemyPortraits()
    {
        foreach (var portrait in _spawnedEnemyPortraits)
        {
            if (portrait != null)
            {
                Destroy(portrait);
            }
        }
        _spawnedEnemyPortraits.Clear();
    }

    private void OnDestroy()
    {
        ClearEnemyPortraits();
    }
}