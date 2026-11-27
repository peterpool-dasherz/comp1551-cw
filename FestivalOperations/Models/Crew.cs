namespace FestivalOperations.Models;

/// <summary>
/// This one is for representing a crew member working at the festival and validates their schedule and pay.
/// </summary>

public sealed class Crew : Person
{
    /// <summary>Gets the crew member's non-negative hourly pay rate.</summary>
    /// <remarks>Changes must pass through UpdateCrewDetails and its validation checks.</remarks>
    public decimal HourlyRate { get; private set; }

    /// <summary>Gets the normalized employment type: FullTime or PartTime.</summary>
    /// <remarks>Canonical spelling keeps stored values and comparisons consistent.</remarks>
    public string EmploymentType { get; private set; }

    /// <summary>Gets the crew member's weekly hours within the employment-type range.</summary>
    /// <remarks>The allowed range depends on the employment type.</remarks>
    public int WeeklyHours { get; private set; }

    /// <summary>Creates a crew member after validating pay, employment type, and weekly hours.</summary>
    /// <param name="personID">The existing database ID, or zero for a new crew member.</param>
    /// <param name="name">The crew member's name.</param>
    /// <param name="telephone">The crew member's telephone number.</param>
    /// <param name="email">The crew member's email address.</param>
    /// <param name="hourlyRate">The non-negative hourly pay rate.</param>
    /// <param name="employmentType">FullTime or PartTime, ignoring input casing.</param>
    /// <param name="weeklyHours">The number of hours worked each week.</param>
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
        // Normalize the employment type first because the valid hour range depends on it.
        string validEmploymentType = ValidateEmploymentType(employmentType);
        int validWeeklyHours = ValidateWeeklyHours(
            weeklyHours,
            validEmploymentType
        );

        HourlyRate = ValidateHourlyRate(hourlyRate);
        EmploymentType = validEmploymentType;
        WeeklyHours = validWeeklyHours;
    }

    /// <summary>Updates crew details only after all new values pass validation.</summary>
    /// <param name="hourlyRate">The new hourly pay rate.</param>
    /// <param name="employmentType">The new employment type.</param>
    /// <param name="weeklyHours">The new weekly hours.</param>
    public void UpdateCrewDetails(
        decimal hourlyRate,
        string employmentType,
        int weeklyHours
    )

    {
        // Validate all replacement values before assigning so a failed update leaves the object unchanged.
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

    /// <summary>Builds the crew details displayed in a role description.</summary>
    /// <returns>The employment type and weekly hours.</returns>
    public override string GetRoleDescription()
    {
        return $"Crew - {EmploymentType}, {WeeklyHours} hours per week";
    }

    /// <summary>Rejects a negative hourly pay rate.</summary>
    /// <param name="hourlyRate">The rate to validate.</param>
    /// <returns>The validated rate.</returns>
    private static decimal ValidateHourlyRate(decimal hourlyRate)
    {
        if (hourlyRate < 0)
        {
            // Negative compensation is invalid for a crew member.
            throw new ArgumentOutOfRangeException(
                nameof(hourlyRate),
                "Hourly rate cannot be negative."
            );
        }

        return hourlyRate;
    }

    /// <summary>Accepts supported employment types and returns their canonical spelling.</summary>
    /// <param name="employmentType">The supplied employment type.</param>
    /// <returns>Either FullTime or PartTime.</returns>
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
            // Return a consistent database value regardless of the user's letter casing.
            return "FullTime";
        }

        if (cleanedType.Equals(
            "PartTime",
            StringComparison.OrdinalIgnoreCase
        ))
        {
            // Normalize accepted part-time input in the same way.
            return "PartTime";
        }

        throw new ArgumentException(
            "Employment type must be Full Time or Part Time.",
            nameof(employmentType)
        );
    }

    /// <summary>Checks weekly hours against the range for the selected employment type.</summary>
    /// <param name="weeklyHours">The proposed weekly hours.</param>
    /// <param name="employmentType">The validated employment type.</param>
    /// <returns>The validated number of weekly hours.</returns>
    private static int ValidateWeeklyHours(
        int weeklyHours,
        string employmentType
    )
    {
        // Full-time staff may work 25-40 hours; part-time staff may work 1-24 hours.
        bool isValid = employmentType == "FullTime"
            ? weeklyHours is >= 25 and <= 40
            : weeklyHours is >= 1 and <= 24;
        
        if (!isValid)
        {
            // Include the applicable range in the error so the user can correct the value.
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
