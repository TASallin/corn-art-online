using UnityEngine;
using UnityEngine.UI;

public class SaveLoadButton : MonoBehaviour
{
    public Button saveButton;
    public Button loadButton;
    public SaveLoadPanelUI saveLoadPanel;
    public AudioClip openClip;

    private void Start()
    {
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OpenSavePanel);
        }

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(OpenLoadPanel);
        }
    }

    public void OpenSavePanel()
    {
        if (saveLoadPanel != null)
        {
            UISfx.Instance.PlayUIAudio(openClip);
            saveLoadPanel.OpenAsSavePanel();
        }
    }

    public void OpenLoadPanel()
    {
        if (saveLoadPanel != null)
        {
            UISfx.Instance.PlayUIAudio(openClip);
            saveLoadPanel.OpenAsLoadPanel();
        }
    }
}