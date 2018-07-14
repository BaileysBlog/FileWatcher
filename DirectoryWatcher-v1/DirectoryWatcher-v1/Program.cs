using DirectoryWatcher;
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

        static readonly Settings Settings;

        static Program()
        {

            //Load text
            var text = File.ReadAllText("Settings.json");

            // Try and read the Settings.json file
            Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(text);
        }

        static void Main(string[] args)
        {
            
            var watcherPath = GetOrCreateWatcherDirectory();

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

        public static DirectoryInfo GetOrCreateWatcherDirectory()
        {
            return new DirectoryInfo(Settings.GetWatcherPath());
        }

        public static void StartWatching(DirectoryInfo _dir)
        {
            fw = new FileSystemWatcher(_dir.FullName)
            {
                Filter = "*.*",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
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
                await FileUnlocked(e.FullPath);
                Console.WriteLine($"{MD5Hash(e.FullPath)}-{GetExtensionlessName(e.FullPath)} has been {e.ChangeType.ToString().ToLower()}");
                await UploadFile(e.FullPath, "anonymous", "anonymous");
            }
            else
            {
                // Special logic for directories?
                Console.WriteLine($"{Path.GetFileNameWithoutExtension(e.FullPath)} is a directory, and has also been {e.ChangeType.ToString().ToLower()}");
            }            
        }

        private static Task FileUnlocked(string fullPath)
        {
            return Task.Factory.StartNew(()=> 
            {

                // If locked wait 1 second and try again
                // Else kill thread

                do
                {
                    Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
                } while (IsFileLocked(fullPath));
            });
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


                    // Check if file needs deleted after upload
                    if (Settings.DeleteFileAfterUpload)
                    {
                        TryDeleteFileAsync(path);
                        Console.WriteLine($"{GetExtensionlessName(path)} has been deleted");
                    }
                }
            });
        }

        public static Task TryDeleteFileAsync(String path)
        {
            return Task.Factory.StartNew(()=> 
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception)
                {
                    
                }
            });
        }

        private static string MD5Hash(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static bool IsFileLocked(String path)
        {
            FileStream stream = null;

            try
            {
                stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
