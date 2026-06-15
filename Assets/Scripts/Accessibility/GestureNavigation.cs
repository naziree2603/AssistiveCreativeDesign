using UnityEngine;

public class GestureNavigation : MonoBehaviour
{
    public PanelManager panelManager;

    float lastTapTime = 0f;

    float holdTime = 0f;


    void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);

            if (t1.phase == TouchPhase.Ended)
            {
                if (Time.time - lastTapTime < 0.5f)
                {
                    panelManager.NextPanel();
                }

                lastTapTime = Time.time;
            }
        }

        if (Input.touchCount == 2)
        {
            holdTime += Time.deltaTime;

            if (holdTime > 1.0f)
            {
                panelManager.PreviousPanel();
                holdTime = 0f;
            }
        }
        else
        {
            holdTime = 0f;
        }
    }
}