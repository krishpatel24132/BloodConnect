using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodConnect.Models;

public class BloodRequest
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Patient name is required")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Name must be 3 to 80 characters")]
    [Display(Name = "Patient Name")]
    public string PatientName { get; set; }

    [Required(ErrorMessage = "Select a blood group")]
    [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Invalid blood group")]
    [StringLength(3)]
    [Display(Name = "Blood Group")]
    public string BloodGroup { get; set; }

    [Required(ErrorMessage = "Units required is mandatory")]
    [Range(1, 10, ErrorMessage = "Units must be between 1 and 10")]
    [Display(Name = "Units Required")]
    public int Units { get; set; } = 1;

    [Required(ErrorMessage = "Hospital name is required")]
    [StringLength(120)]
    public string Hospital { get; set; }

    [Required(ErrorMessage = "City is required")]
    [StringLength(60)]
    public string City { get; set; }

    [Required(ErrorMessage = "Contact number is required")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number")]
    [StringLength(10)]
    [Display(Name = "Contact Number")]
    public string ContactPhone { get; set; }

    [Required(ErrorMessage = "Required-by date is mandatory")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Column(TypeName = "date")]
    [Display(Name = "Required By")]
    public DateTime? RequiredBy { get; set; }

    [Required]
    [StringLength(10)]
    public string Urgency { get; set; } = "Normal";

    [Required]
    [StringLength(10)]
    public string Status { get; set; } = "Open";

    [StringLength(300, ErrorMessage = "Notes can be at most 300 characters")]
    public string Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Donation> Donations { get; set; } = new List<Donation>();

    [NotMapped]
    public int UnitsDonated => Donations?.Sum(d => d.Units) ?? 0;
}
