using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AccessibilityManager : MonoBehaviour
{
    public List<AccessibilityItem> items =
        new List<AccessibilityItem>();

    private int currentIndex = 0;

    public TextToSpeechManager ttsManager;

    float lastTapTime;

    void Start()
    {
        if (items.Count > 0)
        {
            FocusItem(0);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextItem();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousItem();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateCurrentItem();
        }


        DetectSwipe();
        DetectDoubleTap();
    }

    void FocusItem(int index)
    {
        currentIndex = index;

        items[index].Highlight();

        ttsManager.Speak(items[index].accessibilityLabel);
    }

    public void NextItem()
    {
        if (currentIndex < items.Count - 1)
        {
            FocusItem(currentIndex + 1);
        }
    }

    public void PreviousItem()
    {
        if (currentIndex > 0)
        {
            FocusItem(currentIndex - 1);
        }
    }

    private Vector2 startTouch;

    void DetectSwipe()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            startTouch = touch.position;
        }

        if (touch.phase == TouchPhase.Ended)
        {
            Vector2 delta =
                touch.position - startTouch;

            if (Mathf.Abs(delta.x) > 100)
            {
                if (delta.x > 0)
                    NextItem();
                else
                    PreviousItem();
            }
        }
    }

    void DetectDoubleTap()
    {
        if (Input.touchCount == 1)
        {
            Touch touch =
                Input.GetTouch(0);

            if (touch.phase ==
                TouchPhase.Ended)
            {
                if (Time.time - lastTapTime < 0.4f)
                {
                    ActivateCurrentItem();
                }

                lastTapTime = Time.time;
            }
        }
    }

    void ActivateCurrentItem()
    {
        Button btn =
            items[currentIndex]
            .GetComponent<Button>();

        if (btn != null)
        {
            btn.onClick.Invoke();
        }
    }
}