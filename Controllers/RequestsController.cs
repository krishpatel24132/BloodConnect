using BloodConnect.Data;
using BloodConnect.Filters;
using BloodConnect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Controllers;

public class RequestsController : Controller
{
    private const int PageSize = 8;
    private readonly AppDbContext _db;

    public RequestsController(AppDbContext db) => _db = db;

    // READ (list) with filters + paging
    public async Task<IActionResult> Index(string status, string urgency, int page = 1)
    {
        var query = _db.BloodRequests.Include(r => r.Donations).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(urgency))
            query = query.Where(r => r.Urgency == urgency);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Min(Math.Max(page, 1), totalPages);

        // Descending text order puts "Open" first, then "Fulfilled", then "Cancelled"
        var requests = await query
            .OrderByDescending(r => r.Status)
            .ThenBy(r => r.RequiredBy)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.Urgency = urgency;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Total = total;

        return View(requests);
    }

    // READ (single) + matching donors
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var request = await _db.BloodRequests
            .Include(r => r.Donations).ThenInclude(d => d.Donor)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound();

        var groups = Lookups.DonorGroupsFor(request.BloodGroup);
        var cutoff = DateTime.Today.AddDays(-Lookups.DonationGapDays);

        var matches = await _db.Donors
            .Where(d => d.IsAvailable
                        && groups.Contains(d.BloodGroup)
                        && (d.LastDonationDate == null || d.LastDonationDate <= cutoff))
            .ToListAsync();

        // donors from the same city first
        matches = matches
            .OrderByDescending(d => string.Equals(d.City, request.City, StringComparison.OrdinalIgnoreCase))
            .ThenBy(d => d.FullName)
            .Take(10)
            .ToList();

        return View(new RequestDetailsViewModel { Request = request, MatchingDonors = matches });
    }

    // CREATE (open to everyone - anybody can raise a request)
    public IActionResult Create() =>
        View(new BloodRequest { RequiredBy = DateTime.Today.AddDays(2) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("PatientName,BloodGroup,Units,Hospital,City,ContactPhone,RequiredBy,Urgency,Notes")] BloodRequest request)
    {
        if (request.RequiredBy.HasValue && request.RequiredBy.Value.Date < DateTime.Today)
            ModelState.AddModelError(nameof(BloodRequest.RequiredBy), "Required-by date cannot be in the past.");

        if (!Lookups.UrgencyLevels.Contains(request.Urgency))
            ModelState.AddModelError(nameof(BloodRequest.Urgency), "Select a valid urgency level.");

        if (!ModelState.IsValid) return View(request);

        request.Status = "Open";
        _db.BloodRequests.Add(request);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Blood request created. Matching donors are listed below.";
        return RedirectToAction(nameof(Details), new { id = request.Id });
    }

    // UPDATE
    [RequireLogin]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var request = await _db.BloodRequests.FindAsync(id);
        if (request == null) return NotFound();
        return View(request);
    }

    [HttpPost, ActionName("Edit"), ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> EditPost(int id)
    {
        var request = await _db.BloodRequests.FindAsync(id);
        if (request == null) return NotFound();

        if (await TryUpdateModelAsync(request, "",
                r => r.PatientName, r => r.BloodGroup, r => r.Units, r => r.Hospital, r => r.City,
                r => r.ContactPhone, r => r.RequiredBy, r => r.Urgency, r => r.Status, r => r.Notes))
        {
            if (!Lookups.RequestStatuses.Contains(request.Status))
            {
                ModelState.AddModelError(nameof(BloodRequest.Status), "Select a valid status.");
                return View(request);
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Blood request updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(request);
    }

    // Quick status change (Fulfilled / Cancelled / Open)
    [HttpPost, ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var request = await _db.BloodRequests.FindAsync(id);
        if (request == null) return NotFound();

        if (!Lookups.RequestStatuses.Contains(status))
        {
            TempData["Error"] = "Invalid status.";
            return RedirectToAction(nameof(Details), new { id });
        }

        request.Status = status;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Request marked as {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // DELETE
    [RequireLogin]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var request = await _db.BloodRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request == null) return NotFound();
        return View(request);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var request = await _db.BloodRequests.FindAsync(id);
        if (request != null)
        {
            _db.BloodRequests.Remove(request);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Blood request deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
