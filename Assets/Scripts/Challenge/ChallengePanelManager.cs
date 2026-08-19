//using System.Collections;
//using System.Threading.Tasks;
//using TMPro;
//using UnityEngine;
//using static AccessibilityToggle;

//public class ChallengePanelManager : MonoBehaviour
//{
//    public TMP_InputField eventCodeInput;

//    public TMP_Text statusText;

//    public MainMenuManager mainMenuManager;

//    public FullPosterImageAPI posterSystem;



//    public async void JoinChallengeAsync()
//    {

//        ChallengeData challenge = ChallengeManager.Instance.CurrentChallenge;

//        string accountID = FirestoreAccountManager.Instance.CurrentAccount.documentID;

//        bool alreadySubmitted =
//            await FirestoreEntryManager.Instance.HasCompletedChallenge(
//                accountID,
//                challenge.challengeID);

//        if (alreadySubmitted)
//        {
//            posterSystem.ShowLoading(
//                "You have already submitted this challenge. Please view your submission in History.");

//            StartCoroutine(HidePopupAfterDelay());

//            return;
//        }

//        if (challenge == null)
//        {
//            Debug.Log("No challenge selected.");
//            return;
//        }

//        if (eventCodeInput.text.Trim() != challenge.eventCode)
//        {
//            statusText.text = "Invalid event code.";
//            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("Invalid event code. Please try again.");
//            return;
//        }

//        ChallengeManager.Instance.SetCurrentChallenge(challenge);

//        // Load profile from Firestore
//        bool hasProfile =
//            await FirestoreProfileManager.Instance.LoadCurrentProfile();

//        if (!hasProfile)
//        {
//            Debug.Log("Profile not found.");

//            // Open participant details page
//            mainMenuManager.OpenProfile();

//            return;
//        }

//        // Get the loaded profile
//        ProfileData profile =
//            ProfileManager.Instance.CurrentProfile;

//        // Create a new entry for this challenge
//        ParticipantData unfinished = await FirestoreEntryManager.Instance.GetUnfinishedEntry(accountID, challenge.challengeID);

//        if (unfinished != null)
//        {
//            ParticipantManager.Instance.CurrentParticipant = unfinished;

//            posterSystem.LoadParticipant();

//            mainMenuManager.OpenPrompt();
//        }
//        else
//        {
//            posterSystem.PrepareForNewChallenge();

//            ParticipantManager.Instance.InitializeNewEntry(profile, challenge);

//            mainMenuManager.OpenPrompt();
//        }
//    }

//    private IEnumerator HidePopupAfterDelay()
//    {
//        yield return new WaitForSeconds(4f);

//        posterSystem.HideLoading();
//    }
//}