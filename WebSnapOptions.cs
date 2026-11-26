namespace WebSnap;

/// Strongly-typed configuration for the screenshot run, bound from the
/// "WebSnap" section across appsettings.json, environment variables, and args.
public sealed record WebSnapOptions
{
    /// Page to capture.
    public string Url { get; init; } = "http://localhost:5173";

    /// Accessible name of a GDPR/cookie consent button to dismiss, if any.
    public string? CookieButtonName { get; init; }

    /// Capture the full scrollable page rather than just the viewport.
    public bool FullPage { get; init; } = true;

    /// Timeout in milliseconds for dismissing the cookie popup.
    public int TimeoutMs { get; init; } = 3000;
}
