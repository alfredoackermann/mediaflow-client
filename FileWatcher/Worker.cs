using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileWatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1000, stoppingToken);

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = @"C:\WatchFolder";

            watcher.IncludeSubdirectories = false;

            watcher.NotifyFilter = NotifyFilters.CreationTime 
                | NotifyFilters.FileName 
                | NotifyFilters.Size
                | NotifyFilters.LastWrite
                | NotifyFilters.LastAccess;

            watcher.Filter = "*.*";

            watcher.Created += OnChanged;

            watcher.EnableRaisingEvents = true;
        }

        public void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                _logger.LogInformation("Arquivo criado: {name}", e.Name);

                OpenBrowser("https://localhost:44346/metadata?fileName=" + e.Name);
            }
        }

        public static void OpenBrowser(string url)
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw new Exception("Unsupported platform");
            }
        }

        public static bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None);
                return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void WaitForFile(string filename)
        {
            //This will lock the execution until the file is ready
            //TODO: Add some logic to make it async and cancelable
            while (!IsFileReady(filename)) { }
        }
    }
}
