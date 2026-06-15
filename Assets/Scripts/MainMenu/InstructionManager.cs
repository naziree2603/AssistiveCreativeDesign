using UnityEngine;

public class InstructionManager : MonoBehaviour
{
    public GameObject[] pages;

    public GameObject instructionPanel;

    private int currentPage = 0;

    private Vector2 touchStartPos;
    private float swipeThreshold = 100f;

    void Start()
    {
        
    }

    void Update()
    {
        if (!instructionPanel.activeSelf)
            return;

#if UNITY_EDITOR

        // Mouse swipe for testing in Unity Editor
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 endPos = Input.mousePosition;
            HandleSwipe(endPos);
        }

#else

    // Touch swipe on Android
    if (Input.touchCount > 0)
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchStartPos = touch.position;
        }

        if (touch.phase == TouchPhase.Ended)
        {
            HandleSwipe(touch.position);
        }
    }

#endif
    }
    public void OpenInstruction()
    {
        instructionPanel.SetActive(true);

        currentPage = 0;

        ShowPage(currentPage);
    }

    private void HandleSwipe(Vector2 endPos)
    {
        Vector2 delta = endPos - touchStartPos;

        if (Mathf.Abs(delta.x) > swipeThreshold)
        {
            if (delta.x > 0)
            {
                NextPage();
            }
            else
            {
                PreviousPage();
            }
        }
    }

    public void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }

        UAP_AccessibilityManager.Say(
            "Instruction page " + (index + 1) + " of " + pages.Length,
            false,
            true
        );
    }

    public void NextPage()
    {
        currentPage++;

        if (currentPage >= pages.Length)
        {
            instructionPanel.SetActive(false);
            return;
        }

        ShowPage(currentPage);
    }

    public void PreviousPage()
    {
        currentPage--;

        if (currentPage < 0)
        {
            instructionPanel.SetActive(false);
            return;
        }

        ShowPage(currentPage);
    }

    public void CloseInstruction()
    {
        instructionPanel.SetActive(false);
    }
}