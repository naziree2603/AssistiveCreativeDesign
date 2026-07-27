    using UnityEngine;

public class UIFlowManager : MonoBehaviour
{
    public static UIFlowManager Instance;

    public AppState CurrentState;

    private void Awake()
    {
        Instance = this;
    }

    public void SetState(AppState newState)
    {
        CurrentState = newState;

        Debug.Log("Current State : " + CurrentState);
    }

    public bool IsBusy()
    {
        return CurrentState == AppState.GeneratingImage
            || CurrentState == AppState.GeneratingDescription
            || CurrentState == AppState.Submitting;
    }
}
