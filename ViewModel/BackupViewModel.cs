using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Projet_ProgSys.Model;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Concurrent;
using System.Resources;

namespace Projet_ProgSys.ViewModel
{
    class BackupViewModel
    {
        static internal readonly object LogLock = new object();
        public static void SavingPrompt()
        {
            ResourceManager res_mng = new ResourceManager(typeof(Languages.Resource));
            Console.WriteLine(res_mng.GetString("Saving"));
        }

        public BackupViewModel()
        {
            if (DatabaseViewModel.NumberBackups() != 0)
            {

                BackupModel backupModel = DatabaseViewModel.GetBackup();
                if (backupModel.BackupType == (BackupModel.BackupTypes)0)
                {
                    Copy(backupModel.SourcePath, backupModel.DestinationPath, backupModel.Name);
                }
                else if (backupModel.BackupType == (BackupModel.BackupTypes)1)
                {
                    CopyMD5(backupModel.SourcePath, backupModel.DestinationPath, backupModel.Name);
                }
                else
                {
                    Task thread1 = new Task(() => RemoveDeletedFile(backupModel.SourcePath, backupModel.DestinationPath));
                    Task thread2 = new Task(() => CopyMD5(backupModel.SourcePath, backupModel.DestinationPath, backupModel.Name));
                    thread1.Start();
                    thread2.Start();
                }
                LogViewModel.StateLogUpdate(new StateLogModel(backupModel.Name, "", "", "FINISHED", 0, 0, 0, 0));
                GC.Collect();
            }
            else
            {
                DatabaseViewModel.NoBackupsYet();
            }

        }

