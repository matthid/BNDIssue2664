using System.Diagnostics;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

public class TestBenchmarkWithExternalProc
{
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        var dll = typeof(ExternalProgram).Assembly.Location;
        var exe = Path.ChangeExtension(dll, ".exe");
        var psi = new ProcessStartInfo(exe, "config=test.config");
        psi.RedirectStandardError = true;
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        var outputWriter = new StringWriter();
        foreach (var variable in psi.Environment)
        {
            var key = variable.Key;
            if (key?.Contains("bearer", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                outputWriter.WriteLine($"{variable.Key}: ... skipped ...");
            }
            else
            {
                outputWriter.WriteLine($"{variable.Key}: {variable.Value}");
            }
        }
        var proc = Process.Start(psi);
        if (proc == null)
            throw new InvalidOperationException("Process was null");
        
        proc.OutputDataReceived += (sender, data) =>
        {
            if (data?.Data is { } d)
            {
                outputWriter.WriteLine("OUT:" + d);
            }
        };
        proc.ErrorDataReceived += (sender, data) =>
        {
            if (data?.Data is { } d)
            {
                outputWriter.WriteLine("ERR:" + d);
            }
        };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        
        proc.WaitForExit();
        
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"External process failed ({proc.ExitCode}): {outputWriter.ToString()}");
        
    }

    [Benchmark]
    public int Logic()
    {
        int res = 0;
        for (int i = 0; i < 10; i++)
            res += i;
        return res;
    }
}