namespace FestivalOperations.Models;

/// <summary>
/// This one is for representing a crew member working at the festival.
/// </summary>

public sealed class Crew : Person
{
    public decimal HourlyRate { get; private set; }
    public string EmploymentType { get; private set; }
    public int WeeklyHours { get; private set; }

    public Crew(
        int personID,
        string name,
        string telephone,
        string email,
        decimal hourlyRate,
        string employmentType,
        int weeklyHours
    ) : base(personID, name, telephone, email)

    {
        string validEmploymentType = ValidateEmploymentType(employmentType);
        int validWeeklyHours = ValidateWeeklyHours(
            weeklyHours,
            validEmploymentType
        );

        HourlyRate = ValidateHourlyRate(hourlyRate);
        EmploymentType = validEmploymentType;
        WeeklyHours = validWeeklyHours;
    }

    public void UpdateCrewDetails(
        decimal hourlyRate,
        string employmentType,
        int weeklyHours
    )

    {
        string validEmploymentType = ValidateEmploymentType(employmentType);
        int validWeeklyHours = ValidateWeeklyHours(
            weeklyHours,
            validEmploymentType
        );
        decimal validHourlyRate = ValidateHourlyRate(hourlyRate);

        EmploymentType = validEmploymentType;
        WeeklyHours = validWeeklyHours;
        HourlyRate = validHourlyRate;
    }

    public override string GetRoleDescription()
    {
        return $"Crew - {EmploymentType}, {WeeklyHours} hours per week";
    }

    private static decimal ValidateHourlyRate(decimal hourlyRate)
    {
        if (hourlyRate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourlyRate),
                "Hourly rate cannot be negative."
            );
        }

        return hourlyRate;
    }

    private static string ValidateEmploymentType(string employmentType)
    {
        string cleanedType = RequireText(
            employmentType,
            nameof(employmentType)
        );

        if (cleanedType.Equals(
            "FullTime",
            StringComparison.OrdinalIgnoreCase
        ))
        {
            return "FullTime";
        }

        if (cleanedType.Equals(
            "PartTime",
            StringComparison.OrdinalIgnoreCase
        ))
        {
            return "PartTime";
        }

        throw new ArgumentException(
            "Employment type must be Full Time or Part Time.",
            nameof(employmentType)
        );
    }

    private static int ValidateWeeklyHours(
        int weeklyHours,
        string employmentType
    )
    {
        bool isValid = employmentType == "FullTime"
            ? weeklyHours is >= 25 and <= 40
            : weeklyHours is >= 1 and <= 24;
        
        if (!isValid)
        {
            string range = employmentType == "FullTime"
                ? "25 to 40"
                : "1 to 24";
            
            throw new ArgumentOutOfRangeException(
                nameof(weeklyHours),
                $"Weekly hours for {employmentType} must be between {range}."
            );
        }

        return weeklyHours;
    }
}