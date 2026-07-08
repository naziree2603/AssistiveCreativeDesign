using TMPro;
using UnityEngine;

public class ParticipantAutoSave : MonoBehaviour
{
    public TMP_InputField participantNameInput;
    public TMP_InputField institutionInput;
    public TMP_Dropdown categoryDropdown;

    public TMP_InputField promptInput;
    public TMP_InputField revisionPromptInput;
    public TMP_InputField finalExplanationInput;

    public void SaveParticipantDetails()
    {
        if (ParticipantManager.Instance.CurrentParticipant == null)
            return;

        var data = ParticipantManager.Instance.CurrentParticipant;

        data.participantName = participantNameInput.text;
        data.institution = institutionInput.text;
        data.category = categoryDropdown.options[categoryDropdown.value].text;

        ParticipantManager.Instance.Save();
    }

    public void SavePrompt()
    {
        if (ParticipantManager.Instance.CurrentParticipant == null)
            return;

        ParticipantManager.Instance.CurrentParticipant.prompt =
            promptInput.text;

        ParticipantManager.Instance.Save();
    }

    public void SaveRevision()
    {
        if (ParticipantManager.Instance.CurrentParticipant == null)
            return;

        ParticipantManager.Instance.CurrentParticipant.revisionPrompt =
            revisionPromptInput.text;

        ParticipantManager.Instance.Save();
    }

    public void SaveFinalExplanation()
    {
        if (ParticipantManager.Instance.CurrentParticipant == null)
            return;

        ParticipantManager.Instance.CurrentParticipant.finalExplanation =
            finalExplanationInput.text;

        ParticipantManager.Instance.Save();
    }
}