using System.Diagnostics;
using System.Text;
using System.Runtime.Versioning;

namespace GetToKnowYourDevice.Infrastructure.PowerShellSupport;

/// <summary>Outcome of a PowerShell fallback invocation.</summary>
public sealed record PowerShellResult(bool Success, string StandardOutput, string StandardError, int ExitCode);

/// <summary>
/// Runs PowerShell as a controlled fallback only. Always hidden window, always with a
/// timeout and cancellation, captures both streams, validates exit code. Arguments are
/// passed via argument list (never string-concatenated) to avoid command injection.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class PowerShellRunner
{
    /// <summary>
    /// Executes a script block. The script is passed as a single -Command argument through
    /// the argument list; callers must not build it from unsanitized user input.
    /// </summary>
    public async Task<PowerShellResult> RunAsync(string script, TimeSpan timeout, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-OutputFormat");
        psi.ArgumentList.Add("Text");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(script);

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        if (!process.Start())
            return new PowerShellResult(false, "", "Failed to start PowerShell.", -1);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        return new PowerShellResult(
            Success: process.ExitCode == 0,
            StandardOutput: stdout.ToString(),
            StandardError: stderr.ToString(),
            ExitCode: process.ExitCode);
    }

    private static void TryKill(Process p)
    {
        try { if (!p.HasExited) p.Kill(entireProcessTree: true); }
        catch { /* best effort */ }
    }
}
