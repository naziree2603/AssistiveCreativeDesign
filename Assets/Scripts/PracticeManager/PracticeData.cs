using System;

[Serializable]
public class PracticeData
{
    // =========================================
    // BASIC PRACTICE DATA
    // =========================================

    public string prompt = "";

    public string originalImageUrl = "";

    public string posterImageUrl = "";

    public string revisedImageUrl = "";

    public string posterDescription = "";

    public string finalExplanation = "";


    // =========================================
    // REVISION
    // =========================================

    public int revisionCount = 0;

    public string revisionPrompt = "";

    public string revisionHistory = "";


    // =========================================
    // SCORE
    // =========================================

    public int score = 0;

    public int promptQuality = 0;

    public int posterMessage = 0;

    public int designQuality = 0;

    public int accessibilityUnderstanding = 0;

    public int revisionProcessScore = 0;

    public int finalExplanationScore = 0;


    // =========================================
    // FEEDBACK
    // =========================================

    public string feedback = "";

    public string improvementSuggestion = "";


    // =========================================
    // HELPER
    // =========================================

    public string GetLatestPosterUrl()
    {
        if (!string.IsNullOrWhiteSpace(revisedImageUrl))
            return revisedImageUrl;

        if (!string.IsNullOrWhiteSpace(posterImageUrl))
            return posterImageUrl;

        return originalImageUrl ?? "";
    }


    public bool HasPoster()
    {
        return !string.IsNullOrWhiteSpace(
            GetLatestPosterUrl()
        );
    }


    public bool HasScore()
    {
        return score > 0;
    }


    public void Reset()
    {
        prompt = "";

        originalImageUrl = "";

        posterImageUrl = "";

        revisedImageUrl = "";

        posterDescription = "";

        finalExplanation = "";

        revisionCount = 0;

        revisionPrompt = "";

        revisionHistory = "";

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
}