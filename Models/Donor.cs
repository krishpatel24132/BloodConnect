using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodConnect.Models;

public class Donor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Name must be 3 to 80 characters")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [StringLength(120)]
    public string Email { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number")]
    [Display(Name = "Mobile Number")]
    [StringLength(10)]
    public string Phone { get; set; }

    [Required(ErrorMessage = "Select a blood group")]
    [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Invalid blood group")]
    [StringLength(3)]
    [Display(Name = "Blood Group")]
    public string BloodGroup { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Column(TypeName = "date")]
    [MinimumAge(18)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "City is required")]
    [StringLength(60)]
    public string City { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Column(TypeName = "date")]
    [Display(Name = "Last Donation Date")]
    public DateTime? LastDonationDate { get; set; }

    [Display(Name = "Available to donate")]
    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Donation> Donations { get; set; } = new List<Donation>();

    [NotMapped]
    public int Age
    {
        get
        {
            if (!DateOfBirth.HasValue) return 0;
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    /// <summary>True when the donor is available and the 90-day gap since the last donation has passed.</summary>
    [NotMapped]
    public bool IsEligible =>
        IsAvailable &&
        (!LastDonationDate.HasValue ||
         (DateTime.Today - LastDonationDate.Value.Date).TotalDays >= Lookups.DonationGapDays);
}
