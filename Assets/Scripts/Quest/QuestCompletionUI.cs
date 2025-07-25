using UnityEngine;
using TMPro;
using System.Collections;

public class QuestCompletionUI : MonoBehaviour
{
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private float displayDuration = 4.0f; // ������� ������ ���������� ����

    [Header("Audio")]  
    [SerializeField] private AudioClip completionSound;
    private AudioSource audioSource;

    private Coroutine currentDisplayCoroutine;

    private void Start()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("QuestManager �� ������! ���� ���������� ������� �� ����� ��������.");
            gameObject.SetActive(false);
            return;
        }

        // ������������� �� ������� ���������� ������
        QuestManager.Instance.OnQuestCompleted += ShowCompletionPopup;

        audioSource = GetComponent<AudioSource>();

        completionPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= ShowCompletionPopup;
        }
    }

    private void ShowCompletionPopup(Quest completedQuest)
    {
        Debug.Log($"<color=cyan>[QuestCompletionUI]</color> ������� ������ � ���������� ������ '{completedQuest.title}'. ������� �������� ������.");

        // ���� ���������� ���� ��� ������������, ������������� ���
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
        }

        // ��������� ������
        questTitleText.text = completedQuest.title;
        rewardText.text = $"Reward: {completedQuest.rewardXP} XP";

        // ���������� ������ � ��������� ������ �� �������
        completionPanel.SetActive(true);

        if (audioSource != null && completionSound != null)
        {
            audioSource.PlayOneShot(completionSound);
        }

        currentDisplayCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        // ���� ��������� ���������� ������
        yield return new WaitForSeconds(displayDuration);

        // �������� ������
        completionPanel.SetActive(false);
        currentDisplayCoroutine = null;
    }
}