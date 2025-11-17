using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ArenaExitUI : MonoBehaviour
{
    public Button toMainButton;
    public ConfirmationDialog confirmationDialog;
    public AudioClip openClip;

    // Start is called before the first frame update
    private void Start()
    {
        if (toMainButton != null)
            toMainButton.onClick.AddListener(ToMainButtonClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ToMainButtonClick()
    {
        UISfx.Instance.PlayUIAudio(openClip);
        toMainButton.interactable = false;

        confirmationDialog.Show(
            $"This will taint the run! Abandon the arena?",
            () => {
                ToMain();
            },
            () =>
            {
                CancelExit();
            }
        );
    }

    public void ToMain()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void CancelExit()
    {
        Debug.LogWarning("B");
        toMainButton.interactable = true;
    }
}
