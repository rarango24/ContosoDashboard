namespace ContosoDashboard.Services;

/// <summary>
/// Abstraction for file virus/malware scanning.
/// Training implementation: StubFileScanner always returns clean.
/// Production replacement: ClamAvScanner using the nClam NuGet package
/// (dotnet add package nClam) pointing at a local ClamAV daemon.
/// </summary>
public interface IFileScanner
{
    /// <summary>
    /// Scans the provided stream for threats.
    /// Returns true if the file is clean; false if a threat is detected.
    /// The stream position is reset to 0 before scanning and left at 0 after.
    /// </summary>
    Task<bool> ScanAsync(Stream content);
}

/// <summary>
/// Training stub — always reports files as clean.
/// Swap this for ClamAvScanner (nClam) in production deployments.
/// </summary>
public class StubFileScanner : IFileScanner
{
    public Task<bool> ScanAsync(Stream content)
    {
        // TRAINING ONLY: No actual virus scanning performed.
        // In production, replace with:
        //   var clam = new ClamClient("localhost", 3310);
        //   var result = await clam.SendAndScanFileAsync(content);
        //   return result.Result == ClamScanResults.Clean;
        return Task.FromResult(true);
    }
}
