using TMPro;
using UnityEngine;

public class SamplePromptManager : MonoBehaviour
{
    [Header("Sample Poster Promt")]
    [SerializeField] private TMP_InputField promptInput;
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private GameObject samplePromtPopup;

    [Header("Sample Revision Promt")]
    [SerializeField] private TMP_InputField revisionInput;
    [SerializeField] private GameObject revisePanel;
    [SerializeField] private GameObject sampleRevisePopup;


    [Header("Sample Explain Promt")]
    [SerializeField] private TMP_InputField finalExplanationInput;
    [SerializeField] private GameObject finalExplainationPanel;
    [SerializeField] private GameObject sampleFinalExplainationPopup;


    // POSTER PROMPT
    public void OpenSamplePrompt()
    {
        promptPanel.SetActive(false);
        samplePromtPopup.SetActive(true);

        AndroidTTS.Speak(
            "Sample prompts opened."
        );
    }

    public void CloseSamplePrompt()
    {
        samplePromtPopup.SetActive(false);
        promptPanel.SetActive(true);

        AndroidTTS.Speak(
            "Sample prompts closed."
        );
    }

    public void SampleRecycling()
    {
        promptInput.text =
        "Create a recycling awareness poster using green colours, recycling icons, and the slogan Recycle Today Save Tomorrow. Use large fonts and high colour contrast.";

        samplePromtPopup.SetActive(false);
        promptPanel.SetActive(true);

        AndroidTTS.Speak(
        "Sample prompt inserted."
        );
    }

    public void SampleRoadSafety()
    {
        promptInput.text =
        "Create a road safety poster reminding students to use the pedestrian crossing. Use yellow warning colours and large readable text.";

        samplePromtPopup.SetActive(false);
        promptPanel.SetActive(true);

        AndroidTTS.Speak(
        "Sample prompt inserted."
        );
    }

    public void SampleHealthyEating()
    {
        promptInput.text =
        "Create a healthy eating poster encouraging students to eat fruits and vegetables every day. Use colourful illustrations and large fonts.";

        samplePromtPopup.SetActive(false);
        promptPanel.SetActive(true);

        AndroidTTS.Speak(
        "Sample prompt inserted."
        );
    }


    // REVISION PROMPT

    public void OpenSampleRevise()
    {
        revisePanel.SetActive(false);
        sampleRevisePopup.SetActive(true);
        

        AndroidTTS.Speak(
            "Sample revise prompts opened."
        );
    }

    public void CloseSampleRevise()
    {
        sampleRevisePopup.SetActive(false);
        revisePanel.SetActive(true);
        

        AndroidTTS.Speak(
            "Sample revise prompts closed."
        );
    }

    public void SampleRevisionFont()
    {
        revisionInput.text =
        "Increase the title size and improve the colour contrast for better readability.";

        sampleRevisePopup.SetActive(false);
        revisePanel.SetActive(true);
        

        AndroidTTS.Speak(
            "Sample revision inserted."
        );
    }

    public void SampleRevisionLayout()
    {
        revisionInput.text =
        "Simplify the background, align the elements properly, and add clear icons.";

        sampleRevisePopup.SetActive(false);
        revisePanel.SetActive(true);

        AndroidTTS.Speak(
            "Sample revision inserted."
        );
    }

    public void SampleRevisionAccessibility()
    {
        revisionInput.text =
        "Use larger fonts, higher colour contrast, and improve spacing to enhance accessibility.";

        sampleRevisePopup.SetActive(false);
        revisePanel.SetActive(true);

        AndroidTTS.Speak(
            "Sample revision inserted."
        );
    }

    // FINAL EXPLAINATION

    public void OpenSampledFinal()
    {
        finalExplainationPanel.SetActive(false);
        sampleFinalExplainationPopup.SetActive(true);

        AndroidTTS.Speak(
            "Sample final explaination prompts opened."
        );
    }

    public void CloseSampleFinal()
    {
        sampleFinalExplainationPopup.SetActive(false);
        finalExplainationPanel.SetActive(true);
        

        AndroidTTS.Speak(
            "Sample final explaination prompts closed."
        );
    }

    public void SampleExplanation1()
    {
        finalExplanationInput.text =
        "I improved the poster by increasing the font size, enhancing the colour contrast, and simplifying the layout to make it easier for students to understand.";

        sampleFinalExplainationPopup.SetActive(false);
        finalExplainationPanel.SetActive(true);

        AndroidTTS.Speak(
            "Sample final explanation inserted."
        );
    }

    public void SampleExplanation2()
    {
        finalExplanationInput.text =
        "I revised the poster based on the feedback by improving readability, adding clearer icons, and making the message more accessible.";

        sampleFinalExplainationPopup.SetActive(false);
        finalExplainationPanel.SetActive(true);

        AndroidTTS.Speak(
            "Sample final explanation inserted."
        );
    }

    public void SampleExplanation3()
    {
        finalExplanationInput.text =
        "The final poster uses simple language, high contrast colours, and larger text to ensure it is suitable for all students, including users with accessibility needs.";

        sampleFinalExplainationPopup.SetActive(false);
        finalExplainationPanel.SetActive(true);

        AndroidTTS.Speak(
            "Sample final explanation inserted."
        );
    }
}