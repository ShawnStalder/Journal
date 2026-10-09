using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Journal.Core;
using Journal.Core.Rendering;
using Microsoft.Win32;

namespace Journal.App.Services;

public interface ILinkService
{
    /// <summary>Opens a stored link in the browser or the document's own application. Problems are shown to the user.</summary>
    void Open(string href);

    /// <summary>Lets the user choose a document; returns its path, or null if they cancel.</summary>
    string? BrowseForDocument();
}

public sealed class LinkService : ILinkService
{
    public void Open(string href)
    {
        try
        {
            var target = LinkAddress.Resolve(href);
            if (target.Kind == LinkKind.Document && !File.Exists(target.Value))
            {
                throw new JournalException($"The document could not be found: {target.Value}");
            }

            Process.Start(new ProcessStartInfo(target.Value) { UseShellExecute = true });
        }
        catch (JournalException exception)
        {
            ShowWarning(exception.Message);
        }
        catch (Win32Exception exception)
        {
            ShowWarning($"The link could not be opened: {exception.Message}");
        }
    }

    public string? BrowseForDocument()
    {
        var extensions = string.Join(";", LinkAddress.DocumentExtensions.Select(extension => $"*{extension}"));
        var dialog = new OpenFileDialog
        {
            Title = "Link to a document",
            Filter = $"Documents ({extensions})|{extensions}|All files (*.*)|*.*"
        };

        var owner = System.Windows.Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive);
        var isChosen = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return isChosen == true ? dialog.FileName : null;
    }

    private static void ShowWarning(string message)
    {
        System.Windows.MessageBox.Show(message, "Journal", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
