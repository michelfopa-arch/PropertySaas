namespace Runtira.Application.Features
{
    public sealed class RuntiraWorkspaceSummaryDto
    {
        public Guid TenantId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationSlug { get; set; } = string.Empty;
        public string DefaultLocale { get; set; } = string.Empty;
        public string BillingPlan { get; set; } = "Trial";
        public int AssetCount { get; set; }
        public int ConversationCount { get; set; }
        public int WorkflowTemplateCount { get; set; }
        public int ArchiveCount { get; set; }
        public int MonthlyAiLimit { get; set; }
        public int AssetLimit { get; set; }
    }

    public sealed class RuntiraQuestionPromptDto
    {
        public string Intent { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string RequiredQuestionsJson { get; set; } = "[]";
        public string ValidationRulesJson { get; set; } = "{}";
    }
}
