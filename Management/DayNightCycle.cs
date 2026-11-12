
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private bool isApartmentScene;

    [Header("Sun")]
    [SerializeField] private Light sun;
    [SerializeField] private float sunRotationY = -30f;

    [Header("Sun Colors")]
    [SerializeField] private Color dawnSunColor = new Color(1f, 0.72f, 0.45f);
    [SerializeField] private Color daytimeSunColor = Color.white;
    [SerializeField] private Color duskSunColor = new Color(1f, 0.45f, 0.2f);
    [SerializeField] private Color nightSunColor = new Color(0.3f, 0.35f, 0.55f);

    [Header("Sun Rotation")]
    [SerializeField] private float dawnAngle = 10f;
    [SerializeField] private float sunsetAngle = 165f;

    [Header("Lighting")]
    [SerializeField] private float daytimeIntensity = 1.2f;
    [SerializeField] private float nightIntensity = 0.05f;
    [SerializeField] private float duskIntensity = 0.5f;
    [SerializeField] private float dawnIntensity = 0.6f;
    [SerializeField] private float nightAmbientIntensity = 0.2f;
    [SerializeField] private float daytimeAmbientIntensity = 1f;
    [SerializeField] private float duskAmbientIntensity = 0.5f;
    [SerializeField] private float dawnAmbientIntensity = 0.6f;

    private void Start()
    {
        if (sun == null)
        {
            Debug.LogWarning("DayNightCycle: Sun is not assigned.");
            return;
        }

        UpdateLighting();
    }

    private void Update()
    {
        if (sun == null)
            return;

        UpdateLighting();
    }

    private void UpdateLighting()
    {
        if (isApartmentScene)
        {
            SetNight();
            return;
        }

        WorkShift shift = WorkShift.Instance;

        if (shift == null)
            return;

        switch (shift.CurrentPhase)
        {
            case WorkShift.ShiftPhase.Dawn:
                SetSunPosition(dawnAngle);
                sun.color = dawnSunColor;
                sun.intensity = dawnIntensity;
                RenderSettings.ambientIntensity = dawnAmbientIntensity;
                break;


            case WorkShift.ShiftPhase.Daytime:
                float angle = Mathf.Lerp(
                    dawnAngle,
                    sunsetAngle,
                    shift.ShiftProgress01
                );

                SetSunPosition(angle);
                sun.color = daytimeSunColor;
                sun.intensity = daytimeIntensity;
                RenderSettings.ambientIntensity = daytimeAmbientIntensity;
                break;

            case WorkShift.ShiftPhase.Dusk:
                SetSunPosition(sunsetAngle);
                sun.color = duskSunColor;
                sun.intensity = duskIntensity;
                RenderSettings.ambientIntensity = duskAmbientIntensity;
                break;

            case WorkShift.ShiftPhase.Afterhours:
                SetNight();
                break;
        }
    }

    private void SetSunPosition(float angle)
    {
        sun.transform.rotation = Quaternion.Euler(
            angle,
            sunRotationY,
            0f
        );
    }

    private void SetNight()
    {
        SetSunPosition(200f);

        sun.intensity = nightIntensity;
        sun.color = nightSunColor;
        RenderSettings.ambientIntensity = nightAmbientIntensity;
    }
}
