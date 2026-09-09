using System;
using UnityEngine;

[Serializable]
public class ParticipantData
{
    // =========================================================
    // USER / ACCOUNT
    // =========================================================

    public string accountID;
    public string email;
    public string username;


    // =========================================================
    // PARTICIPANT PROFILE
    // =========================================================

    public string participantName;
    public string institution;
    public string categoryType;
    public string subCategory;


    // =========================================================
    // CURRENT CHALLENGE
    // =========================================================

    public string challengeID;
    public string challengeTitle;
    public string eventCode;


    // =========================================================
    // SUBMISSION
    // =========================================================

    public string submissionID;

    // TRUE = final submission has been submitted
    // FALSE = submission is still in progress
    public bool isSubmitted;

    public string completedDate;
    public string lastPage;


    // =========================================================
    // ORIGINAL DESIGN PROMPT
    // =========================================================

    public string prompt;
    public string promptUsed;


    // =========================================================
    // ORIGINAL POSTER
    // =========================================================

    public string originalImageUrl;
    public string storagePath;


    // =========================================================
    // POSTER DESCRIPTION
    // =========================================================

    public string posterDescription;


    // =========================================================
    // REVISION
    // =========================================================

    public string revisionPrompt;
    public string revisionHistory;
    public int revisionCount;
    public string revisedImageUrl;


    // =========================================================
    // FINAL POSTER
    // =========================================================

    public string posterImageUrl;


    // =========================================================
    // FINAL EXPLANATION
    // =========================================================

    public string finalExplanation;


    // =========================================================
    // SCORE
    // =========================================================

    public int score;

    public int promptQuality;
    public int posterMessage;
    public int designQuality;
    public int accessibilityUnderstanding;

    public int revisionProcessScore;
    public int finalExplanationScore;


    // =========================================================
    // FEEDBACK
    // =========================================================

    public string feedback;
    public string improvementSuggestion;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ParticipantData()
    {
        Reset();
    }


    // =========================================================
    // RESET EVERYTHING
    // =========================================================

    public void Reset()
    {
        accountID = "";
        email = "";
        username = "";

        participantName = "";
        institution = "";
        categoryType = "";
        subCategory = "";

        challengeID = "";
        challengeTitle = "";
        eventCode = "";

        submissionID = "";
        isSubmitted = false;
        completedDate = "";
        lastPage = "";

        prompt = "";
        promptUsed = "";

        originalImageUrl = "";
        storagePath = "";

        posterDescription = "";

        revisionPrompt = "";
        revisionHistory = "";
        revisionCount = 0;
        revisedImageUrl = "";

        posterImageUrl = "";

        finalExplanation = "";

        score = 0;
        promptQuality = 0;
        posterMessage = 0;
        designQuality = 0;
        accessibilityUnderstanding = 0;

        revisionProcessScore = 0;
        finalExplanationScore = 0;

        feedback = "";
        improvementSuggestion = "";
    }


    // =========================================================
    // RESET CURRENT CHALLENGE
    // =========================================================

    public void ResetChallengeData()
    {
        challengeID = "";
        challengeTitle = "";
        eventCode = "";

        submissionID = "";
        isSubmitted = false;
        completedDate = "";
        lastPage = "";

        prompt = "";
        promptUsed = "";

        originalImageUrl = "";
        storagePath = "";

        posterDescription = "";

        revisionPrompt = "";
        revisionHistory = "";
        revisionCount = 0;
        revisedImageUrl = "";

        posterImageUrl = "";

        finalExplanation = "";

        score = 0;
        promptQuality = 0;
        posterMessage = 0;
        designQuality = 0;
        accessibilityUnderstanding = 0;

        revisionProcessScore = 0;
        finalExplanationScore = 0;

        feedback = "";
        improvementSuggestion = "";
    }


    // =========================================================
    // PARTICIPANT DETAILS CHECK
    // =========================================================

    public bool HasParticipantDetails()
    {
        return
            !string.IsNullOrWhiteSpace(participantName) &&
            !string.IsNullOrWhiteSpace(institution) &&
            !string.IsNullOrWhiteSpace(categoryType) &&
            !string.IsNullOrWhiteSpace(subCategory);
    }


    // =========================================================
    // CHALLENGE CHECK
    // =========================================================

    public bool HasChallenge()
    {
        return !string.IsNullOrWhiteSpace(challengeID);
    }


    // =========================================================
    // SUBMISSION CHECK
    // =========================================================

    public bool HasSubmission()
    {
        return !string.IsNullOrWhiteSpace(submissionID);
    }


    // =========================================================
    // SUBMITTED CHECK
    // =========================================================

    public bool HasBeenSubmitted()
    {
        return isSubmitted;
    }


    // =========================================================
    // PROMPT CHECK
    // =========================================================

    public bool HasPrompt()
    {
        return !string.IsNullOrWhiteSpace(prompt);
    }


    // =========================================================
    // POSTER CHECK
    // =========================================================
    //
    // A poster exists if either:
    // - a runtime image URL exists, OR
    // - a backend storagePath exists.
    //
    // The actual image is NOT stored in Firestore.
    // =========================================================

    public bool HasPoster()
    {
        return
            !string.IsNullOrWhiteSpace(posterImageUrl) ||
            !string.IsNullOrWhiteSpace(revisedImageUrl) ||
            !string.IsNullOrWhiteSpace(originalImageUrl) ||
            !string.IsNullOrWhiteSpace(storagePath);
    }


