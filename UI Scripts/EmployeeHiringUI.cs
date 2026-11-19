
using UnityEngine;

public class EmployeeHiringUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EmployeeManager employeeManager;

    [Header("Applicant Display")]
    [SerializeField] private Transform applicantContainer;
    [SerializeField] private EmployeeApplicantCard applicantCardPrefab;

    [Header("Hired Employee Display")]
    [SerializeField] private Transform employeeContainer;


    private void OnEnable()
    {
        Debug.Log("EmployeeHiringUI enabled.");

        if (employeeManager == null)
            employeeManager = EmployeeManager.Instance;

        if (employeeManager == null)
        {
            Debug.LogError("EmployeeHiringUI: EmployeeManager is missing!");
            return;
        }

        if (applicantContainer == null)
        {
            Debug.LogError("EmployeeHiringUI: Applicant Container is not assigned!");
            return;
        }

        if (applicantCardPrefab == null)
        {
            Debug.LogError("EmployeeHiringUI: Applicant Card Prefab is not assigned!");
            return;
        }

        employeeManager.OnApplicantsChanged += RefreshApplicants;
        employeeManager.OnEmployeesChanged += RefreshEmployees;

        RefreshApplicants();
        RefreshEmployees();
    }


    private void OnDisable()
    {
        if (employeeManager == null)
            return;

        employeeManager.OnApplicantsChanged -= RefreshApplicants;
        employeeManager.OnEmployeesChanged -= RefreshEmployees;
    }

    // --------------------------------------------------
    // APPLICANTS
    // --------------------------------------------------

    private void RefreshApplicants()
    {
        if (employeeManager == null ||
            applicantContainer == null ||
            applicantCardPrefab == null)
            return;

        Debug.Log(
            $"Refreshing applicants. Count: {employeeManager.Applicants.Count}"
        );

        ClearContainer(applicantContainer);

        foreach (EmployeeApplicant applicant in employeeManager.Applicants)
        {
            EmployeeApplicantCard card = Instantiate(
                applicantCardPrefab,
                applicantContainer,
                false
            );

            card.gameObject.SetActive(true);
            card.SetupApplicant(applicant, HandleHire);

            Debug.Log($"Created applicant card for {applicant.employeeName}");
        }

        Debug.Log(
            $"Applicant container now has {applicantContainer.childCount} children."
        );
    }

    // --------------------------------------------------
    // HIRED EMPLOYEES
    // --------------------------------------------------

    private void RefreshEmployees()
    {
        if (employeeManager == null ||
            employeeContainer == null ||
            applicantCardPrefab == null)
            return;

        ClearContainer(employeeContainer);

        foreach (EmployeeApplicant employee in employeeManager.HiredEmployees)
        {
            EmployeeApplicantCard card = Instantiate(
                applicantCardPrefab,
                employeeContainer
            );

            card.SetupEmployee(employee, HandleFire);
        }
    }

    // --------------------------------------------------
    // BUTTON ACTIONS
    // --------------------------------------------------

    private void HandleHire(string applicantId)
    {
        if (employeeManager == null)
            return;

        employeeManager.HireApplicant(applicantId);
    }

    private void HandleFire(string employeeId)
    {
        if (employeeManager == null)
            return;

        employeeManager.FireEmployee(employeeId);
    }

    // --------------------------------------------------
    // UI CLEANUP
    // --------------------------------------------------

    private void ClearContainer(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}
