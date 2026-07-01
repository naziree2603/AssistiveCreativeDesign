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

    // Description
    public string posterDescription;

    // Revision
    public string revisionPrompt;
    public int revisionCount;

    // Final
    public string finalExplanation;

    // Score
    public float score;

    // Date
    public string createdDate;
}