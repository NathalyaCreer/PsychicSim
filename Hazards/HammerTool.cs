using UnityEngine;

public class HammerTool : MonoBehaviour
{
    [Header("Swing")]
    [SerializeField] private Animator animator;
    [SerializeField] private string swingTrigger = "Swing";

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swingSound;

    [Header("Repair")]
    [SerializeField, Min(0.1f)] private float swingInterval = 0.6f;

    public float SwingInterval => swingInterval;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void Swing()
    {
        if (animator != null && !string.IsNullOrEmpty(swingTrigger))
            animator.SetTrigger(swingTrigger);

        if (audioSource != null && swingSound != null)
            audioSource.PlayOneShot(swingSound);
    }
}