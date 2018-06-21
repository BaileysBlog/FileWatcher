using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DirectoryWatcher_v1
{
    class Program
    {
        static FileSystemWatcher fw;
        static ManualResetEvent ExitHandler = new ManualResetEvent(false); 

        static void Main(string[] args)
        {

            var watcherPath = CreateWatcherDirectory();

            Console.WriteLine($"Beginning to watch directory {watcherPath.FullName}");
            Console.WriteLine("Press Ctrl+C to exit!");

            Console.CancelKeyPress += ExitApplication;

            StartWatching(watcherPath);
            
            ExitHandler.WaitOne();

            //Environment.Exit(0);
            //Time to exit!
        }

        private static void ExitApplication(object sender, ConsoleCancelEventArgs e)
        {
            fw.EnableRaisingEvents = false;
            fw.Dispose();
            ExitHandler.Set();
        }

        public static DirectoryInfo CreateWatcherDirectory()
        {
            var Desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var FolderName = "Watch ME";
            return Directory.CreateDirectory(Path.Combine(Desktop, FolderName));
        }

        public static void StartWatching(DirectoryInfo _dir)
        {
            fw = new FileSystemWatcher(_dir.FullName)
            {
                Filter = "*.*",
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                         | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            fw.Created += FileCreated;
            fw.Renamed += FileRenamed;
            fw.EnableRaisingEvents = true;
            
        }

        private static void FileRenamed(object sender, RenamedEventArgs e)
        {
            if (!IsDirectory(e.FullPath))
            {
                Console.WriteLine($"{e.OldName} --> {e.Name}");
            }
            else
            {
                Console.WriteLine($"{GetExtensionlessName(e.OldFullPath)} --> {GetExtensionlessName(e.FullPath)}");
            }
        }

        private static string GetExtensionlessName(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        private static void FileCreated(object sender, FileSystemEventArgs e)
        {
            //If file!
            if (!IsDirectory(e.FullPath))
            {
                Console.WriteLine($"{GetExtensionlessName(e.FullPath)} has been {e.ChangeType.ToString().ToLower()}");
            }
            else
            {
                // Special logic for directories?
                Console.WriteLine($"{Path.GetFileNameWithoutExtension(e.FullPath)} is a directory, and has also been {e.ChangeType.ToString().ToLower()}");
            }
            

            
        }

        private static bool IsDirectory(string path)
        {
            FileAttributes fa = File.GetAttributes(path);
            return (fa & FileAttributes.Directory) != 0;
        }
    }
}
