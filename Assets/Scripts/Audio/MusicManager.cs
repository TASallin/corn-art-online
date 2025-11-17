using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;
    public static MusicManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MusicManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MusicManager");
                    instance = go.AddComponent<MusicManager>();
                }
            }
            return instance;
        }
    }

    [Header("Audio Settings")]
    [SerializeField] private string csvFileName = "music_data";
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private string[] battleSceneNames = { "Battle", "Combat" };

    private AudioSource audioSource;
    private List<MusicTrackData> allTracks = new List<MusicTrackData>();
    private List<MusicTrackData> battleTracks = new List<MusicTrackData>();
    private List<MusicTrackData> menuTracks = new List<MusicTrackData>();

    private MusicTrackData currentTrack;
    private string selectedBattleTrack = "default";
    private string selectedMenuTrack = "default";
    private Coroutine loopCoroutine;
    private float baseVolume = 1f;
    private bool menuMusicPlaying = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.ignoreListenerPause = true;

        LoadMusicData();
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioSettings.OnAudioSettingsChanged += UpdateMusicVolume;

        // Sync with MenuSettings if it exists
        SyncWithMenuSettings();

        UpdateMusicVolume();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AudioSettings.OnAudioSettingsChanged -= UpdateMusicVolume;
    }

    private void SyncWithMenuSettings()
    {
        if (MenuSettings.Instance != null)
        {
            selectedBattleTrack = MenuSettings.Instance.selectedBattleMusic;
            selectedMenuTrack = MenuSettings.Instance.selectedMenuMusic;
        }
    }

    private void LoadMusicData()
    {
        allTracks = CSVMusicReader.ReadMusicCSV(csvFileName);
        battleTracks = allTracks.Where(t => t.isBattleTrack).ToList();
        menuTracks = allTracks.Where(t => t.isMenuTrack).ToList();

        Debug.Log($"Loaded {allTracks.Count} tracks total. Battle: {battleTracks.Count}, Menu: {menuTracks.Count}");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isBattleScene = battleSceneNames.Any(name => scene.name.Contains(name));

        if (isBattleScene)
        {
            PlayBattleMusic();
        } else if (!menuMusicPlaying)
        {
            PlayMenuMusic();
        }
    }

    private void UpdateMusicVolume()
    {
        if (audioSource != null && AudioSettings.Instance != null)
        {
            audioSource.volume = baseVolume * AudioSettings.Instance.GetMusicVolume();
        }
    }

    public void SetBattleTrack(string trackName)
    {
        selectedBattleTrack = trackName;

        // Update MenuSettings if it exists
        if (MenuSettings.Instance != null)
        {
            MenuSettings.Instance.selectedBattleMusic = trackName;
        }

        Scene currentScene = SceneManager.GetActiveScene();
        bool isBattleScene = battleSceneNames.Any(name => currentScene.name.Contains(name));
        if (isBattleScene)
        {
            PlayBattleMusic();
        }
    }

    public void SetMenuTrack(string trackName)
    {
        if (selectedMenuTrack == trackName && menuMusicPlaying)
        {
            return;
        }
        selectedMenuTrack = trackName;

        // Update MenuSettings if it exists
        if (MenuSettings.Instance != null)
        {
            MenuSettings.Instance.selectedMenuMusic = trackName;
        }

        Scene currentScene = SceneManager.GetActiveScene();
        bool isBattleScene = battleSceneNames.Any(name => currentScene.name.Contains(name));
        if (!isBattleScene)
        {
            PlayMenuMusic();
        }
    }

    private void PlayBattleMusic()
    {
        MusicTrackData trackToPlay = null;

        if (selectedBattleTrack == "default")
        {
            // Default implementation: Use level-specific music from MenuSettings
            if (MenuSettings.Instance != null && !string.IsNullOrEmpty(MenuSettings.Instance.levelMusic))
            {
                // Try to find the level-specific music in battle tracks
                trackToPlay = battleTracks.FirstOrDefault(t => t.songName == MenuSettings.Instance.levelMusic);

                if (trackToPlay == null)
                {
                    // If not found in battle tracks, check all tracks
                    trackToPlay = allTracks.FirstOrDefault(t => t.songName == MenuSettings.Instance.levelMusic);
                }

                if (trackToPlay == null)
                {
                    Debug.LogWarning($"Level music '{MenuSettings.Instance.levelMusic}' not found, falling back to random");
                    if (battleTracks.Count > 0)
                    {
                        trackToPlay = battleTracks[Random.Range(0, battleTracks.Count)];
                    }
                }
            } else
            {
                // No level music specified, play random
                if (battleTracks.Count > 0)
                {
                    trackToPlay = battleTracks[Random.Range(0, battleTracks.Count)];
                }
            }
        } else if (selectedBattleTrack == "random")
        {
            if (battleTracks.Count > 0)
            {
                trackToPlay = battleTracks[Random.Range(0, battleTracks.Count)];
            }
        } else
        {
            trackToPlay = battleTracks.FirstOrDefault(t => t.songName == selectedBattleTrack);
        }

        if (trackToPlay != null)
        {
            menuMusicPlaying = false;
            PlayTrack(trackToPlay);
        }
    }

    private void PlayMenuMusic()
    {
        MusicTrackData trackToPlay = null;

        if (selectedMenuTrack == "random")
        {
            if (menuTracks.Count > 0)
            {
                trackToPlay = menuTracks[Random.Range(0, menuTracks.Count)];
            }
        } else
        {
            trackToPlay = menuTracks.FirstOrDefault(t => t.songName == selectedMenuTrack);
        }

        if (trackToPlay != null)
        {
            menuMusicPlaying = true;
            PlayTrack(trackToPlay);
        }
    }

    private void PlayTrack(MusicTrackData track)
    {
        if (currentTrack != null && currentTrack.songName == track.songName && audioSource.isPlaying)
        {
            return;
        }

        StartCoroutine(FadeAndPlayTrack(track));
    }

    private IEnumerator FadeAndPlayTrack(MusicTrackData track)
    {

        float targetVolume = AudioSettings.Instance != null ?
            baseVolume * AudioSettings.Instance.GetMusicVolume() : baseVolume;

        if (audioSource.isPlaying)
        {
            float startVolume = audioSource.volume;
            float elapsedTime = 0;

            while (elapsedTime < fadeTime)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0, elapsedTime / fadeTime);
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            audioSource.Stop();
        }

        AudioClip clip = Resources.Load<AudioClip>($"Audio/Music/{track.songName}");
        if (clip != null)
        {
            // Wait until game is unpaused before starting new music
            while (Time.timeScale == 0)
            {
                yield return null;
            }
            currentTrack = track;
            audioSource.clip = clip;
            audioSource.Play();

            //float elapsedTime = 0;
            //while (elapsedTime < fadeTime)
            //{
            //    audioSource.volume = Mathf.Lerp(0, targetVolume, elapsedTime / fadeTime);
            //    elapsedTime += Time.unscaledDeltaTime;
            //    yield return null;
            //}

            audioSource.volume = targetVolume;

            if (loopCoroutine != null)
            {
                StopCoroutine(loopCoroutine);
            }
            loopCoroutine = StartCoroutine(HandleLooping(track));
        } else
        {
            Debug.LogError($"Could not load audio clip: Audio/Music/{track.songName}");
        }
    }

    private IEnumerator HandleLooping(MusicTrackData track)
    {
        //Debug.LogWarning("Will loop from " + track.loopStart + " to " + track.loopEnd);
        while (audioSource.isPlaying)
        {
            //Debug.Log(audioSource.time);
            // Use unscaledTime for checking loop points
            if (track.loopEnd > 0 && audioSource.time >= track.loopEnd)
            {
                audioSource.time = track.loopStart;
            }
            yield return null;
        }
    }

    public List<string> GetBattleTrackNames()
    {
        List<string> names = new List<string> { "default", "random" };
        names.AddRange(battleTracks.Select(t => t.songName));
        return names;
    }

    public List<string> GetMenuTrackNames()
    {
        List<string> names = new List<string> { "random" };
        names.AddRange(menuTracks.Select(t => t.songName));
        return names;
    }

    public string GetCurrentBattleTrack() => selectedBattleTrack;
    public string GetCurrentMenuTrack() => selectedMenuTrack;
}