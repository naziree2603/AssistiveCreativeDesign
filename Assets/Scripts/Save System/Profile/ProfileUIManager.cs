using System;
using UnityEngine;
using TMPro;

public class ProfileUIManager : MonoBehaviour
{
    public MainMenuManager mainMenuManager;

    public FullPosterImageAPI posterSystem;

    public TMP_InputField participantNameInput;
    public TMP_InputField institutionInput;

    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown subCategoryDropdown;

    public async void SaveProfile()
    {
        AccountData account =
            FirestoreAccountManager.Instance.CurrentAccount;

        ProfileData profile =
            new ProfileData();

        profile.documentID = Guid.NewGuid().ToString();

        profile.accountID = account.documentID;

        profile.participantID = Guid.NewGuid().ToString();

        profile.participantName =
            participantNameInput.text;

        profile.institution =
            institutionInput.text;

        profile.categoryType =
            categoryDropdown.options[
                categoryDropdown.value].text;

        profile.subCategory =
            subCategoryDropdown.options[
                subCategoryDropdown.value].text;

        profile.profileCompleted = true;

        await FirestoreProfileManager.Instance
            .SaveProfile(profile);

        ProfileManager.Instance.CurrentProfile =
            profile;

        ChallengeData challenge =
            ChallengeManager.Instance.CurrentChallenge;

        if (challenge == null)
        {
            Debug.LogError("CurrentChallenge is NULL.");
            return;
        }

        // Create a new entry for this challenge
        ParticipantManager.Instance.InitializeNewEntry(
            profile,
            challenge);

        // Save the new entry to Firestore
        await ParticipantManager.Instance.Save();



        // Clear prompt/revision UI for a new challenge
        posterSystem.PrepareForNewChallenge();

        // Open the Prompt page
        mainMenuManager.OpenPrompt();

        Debug.Log("Profile Created and Entry Created");
    }
}