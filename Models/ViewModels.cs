using System.ComponentModel.DataAnnotations;

namespace BloodConnect.Models;

public class GroupCount
{
    public string BloodGroup { get; set; }
    public int Count { get; set; }
}

public class LabelCount
{
    public string Label { get; set; }
    public int Count { get; set; }
}

public class DashboardViewModel
{
    public int TotalDonors { get; set; }
    public int EligibleDonors { get; set; }
    public int OpenRequests { get; set; }
    public int CriticalRequests { get; set; }
    public int UnitsDonated { get; set; }
    public List<GroupCount> GroupCounts { get; set; } = new();
    public List<LabelCount> UrgencyCounts { get; set; } = new();
    public List<BloodRequest> UrgentRequests { get; set; } = new();
    public string LastViewedDonor { get; set; }
}

public class RequestDetailsViewModel
{
    public BloodRequest Request { get; set; }
    public List<Donor> MatchingDonors { get; set; } = new();
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}
