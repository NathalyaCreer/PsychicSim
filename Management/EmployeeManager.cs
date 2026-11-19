
using System;
using System.Collections.Generic;
using UnityEngine;

public class EmployeeManager : MonoBehaviour
{
    public static EmployeeManager Instance { get; private set; }

    [Header("Applicant Generation")]
    [SerializeField, Min(1)] private int minimumApplicants = 3;
    [SerializeField, Min(1)] private int maximumApplicants = 4;

    [Header("Employee Role Generation")]
    [SerializeField, Range(0, 100)]
    private int cashierChance = 50;

    [SerializeField, Range(0, 100)]
    private int maintenanceChance = 50;

    [Header("Random Stat Range")]
    [SerializeField] private int minimumStat = 20;
    [SerializeField] private int maximumStat = 100;

    [Header("Daily Pay")]
    [SerializeField] private int baseDailyPay = 15;
    [SerializeField] private float payPerStatPoint = 0.15f;

    [Header("Unpaid Employees")]
    [SerializeField, Min(1)]
    private int missedPaymentsBeforeQuitting = 2;

    private readonly List<EmployeeApplicant> applicants =
        new List<EmployeeApplicant>();

    public IReadOnlyList<EmployeeApplicant> Applicants => applicants;

    private readonly List<EmployeeApplicant> hiredEmployees =
        new List<EmployeeApplicant>();

    public IReadOnlyList<EmployeeApplicant> HiredEmployees =>
        hiredEmployees;

    private readonly Dictionary<string, int> missedPayments =
        new Dictionary<string, int>();

    private int lastPayrollDayNumber = -1;

    public event Action OnApplicantsChanged;
    public event Action OnEmployeesChanged;
    public event Action<int, int, int> OnPayrollCompleted;
    public event Action<EmployeeApplicant> OnEmployeeDied;
    public event Action<EmployeeApplicant> OnEmployeeQuit;

    private readonly string[] firstNames =
    {
        "Alex", "Jordan", "Morgan", "Taylor", "Riley", "Evan",
        "Caseo", "Mark", "Safiya", "Aisha", "Nia", "Zara",
        "Liam", "Noah", "Oliver", "Elijah", "Lucas", "Casey",
        "Jamie", "Drew", "Avery", "Cameron", "Ilyas", "Toyin",
        "Ana", "Lucia", "Malinda", "Marcella", "Mitchell",
        "Matthew", "Elliot", "Harper", "Logan", "Quinn",
        "Rowan", "Harmony", "Jose", "Deysi", "Abiola", "Ari",
        "Melanie", "Ademar", "Umran", "Sam", "Parker", "Reese",
        "Blake", "Dakota", "Saed", "Devin", "Jesse", "Kendall",
        "Skyler", "Tatum", "Emerson", "Finley", "Hayden"
    };

    private readonly string[] lastNames =
    {
        "Bennett", "Carter", "Hayes", "Morgan", "Parker",
        "Xue", "Young", "Adams", "Baker", "Clark", "Reed",
        "Brooks", "Foster", "Graves", "Holloway", "Pedro",
        "Hai", "Ismaeel", "Solano", "Miller", "Sullivan",
        "Turner", "Walker", "Wells", "Bishop", "Coleman",
        "Price", "Ramsey", "Stone"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GenerateDailyApplicants();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // --------------------------------------------------
    // APPLICANT GENERATION
    // --------------------------------------------------

    public void GenerateDailyApplicants()
    {
        applicants.Clear();

        int min = Mathf.Max(1, minimumApplicants);
        int max = Mathf.Max(min, maximumApplicants);

        int count = UnityEngine.Random.Range(min, max + 1);

        HashSet<string> generatedNames = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            applicants.Add(CreateRandomApplicant(generatedNames));
        }

        OnApplicantsChanged?.Invoke();

        Debug.Log($"Generated {applicants.Count} employee applicants.");
    }

    private EmployeeApplicant CreateRandomApplicant(
        HashSet<string> generatedNames)
    {
        string fullName;

        do
        {
            string firstName = firstNames[
                UnityEngine.Random.Range(0, firstNames.Length)
            ];

            string lastName = lastNames[
                UnityEngine.Random.Range(0, lastNames.Length)
            ];

            fullName = $"{firstName} {lastName}";

        } while (generatedNames.Contains(fullName));

        generatedNames.Add(fullName);

        int minStat = Mathf.Clamp(minimumStat, 0, 100);
        int maxStat = Mathf.Clamp(maximumStat, minStat, 100);

        int fortitude = UnityEngine.Random.Range(minStat, maxStat + 1);
        int workEthic = UnityEngine.Random.Range(minStat, maxStat + 1);
        int speed = UnityEngine.Random.Range(minStat, maxStat + 1);

        EmployeeRole role = GenerateRandomRole();

        int dailyPay = baseDailyPay + Mathf.RoundToInt(
            (fortitude + workEthic + speed) * payPerStatPoint
        );

        return new EmployeeApplicant(
            Guid.NewGuid().ToString(),
            fullName,
            role,
            fortitude,
            workEthic,
            speed,
            dailyPay
        );
    }

