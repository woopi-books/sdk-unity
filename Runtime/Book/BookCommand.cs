namespace Woopi.Core.Book
{
    /// <summary>A custom command from the host page, handed to a handler registered with
    /// <see cref="WoopiBookHost.RegisterCommand"/>.</summary>
    public readonly struct BookCommand
    {
        public string Action { get; }

        /// <summary>The id the host attached, or empty.</summary>
        public string RequestId { get; }

        /// <summary>The command's value as text, or empty.</summary>
        public string Text { get; }

        /// <summary>True when <see cref="Text"/> is a whole number (then <see cref="Number"/> holds it).</summary>
        public bool HasNumber { get; }

        public int Number { get; }

        public BookCommand(string action, string requestId, string text)
        {
            Action = action ?? "";
            RequestId = requestId ?? "";
            Text = text ?? "";
            HasNumber = int.TryParse(Text, out var number);
            Number = HasNumber ? number : 0;
        }
    }
}
