namespace ShareSphere.Api.Models
{
    /// <summary>
    /// Represents the result of a share purchase operation.
    /// </summary>
    public class PurchaseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Trade? Trade { get; set; }
        public Portfolio? Portfolio { get; set; }
    }
}
