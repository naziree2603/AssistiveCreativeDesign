using System;

[Serializable]
public class ParticipantData
{
    // Unique ID
    public string participantID;

    // Participant Info
    public string participantName;
    public string institution;
    public string category;

    // Current Page
    public string lastPage;

    // Prompt
    public string prompt;

    // AI Image
    public string originalImageUrl;
    public string revisedImageUrl;
    public string originalLocalPath;
    public string revisedLocalPath;

    // Description
    public string posterDescription;

    // Revision
    public string revisionPrompt;
    public int revisionCount;

    // Final
    public string finalExplanation;

    // Score
    public float score;

    public int promptQuality;
    public int posterMessage;
    public int designQuality;
    public int accessibilityUnderstanding;
    public int revisionProcessScore;
    public int finalExplanationScore;

    public string feedback;
    public string improvementSuggestion;

    // Date
    public string createdDate;
}