
using UnityEngine;
using UnityEngine.UI;

public class CustomerPatienceUI : MonoBehaviour
{
    [SerializeField] private Slider patienceSlider;
    [SerializeField] private CanvasGroup canvasGroup;

    public void SetPatience(float normalizedPatience)
    {
        if (patienceSlider != null)
            patienceSlider.value = Mathf.Clamp01(normalizedPatience);
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = visible ? 1f : 0f;
        else
            gameObject.SetActive(visible);
    }
}

