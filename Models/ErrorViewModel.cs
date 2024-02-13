namespace Anthology.Models
{
    /// <summary>
    /// Encapsulation for error checking on requests.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// The request id.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// True if request id is present.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}