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
                $"<html><body style=\"font-family:Arial,sans-serif;color:#1f2937;line-height:1.5\">"
                + $"<p>Hello {encodedName},</p><p>{encodedMessage}</p></body></html>",
            Tag = "timesheet-submission-escalation"
        };
    }
}
