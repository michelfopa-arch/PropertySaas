namespace Runtira.Web.Components.Shared;

/// <summary>
/// A copyable AI prompt suggestion tied to the current entity (e.g. draft a reminder email for this
/// resident, or summarize this lease). Runtira's AI layer is currently simulated: the prompt is meant
/// to be copied into the assistant/chat rather than triggering a live model call.
/// </summary>
public sealed record AiPromptSuggestion(string Label, string Prompt);
