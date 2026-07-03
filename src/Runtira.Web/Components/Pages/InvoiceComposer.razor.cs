using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using Runtira.Application.Common;
using Runtira.Application.Features;
using Runtira.Web.Localization;

namespace Runtira.Web.Components.Pages;

public partial class InvoiceComposer : ComponentBase
{
    [Parameter]
    public string? TenantSlug { get; set; }

    [Inject]
    public CurrentOrganization CurrentOrganization { get; set; } = default!;

    [Inject]
    public RuntiraWorkspaceService WorkspaceService { get; set; } = default!;

    [Inject]
    public HttpClient Http { get; set; } = default!;

    private RuntiraInvoiceComposerDto? _invoice;
    private HashSet<string> _visibleOptions = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _requiredQuestions = new();
    private string _draftEmailStatus = string.Empty;
    private bool _hasInitialized;
    private bool _isSendingDraftEmail;

    protected override async Task OnInitializedAsync()
    {
        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;

        _invoice = await WorkspaceService.GetInvoiceComposerAsync();
        if (_invoice is null)
        {
            return;
        }

        _requiredQuestions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(_invoice.RequiredQuestionsJson) ?? new List<string>();
        using var rulesDocument = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(_invoice.InvoiceRulesJson) ? "{}" : _invoice.InvoiceRulesJson);
        foreach (var property in rulesDocument.RootElement.EnumerateObject())
        {
            var isVisibleOption = string.Equals(property.Name, "includePropertyAddress", StringComparison.OrdinalIgnoreCase)
                || string.Equals(property.Name, "includeBillingPeriod", StringComparison.OrdinalIgnoreCase)
                || string.Equals(property.Name, "generatePdf", StringComparison.OrdinalIgnoreCase);

            if (property.Value.ValueKind == System.Text.Json.JsonValueKind.True && isVisibleOption)
            {
                _visibleOptions.Add(property.Name);
            }
        }
    }

    private string GetTitle()
    {
        if (string.Equals(CurrentOrganization.CountryCode, "US", StringComparison.OrdinalIgnoreCase))
        {
            return RuntiraText.Get("Invoice_Title_US", CurrentOrganization.PreferredLanguage);
        }

        if (string.Equals(CurrentOrganization.CountryCode, "CA", StringComparison.OrdinalIgnoreCase))
        {
            return RuntiraText.Get("Invoice_Title_Canada", CurrentOrganization.PreferredLanguage);
        }

        return RuntiraText.Get("Invoice_Title_Default", CurrentOrganization.PreferredLanguage);
    }

    private string GetCopy()
    {
        if (string.Equals(CurrentOrganization.CountryCode, "US", StringComparison.OrdinalIgnoreCase))
        {
            return RuntiraText.Get("Invoice_Copy_US", CurrentOrganization.PreferredLanguage);
        }

        if (string.Equals(CurrentOrganization.CountryCode, "CA", StringComparison.OrdinalIgnoreCase))
        {
            return RuntiraText.Get("Invoice_Copy_Canada", CurrentOrganization.PreferredLanguage);
        }

        return RuntiraText.Get("Invoice_Copy_Default", CurrentOrganization.PreferredLanguage);
    }

    private string GetBooleanLabel(bool value)
    {
        return value
            ? RuntiraText.Get("Common_Yes", CurrentOrganization.PreferredLanguage)
            : RuntiraText.Get("Common_No", CurrentOrganization.PreferredLanguage);
    }

    private bool ShouldShowPropertyAddress()
    {
        return _visibleOptions.Contains("includePropertyAddress")
            || _requiredQuestions.Contains("propertyAddress")
            || _requiredQuestions.Contains("address");
    }

    private bool ShouldShowBillingPeriod()
    {
        return _visibleOptions.Contains("includeBillingPeriod")
            || _requiredQuestions.Contains("billingPeriod")
            || _requiredQuestions.Contains("period");
    }

    private bool ShouldShowMonthlyRent()
    {
        return _requiredQuestions.Contains("monthlyRent");
    }

    private bool ShouldShowGeneratePdf()
    {
        return _visibleOptions.Contains("generatePdf");
    }

    private async Task SendDraftEmailAsync()
    {
        if (_isSendingDraftEmail)
        {
            return;
        }

        _isSendingDraftEmail = true;

        try
        {
            var response = await Http.PostAsync("/api/invoices/draft-email", content: null);
            _draftEmailStatus = response.IsSuccessStatusCode
                ? RuntiraText.Get("Invoice_DraftEmailReady", CurrentOrganization.PreferredLanguage)
                : RuntiraText.Get("Invoice_DraftEmailError", CurrentOrganization.PreferredLanguage);
        }
        catch
        {
            _draftEmailStatus = RuntiraText.Get("Invoice_DraftEmailError", CurrentOrganization.PreferredLanguage);
        }
        finally
        {
            _isSendingDraftEmail = false;
        }
    }
}
