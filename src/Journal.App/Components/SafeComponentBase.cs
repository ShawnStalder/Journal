using System.IO;
using Journal.Core;
using Microsoft.AspNetCore.Components;

namespace Journal.App.Components;

/// <summary>Turns the file-system and journal errors a user can recover from into a message on the page.</summary>
public abstract class SafeComponentBase : ComponentBase
{
    protected string? ErrorMessage { get; set; }

    protected void ClearError()
    {
        ErrorMessage = null;
    }

    protected async Task RunSafelyAsync(Func<Task> action, bool keepExistingError = false)
    {
        if (!keepExistingError)
        {
            ErrorMessage = null;
        }

        try
        {
            await action();
        }
        catch (JournalException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (IOException exception)
        {
            ErrorMessage = $"The journal files could not be accessed: {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            ErrorMessage = $"Access to the journal files was denied: {exception.Message}";
        }
    }
}
