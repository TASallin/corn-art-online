using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioClip buttonClip;

    // Start is called before the first frame update
    void Start()
    {
        // Reset settings when returning to main menu
        MenuSettings.Instance.Reset();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void RecruitmeButton()
    {
        MenuSettings.Instance.streamMode = "Recruit";
        UISfx.Instance.PlayUIAudio(buttonClip);
        SceneManager.LoadScene("Recruitme Menu");
    }

    public void WheelOfDeathButton()
    {
        MenuSettings.Instance.streamMode = "WheelOfDeath";
        UISfx.Instance.PlayUIAudio(buttonClip);
        SceneManager.LoadScene("Recruitme Menu");
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}