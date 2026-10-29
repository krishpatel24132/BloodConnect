using BloodConnect.Data;
using BloodConnect.Filters;
using BloodConnect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Controllers;

public class DonationsController : Controller
{
    private readonly AppDbContext _db;

    public DonationsController(AppDbContext db) => _db = db;

    // READ (list)
    public async Task<IActionResult> Index()
    {
        var donations = await _db.Donations
            .Include(d => d.Donor)
            .Include(d => d.BloodRequest)
            .OrderByDescending(d => d.DonatedOn)
            .ToListAsync();

        return View(donations);
    }

    // CREATE (record a donation) - admin only
    [RequireLogin]
    public async Task<IActionResult> Create(int? donorId, int? requestId)
    {
        await LoadListsAsync();
        return View(new Donation
        {
            DonorId = donorId ?? 0,
            BloodRequestId = requestId,
            DonatedOn = DateTime.Today,
            Units = 1
        });
    }

    [HttpPost, ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> Create(
        [Bind("DonorId,BloodRequestId,DonatedOn,Units,Notes")] Donation donation)
    {
        var donor = await _db.Donors.FindAsync(donation.DonorId);
        BloodRequest request = null;

        if (donor == null)
            ModelState.AddModelError(nameof(Donation.DonorId), "Selected donor was not found.");

        if (donation.DonatedOn.HasValue && donation.DonatedOn.Value.Date > DateTime.Today)
            ModelState.AddModelError(nameof(Donation.DonatedOn), "Donation date cannot be in the future.");

        // Business rule 1: 90-day gap between donations
        if (donor != null && donation.DonatedOn.HasValue && donor.LastDonationDate.HasValue)
        {
            var gap = (donation.DonatedOn.Value.Date - donor.LastDonationDate.Value.Date).TotalDays;
            if (gap < Lookups.DonationGapDays)
                ModelState.AddModelError(nameof(Donation.DonorId),
                    $"{donor.FullName} donated on {donor.LastDonationDate:dd MMM yyyy}. " +
                    $"A gap of {Lookups.DonationGapDays} days is required.");
        }

        // Business rule 2: blood group must be compatible with the request
        if (donation.BloodRequestId.HasValue)
        {
            request = await _db.BloodRequests.FindAsync(donation.BloodRequestId.Value);

            if (request == null)
            {
                ModelState.AddModelError(nameof(Donation.BloodRequestId), "Selected request was not found.");
            }
            else if (request.Status != "Open")
            {
                ModelState.AddModelError(nameof(Donation.BloodRequestId), "This request is no longer open.");
            }
            else if (donor != null && !Lookups.DonorGroupsFor(request.BloodGroup).Contains(donor.BloodGroup))
            {
                ModelState.AddModelError(nameof(Donation.DonorId),
                    $"{donor.BloodGroup} blood is not compatible with the {request.BloodGroup} request.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View(donation);
        }

        _db.Donations.Add(donation);
        donor.LastDonationDate = donation.DonatedOn;

        // Mark the request fulfilled once enough units are collected
        if (request != null)
        {
            var already = await _db.Donations
                .Where(d => d.BloodRequestId == request.Id)
                .SumAsync(d => (int?)d.Units) ?? 0;

            if (already + donation.Units >= request.Units)
                request.Status = "Fulfilled";
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Donation recorded successfully.";
        return RedirectToAction(nameof(Index));
    }

    // DELETE - admin only
    [RequireLogin]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var donation = await _db.Donations
            .Include(d => d.Donor)
            .Include(d => d.BloodRequest)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (donation == null) return NotFound();
        return View(donation);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var donation = await _db.Donations.FindAsync(id);
        if (donation == null) return RedirectToAction(nameof(Index));

        var requestId = donation.BloodRequestId;
        _db.Donations.Remove(donation);
        await _db.SaveChangesAsync();

        // Re-open the request if it is no longer fully covered
        if (requestId.HasValue)
        {
            var request = await _db.BloodRequests.FindAsync(requestId.Value);
            if (request != null && request.Status == "Fulfilled")
            {
                var remaining = await _db.Donations
                    .Where(d => d.BloodRequestId == requestId)
                    .SumAsync(d => (int?)d.Units) ?? 0;

                if (remaining < request.Units)
                {
                    request.Status = "Open";
                    await _db.SaveChangesAsync();
                }
            }
        }

        TempData["Success"] = "Donation record deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadListsAsync()
    {
        var donors = await _db.Donors.OrderBy(d => d.FullName).ToListAsync();
        ViewBag.Donors = new SelectList(
            donors.Select(d => new { d.Id, Label = $"{d.FullName} ({d.BloodGroup}) - {d.City}" }),
            "Id", "Label");

        var requests = await _db.BloodRequests
            .Where(r => r.Status == "Open")
            .OrderBy(r => r.RequiredBy)
            .ToListAsync();
        ViewBag.Requests = new SelectList(
            requests.Select(r => new { r.Id, Label = $"#{r.Id} {r.PatientName} - {r.BloodGroup} ({r.Hospital})" }),
            "Id", "Label");
    }
}
