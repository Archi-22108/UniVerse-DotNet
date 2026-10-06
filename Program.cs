using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

// Locate UniVerse.Server folder
var rootDir = Directory.GetCurrentDirectory();
var serverDir = Path.Combine(rootDir, "UniVerse.Server");
if (!Directory.Exists(serverDir))
{
    serverDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "UniVerse.Server");
    serverDir = Path.GetFullPath(serverDir);
}

var serverProj = Path.Combine(serverDir, "UniVerse.Server.csproj");

// Launch browser to http://localhost:5277/Dashboard in background after a brief delay
new Thread(() =>
{
    Thread.Sleep(1200);
    try
    {
        Process.Start(new ProcessStartInfo("cmd", "/c start http://localhost:5277/Dashboard")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }
    catch
    {
        // Ignore if browser launch is not supported
    }
}) { IsBackground = true }.Start();

// Forward execution to UniVerse.Server
var psi = new ProcessStartInfo("dotnet", $"run --project \"{serverProj}\" " + string.Join(" ", args))
{
    WorkingDirectory = serverDir,
    UseShellExecute = false
};

var proc = Process.Start(psi);
proc?.WaitForExit();
return proc?.ExitCode ?? 0;
