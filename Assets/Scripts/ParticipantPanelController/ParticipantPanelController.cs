using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParticipantPanelController : MonoBehaviour
{
    // =========================================================
    // PARTICIPANT INPUT
    // =========================================================

    [Header("Participant Details")]

    [SerializeField]
    private TMP_InputField participantNameInput;

    [SerializeField]
    private TMP_InputField institutionInput;


    // =========================================================
    // CATEGORY
    // =========================================================

    [Header("Category")]

    [SerializeField]
    private TMP_Dropdown categoryDropdown;

    [SerializeField]
    private TMP_Dropdown subCategoryDropdown;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private Button saveParticipantButton;

    [SerializeField]
    private TMP_Text statusText;


    // =========================================================
    // OPTIONAL
    // =========================================================

    [Header("Options")]

    [SerializeField]
    private bool loadExistingDataOnEnable = true;


    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        if (loadExistingDataOnEnable)
        {
            LoadParticipantDetails();
        }

        ClearStatus();
    }


    // =========================================================
    // LOAD EXISTING PARTICIPANT DETAILS
    // =========================================================
    //
    // Used when:
    //
    // Main Dashboard
    //      ↓
    // Participant Details
    //
    // or:
    //
    // Challenge
    //      ↓
    // Participant Details
    //
    // If participant data already exists,
    // it will be displayed in the fields.
    //
    // =========================================================

    public void LoadParticipantDetails()
    {
        if (ParticipantManager.Instance == null)
        {
            Debug.LogWarning(
                "ParticipantPanelController: ParticipantManager is not available."
            );

            return;
        }


        ParticipantData participant =
            ParticipantManager.Instance
                .GetCurrentParticipant();


        if (participant == null)
        {
            Debug.Log(
                "ParticipantPanelController: No participant data found."
            );

            return;
        }


        // -----------------------------------------------------
        // NAME
        // -----------------------------------------------------

        if (participantNameInput != null)
        {
            participantNameInput.text =
                participant.participantName ?? "";
        }


        // -----------------------------------------------------
        // INSTITUTION
        // -----------------------------------------------------

        if (institutionInput != null)
        {
            institutionInput.text =
                participant.institution ?? "";
        }


        // -----------------------------------------------------
        // CATEGORY
        // -----------------------------------------------------

        SetDropdownValue(
            categoryDropdown,
            participant.categoryType
        );


        // -----------------------------------------------------
        // SUB CATEGORY
        // -----------------------------------------------------

        SetDropdownValue(
            subCategoryDropdown,
            participant.subCategory
        );


        Debug.Log(
            "ParticipantPanelController: Participant details loaded."
        );
    }


    // =========================================================
    // SAVE PARTICIPANT
    // =========================================================
    //
    // Button:
    //
    // SAVE PARTICIPANT
    //
    // This method:
    //
    // 1. Reads UI fields
    // 2. Validates them
    // 3. Updates ParticipantManager
    // 4. Saves to Firebase
    // 5. Decides where to go next
    //
    // =========================================================

    public async void SaveParticipant()
    {
        // ---------------------------------------------------------
        // CHECK PARTICIPANT MANAGER
        // ---------------------------------------------------------

        if (ParticipantManager.Instance == null)
        {
            ShowError(
                "Participant Manager is not available."
            );

            return;
        }


        // ---------------------------------------------------------
        // CHECK ACCOUNT
        // ---------------------------------------------------------

        if (AccountManager.Instance == null)
        {
            ShowError(
                "Account Manager is not available."
            );

            return;
        }


        if (
            !AccountManager.Instance
                .IsUserLoggedIn()
        )
        {
            ShowError(
                "Please login first."
            );

            return;
        }


        // ---------------------------------------------------------
        // GET INPUT
        // ---------------------------------------------------------

        string participantName =
            participantNameInput != null
                ? participantNameInput.text.Trim()
                : "";


        string institution =
            institutionInput != null
                ? institutionInput.text.Trim()
                : "";


        string categoryType =
            GetDropdownValue(
                categoryDropdown
            );


        string subCategory =
            GetDropdownValue(
                subCategoryDropdown
            );


        // ---------------------------------------------------------
        // VALIDATE NAME
        // ---------------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                participantName
            )
        )
        {
            ShowError(
                "Please enter participant name."
            );

            FocusInput(
                participantNameInput
            );

            return;
        }


        // ---------------------------------------------------------
        // VALIDATE INSTITUTION
        // ---------------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                institution
            )
        )
        {
            ShowError(
                "Please enter institution."
            );

            FocusInput(
                institutionInput
            );

            return;
        }


        // ---------------------------------------------------------
        // VALIDATE CATEGORY
        // ---------------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                categoryType
            )
        )
        {
            ShowError(
                "Please select a category."
            );

            return;
        }


        // ---------------------------------------------------------
        // VALIDATE SUB CATEGORY
        // ---------------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                subCategory
            )
        )
        {
            ShowError(
                "Please select a subcategory."
            );

            return;
        }


        // ---------------------------------------------------------
        // DETERMINE ENTRY MODE
        // ---------------------------------------------------------

        bool isChallengeFlow =
            CompetitionManager.Instance != null &&
            CompetitionManager.Instance
                .CurrentParticipantEntryMode ==
            CompetitionManager.ParticipantEntryMode.ChallengeJoin;


        Debug.Log(
            "ParticipantPanelController: Entry mode = " +
            (isChallengeFlow
                ? "ChallengeJoin"
                : "MainDashboard")
        );


        // ---------------------------------------------------------
        // START SAVING
        // ---------------------------------------------------------

        SetSavingState(true);

        ShowStatus(
            "Saving participant details..."
        );


        try
        {
            // -----------------------------------------------------
            // UPDATE LOCAL PARTICIPANT DATA
            // -----------------------------------------------------

            ParticipantManager.Instance
                .SetParticipantDetails(
                    participantName,
                    institution,
                    categoryType,
                    subCategory
                );


            // -----------------------------------------------------
            // SAVE PERMANENT PROFILE
            // -----------------------------------------------------
            //
            // This always happens.
            //
            // Main Dashboard → saves profile
            // Challenge → saves profile
            //
            // -----------------------------------------------------

            bool profileSaved =
                await ParticipantManager.Instance
                    .SaveProfile();


            if (!profileSaved)
            {
                string error =
                    ParticipantManager.Instance
                        .LastError;


                if (
                    string.IsNullOrWhiteSpace(
                        error
                    )
                )
                {
                    error =
                        "Failed to save participant profile.";
                }


                ShowError(error);

                return;
            }


            // -----------------------------------------------------
            // CHALLENGE FLOW
            // -----------------------------------------------------
            //
            // If participant came from Challenge:
            //
            // Save profile
            //      ↓
            // Update challenge submission
            //      ↓
            // Idea Prompt
            //
            // -----------------------------------------------------

            if (isChallengeFlow)
            {
                Debug.Log(
                    "ParticipantPanelController: Challenge flow detected."
                );


                bool submissionSaved =
                    await ParticipantManager.Instance
                        .SaveCurrentSubmission();


                if (!submissionSaved)
                {
                    string error =
                        ParticipantManager.Instance
                            .LastError;


                    if (
                        string.IsNullOrWhiteSpace(
                            error
                        )
                    )
                    {
                        error =
                            "Failed to save challenge information.";
                    }


                    ShowError(error);

                    return;
                }


                Debug.Log(
                    "ParticipantPanelController: " +
                    "Participant profile and challenge submission saved."
                );


                ShowStatus(
                    "Participant details saved successfully."
                );


                // -------------------------------------------------
                // GO TO IDEA PROMPT
                // -------------------------------------------------

                GoToIdeaPrompt();

                return;
            }


            // -----------------------------------------------------
            // MAIN DASHBOARD FLOW
            // -----------------------------------------------------
            //
            // Profile was opened from Main Dashboard.
            //
            // Save only.
            //
            // DO NOT open Idea Prompt.
            //
            // -----------------------------------------------------

            Debug.Log(
                "ParticipantPanelController: " +
                "Main Dashboard profile flow detected."
            );


            ShowStatus(
                "Participant details saved successfully."
            );


            ReturnToMainDashboard();
        }
        catch (Exception exception)
        {
            ShowError(
                "Failed to save participant: " +
                exception.Message
            );


            Debug.LogError(
                "ParticipantPanelController: " +
                exception
            );
        }
        finally
        {
            SetSavingState(false);
        }
    }


    // =========================================================
    // HANDLE SUCCESSFUL SAVE
    // =========================================================
    //
    // There are TWO possible entry paths.
    //
    // ---------------------------------------------------------
    //
    // MAIN DASHBOARD
    //
    // Main Dashboard
    //      ↓
    // Participant Details
    //      ↓
    // Save
    //      ↓
    // Main Dashboard
    //
    // ---------------------------------------------------------
    //
    // CHALLENGE
    //
    // Challenge
    //      ↓
    // Join Event
    //      ↓
    // Participant Details
    //      ↓
    // Save
    //      ↓
    // Idea / Prompt
    //
    // =========================================================



    // =========================================================
    // GO TO IDEA / PROMPT
    // =========================================================

    private async void GoToIdeaPrompt()
    {
        Debug.Log(
            "ParticipantPanelController: Continuing to Idea/Prompt."
        );


        if (UIManager.Instance == null)
        {
            Debug.LogError(
                "ParticipantPanelController: UIManager is not available."
            );

            return;
        }


        // ---------------------------------------------------------
        // SAVE LAST PAGE
        // ---------------------------------------------------------

        if (ParticipantManager.Instance != null)
        {
            ParticipantManager.Instance
                .SetLastPage(
                    "Prompt"
                );


            bool saved =
                await ParticipantManager.Instance
                    .SaveCurrentSubmission();


            if (!saved)
            {
                Debug.LogWarning(
                    "ParticipantPanelController: Failed to save last page."
                );
            }
        }


        // ---------------------------------------------------------
        // OPEN IDEA PROMPT
        // ---------------------------------------------------------

        Debug.Log(
            "ParticipantPanelController: Calling UIManager.OpenIdeaPrompt()."
        );


        UIManager.Instance
            .OpenIdeaPrompt();
    }


    // =========================================================
    // RETURN TO MAIN DASHBOARD
    // =========================================================

    private void ReturnToMainDashboard()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning(
                "ParticipantPanelController: UIManager is not available."
            );

            return;
        }


        UIManager.Instance
            .ShowMainMenu();
    }


    // =========================================================
    // GET DROPDOWN VALUE
    // =========================================================

    private string GetDropdownValue(
        TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return "";
        }


        if (
            dropdown.options == null ||
            dropdown.options.Count == 0
        )
        {
            return "";
        }


        int index =
            dropdown.value;


        if (
            index < 0 ||
            index >= dropdown.options.Count
        )
        {
            return "";
        }


        string value =
            dropdown.options[index]
                .text;


        if (
            string.IsNullOrWhiteSpace(
                value
            )
        )
        {
            return "";
        }


        // -----------------------------------------------------
        // OPTIONAL PLACEHOLDER
        // -----------------------------------------------------
        //
        // If your first dropdown option is:
        //
        // "Select Category"
        //
        // treat it as empty.
        //
        // -----------------------------------------------------

        if (
            string.Equals(
                value.Trim(),
                "Select Category",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return "";
        }


        if (
            string.Equals(
                value.Trim(),
                "Select Sub Category",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return "";
        }


        return value.Trim();
    }


    // =========================================================
    // SET DROPDOWN VALUE
    // =========================================================

    private void SetDropdownValue(
        TMP_Dropdown dropdown,
        string value)
    {
        if (
            dropdown == null ||
            string.IsNullOrWhiteSpace(
                value
            )
        )
        {
            return;
        }


        if (
            dropdown.options == null ||
            dropdown.options.Count == 0
        )
        {
            return;
        }


        for (
            int i = 0;
            i < dropdown.options.Count;
            i++
        )
        {
            string option =
                dropdown.options[i]
                    .text;


            if (
                string.Equals(
                    option.Trim(),
                    value.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                dropdown.SetValueWithoutNotify(
                    i
                );


                dropdown.RefreshShownValue();


                return;
            }
        }


        Debug.LogWarning(
            "ParticipantPanelController: Dropdown option not found: " +
            value
        );
    }


    // =========================================================
    // FOCUS INPUT
    // =========================================================

    private void FocusInput(
        TMP_InputField input)
    {
        if (input == null)
        {
            return;
        }


        input.Select();


        input.ActivateInputField();
    }


    // =========================================================
    // SAVING STATE
    // =========================================================

    private void SetSavingState(
        bool saving)
    {
        if (saveParticipantButton != null)
        {
            saveParticipantButton.interactable =
                !saving;
        }


        if (participantNameInput != null)
        {
            participantNameInput.interactable =
                !saving;
        }


        if (institutionInput != null)
        {
            institutionInput.interactable =
                !saving;
        }


        if (categoryDropdown != null)
        {
            categoryDropdown.interactable =
                !saving;
        }


        if (subCategoryDropdown != null)
        {
            subCategoryDropdown.interactable =
                !saving;
        }
    }


    // =========================================================
    // STATUS
    // =========================================================

    private void ShowStatus(
        string message)
    {
        if (statusText != null)
        {
            statusText.text =
                message;
        }


        Debug.Log(
            "ParticipantPanelController: " +
            message
        );
    }


    // =========================================================
    // ERROR
    // =========================================================

    private void ShowError(
        string message)
    {
        if (statusText != null)
        {
            statusText.text =
                message;
        }


        Debug.LogError(
            "ParticipantPanelController: " +
            message
        );
    }


    // =========================================================
    // CLEAR STATUS
    // =========================================================

    private void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text =
                "";
        }
    }


    // =========================================================
    // CLEAR FORM
    // =========================================================
    //
    // OPTIONAL.
    //
    // Do NOT connect this automatically to OnEnable.
    //
    // This is only for a "Clear" button if you need one.
    //
    // =========================================================

    public void ClearForm()
    {
        if (participantNameInput != null)
        {
            participantNameInput.text =
                "";
        }


        if (institutionInput != null)
        {
            institutionInput.text =
                "";
        }


        if (categoryDropdown != null)
        {
            categoryDropdown.value =
                0;

            categoryDropdown.RefreshShownValue();
        }


        if (subCategoryDropdown != null)
        {
            subCategoryDropdown.value =
                0;

            subCategoryDropdown.RefreshShownValue();
        }


        ClearStatus();


        Debug.Log(
            "ParticipantPanelController: Form cleared."
        );
    }
}