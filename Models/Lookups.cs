namespace BloodConnect.Models;

/// <summary>Shared constants and small helper rules used across the app.</summary>
public static class Lookups
{
    public static readonly string[] BloodGroups = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
    public static readonly string[] UrgencyLevels = { "Normal", "Urgent", "Critical" };
    public static readonly string[] RequestStatuses = { "Open", "Fulfilled", "Cancelled" };

    /// <summary>Minimum number of days between two whole-blood donations.</summary>
    public const int DonationGapDays = 90;

    // recipient group -> donor groups that can safely donate to them
    private static readonly Dictionary<string, string[]> Compatible = new()
    {
        ["A+"]  = new[] { "A+", "A-", "O+", "O-" },
        ["A-"]  = new[] { "A-", "O-" },
        ["B+"]  = new[] { "B+", "B-", "O+", "O-" },
        ["B-"]  = new[] { "B-", "O-" },
        ["AB+"] = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" },
        ["AB-"] = new[] { "A-", "B-", "AB-", "O-" },
        ["O+"]  = new[] { "O+", "O-" },
        ["O-"]  = new[] { "O-" }
    };

    public static string[] DonorGroupsFor(string recipientGroup) =>
        recipientGroup != null && Compatible.TryGetValue(recipientGroup, out var groups)
            ? groups
            : Array.Empty<string>();

    public static string UrgencyBadge(string urgency) => urgency switch
    {
        "Critical" => "danger",
        "Urgent" => "warning text-dark",
        _ => "secondary"
    };

    public static string StatusBadge(string status) => status switch
    {
        "Open" => "primary",
        "Fulfilled" => "success",
        _ => "secondary"
    };
}
