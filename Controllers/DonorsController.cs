using System.Text;
using BloodConnect.Data;
using BloodConnect.Filters;
using BloodConnect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Controllers;

public class DonorsController : Controller
{
    private const int PageSize = 8;
    private readonly AppDbContext _db;

    public DonorsController(AppDbContext db) => _db = db;

    // READ (list) with search, filter, sorting, paging. Search/filter are remembered in Session.
    public async Task<IActionResult> Index(string search, string bloodGroup, string sort, int page = 1, bool clear = false)
    {
        if (clear)
        {
            HttpContext.Session.Remove("DonorSearch");
            HttpContext.Session.Remove("DonorGroup");
            return RedirectToAction(nameof(Index));
        }

        if (Request.Query.ContainsKey("search") || Request.Query.ContainsKey("bloodGroup"))
        {
            HttpContext.Session.SetString("DonorSearch", search ?? "");
            HttpContext.Session.SetString("DonorGroup", bloodGroup ?? "");
        }
        else
        {
            search = HttpContext.Session.GetString("DonorSearch");
            bloodGroup = HttpContext.Session.GetString("DonorGroup");
        }

        var query = _db.Donors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.FullName.Contains(search) || d.City.Contains(search));

        if (!string.IsNullOrWhiteSpace(bloodGroup))
            query = query.Where(d => d.BloodGroup == bloodGroup);

        query = sort switch
        {
            "group" => query.OrderBy(d => d.BloodGroup).ThenBy(d => d.FullName),
            "city" => query.OrderBy(d => d.City).ThenBy(d => d.FullName),
            "newest" => query.OrderByDescending(d => d.CreatedAt),
            _ => query.OrderBy(d => d.FullName)
        };

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Min(Math.Max(page, 1), totalPages);

        var donors = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.BloodGroup = bloodGroup;
        ViewBag.Sort = sort;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Total = total;

        return View(donors);
    }

    // READ (single)
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var donor = await _db.Donors
            .Include(d => d.Donations).ThenInclude(x => x.BloodRequest)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (donor == null) return NotFound();

        HttpContext.Session.SetString("LastViewedDonor", donor.FullName);
        return View(donor);
    }

    // CREATE
    [RequireLogin]
    public IActionResult Create() => View(new Donor());

    [HttpPost, ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> Create(
        [Bind("FullName,Email,Phone,BloodGroup,DateOfBirth,City,LastDonationDate,IsAvailable")] Donor donor)
    {
        donor.Email = donor.Email?.Trim().ToLowerInvariant();

        if (await _db.Donors.AnyAsync(d => d.Email == donor.Email))
            ModelState.AddModelError(nameof(Donor.Email), "A donor with this email already exists.");

        if (!ModelState.IsValid) return View(donor);

        _db.Donors.Add(donor);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Donor {donor.FullName} registered successfully.";
        return RedirectToAction(nameof(Index));
    }

    // CREATE (bulk): add the fictional example donors, skipping any whose email already exists
    [HttpPost, ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> AddSamples()
    {
        var existing = await _db.Donors.Select(d => d.Email).ToListAsync();
        var toAdd = SeedData.SampleDonors(DateTime.Today)
            .Where(s => !existing.Contains(s.Email))
            .ToList();

        if (toAdd.Count == 0)
        {
            TempData["Success"] = "Example donors are already in the list.";
            return RedirectToAction(nameof(Index), new { clear = true });
        }

        _db.Donors.AddRange(toAdd);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"{toAdd.Count} example donors added.";
        return RedirectToAction(nameof(Index), new { clear = true });
    }

    // UPDATE
    [RequireLogin]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var donor = await _db.Donors.FindAsync(id);
        if (donor == null) return NotFound();
        return View(donor);
    }

    [HttpPost, ActionName("Edit"), ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> EditPost(int id)
    {
        var donor = await _db.Donors.FindAsync(id);
        if (donor == null) return NotFound();

        if (await TryUpdateModelAsync(donor, "",
                d => d.FullName, d => d.Email, d => d.Phone, d => d.BloodGroup,
                d => d.DateOfBirth, d => d.City, d => d.LastDonationDate, d => d.IsAvailable))
        {
            donor.Email = donor.Email?.Trim().ToLowerInvariant();

            if (await _db.Donors.AnyAsync(d => d.Email == donor.Email && d.Id != id))
            {
                ModelState.AddModelError(nameof(Donor.Email), "Another donor already uses this email.");
                return View(donor);
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Donor details updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(donor);
    }

    // One-click availability switch from the list page
    [HttpPost, ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        var donor = await _db.Donors.FindAsync(id);
        if (donor == null) return NotFound();

        donor.IsAvailable = !donor.IsAvailable;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"{donor.FullName} is now marked {(donor.IsAvailable ? "available" : "unavailable")}.";
        return RedirectToAction(nameof(Index));
    }

    // DELETE
    [RequireLogin]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var donor = await _db.Donors.FirstOrDefaultAsync(d => d.Id == id);
        if (donor == null) return NotFound();
        return View(donor);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, RequireLogin]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var donor = await _db.Donors.FindAsync(id);
        if (donor != null)
        {
            _db.Donors.Remove(donor);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Donor deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    // EXPORT all donors as a CSV file (admin only, contains personal data)
    [RequireLogin]
    public async Task<IActionResult> ExportCsv()
    {
        var donors = await _db.Donors.OrderBy(d => d.FullName).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Name,Email,Mobile,BloodGroup,City,DateOfBirth,LastDonation,Available");
        foreach (var d in donors)
        {
            sb.AppendLine(string.Join(",",
                Csv(d.FullName), Csv(d.Email), Csv(d.Phone), Csv(d.BloodGroup), Csv(d.City),
                Csv(d.DateOfBirth?.ToString("yyyy-MM-dd")),
                Csv(d.LastDonationDate?.ToString("yyyy-MM-dd")),
                d.IsAvailable ? "Yes" : "No"));
        }

        // UTF-8 with BOM so Excel opens it correctly
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"donors_{DateTime.Today:yyyyMMdd}.csv");
    }

    private static string Csv(string value)
    {
        value ??= string.Empty;
        // stop spreadsheet formula injection (=, +, -, @ at the start of a cell)
        if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            value = "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
