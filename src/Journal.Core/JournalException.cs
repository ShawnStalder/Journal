namespace Journal.Core;

/// <summary>A problem with user-supplied input or journal state that the user can act on.</summary>
public sealed class JournalException : Exception
{
    public JournalException(string message)
        : base(message)
    {
    }
}
