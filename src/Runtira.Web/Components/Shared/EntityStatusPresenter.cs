namespace Runtira.Web.Components.Shared;

/// <summary>
/// Centralizes the display label/CSS mapping for entity status badges (units, leases, residents)
/// so list pages and their dedicated detail pages stay visually consistent.
/// </summary>
public static class EntityStatusPresenter
{
    public static string UnitOccupancyLabel(string status)
        => status switch
        {
            "Occupied" => "Occupée",
            "Available" => "Disponible",
            "Maintenance" => "Maintenance",
            "Reserved" => "Réservée",
            _ => status
        };

    public static string UnitOccupancyCss(string status)
        => status switch
        {
            "Occupied" => "status-pill status-pill-success",
            "Available" => "status-pill status-pill-info",
            "Maintenance" => "status-pill status-pill-warning",
            _ => "status-pill status-pill-muted"
        };

    public static string LeasePaymentLabel(string status)
        => status switch
        {
            "Active" => "Loyer à jour",
            "Review" => "À vérifier",
            "Ended" => "Bail terminé",
            _ => status
        };

    public static string LeasePaymentCss(string status)
        => status switch
        {
            "Active" => "status-pill status-pill-success",
            "Review" => "status-pill status-pill-warning",
            _ => "status-pill status-pill-muted"
        };

    public static string ResidentStatusLabel(string status)
        => status switch
        {
            "Active" => "Actif",
            "Watch" => "À surveiller",
            _ => status
        };

    public static string ResidentStatusCss(string status)
        => status switch
        {
            "Active" => "status-pill status-pill-success",
            "Watch" => "status-pill status-pill-warning",
            _ => "status-pill status-pill-muted"
        };

    public static string LeadStatusCss(string status)
        => status switch
        {
            "Qualified" => "status-pill status-pill-success",
            "New" => "status-pill status-pill-info",
            "Archived" => "status-pill status-pill-muted",
            _ => "status-pill status-pill-muted"
        };
}
