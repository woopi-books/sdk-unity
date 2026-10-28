namespace Woopi.Core
{
    /// <summary>Version information the book reports to the host page in its <c>ready</c> event.</summary>
    public static class WoopiSdk
    {
        /// <summary>
        /// Package version. Must match package.json; the Editor warns when it does not
        /// (Woopi.Core.Editor.WoopiVersionCheck).
        /// </summary>
        public const string Version = "0.3.1";

        /// <summary>
        /// Version of the host/book message contract. It only changes when an existing command or
        /// event stops working the way a host expects; new commands and new event fields do not
        /// change it (hosts feature-detect with the <c>commands</c> and <c>capabilities</c> lists).
        /// </summary>
        public const int ProtocolVersion = 1;
    }
}
