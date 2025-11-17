using UnityEngine;

public class GameUIToggle : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject uiBlockToToggle;
    [SerializeField] private KeyCode toggleKey = KeyCode.Space;
    [SerializeField] private bool startVisible = true;

    private void Start()
    {
        if (uiBlockToToggle != null)
        {
            uiBlockToToggle.SetActive(startVisible);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleUI();
        }
    }

    public void ToggleUI()
    {
        if (uiBlockToToggle != null)
        {
            uiBlockToToggle.SetActive(!uiBlockToToggle.activeSelf);
        }
    }

    public void ShowUI()
    {
        if (uiBlockToToggle != null)
        {
            uiBlockToToggle.SetActive(true);
        }
    }

    public void HideUI()
    {
        if (uiBlockToToggle != null)
        {
            uiBlockToToggle.SetActive(false);
        }
    }

    public bool IsUIVisible()
    {
        return uiBlockToToggle != null && uiBlockToToggle.activeSelf;
    }
}