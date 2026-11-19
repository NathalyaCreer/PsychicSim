
using UnityEngine;

public class MessCleaningTool : MonoBehaviour
{
    public enum ToolType
    {
        Mop,
        TrashBag
    }

    [Header("Tool Settings")]
    [SerializeField] private ToolType toolType;

    [Header("Cleaning Settings")]
    [SerializeField] private float cleaningDistance = 3f;
    [SerializeField] private LayerMask messLayerMask = ~0;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spillCleaningSound;
    [SerializeField] private AudioClip trashPickupSound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchVariation = new Vector2(0.9f, 1.1f);

    public ToolType Type => toolType;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public bool TryClean(Camera playerCamera)
    {
        if (playerCamera == null)
            return false;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            cleaningDistance,
            messLayerMask,
            QueryTriggerInteraction.Collide))
        {
            return false;
        }

        StoreMess mess =
            hit.collider.GetComponentInParent<StoreMess>();

        if (mess == null)
            return false;

        bool correctMess =
            (toolType == ToolType.Mop &&
             mess.Type == StoreMess.MessType.Spill)
            ||
            (toolType == ToolType.TrashBag &&
             mess.Type == StoreMess.MessType.Trash);

        if (!correctMess)
            return false;

        PlayCleaningSound();

        mess.CompleteCleanup();

        return true;
    }

    private void PlayCleaningSound()
    {
        if (audioSource == null)
            return;

        AudioClip clip = toolType == ToolType.Mop
            ? spillCleaningSound
            : trashPickupSound;

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(
            pitchVariation.x,
            pitchVariation.y
        );

        audioSource.PlayOneShot(clip, volume);
    }
}
