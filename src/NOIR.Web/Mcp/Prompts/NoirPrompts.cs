using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace NOIR.Web.Mcp.Prompts;

/// <summary>
/// MCP prompt templates for common NOIR operational workflows.
/// Prompts are task templates that guide AI agents through specific analyses.
/// Use these before calling tools to get focused, structured responses.
/// </summary>
[McpServerPromptType]
public sealed class NoirPrompts
{
    [McpServerPrompt(Name = "noir_analyze_orders")]
    [Description("Prepare to analyze the current order pipeline. Returns a structured prompt that guides the AI to review pending/processing orders, identify bottlenecks, and suggest prioritization.")]
    public IEnumerable<PromptMessage> AnalyzeOrders(
        [Description("Optional focus area: 'overdue', 'high-value', 'fulfillment', or leave empty for full analysis")] string? focus = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are analyzing the NOIR order management pipeline. Your task:");
        sb.AppendLine();
        sb.AppendLine("1. Call `noir_orders_list` with status='Pending' to see orders awaiting confirmation");
        sb.AppendLine("2. Call `noir_orders_list` with status='Confirmed' to see orders ready for processing");
        sb.AppendLine("3. Call `noir_orders_list` with status='Processing' to see orders being prepared");
        sb.AppendLine("4. Identify any orders that appear stuck or delayed");

        if (!string.IsNullOrWhiteSpace(focus))
        {
            sb.AppendLine();
            sb.Append("Focus specifically on: ");
            sb.AppendLine(focus switch
            {
                "overdue" => "orders that have been in the same status for an unusually long time.",
                "high-value" => "orders with the highest total value that need priority handling.",
                "fulfillment" => "orders ready to ship and what tracking steps are needed.",
                _ => focus
            });
        }

        sb.AppendLine();
        sb.AppendLine("Provide a concise summary with: total counts per status, any flagged items, and recommended next actions.");

        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock { Text = sb.ToString() }
        };
    }

    [McpServerPrompt(Name = "noir_revenue_analysis")]
    [Description("Prepare a revenue analysis prompt for a specific time period. Guides the AI to fetch revenue data and provide business insights.")]
    public IEnumerable<PromptMessage> RevenueAnalysis(
        [Description("Start date in ISO 8601 format (e.g. '2025-01-01')")] string fromDate,
        [Description("End date in ISO 8601 format (e.g. '2025-01-31')")] string toDate,
        [Description("Comparison period: 'previous-period', 'year-over-year', or leave empty to skip comparison")] string? comparison = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are performing a revenue analysis for the period {fromDate} to {toDate}.");
        sb.AppendLine();
        sb.AppendLine("Steps:");
        sb.AppendLine($"1. Call `noir_reports_revenue` with fromDate='{fromDate}' and toDate='{toDate}'");
        sb.AppendLine("2. Call `noir_reports_best_sellers` to identify top-performing products");
        sb.AppendLine("3. Call `noir_dashboard_ecommerce` for overall e-commerce KPIs");

        if (comparison == "previous-period")
        {
            sb.AppendLine("4. Also fetch the previous equivalent period for comparison");
        }
        else if (comparison == "year-over-year")
        {
            sb.AppendLine("4. Also fetch the same period from the previous year for YoY comparison");
        }

        sb.AppendLine();
        sb.AppendLine("Provide: total revenue, order count, AOV, top products, revenue trend, and actionable recommendations to improve performance.");

        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock { Text = sb.ToString() }
        };
    }

    [McpServerPrompt(Name = "noir_inventory_health_check")]
    [Description("Prepare an inventory health check prompt. Guides the AI to identify low-stock, overstock, and stale inventory issues.")]
    public IEnumerable<PromptMessage> InventoryHealthCheck(
        [Description("Low stock threshold: warn when quantity falls below this level (default: 10)")] int lowStockThreshold = 10)
    {
        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock
            {
                Text = $"""
                    You are performing an inventory health check for the NOIR warehouse.

                    Steps:
                    1. Call `noir_inventory_dashboard` to get overall inventory metrics
                    2. Call `noir_reports_inventory` to get detailed inventory report
                    3. Call `noir_inventory_receipts_list` to review recent stock movements

                    Flag items where:
                    - Quantity on hand is below {lowStockThreshold} units (LOW STOCK)
                    - No stock movements in the last 60 days (STALE)
                    - Stock receipt is pending confirmation for more than 3 days (PENDING REVIEW)

                    Provide: a prioritized list of inventory actions, reorder recommendations, and an overall health score (0-100).
                    """
            }
        };
    }

    [McpServerPrompt(Name = "noir_crm_pipeline_review")]
    [Description("Prepare a CRM pipeline review prompt. Guides the AI to identify leads needing attention, stalled deals, and conversion opportunities.")]
    public IEnumerable<PromptMessage> CrmPipelineReview(
        [Description("Specific pipeline stage to focus on, or leave empty for all stages")] string? stage = null)
    {
        var stageFilter = string.IsNullOrWhiteSpace(stage) ? "all pipeline stages" : $"the '{stage}' stage";

        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock
            {
                Text = $"""
                    You are reviewing the CRM sales pipeline, focusing on {stageFilter}.

                    Steps:
                    1. Call `noir_crm_leads_list` to get open leads
                    2. Call `noir_crm_dashboard` for pipeline metrics and conversion rates
                    3. Review lead activity dates to identify stalled deals

                    Identify:
                    - Leads with no activity in the last 7 days (NEEDS FOLLOW-UP)
                    - High-value leads that are close to closing (PRIORITIZE)
                    - Leads stuck in the same stage for more than 14 days (AT RISK)

                    Provide: a prioritized action list, pipeline health summary, and recommended next steps for top 5 leads.
                    """
            }
        };
    }

    [McpServerPrompt(Name = "noir_hr_team_briefing")]
    [Description("Prepare an HR team briefing prompt. Guides the AI to summarize headcount, department structure, and recent HR activities.")]
    public IEnumerable<PromptMessage> HrTeamBriefing(
        [Description("Specific department name to focus on, or leave empty for the entire organization")] string? department = null)
    {
        var scope = string.IsNullOrWhiteSpace(department) ? "the entire organization" : $"the '{department}' department";

        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock
            {
                Text = $"""
                    You are preparing an HR team briefing for {scope}.

                    Steps:
                    1. Call `noir_hr_departments_list` to get the organizational structure
                    2. Call `noir_hr_employees_list` to get employee roster
                    3. Call `noir_hr_reports` for headcount and HR analytics

                    Summarize:
                    - Total headcount by department
                    - Employment type breakdown (full-time, part-time, contract)
                    - Any vacant positions or recent organizational changes
                    - Key HR metrics (if available)

                    Format as a concise executive briefing suitable for a leadership review.
                    """
            }
        };
    }
}
