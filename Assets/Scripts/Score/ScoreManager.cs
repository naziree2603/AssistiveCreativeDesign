using System;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Score UI")]
    [SerializeField] private TMP_InputField finalExplanationInput;

    [SerializeField] private TMP_Text promptQualityText;
    [SerializeField] private TMP_Text posterMessageText;
    [SerializeField] private TMP_Text designOutputText;
    [SerializeField] private TMP_Text accessibilityText;
    [SerializeField] private TMP_Text revisionText;
    [SerializeField] private TMP_Text finalExplanationScoreText;
    [SerializeField] private TMP_Text totalScoreText;

    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text suggestionText;
}



[Serializable]
public class ScoreRequest
{
    public string userPrompt;
    public string imageUrl;
    public string revisionPrompt;
    public string finalExplanation;
}

[Serializable]
public class ScoreResponse
{
    public bool success;

    public ScoreBreakdown score;
}

[Serializable]
public class ScoreBreakdown
{
    public int promptQuality;
    public int posterMessageContent;
    public int designOutputQuality;
    public int accessibilityUnderstanding;
    public int revisionProcess;
    public int finalExplanation;

    public int total;

    public string feedback;
    public string improvementSuggestion;
}


