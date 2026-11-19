
using System.Collections.Generic;
using UnityEngine;

public class EmployeeSpawner : MonoBehaviour
{
    [Header("Employee Setup")]
    [SerializeField] private EmployeeAI employeePrefab;

    [Header("Store Locations")]
    [SerializeField] private Transform[] employeeSpawnPoints;
    [SerializeField] private Transform employeeExitPoint;

    [Header("Cashier Station")]
    [SerializeField] private QueueLine checkoutQueue;
    [SerializeField] private CheckoutUI checkoutUI;
    [SerializeField] private BagSpawn bagSpawn;
    [SerializeField] private Transform cashierStation;

    private readonly List<EmployeeAI> activeEmployees =
        new List<EmployeeAI>();

    public IReadOnlyList<EmployeeAI> ActiveEmployees =>
        activeEmployees;

    private bool shiftActive;
    private bool employeesLeaving;
    private EmployeeAI assignedCashier;

    private void Start()
    {
        if (WorkShift.Instance == null)
        {
            Debug.LogError("EmployeeSpawner: WorkShift is missing.");
            return;
        }

        WorkShift.Instance.PhaseChanged += OnPhaseChanged;

        if (WorkShift.Instance.CurrentPhase ==
            WorkShift.ShiftPhase.Daytime)
        {
            SpawnEmployees();
        }
        else if (WorkShift.Instance.CurrentPhase ==
                 WorkShift.ShiftPhase.Dusk)
        {
            SendEmployeesHome();
        }
    }

    private void OnDestroy()
    {
        if (WorkShift.Instance != null)
            WorkShift.Instance.PhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(WorkShift.ShiftPhase phase)
    {
        switch (phase)
        {
            case WorkShift.ShiftPhase.Daytime:
                SpawnEmployees();
                break;

            case WorkShift.ShiftPhase.Dusk:
                SendEmployeesHome();
                break;
        }
    }

    // --------------------------------------------------
    // SPAWNING
    // --------------------------------------------------

    private void SpawnEmployees()
    {
        if (shiftActive)
            return;

        if (EmployeeManager.Instance == null)
        {
            Debug.LogError("EmployeeManager is missing.");
            return;
        }

        if (employeePrefab == null)
        {
            Debug.LogError("Employee prefab is missing.");
            return;
        }

        if (employeeSpawnPoints == null ||
            employeeSpawnPoints.Length == 0)
        {
            Debug.LogError("No employee spawn points assigned.");
            return;
        }

        shiftActive = true;
        employeesLeaving = false;
        assignedCashier = null;

        IReadOnlyList<EmployeeApplicant> employees =
            EmployeeManager.Instance.HiredEmployees;

        for (int i = 0; i < employees.Count; i++)
        {
            Transform spawnPoint =
                employeeSpawnPoints[i % employeeSpawnPoints.Length];

            if (spawnPoint == null)
                continue;

            EmployeeAI employee = Instantiate(
                employeePrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            employee.Initialize(employees[i]);

            activeEmployees.Add(employee);

            // Only one employee can operate the staffed checkout.
            if (employee.Role == EmployeeRole.Cashier &&
                assignedCashier == null)
            {
                if (ConfigureCashier(employee))
                {
                    assignedCashier = employee;
                }
            }
        }

        if (assignedCashier == null)
        {
            Debug.Log(
                "No cashier assigned. Player-operated checkout remains available."
            );
        }
        else
        {
            Debug.Log(
                $"{assignedCashier.EmployeeName} assigned to the staffed checkout."
            );
        }

        Debug.Log(
            $"Employee shift started. " +
            $"Spawned {activeEmployees.Count} employees."
        );
    }

    private bool ConfigureCashier(EmployeeAI employee)
    {
        if (checkoutQueue == null ||
            checkoutUI == null ||
            bagSpawn == null ||
            cashierStation == null)
        {
            Debug.LogError(
                "EmployeeSpawner: Cashier station references are incomplete. " +
                "Assign the QueueLine, CheckoutUI, BagSpawn, and cashier station."
            );

            return false;
        }

        employee.ConfigureCashier(
            checkoutQueue,
            checkoutUI,
            bagSpawn,
            cashierStation
        );

        return true;
    }

    // --------------------------------------------------
    // ENDING THE SHIFT
    // --------------------------------------------------

    private void SendEmployeesHome()
    {
        if (!shiftActive || employeesLeaving)
            return;

        employeesLeaving = true;

        Debug.Log("Employee shift ending. Sending employees home.");

        activeEmployees.RemoveAll(employee => employee == null);

        if (activeEmployees.Count == 0)
        {
            FinishShift();
            return;
        }

        foreach (EmployeeAI employee in activeEmployees)
        {
            if (employee == null)
                continue;

            employee.RequestEndShift(
                employeeExitPoint,
                () => RemoveEmployee(employee)
            );
        }
    }

    private void RemoveEmployee(EmployeeAI employee)
    {
        activeEmployees.Remove(employee);

        if (assignedCashier == employee)
            assignedCashier = null;

        if (employee != null)
            Destroy(employee.gameObject);

        if (employeesLeaving && activeEmployees.Count == 0)
            FinishShift();
    }

    private void FinishShift()
    {
        shiftActive = false;
        employeesLeaving = false;
        assignedCashier = null;

        activeEmployees.Clear();

        Debug.Log("All employees have left the store.");
    }
}