    private EmployeeRole GenerateRandomRole()
    {
        int totalChance = Mathf.Max(
            0,
            cashierChance
        ) + Mathf.Max(
            0,
            maintenanceChance
        );

        if (totalChance <= 0)
            return EmployeeRole.Cashier;

        int roll = UnityEngine.Random.Range(0, totalChance);

        if (roll < cashierChance)
            return EmployeeRole.Cashier;

        return EmployeeRole.Maintenance;
    }

    // --------------------------------------------------
    // HIRING
    // --------------------------------------------------

    public bool HireApplicant(string applicantId)
    {
        EmployeeApplicant applicant = applicants.Find(
            a => a.applicantId == applicantId
        );

        if (applicant == null)
            return false;

        hiredEmployees.Add(applicant);
        applicants.Remove(applicant);

        OnApplicantsChanged?.Invoke();
        OnEmployeesChanged?.Invoke();

        Debug.Log(
            $"Hired {applicant.employeeName} as {applicant.role}."
        );

        return true;
    }

    // --------------------------------------------------
    // FIRING
    // --------------------------------------------------

    public bool FireEmployee(string employeeId)
    {
        EmployeeApplicant employee = hiredEmployees.Find(
            e => e.applicantId == employeeId
        );

        if (employee == null)
            return false;

        hiredEmployees.Remove(employee);
        missedPayments.Remove(employeeId);

        OnEmployeesChanged?.Invoke();

        Debug.Log($"Fired employee: {employee.employeeName}");

        return true;
    }

    // --------------------------------------------------
    // DAILY PAYROLL
    // --------------------------------------------------

    public void PayEmployeesForDay(int dayNumber)
    {
        if (lastPayrollDayNumber == dayNumber)
        {
            Debug.LogWarning(
                $"Payroll has already been processed for Day {dayNumber}."
            );
            return;
        }

        lastPayrollDayNumber = dayNumber;

        if (hiredEmployees.Count == 0)
        {
            Debug.Log("No employees to pay today.");
            OnPayrollCompleted?.Invoke(0, 0, 0);
            return;
        }

        if (MoneyManager.Instance == null)
        {
            Debug.LogError(
                "Cannot process employee payroll. MoneyManager is missing."
            );

            return;
        }

        int totalPaid = 0;
        int unpaidCount = 0;
        int quitCount = 0;

        for (int i = hiredEmployees.Count - 1; i >= 0; i--)
        {
            EmployeeApplicant employee = hiredEmployees[i];

            int wage = Mathf.Max(0, employee.dailyPay);

            bool paid = wage == 0 ||
                        MoneyManager.Instance.TrySpendMoney(wage);

            if (paid)
            {
                totalPaid += wage;
                missedPayments.Remove(employee.applicantId);

                Debug.Log(
                    $"Paid {employee.employeeName} ${wage} for Day {dayNumber}."
                );
            }
            else
            {
                unpaidCount++;

                if (!missedPayments.ContainsKey(employee.applicantId))
                    missedPayments[employee.applicantId] = 0;

                missedPayments[employee.applicantId]++;

                int missedCount = missedPayments[employee.applicantId];

                Debug.LogWarning(
                    $"{employee.employeeName} was not paid. " +
                    $"Missed payments: {missedCount}/{missedPaymentsBeforeQuitting}."
                );
                if (missedCount >= missedPaymentsBeforeQuitting)
                {
                    employee.MarkWillQuit();

                    hiredEmployees.RemoveAt(i);
                    missedPayments.Remove(employee.applicantId);

                    quitCount++;

                    OnEmployeeQuit?.Invoke(employee);

                    Debug.LogWarning(
                        $"{employee.employeeName} quit because they were not paid."
                    );
                }
            }
        }

        if (quitCount > 0)
            OnEmployeesChanged?.Invoke();

        Debug.Log(
            $"Day {dayNumber} payroll complete. " +
            $"Paid: ${totalPaid} | Unpaid: {unpaidCount} | Quit: {quitCount}"
        );

        OnPayrollCompleted?.Invoke(totalPaid, unpaidCount, quitCount);
    }

    // --------------------------------------------------
    // EMPLOYEE HEALTH AND DEATH
    // --------------------------------------------------

    public void DamageEmployee(string employeeId, int damage)
    {
        EmployeeApplicant employee = hiredEmployees.Find(
            e => e.applicantId == employeeId
        );

        if (employee == null || !employee.IsAlive)
            return;

        bool died = employee.TakeDamage(damage);

        if (!died)
        {
            OnEmployeesChanged?.Invoke();
            return;
        }

        hiredEmployees.Remove(employee);
        missedPayments.Remove(employeeId);

        OnEmployeeDied?.Invoke(employee);
        OnEmployeesChanged?.Invoke();

        Debug.LogWarning(
            $"{employee.employeeName} has died and is no longer employed."
        );
    }

    // --------------------------------------------------
    // EMPLOYEE QUITTING
    // --------------------------------------------------

    public void QuitEmployee(string employeeId)
    {
        EmployeeApplicant employee = hiredEmployees.Find(
            e => e.applicantId == employeeId
        );

        if (employee == null)
            return;

        employee.MarkWillQuit();

        hiredEmployees.Remove(employee);
        missedPayments.Remove(employeeId);

        OnEmployeeQuit?.Invoke(employee);
        OnEmployeesChanged?.Invoke();

        Debug.Log(
            $"{employee.employeeName} has left the workforce."
        );
    }
}
