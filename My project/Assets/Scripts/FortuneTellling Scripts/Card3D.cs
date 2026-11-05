using System.Collections;
using TMPro;
using UnityEngine;

public class Card3D : MonoBehaviour
{
    [Header("Card Visuals")]
    [SerializeField] private GameObject frontFace;
    [SerializeField] private GameObject backFace;
    [SerializeField] private SpriteRenderer artworkRenderer;

    [Header("3D Labels")]
    [SerializeField] private TextMeshPro cardNameLabel;

    [Header("Selection Visual")]
    [SerializeField] private GameObject selectionHighlight;

    private ReadingCards readingManager;
    private Customer.ReadingType readingType;

    public CardData Data { get; private set; }
    public bool IsSelected { get; private set; }

    public void Setup(
        CardData data,
        ReadingCards manager,
        Customer.ReadingType type)
    {
        Data = data;
        readingManager = manager;
        readingType = type;

        if (cardNameLabel != null)
            cardNameLabel.text = data.CardName;

        if (artworkRenderer != null)
            artworkRenderer.sprite = data.Image;

        SetFaceUp(false);
        SetSelected(false);
    }

    public void Interact()
    {
        Debug.Log($"Card3D.Interact reached: {name}");
        
        if (readingManager == null)
        {
            Debug.LogWarning("Card3D: Reading manager reference is missing");
            return;
        }

        readingManager.SelectCard(this);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selectionHighlight != null)
            selectionHighlight.SetActive(selected);
    }

    public void SetFaceUp(bool faceUp)
    {
        if (frontFace != null)
            frontFace.SetActive(faceUp);

        if (backFace != null)
            backFace.SetActive(!faceUp);
    }

    public IEnumerator RevealFlip(float duration)
    {
        // Rotate halfway, switch faces, then finish the flip.
        float halfDuration = Mathf.Max(0.01f, duration * 0.5f);
        Quaternion startRotation = transform.rotation;
        Quaternion halfwayRotation =
            startRotation * Quaternion.Euler(0f, 90f, 0f);
        Quaternion endRotation =
            startRotation * Quaternion.Euler(0f, 180f, 0f);

        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(
                startRotation,
                halfwayRotation,
                Mathf.Clamp01(elapsed / halfDuration)
            );

            yield return null;
        }

        SetFaceUp(true);

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(
                halfwayRotation,
                endRotation,
                Mathf.Clamp01(elapsed / halfDuration)
            );

            yield return null;
        }

        transform.rotation = endRotation;
    }
}
