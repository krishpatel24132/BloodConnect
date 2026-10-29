using System.ComponentModel.DataAnnotations;

namespace BloodConnect.Models;

/// <summary>Custom validation attribute: the date of birth must make the person at least N years old.</summary>
public class MinimumAgeAttribute : ValidationAttribute
{
    private readonly int _minimumAge;

    public MinimumAgeAttribute(int minimumAge)
    {
        _minimumAge = minimumAge;
        ErrorMessage = $"Donor must be at least {minimumAge} years old.";
    }

    public override bool IsValid(object value)
    {
        if (value is not DateTime dob) return true; // [Required] handles empty values
        return dob.Date <= DateTime.Today.AddYears(-_minimumAge);
    }
}
