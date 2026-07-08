namespace Runtira.Web.Components.Shared;

/// <summary>
/// A single actionable navigation link surfaced in a QuickLinksPanel (phrased in natural language,
/// e.g. "Imprimer la facture du mois courant" rather than a bare noun like "Invoice").
/// </summary>
public sealed record QuickLinkAction(string Label, string Href, bool IsAccent = false, string Target = "_self");
