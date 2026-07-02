using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Reveals a TextMeshPro text one visible character at a time.
/// Attach this to the same GameObject as a TMP_Text component.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public sealed class TypewriterTMP : MonoBehaviour
{
    [TextArea(4, 12)]
    public string openingText;

    [Header("Timing")]
    [Min(0.001f)]
    public float charactersPerSecond = 18f;
    public float startDelay = 0.35f;
    public bool playOnEnable = true;
    public bool useUnscaledTime = false;

    [Header("End")]
    public bool hideWhenFinished = false;
    public float hideDelay = 2f;

    TMP_Text _text;
    Coroutine _typingRoutine;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    [ContextMenu("Play Typewriter")]
    public void Play()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();

        if (_typingRoutine != null)
            StopCoroutine(_typingRoutine);

        _typingRoutine = StartCoroutine(TypeRoutine());
    }

    IEnumerator TypeRoutine()
    {
        if (!string.IsNullOrEmpty(openingText))
            _text.text = openingText;

        _text.ForceMeshUpdate();
        int characterCount = _text.textInfo.characterCount;
        _text.maxVisibleCharacters = 0;

        if (startDelay > 0f)
            yield return Wait(startDelay);

        float interval = 1f / charactersPerSecond;
        for (int i = 1; i <= characterCount; i++)
        {
            _text.maxVisibleCharacters = i;
            yield return Wait(interval);
        }

        _text.maxVisibleCharacters = int.MaxValue;

        if (hideWhenFinished)
        {
            yield return Wait(hideDelay);
            gameObject.SetActive(false);
        }

        _typingRoutine = null;
    }

    IEnumerator Wait(float seconds)
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(seconds);
        else
            yield return new WaitForSeconds(seconds);
    }
}
