using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using WebSnap;

// `websnap --version` prints the version and exits without launching a browser.
if (args.Contains("--version"))
{
    Console.WriteLine($"websnap {GetVersion()}");
    return;
}

// Convenience: leading positional args map to Url then CookieButtonName,
// e.g. `dotnet run -- https://www.msn.com "Accept all"`. Named flags still work.
var (positional, namedArgs) = SplitArgs(args);
var positionalOverrides = new Dictionary<string, string?>();
if (positional.Count > 0) positionalOverrides["WebSnap:Url"] = positional[0];
if (positional.Count > 1) positionalOverrides["WebSnap:CookieButtonName"] = positional[1];

// Build configuration: appsettings.json -> environment variables -> named args -> positional args.
// Later sources override earlier ones, so a positional value wins over the JSON file.
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables(prefix: "WEBSNAP_")
    .AddCommandLine(namedArgs)
    .AddInMemoryCollection(positionalOverrides)
    .Build();

var options = config.GetSection("WebSnap").Get<WebSnapOptions>() ?? new WebSnapOptions();

// create a Playwright instance and launch a Chromium browser
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync();
var page = await browser.NewPageAsync();

// navigate to the configured URL and wait until the network is idle
await NavigateAsync(page, options.Url);
await DismissGdprCookiePopupAsync(page, options.CookieButtonName, options.TimeoutMs);

var filename = BuildScreenshotFilename(options.Url);

await page.ScreenshotAsync(new()
{
    Path = filename,
    FullPage = options.FullPage
});

#region Version

/// Returns the product version, e.g. "1.0.0+1837e7c". The SDK appends the
/// full git commit hash to the informational version; shorten it for display.
static string GetVersion()
{
    var version = typeof(WebSnapOptions).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";

    var plus = version.IndexOf('+');
    return plus >= 0 && version.Length > plus + 8 ? version[..(plus + 8)] : version;
}

#endregion

#region Arguments Parsing

/// Splits leading bare tokens (positional convenience args) from named flags.
/// Once a token starts with '-' or '/' or contains '=', it and the rest are
/// treated as named args and handed to AddCommandLine.
static (List<string> Positional, string[] Named) SplitArgs(string[] args)
{
    var positional = new List<string>();
    var index = 0;
    while (index < args.Length
        && !args[index].StartsWith('-')
        && !args[index].StartsWith('/')
        && !args[index].Contains('='))
    {
        positional.Add(args[index]);
        index++;
    }

    return (positional, args[index..]);
}

#endregion

#region Website Navigation and Interaction

/// Navigates to the specified URL and waits until the network is idle.
static async Task NavigateAsync(IPage page, string url) =>
    await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });

/// Clicks a GDPR/cookie popup button by accessible name when provided.
static async Task DismissGdprCookiePopupAsync(IPage page, string? buttonName, int timeoutMs)
{
    if (string.IsNullOrWhiteSpace(buttonName))
    {
        return;
    }

    var button = page.GetByRole(AriaRole.Button, new() { Name = buttonName });

    try
    {
        await button.First.ClickAsync(new() { Timeout = timeoutMs });
    }
    catch (PlaywrightException)
    {
    }
}

#endregion

#region Screenshot Filename Generation

/// Builds a filename for the screenshot based on the URL and the current timestamp.
static string BuildScreenshotFilename(string url)
{
    var uri = new Uri(url);
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var hostname = uri.Host.Replace('.', '_');
    var port = uri.Port;
    return $"{timestamp}_{hostname}_{port}.png";
}

#endregion
