using UnityEngine;

public class GlobalGestureManager : MonoBehaviour
{
    Vector2 startPos;

    private void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
                startPos = touch.position;

            if (touch.phase == TouchPhase.Ended)
            {
                Vector2 delta = touch.position - startPos;

                if (Mathf.Abs(delta.y) > 150f)
                {
                    if (delta.y < 0)
                    {
                        StopSpeech();
                    }
                }
            }
        }
    }

    void StopSpeech()
    {
        UAP_AccessibilityManager.StopSpeaking();

        Debug.Log("Speech Stopped");
    }
}