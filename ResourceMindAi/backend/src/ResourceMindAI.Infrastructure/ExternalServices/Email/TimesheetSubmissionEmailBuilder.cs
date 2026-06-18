using System.Net;
using ResourceMindAI.Application.DTOs.Notifications;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.ExternalServices.Email;

internal static class TimesheetSubmissionEmailBuilder
{
    internal static EmailMessageDto FirstReminder(
        User employee,
        DateTime weekStartDate)
    {
        return Build(
            employee,
            $"Timesheet Reminder - Missing Submission for Week of {weekStartDate:dd-MM-yyyy}",
            "You missed last week's timesheet submission. Please submit it as soon as possible.");
    }

    internal static EmailMessageDto SecondReminder(
        User employee,
        DateTime weekStartDate)
    {
        return Build(
            employee,
            $"Second Reminder - Missing Timesheet for Week of {weekStartDate:dd-MM-yyyy}",
            "This is the second reminder for your missed timesheet submission.");
    }

    internal static EmailMessageDto Frozen(
        User recipient,
        User employee,
        DateTime weekStartDate)
    {
        var employeeContext = recipient.Id == employee.Id
            ? string.Empty
            : $" for {employee.FullName}";
        return Build(
            recipient,
            "Timesheet Frozen - Manager Review Required",
            $"Timesheet submission{employeeContext} for the week of {weekStartDate:dd-MM-yyyy} "
            + "is now frozen because it was not submitted by the escalation deadline. "
            + "Manager review is required to restore access.");
    }

    private static EmailMessageDto Build(
        User recipient,
        string subject,
        string message)
    {
        var encodedName = WebUtility.HtmlEncode(recipient.FullName);
        var encodedMessage = WebUtility.HtmlEncode(message);

        return new EmailMessageDto
        {
            RecipientEmail = recipient.Email,
            RecipientName = recipient.FullName,
            Subject = subject,
            TextContent = $"Hello {recipient.FullName},{Environment.NewLine}{Environment.NewLine}{message}",
            HtmlContent =
                "<html><body style=\"margin:0;background:#f4f7fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;line-height:1.55\">"
                + "<div style=\"max-width:640px;margin:0 auto;padding:28px 18px\">"
                + "<div style=\"background:#ffffff;border:1px solid #e5e7eb;border-radius:14px;overflow:hidden;box-shadow:0 14px 36px rgba(15,23,42,0.08)\">"
                + "<div style=\"background:#111827;color:#ffffff;padding:22px 26px\">"
                + "<div style=\"font-size:12px;text-transform:uppercase;letter-spacing:0.08em;color:#c7d2fe\">Timesheet Notification</div>"
                + $"<h1 style=\"margin:8px 0 0;font-size:22px;line-height:1.25\">{WebUtility.HtmlEncode(subject)}</h1>"
                + "</div>"
                + "<div style=\"padding:24px 26px\">"
                + $"<p style=\"margin:0 0 16px\">Hello {encodedName},</p>"
                + "<div style=\"border:1px solid #e5e7eb;border-radius:10px;background:#f9fafb;padding:16px\">"
                + $"<p style=\"margin:0;color:#374151\">{encodedMessage}</p>"
                + "</div>"
                + "<p style=\"margin:18px 0 0;color:#6b7280;font-size:12px\">Please keep your timesheets current so project reporting and utilisation stay accurate.</p>"
                + "</div></div></div></body></html>",
            Tag = "timesheet-submission-escalation"
        };
    }
}
