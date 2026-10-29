using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodConnect.Models;

public class Donation
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Select a donor")]
    [Display(Name = "Donor")]
    public int DonorId { get; set; }
    public Donor Donor { get; set; }

    [Display(Name = "Blood Request")]
    public int? BloodRequestId { get; set; }
    public BloodRequest BloodRequest { get; set; }

    [Required(ErrorMessage = "Donation date is required")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Column(TypeName = "date")]
    [Display(Name = "Donated On")]
    public DateTime? DonatedOn { get; set; }

    [Required(ErrorMessage = "Units are required")]
    [Range(1, 2, ErrorMessage = "A donor can give 1 or 2 units at a time")]
    public int Units { get; set; } = 1;

    [StringLength(200)]
    public string Notes { get; set; }
}
