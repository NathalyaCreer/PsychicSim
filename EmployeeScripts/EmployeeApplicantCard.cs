using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmployeeApplicantCard : MonoBehaviour
{
    [Header("Profile Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text fortitudeText;
    [SerializeField] private TMP_Text workEthicText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text dailyPayText;

    [Header("Action Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    public void SetupApplicant(
        EmployeeApplicant applicant,
        Action<string> onHire)
    {
        if (applicant == null)
            return;

        DisplayEmployee(applicant);

        actionButton.onClick.RemoveAllListeners();

        if (actionButtonText != null)
            actionButtonText.text = "Hire";

        actionButton.gameObject.SetActive(true);

        actionButton.onClick.AddListener(() =>
        {
            onHire?.Invoke(applicant.applicantId);
        });
    }

    public void SetupEmployee(
        EmployeeApplicant employee,
        Action<string> onFire)
    {
        if (employee == null)
            return;

        DisplayEmployee(employee);

        actionButton.onClick.RemoveAllListeners();

        if (actionButtonText != null)
            actionButtonText.text = "Fire";

        actionButton.gameObject.SetActive(true);

        actionButton.onClick.AddListener(() =>
        {
            onFire?.Invoke(employee.applicantId);
        });
    }

    private void DisplayEmployee(EmployeeApplicant employee)
    {
        nameText.text = employee.employeeName;

        if (roleText != null)
            roleText.text = employee.role.ToString();

        fortitudeText.text = $"Fortitude: {employee.fortitude}";
        workEthicText.text = $"Work Ethic: {employee.workEthic}";
        speedText.text = $"Speed: {employee.speed}";
        dailyPayText.text = $"Daily Pay: ${employee.dailyPay}";
    }
}
