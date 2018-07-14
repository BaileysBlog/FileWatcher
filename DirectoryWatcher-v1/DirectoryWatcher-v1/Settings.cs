using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DirectoryWatcher
{
    public class Settings
    {
        public String BackupPath { get; set; }
        public String WatchPath { get; set; }

        public Boolean DeleteFileAfterUpload { get; set; }


        public String GetWatcherPath()
        {
            if (!String.IsNullOrEmpty(WatchPath))
            {
                //Watch path has value
                if (Directory.Exists(WatchPath))
                {
                    // Good to go
                    return WatchPath;
                }
                else
                {
                    return ConvertBackupPath();
                }
            }
            else
            {
                return ConvertBackupPath();
            }
        }

        private String ConvertBackupPath()
        {
            var Desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var _Desktop = BackupPath.Replace("{{Desktop}}", Desktop);

            if (!Directory.Exists(_Desktop))
            {
                Directory.CreateDirectory(_Desktop);
            }


            return _Desktop;
        }

    }
}
