using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance;

    public TMP_Dropdown eventDropdown;

    public ChallengeData CurrentChallenge;

    public int CurrentChallengeIndex = 0;

    public List<ChallengeData> ChallengeList = new List<ChallengeData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetChallengeList(List<ChallengeData> list)
    {
        ChallengeList = list;

        eventDropdown.ClearOptions();

        List<string> options = new List<string>();

        foreach (ChallengeData challenge in list)
        {
            options.Add(challenge.title);
        }

        eventDropdown.AddOptions(options);

        if (list.Count > 0)
        {
            SetCurrentChallenge(list[0], 0);
        }
    }

    public void SetCurrentChallenge(ChallengeData challenge, int index = 0)
    {
        CurrentChallenge = challenge;
        CurrentChallengeIndex = index;

        Debug.Log("Current Challenge : " + challenge.title);
    }

    public async void LoadChallenges()
    {
        ChallengeList =
            await FirestoreChallengeManager.Instance.LoadChallenges();

        eventDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options =
            new List<TMP_Dropdown.OptionData>();

        List<ChallengeData> activeList = ChallengeList.FindAll(x => x.isActive);

        foreach (ChallengeData challenge in ChallengeList)
        {
            options.Add(
                new TMP_Dropdown.OptionData(challenge.title));
        }

        eventDropdown.AddOptions(options);

        eventDropdown.onValueChanged.RemoveAllListeners();
        eventDropdown.onValueChanged.AddListener(OnChallengeChanged);

        if (ChallengeList.Count > 0)
        {
            SetCurrentChallenge(ChallengeList[0], 0);
        }

        Debug.Log("Challenge Loaded");
    }

    void OnChallengeChanged(int index)
    {
        SetCurrentChallenge(ChallengeList[index], index);
    }

    public void ResetChallenge()
    {
        CurrentChallenge = null;
    }
}