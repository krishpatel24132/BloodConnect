using BloodConnect.Data;
using BloodConnect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var cutoff = DateTime.Today.AddDays(-Lookups.DonationGapDays);

        var vm = new DashboardViewModel
        {
            TotalDonors = await _db.Donors.CountAsync(),
            EligibleDonors = await _db.Donors.CountAsync(d =>
                d.IsAvailable && (d.LastDonationDate == null || d.LastDonationDate <= cutoff)),
            OpenRequests = await _db.BloodRequests.CountAsync(r => r.Status == "Open"),
            CriticalRequests = await _db.BloodRequests.CountAsync(r => r.Status == "Open" && r.Urgency == "Critical"),
            UnitsDonated = await _db.Donations.SumAsync(d => (int?)d.Units) ?? 0,
            GroupCounts = await _db.Donors
                .GroupBy(d => d.BloodGroup)
                .Select(g => new GroupCount { BloodGroup = g.Key, Count = g.Count() })
                .ToListAsync(),
            UrgentRequests = await _db.BloodRequests
                .Where(r => r.Status == "Open")
                .OrderBy(r => r.RequiredBy)
                .Take(5)
                .ToListAsync(),
            // Session state: the donor profile this visitor opened last
            LastViewedDonor = HttpContext.Session.GetString("LastViewedDonor")
        };

        // keep the chart in a stable A+, A-, B+ ... order
        vm.GroupCounts = Lookups.BloodGroups
            .Select(g => vm.GroupCounts.FirstOrDefault(x => x.BloodGroup == g) ?? new GroupCount { BloodGroup = g, Count = 0 })
            .ToList();

        // open requests per urgency level (for the doughnut chart)
        var urgencyRows = await _db.BloodRequests
            .Where(r => r.Status == "Open")
            .GroupBy(r => r.Urgency)
            .Select(g => new LabelCount { Label = g.Key, Count = g.Count() })
            .ToListAsync();

        vm.UrgencyCounts = Lookups.UrgencyLevels
            .Select(u => new LabelCount
            {
                Label = u,
                Count = urgencyRows.FirstOrDefault(x => x.Label == u)?.Count ?? 0
            })
            .ToList();

        return View(vm);
    }

    public IActionResult Error() => View();

    // Shown for 404 and other status codes (see UseStatusCodePagesWithReExecute in Program.cs)
    public IActionResult PageNotFound(int code = 404)
    {
        ViewBag.Code = code;
        return View();
    }
}
