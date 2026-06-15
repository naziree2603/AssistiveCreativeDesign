using UnityEngine;

public class PanelManager : MonoBehaviour
{
    public GameObject[] panels;

    private int currentIndex = 0;

    void Start()
    {
        ShowPanel(0);
    }

    public void NextPanel()
    {
        if (currentIndex < panels.Length - 1)
        {
            currentIndex++;
            ShowPanel(currentIndex);
        }
    }

    public void PreviousPanel()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowPanel(currentIndex);
        }
    }

    void ShowPanel(int index)
    {
        foreach (GameObject panel in panels)
        {
            panel.SetActive(false);
        }

        panels[index].SetActive(true);
    }
}