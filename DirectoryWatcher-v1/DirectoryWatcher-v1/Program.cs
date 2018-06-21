using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
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

        private static async void FileCreated(object sender, FileSystemEventArgs e)
        {
            //If file!
            if (!IsDirectory(e.FullPath))
            {
                Console.WriteLine($"{MD5Hash(e.FullPath)}-{GetExtensionlessName(e.FullPath)} has been {e.ChangeType.ToString().ToLower()}");
                await UploadFile(e.FullPath, "anonymous", "anonymous");
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

        private static async Task UploadFile(string path, string username, string password)
        {
            const string FtpRoot = "ftp://speedtest.tele2.net/upload";
            await Task.Factory.StartNew(()=> 
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Credentials = new NetworkCredential(username, password);
                    wc.UploadFile($"{FtpRoot}/${Path.GetFileName(path)}", WebRequestMethods.Ftp.UploadFile, path);

                    // Afterwards emit SendEmailEvent
                    Console.WriteLine($"{MD5Hash(path)}-{GetExtensionlessName(path)} uploaded to FTP!");
                }
            });
        }

        private static string MD5Hash(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