        public static void CopyFile( string SourcePath, string DestinationPath , FileModel SourceFile, string Name, StateLogModel TempStateLog)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(DestinationPath, SourceFile.Path)));
            FileInfo fileInfo = new FileInfo(Path.Combine(SourcePath, SourceFile.Path));
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            File.Copy(Path.Combine(SourcePath, SourceFile.Path), Path.Combine(DestinationPath, SourceFile.Path), true);
            stopWatch.Stop();
            // Send models to queue
            lock (LogLock)
            {
                TempStateLog.TotalFilesSizeRemaining -= fileInfo.Length;
                TempStateLog.NbFilesLeftToDo--;
                TempStateLog.SourceFilePath = Path.Combine(SourcePath, SourceFile.Path);
                TempStateLog.TargetFilePath = Path.Combine(DestinationPath, SourceFile.Path);
                FileLogModel TempFileLog = new FileLogModel(Name, Path.Combine(SourcePath, SourceFile.Path), Path.Combine(DestinationPath, SourceFile.Path), String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    stopWatch.Elapsed.Hours, stopWatch.Elapsed.Minutes, stopWatch.Elapsed.Seconds,
                    stopWatch.Elapsed.Milliseconds / 10), fileInfo.Length, DateTime.Now.ToString(new CultureInfo("fr-FR")), 0);

                //LogFiles TempLogs = new LogFiles(TempStateLog, TempFileLog);
                // Write to logs
                LogViewModel.StateLogUpdate(TempStateLog);
                LogViewModel.FileLogUpdate(TempFileLog);
            }
        }

        // Function that copies and pastes a file
        public static void Copy(string SourcePath, string DestinationPath, string Name)
        {
            string[] SFileList = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories);
            List<FileModel> nameSFL = new List<FileModel>();

            long TotalSize = 0;
            Task ThreadSource = new Task(() =>
            {
                foreach (string FileSD in SFileList)
                {
                    while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                    FileInfo fileInfo = new FileInfo(FileSD);
                    TotalSize += fileInfo.Length;
                    string TempPath = Path.GetFullPath(FileSD).Substring(Path.GetFullPath(SourcePath).Length).TrimStart(Path.DirectorySeparatorChar);
                    nameSFL.Add(new FileModel(Path.GetFileName(FileSD), TempPath, fileInfo.Length));
                }
            });

            ThreadSource.Start();

            Task t1 = new Task(() =>
            {
                StateLogModel TempStateLog = new StateLogModel(Name, "", "", "RUNNING", nameSFL.Count(), TotalSize, nameSFL.Count(), TotalSize);
                SavingPrompt();
                Parallel.ForEach(nameSFL, SourceFile =>
                {
                    while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                    if (!Directory.Exists(SourceFile.Path))
                    {
                        CopyFile(SourcePath, DestinationPath, SourceFile, Name, TempStateLog);
                    }

                });
            });

            // Then start the copy and log writing
            ThreadSource.Wait();
            t1.Start();
            // Wait for it to end before going back to the menu
            t1.Wait();
            GC.Collect();
        }

        // Function used for make a differential backup
        public static void CopyMD5(string SourcePath, string DestinationPath, string Name)
        {
            // Recovering in a variable table the files
            string[] SFileList = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories);
            string[] DFileList = Directory.GetFiles(DestinationPath, "*", SearchOption.AllDirectories);

            // List
            List<FileModel> nameSFL = new List<FileModel>();
            List<FileModel> nameDFL = new List<FileModel>();
            long TotalSize = 0;
            Task ThreadSource = new Task(() =>
            {
                foreach (string FileSD in SFileList)
                {
                    while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                    FileInfo fileInfo = new FileInfo(FileSD);
                    TotalSize += fileInfo.Length;
                    string TempPath = Path.GetFullPath(FileSD).Substring(Path.GetFullPath(SourcePath).Length).TrimStart(Path.DirectorySeparatorChar);
                    nameSFL.Add(new FileModel(Path.GetFileName(FileSD), TempPath, fileInfo.Length));
                }
            });

            Task ThreadDestination = new Task(() =>
            {
                foreach (string FileDD in DFileList)
                {
                    while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                    FileInfo fileInfo = new FileInfo(FileDD);
                    string TempPath = Path.GetFullPath(FileDD).Substring(Path.GetFullPath(DestinationPath).Length).TrimStart(Path.DirectorySeparatorChar);
                    nameDFL.Add(new FileModel(Path.GetFileName(FileDD), TempPath, fileInfo.Length));
                }
            });

            ThreadSource.Start();
            ThreadDestination.Start();

            Task t1 = new Task(() =>
            {
                StateLogModel TempStateLog = new StateLogModel(Name, "", "", "RUNNING", nameSFL.Count(), TotalSize, nameSFL.Count(), TotalSize);
                Parallel.ForEach(nameSFL, SourceFile =>
                {
                    while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                    if (!Directory.Exists(SourceFile.Path))
                    {
                        FileModel TestDuplicate = nameDFL.FirstOrDefault(o => o.Path == SourceFile.Path);
                        if (TestDuplicate == null || CalculateMD5(Path.Combine(SourcePath, SourceFile.Path)) != CalculateMD5(Path.Combine(DestinationPath, TestDuplicate.Path)))
                        {
                            CopyFile(SourcePath, DestinationPath, SourceFile, Name, TempStateLog);
                        }
                    }
                });
            });

            // Wait for the lists to be completed
            Task.WhenAll(ThreadDestination, ThreadSource).Wait();
            // Then start the copy and log writing
            t1.Start();
            // Wait for it to end before going back to the menu
            t1.Wait();
            GC.Collect();
        }

        public static void RemoveDeletedFile(string SourcePath, string DestinationPath)
        {
            // Recovering in a variable table the files
            string[] SFileList = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories);
            string[] DFileList = Directory.GetFiles(DestinationPath, "*", SearchOption.AllDirectories);


            // List
            List<FileModel> nameSFL = new List<FileModel>();
            List<FileModel> nameDFL = new List<FileModel>();
            Task ThreadSource = new Task(() =>
            {
                foreach (string FileSD in SFileList)
                {
                    FileInfo fileInfo = new FileInfo(FileSD);
                    string TempPath = Path.GetFullPath(FileSD).Substring(Path.GetFullPath(SourcePath).Length).TrimStart(Path.DirectorySeparatorChar);
                    nameSFL.Add(new FileModel(Path.GetFileName(FileSD), TempPath, fileInfo.Length));
                }
            });

            Task ThreadDestination = new Task(() =>
            {
                foreach (string FileDD in DFileList)
                {
                    FileInfo fileInfo = new FileInfo(FileDD);
                    string TempPath = Path.GetFullPath(FileDD).Substring(Path.GetFullPath(DestinationPath).Length).TrimStart(Path.DirectorySeparatorChar);
                    nameDFL.Add(new FileModel(Path.GetFileName(FileDD), TempPath, fileInfo.Length));
                }
            });

            ThreadSource.Start();
            ThreadDestination.Start();

            // Might want to count number of copies

            Parallel.ForEach(nameDFL, SourceFile =>
            {
                if (!Directory.Exists(SourceFile.Path))
                {
                    FileModel TestDuplicate = nameSFL.FirstOrDefault(o => o.Path == SourceFile.Path);
                    if (TestDuplicate == null)
                    {
                        File.Delete(Path.Combine(DestinationPath, SourceFile.Path));
                    }
                }
            });
        }

        // Function that calculates the MD5 checksum of a file
        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

    }
}