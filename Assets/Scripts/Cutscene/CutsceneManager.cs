using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class CutsceneManager : MonoBehaviour
{
    [System.Serializable]
    public class CutsceneFrame
    {
        public Sprite image;
        public string text;
        public AudioClip audioClip;
        [HideInInspector] public float typingSpeed = 20f;
        [HideInInspector] public string[] sentences;
        [HideInInspector] public float fadeDuration = 2f;
        public bool hideUI = false;
        public bool skipAudio = false;
        public bool syncAudioWithText = true;
    }

    [Header("Fade Settings")]
    private float minFadeAlpha = 0.1f;
    private float maxFadeAlpha = 1f;

    public CutsceneFrame[] frames;
    public Image displayImage;
    public GameObject textPanel;
    public TextMeshProUGUI displayText;
    public GameObject cutsceneCanvas;
    public Button skipButton;
    private Coroutine typingCoroutine;
    private Sprite originalSprite;
    public AudioSource radioMusic;
    public AudioSource radioMusicOff;
    public AudioSource audioSource;
    public AudioSource closeSource;
    private AudioClip closeClip;
    private Color originalColor;

    private float delayBeforeLoad = 2f;
    private int currentFrameIndex = 0;
    private int currentSentenceIndex = 0;

    private bool isCutsceneActive = false;
    private bool isTyping = false;

    void Start()
    {
        originalSprite = displayImage.sprite;
        originalColor = displayImage.color;

        cutsceneCanvas.SetActive(true);
        skipButton.gameObject.SetActive(true);
        textPanel.SetActive(true);
        displayText.gameObject.SetActive(true);

        displayImage.sprite = frames[0].image;
        displayImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, maxFadeAlpha);

        if (radioMusicOff != null)
        {
            radioMusicOff.Stop();
        }

        foreach (var frame in frames)
        {
            var sentences = new List<string>();
            int start = 0;
            bool inRadioVoice = false;

            for (int i = 0; i < frame.text.Length; i++)
            {
                if (i + 1 < frame.text.Length && frame.text[i] == '*' && !inRadioVoice)
                {
                    if (i > start)
                    {
                        string regularSentence = frame.text.Substring(start, i - start).Trim();
                        if (!string.IsNullOrEmpty(regularSentence))
                        {
                            sentences.Add(regularSentence);
                        }
                    }
                    start = i;
                    inRadioVoice = true;
                }
                else if (frame.text[i] == '*' && inRadioVoice)
                {
                    string radioSentence = frame.text.Substring(start, i - start + 1).Trim();
                    sentences.Add(radioSentence);
                    start = i + 1;
                    inRadioVoice = false;
                }
                else if (!inRadioVoice && (frame.text[i] == '.' || frame.text[i] == '!' || frame.text[i] == '?'))
                {
                    if (i > start)
                    {
                        string sentence = frame.text.Substring(start, i - start + 1).Trim();
                        sentences.Add(sentence);
                    }
                    start = i + 1;
                }
            }

            if (start < frame.text.Length)
            {
                string lastPart = frame.text.Substring(start).Trim();
                if (!string.IsNullOrEmpty(lastPart))
                {
                    sentences.Add(lastPart);
                }
            }

            frame.sentences = sentences.ToArray();
        }

        StartCutsceneImmediately();

        if (radioMusic != null)
        {
            radioMusic.Play();
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(InstantSkipCutscene);
        }
    }

    void StartCutsceneImmediately()
    {
        isCutsceneActive = true;
        PlayFrameAudio(currentFrameIndex);
        ShowFirstSentence();

        if (radioMusic != null)
        {
            StartCoroutine(FadeAudio(radioMusic, 0f, 1f, frames[0].fadeDuration));
        }
    }

    void PlayFrameAudio(int frameIndex)
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (frameIndex < 0 || frameIndex >= frames.Length) return;

        var frame = frames[frameIndex];
        if (frame.skipAudio) return;

        if (frame.audioClip != null && audioSource != null)
        {
            audioSource.clip = frame.audioClip;
            audioSource.Play();

            if (frame.syncAudioWithText)
            {
                StartCoroutine(AutoAdvanceAfterAudio(frame.audioClip.length));
            }
        }
    }

    IEnumerator AutoAdvanceAfterAudio(float clipLength)
    {
        yield return new WaitForSeconds(clipLength);

        if (isCutsceneActive && !isTyping)
        {
            ShowNextFrame();
        }
    }

    void Update()
    {
        if (!isCutsceneActive) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                StopTypingAndShowFullText();
            }
            else
            {
                ShowNextSentenceOrFrame();
            }
        }
    }

    void ShowFirstSentence()
    {
        currentSentenceIndex = 0;
        ShowCurrentSentence();
    }

    void ShowCurrentSentence()
    {
        var frame = frames[currentFrameIndex];

        if (frame.hideUI)
        {
            textPanel.gameObject.SetActive(false);
            displayText.gameObject.SetActive(false);
        }
        else
        {
            textPanel.gameObject.SetActive(true);
            displayText.gameObject.SetActive(true);
        }

        if (currentSentenceIndex < frame.sentences.Length)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeText(frame.sentences[currentSentenceIndex], frame.typingSpeed));
        }
    }

    void ShowNextSentenceOrFrame()
    {
        var frame = frames[currentFrameIndex];
        currentSentenceIndex++;

        if (currentSentenceIndex < frame.sentences.Length)
        {
            ShowCurrentSentence();
        }
        else
        {
            currentSentenceIndex = 0;
            ShowNextFrame();
        }
    }

    void ShowFrame(int index)
    {
        if (currentFrameIndex + 1 < frames.Length)
        {
            StartCoroutine(TransitionToNextFrame());
        }
        else
        {
            EndCutscene();
        }
    }

    IEnumerator TransitionToNextFrame()
    {
        int nextFrameIndex = currentFrameIndex + 1;
        float fadeDuration = frames[currentFrameIndex].fadeDuration;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        yield return StartCoroutine(FadeImage(1f, minFadeAlpha, fadeDuration / 2));

        displayImage.sprite = frames[nextFrameIndex].image;
        PlayFrameAudio(nextFrameIndex);

        yield return StartCoroutine(FadeImage(minFadeAlpha, maxFadeAlpha, fadeDuration / 2));

        if (nextFrameIndex == 5)
        {
            if (radioMusic != null && radioMusic.isPlaying)
            {
                radioMusic.Stop();
            }
            if (radioMusicOff != null)
            {
                radioMusicOff.Play();
            }
        }

        currentFrameIndex = nextFrameIndex;
        ShowFirstSentence();
    }

    IEnumerator FadeImage(float startAlpha, float endAlpha, float duration)
    {
        float time = 0;
        Color color = displayImage.color;

        while (time < duration)
        {
            time += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            displayImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        displayImage.color = color;
    }

    IEnumerator FadeAudio(AudioSource audioSource, float startVolume, float endVolume, float duration)
    {
        if (!audioSource.isPlaying && endVolume > 0)
        {
            audioSource.volume = 0;
            audioSource.Play();
        }

        float time = 0;
        audioSource.volume = startVolume;

        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, endVolume, time / duration);
            yield return null;
        }

        audioSource.volume = endVolume;

        if (endVolume <= 0)
        {
            audioSource.Stop();
        }
    }

    IEnumerator TypeText(string text, float typingSpeed)
    {
        isTyping = true;
        displayText.text = "";

        var frame = frames[currentFrameIndex];
        bool isRadioVoice = text.StartsWith("*") && text.EndsWith("*");
        string displayTextContent = isRadioVoice ? text.Trim('*') : text;

        if (frame.audioClip != null && frame.syncAudioWithText && !frame.skipAudio)
        {
            float sentenceDuration = frame.audioClip.length / frame.sentences.Length;

            displayText.text = displayTextContent;

            yield return new WaitForSeconds(sentenceDuration);

            if (!isRadioVoice)
            {
                ShowNextSentenceOrFrame();
            }
        }
        else
        {
            foreach (char letter in displayTextContent.ToCharArray())
            {
                displayText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTyping = false;
    }

    void StopTypingAndShowFullText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        displayText.text = frames[currentFrameIndex].sentences[currentSentenceIndex];
        isTyping = false;
    }

    void ShowNextFrame()
    {
        if (currentFrameIndex + 1 == 5)
        {
            if (radioMusic != null && radioMusic.isPlaying)
            {
                radioMusic.Stop();
            }
        }

        ShowFrame(currentFrameIndex + 1);
    }

    void InstantSkipCutscene()
    {
        if (!isCutsceneActive) return;

        isCutsceneActive = false;

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (radioMusic != null)
        {
            radioMusic.Stop();
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        StopAllCoroutines();
        PrepareForSceneTransition();
    }

    void PrepareForSceneTransition()
    {
        displayImage.sprite = originalSprite;
        displayText.text = "";

        skipButton.gameObject.SetActive(false);
        textPanel.gameObject.SetActive(false);
        displayText.gameObject.SetActive(false);

        StartCoroutine(LoadGameSceneAfterDelay());
    }

    void EndCutscene()
    {
        isCutsceneActive = false;
        PrepareForSceneTransition();
    }

    IEnumerator LoadGameSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene("SampleScene");
    }
}