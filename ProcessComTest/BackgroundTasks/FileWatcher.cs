using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessComTest.BackgroundTasks
{
    public class FileWatcher : IHostedService, IDisposable
    {
        private readonly IMemoryCacheTest _memoryCacheTest;
        private readonly FileSystemWatcher _watcher;

        public FileWatcher(IMemoryCacheTest memoryCacheTest, IHostEnvironment hostEnvironment)
        {
            _memoryCacheTest = memoryCacheTest;

            // Create a new FileSystemWatcher and set its properties.
            _watcher = new FileSystemWatcher
            {
                Path = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "test"),

                /* Watch for changes in LastAccess and LastWrite times, and  the renaming of files or directories. */
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,

                // Only watch text files.
                Filter = "*.txt"
            };
        }


        public void Dispose()
        {
            _watcher?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if(!cancellationToken.IsCancellationRequested)
            {
                CreateFileWatcher();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
                Thread.Sleep(1000);

            if (cancellationToken.IsCancellationRequested)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= new FileSystemEventHandler(OnChanged);
                //_watcher.Created -= new FileSystemEventHandler(OnChanged);
                //_watcher.Deleted -= new FileSystemEventHandler(OnChanged);
                //_watcher.Renamed -= new RenamedEventHandler(OnRenamed);
            }

            return Task.CompletedTask;    
        }

        private void CreateFileWatcher()
        {
            // Add event handlers.
            _watcher.Changed += new FileSystemEventHandler(OnChanged);
            //_watcher.Created += new FileSystemEventHandler(OnChanged);
            //_watcher.Deleted += new FileSystemEventHandler(OnChanged);
            //_watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            _watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Debug.WriteLine($"File with Path: {e.FullPath} and name {e.Name} has change type of {e.ChangeType}");
            _memoryCacheTest.SetItemInCacheTest(e.FullPath);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Debug.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }
    }
}
