using BloodConnect.Models;

namespace BloodConnect.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (db.Donors.Any()) return;

        var today = DateTime.Today;

        var donors = SampleDonors(today);
        db.Donors.AddRange(donors);

        var requests = new List<BloodRequest>
        {
            new() { PatientName = "Ramesh Kumar",  BloodGroup = "O+",  Units = 2, Hospital = "Civil Hospital",        City = "Ahmedabad", ContactPhone = "9811122233", RequiredBy = today.AddDays(1),  Urgency = "Critical", Notes = "Emergency surgery scheduled." },
            new() { PatientName = "Sunita Verma",  BloodGroup = "A+",  Units = 3, Hospital = "Sterling Hospital",     City = "Ahmedabad", ContactPhone = "9822233344", RequiredBy = today.AddDays(3),  Urgency = "Urgent" },
            new() { PatientName = "Imran Qureshi", BloodGroup = "B+",  Units = 1, Hospital = "New Civil Hospital",    City = "Surat",     ContactPhone = "9833344455", RequiredBy = today.AddDays(6),  Urgency = "Normal" },
            new() { PatientName = "Lata Pandya",   BloodGroup = "AB-", Units = 2, Hospital = "SSG Hospital",          City = "Vadodara",  ContactPhone = "9844455566", RequiredBy = today.AddDays(2),  Urgency = "Urgent",   Notes = "Thalassemia patient." }
        };
        db.BloodRequests.AddRange(requests);
        db.SaveChanges();

        db.Donations.AddRange(
            new Donation { DonorId = donors[0].Id, BloodRequestId = null,            DonatedOn = today.AddDays(-150), Units = 1, Notes = "Camp donation" },
            new Donation { DonorId = donors[2].Id, BloodRequestId = requests[2].Id, DonatedOn = today.AddDays(-30),  Units = 1, Notes = "Direct donation" }
        );
        requests[2].Status = "Fulfilled";
        db.SaveChanges();
    }

    /// <summary>Fictional example donors (dummy emails and phone numbers) used for demo data and the "Add example donors" button.</summary>
    public static List<Donor> SampleDonors(DateTime today)
    {
        return new List<Donor>
        {
            new() { FullName = "Aarav Patel",   Email = "aarav.patel@example.com",   Phone = "9876543210", BloodGroup = "O+",  DateOfBirth = new DateTime(2001, 5, 14),  City = "Ahmedabad", LastDonationDate = today.AddDays(-150) },
            new() { FullName = "Diya Shah",     Email = "diya.shah@example.com",     Phone = "9898012345", BloodGroup = "A+",  DateOfBirth = new DateTime(1999, 11, 2),  City = "Ahmedabad", LastDonationDate = today.AddDays(-200) },
            new() { FullName = "Rohan Mehta",   Email = "rohan.mehta@example.com",   Phone = "9725011122", BloodGroup = "B+",  DateOfBirth = new DateTime(1995, 3, 21),  City = "Surat",     LastDonationDate = today.AddDays(-30) },
            new() { FullName = "Isha Desai",    Email = "isha.desai@example.com",    Phone = "9427033344", BloodGroup = "O-",  DateOfBirth = new DateTime(1998, 8, 9),   City = "Ahmedabad", LastDonationDate = null },
            new() { FullName = "Kabir Joshi",   Email = "kabir.joshi@example.com",   Phone = "9638055566", BloodGroup = "AB+", DateOfBirth = new DateTime(1992, 1, 30),  City = "Vadodara",  LastDonationDate = today.AddDays(-400) },
            new() { FullName = "Meera Nair",    Email = "meera.nair@example.com",    Phone = "8200077788", BloodGroup = "A-",  DateOfBirth = new DateTime(2000, 12, 12), City = "Rajkot",    LastDonationDate = today.AddDays(-120) },
            new() { FullName = "Vivaan Trivedi",Email = "vivaan.trivedi@example.com",Phone = "7048099900", BloodGroup = "B-", DateOfBirth = new DateTime(1990, 7, 7), City = "Ahmedabad", LastDonationDate = today.AddDays(-95) },
            new() { FullName = "Anaya Bhatt",   Email = "anaya.bhatt@example.com",   Phone = "9099011223", BloodGroup = "O+",  DateOfBirth = new DateTime(2002, 4, 25),  City = "Surat",     LastDonationDate = today.AddDays(-10), IsAvailable = false },
            new() { FullName = "Harsh Vyas",    Email = "harsh.vyas@example.com",    Phone = "9512044455", BloodGroup = "AB-", DateOfBirth = new DateTime(1996, 9, 18),  City = "Ahmedabad", LastDonationDate = null },
            new() { FullName = "Sneha Rana",    Email = "sneha.rana@example.com",    Phone = "9328066677", BloodGroup = "A+",  DateOfBirth = new DateTime(1997, 6, 3),   City = "Gandhinagar", LastDonationDate = today.AddDays(-250) }
        };
    }
}
