using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MiscRecruitMeSettingsUI : MonoBehaviour
{

    [SerializeField] private Toggle corrinOnlyToggle;
    [SerializeField] private Toggle randomWinConditionToggle;
    [SerializeField] private Toggle randomEnemyToggle;
    public AudioClip toggleClip;
    public AudioClip exitClip;

    // Start is called before the first frame update
    void Start()
    {
        RefreshToggles();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCorrinOnlyToggle()
    {
        MenuSettings.Instance.corrinPlayerTeam = corrinOnlyToggle.isOn;
        UISfx.Instance.PlayUIAudio(toggleClip);
    }

    public void OnWinConditionToggle()
    {
        MenuSettings.Instance.randomWinConditions = randomWinConditionToggle.isOn;
        UISfx.Instance.PlayUIAudio(toggleClip);
    }

    public void OnRandomEnemyToggle()
    {
        MenuSettings.Instance.randomEnemyTeam = randomEnemyToggle.isOn;
        UISfx.Instance.PlayUIAudio(toggleClip);
    }

    public void RefreshToggles()
    {
        corrinOnlyToggle.isOn = MenuSettings.Instance.corrinPlayerTeam;
        randomWinConditionToggle.isOn = MenuSettings.Instance.randomWinConditions;
        randomEnemyToggle.isOn = MenuSettings.Instance.randomEnemyTeam;
    }

    public void ExitToMain()
    {
        UISfx.Instance.PlayUIAudio(exitClip);
        SceneManager.LoadScene("Main Menu");
    }
}
