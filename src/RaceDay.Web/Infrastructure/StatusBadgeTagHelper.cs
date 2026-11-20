using Microsoft.AspNetCore.Razor.TagHelpers;

namespace RaceDay.Web.Infrastructure;

[HtmlTargetElement("status-badge", TagStructure = TagStructure.WithoutEndTag)]
public sealed class StatusBadgeTagHelper : TagHelper
{
    private static readonly Dictionary<string, (string Css, string Icon, string Label)> Styles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Confirmed"] = ("confirmed", "bi-check-circle-fill", "Confirmed"),
        ["Pending"] = ("pending", "bi-hourglass-split", "Pending payment"),
        ["Cancelled"] = ("cancelled", "bi-x-circle-fill", "Cancelled"),
        ["Withdrawn"] = ("withdrawn", "bi-dash-circle", "Withdrawn"),
        ["Draft"] = ("draft", "bi-pencil-square", "Draft"),
        ["Published"] = ("published", "bi-megaphone-fill", "Published"),
        ["Completed"] = ("completed", "bi-flag-fill", "Completed"),
        ["Finished"] = ("finished", "bi-trophy-fill", "Finished"),
        ["DNF"] = ("dnf", "bi-exclamation-triangle-fill", "Did not finish"),
        ["DNS"] = ("dns", "bi-slash-circle", "Did not start"),
        ["DQ"] = ("dq", "bi-x-octagon-fill", "Disqualified"),
        ["Open"] = ("open", "bi-door-open-fill", "Entries open"),
        ["Closed"] = ("closed", "bi-door-closed-fill", "Entries closed"),
        ["Soon"] = ("soon", "bi-clock-fill", "Opening soon")
    };

    public string? Value { get; set; }

    public bool Short { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var value = Value ?? string.Empty;
        var (css, icon, label) = Styles.TryGetValue(value, out var style) ? style : ("draft", "bi-circle", value);

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"rd-status rd-status--{css}");
        output.Content.SetHtmlContent($"<i class=\"bi {icon}\" aria-hidden=\"true\"></i>");
        output.Content.Append(Short ? value : label);
    }
}