    // =========================================================
    // REVISION CHECK
    // =========================================================

    public bool HasRevision()
    {
        return
            revisionCount > 0 &&
            (
                !string.IsNullOrWhiteSpace(revisedImageUrl) ||
                !string.IsNullOrWhiteSpace(storagePath)
            );
    }


    // =========================================================
    // FINAL EXPLANATION CHECK
    // =========================================================

    public bool HasFinalExplanation()
    {
        return !string.IsNullOrWhiteSpace(finalExplanation);
    }


    // =========================================================
    // SCORE CHECK
    // =========================================================

    public bool HasScore()
    {
        return score > 0;
    }


    // =========================================================
    // MARK SUBMITTED
    // =========================================================

    public void MarkSubmitted()
    {
        isSubmitted = true;

        completedDate =
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        lastPage = "Feedback";
    }


    // =========================================================
    // LATEST POSTER
    // =========================================================

    public string GetLatestPosterUrl()
    {
        if (!string.IsNullOrWhiteSpace(revisedImageUrl))
            return revisedImageUrl;

        if (!string.IsNullOrWhiteSpace(posterImageUrl))
            return posterImageUrl;

        return originalImageUrl ?? "";
    }

    // =========================================================
    // GET BACKEND STORAGE PATH
    // =========================================================

    public string GetStoragePath()
    {
        return storagePath ?? "";
    }


    // =========================================================
    // FINAL DESIGN JUSTIFICATION
    // =========================================================

    public int GetFinalDesignJustificationScore()
    {
        return Mathf.Clamp(
            revisionProcessScore +
            finalExplanationScore,
            0,
            20
        );
    }


    // =========================================================
    // CALCULATE TOTAL SCORE
    // =========================================================

    public int CalculateTotalScore()
    {
        int finalDesignJustification =
            GetFinalDesignJustificationScore();

        return Mathf.Clamp(
            promptQuality +
            posterMessage +
            designQuality +
            accessibilityUnderstanding +
            finalDesignJustification,
            0,
            100
        );
    }


    // =========================================================
    // SET SCORE
    // =========================================================

    public void SetScore(
        int promptQualityScore,
        int posterMessageScore,
        int designQualityScore,
        int accessibilityScore,
        int revisionProcessScoreValue,
        int finalExplanationScoreValue)
    {
        promptQuality =
            Mathf.Clamp(promptQualityScore, 0, 20);

        posterMessage =
            Mathf.Clamp(posterMessageScore, 0, 20);

        designQuality =
            Mathf.Clamp(designQualityScore, 0, 20);

        accessibilityUnderstanding =
            Mathf.Clamp(accessibilityScore, 0, 20);

        revisionProcessScore =
            Mathf.Clamp(revisionProcessScoreValue, 0, 10);

        finalExplanationScore =
            Mathf.Clamp(finalExplanationScoreValue, 0, 10);

        score = CalculateTotalScore();
    }


    // =========================================================
    // SET REVISION
    // =========================================================

    public void SetRevision(
        string revisionPromptValue,
        string revisionHistoryValue,
        int revisionCountValue,
        string revisedImageUrlValue)
    {
        revisionPrompt =
            revisionPromptValue ?? "";

        revisionHistory =
            revisionHistoryValue ?? "";

        revisionCount =
            Mathf.Clamp(revisionCountValue, 0, 3);

        revisedImageUrl =
            revisedImageUrlValue ?? "";

        posterImageUrl =
            GetLatestPosterUrl();
    }


    // =========================================================
    // SET FINAL POSTER
    // =========================================================

    public void SetFinalPoster(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        posterImageUrl = imageUrl;
    }


    // =========================================================
    // SET FINAL EXPLANATION
    // =========================================================

    public void SetFinalExplanation(string explanation)
    {
        finalExplanation =
            explanation != null
                ? explanation.Trim()
                : "";
    }


    // =========================================================
    // SET FEEDBACK
    // =========================================================

    public void SetFeedback(
        string feedbackValue,
        string suggestionValue)
    {
        feedback =
            feedbackValue ?? "";

        improvementSuggestion =
            suggestionValue ?? "";
    }


    // =========================================================
    // GENERATE SUBMISSION DOCUMENT ID
    // =========================================================

    public string GetSubmissionDocumentID()
    {
        if (string.IsNullOrWhiteSpace(accountID))
            return "";

        if (string.IsNullOrWhiteSpace(challengeID))
            return "";

        return
            accountID.Trim() +
            "_" +
            challengeID.Trim();
    }


    // =========================================================
    // CAN CONTINUE CHALLENGE
    // =========================================================

    public bool CanContinueChallenge()
    {
        return !isSubmitted;
    }


    // =========================================================
    // CAN SUBMIT
    // =========================================================

    public bool CanSubmit()
    {
        return
            !isSubmitted &&
            HasPrompt() &&
            HasPoster() &&
            HasFinalExplanation();
    }


    // =========================================================
    // REVISION AVAILABLE
    // =========================================================

    public bool CanRevise()
    {
        return
            !isSubmitted &&
            revisionCount < 3;
    }


    // =========================================================
    // REMAINING REVISION ATTEMPTS
    // =========================================================

    public int GetRemainingRevisionCount()
    {
        return Mathf.Max(
            0,
            3 - revisionCount
        );
    }
}