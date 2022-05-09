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
using Projet_ProgSys.Model;

namespace Projet_ProgSys.ViewModel
{
    class BackupViewModelRewrite
    {
        static internal readonly object LogLock = new object();
        static internal readonly object PauseLock = new object();
        static internal readonly object SizeLock = new object();
        static public int LockingNumber = 0;

        public BackupViewModelRewrite()
        {
        }

        static public void StartBackup(BackupModel backupModel)
        {
            string Status;
            lock (PauseLock) { Status = DatabaseViewModel.ReturnStatus(backupModel.Name); }
            if (Status == "STOPPED" || Status == "")
            {
                lock (LogLock)
                {
                    DatabaseViewModel.SetStatus(backupModel, "RUNNING");
                }
                if (backupModel.BackupType == (BackupModel.BackupTypes)0)
                {
                    Copy(backupModel.SourcePath, backupModel.DestinationPath, backupModel.Name);
                }
                else if (backupModel.BackupType == (BackupModel.BackupTypes)1)
                {
                    CopyMD5(backupModel.SourcePath, backupModel.DestinationPath, backupModel.Name);
                }
                
                lock (LogLock)
                {
                    UpdateStateLog(backupModel.Name, "FINISHED");
                    DatabaseViewModel.SetStatus(backupModel, "STOPPED");
                }
                GC.Collect();
            }
        }

        static public void UpdateStateLog(string Name, string Status)
        {
            lock (PauseLock)
            {
                Thread thread = new Thread(() =>
                {
                    bool stopped = false;
                    while (!stopped)
                    {
                        LogViewModel.StateLogUpdate(new StateLogModel(Name, "", "", Status, 0, 0, 0, 0));
                        Thread.Sleep(10000);
                        List<StateLogModel> backupModelsState = BackupViewModelRewrite.ListState();
                        var index = backupModelsState.FindIndex(o => o.Name == Name);
                        if (backupModelsState[index].Name == "STOPPED")
                        {
                            stopped = true;
                        }
                    }
                });

                thread.Start();
            }
        }

        static public void CreateStateLog()
        {
            lock (PauseLock)
            {
                LogViewModel.StateLogCreation();
            }
        }

        public static void CopyFile( string SourcePath, string DestinationPath , FileModel SourceFile, string Name, StateLogModel TempStateLog)
        {
            var MyIni = new IniFile("Settings.ini");
            int DefaultVolume = Int32.Parse(MyIni.Read("DefaultSize"));
            FileInfo fileInfo = new FileInfo(Path.Combine(SourcePath, SourceFile.Path));
            lock (SizeLock)
            {
                while (LockingNumber >= 1) { Thread.Sleep(2000); }
                if (fileInfo.Length >= (1000000* DefaultVolume))
                {
                    LockingNumber++;
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(DestinationPath, SourceFile.Path)));
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
                    stopWatch.Elapsed.Milliseconds / 10), fileInfo.Length, DateTime.Now.ToString(new CultureInfo("fr-FR")),0);

                //LogFiles TempLogs = new LogFiles(TempStateLog, TempFileLog);
                // Write to logs
                lock (PauseLock)
                {
                    LogViewModel.StateLogUpdate(TempStateLog);
                }
                LogViewModel.FileLogUpdate(TempFileLog);
            }
            if (fileInfo.Length >= 10000)
            {
                lock (SizeLock)
                {
                    LockingNumber--;
                }
            }
        }

        static public List<StateLogModel> ListState()
        {
            lock (LogLock) { return LogViewModel.ListState(); }
        }

        // Function that copies and pastes a file
        public static void Copy(string SourcePath, string DestinationPath, string Name)
        {
            string[] SFileList = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories);
            List<FileModel> nameSFL = new List<FileModel>();
            List<FileModel> nameSFLP = new List<FileModel>();
            List<string> ListExtensionPrio = DatabaseViewModel.ListExtensions("hpextensions");
            List<string> ListExtensionCrypt = DatabaseViewModel.ListExtensions("extensions");

            long TotalSize = 0;
            Task ThreadSource = new Task(() =>
            {
                foreach (string FileSD in SFileList)
                {
                    FileInfo fileInfo = new FileInfo(FileSD);
                    TotalSize += fileInfo.Length;
                    string TempPath = Path.GetFullPath(FileSD).Substring(Path.GetFullPath(SourcePath).Length).TrimStart(Path.DirectorySeparatorChar);
                    string extension = Path.GetExtension(FileSD);
                    if (ListExtensionPrio.Contains(extension))
                    {
                        nameSFLP.Add(new FileModel(Path.GetFileName(FileSD), TempPath, fileInfo.Length));
                    } else
                    {
                        nameSFL.Add(new FileModel(Path.GetFileName(FileSD), TempPath, fileInfo.Length));
                    }
                }
            });

            ThreadSource.Start();
            Task t1 = new Task(() =>
            {
                StateLogModel TempStateLog = new StateLogModel(Name, "", "", "RUNNING", nameSFL.Count() + nameSFLP.Count(), TotalSize, nameSFL.Count() + nameSFLP.Count(), TotalSize);
                bool Pause = false;
                Parallel.ForEach(nameSFLP, (SourceFile, state) =>
                {
                    lock (PauseLock)
                    {
                        if (DatabaseViewModel.ReturnStatus(Name) == "STOPPED")
                        {
                            state.Break();
                        }
                        else if (DatabaseViewModel.ReturnStatus(Name) == "PAUSED")
                        {
                            Pause = true;
                        }
                        List<string> ListProcess = DatabaseViewModel.ListSoftwares();
                        foreach (string DesiredProcess in ListProcess)
                        {
                            if (Process.GetProcessesByName(DesiredProcess).Length > 0)
                            {
                                Pause = true;
                            }
                        }
                    }
                    while (Pause)
                    {
                        Thread.Sleep(2000);
                        lock (PauseLock)
                        {
                            if (DatabaseViewModel.ReturnStatus(Name) == "RUNNING")
                            {
                                Pause = false;
                            }
                            List<string> ListProcess = DatabaseViewModel.ListSoftwares();
                            foreach (string DesiredProcess in ListProcess)
                            {
                                if (Process.GetProcessesByName(DesiredProcess).Length > 0)
                                {
                                    Pause = true;
                                }
                            }
                        }
                    }
                    if (!Directory.Exists(SourceFile.Path))
                    {
                        lock (PauseLock)
                        {
                            while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                        }
                        string extension = Path.GetExtension(SourceFile.Name);
                        if (ListExtensionCrypt.Contains(extension))
                        {
                            var MyIni = new IniFile("Settings.ini");
                            string EncryptionKey = MyIni.Read("EncryptionKey");
                            int DefaultVolume = Int32.Parse(MyIni.Read("DefaultSize"));
                            FileInfo fileInfo = new FileInfo(Path.Combine(SourcePath, SourceFile.Path));
                            lock (SizeLock)
                            {
                                while (LockingNumber >= 1) { Thread.Sleep(2000); }
                                if (fileInfo.Length >= (1000000 * DefaultVolume))
                                {
                                    LockingNumber++;
                                }
                            }
                            Process p = new Process();
                            p.StartInfo.FileName = "CryptoSoft/CryptoSoft.exe";
                            p.StartInfo.Arguments = $"{Path.Combine(SourcePath, SourceFile.Path)} {Path.Combine(DestinationPath, SourceFile.Path)} {EncryptionKey} COMPLETE";
                            p.StartInfo.CreateNoWindow = true;
                            Stopwatch stopWatch = new Stopwatch();
                            stopWatch.Start();
                            p.Start();
                            p.WaitForExit();
                            stopWatch.Stop();
                            int result = p.ExitCode;

                            lock (LogLock)
                            {
                                TempStateLog.TotalFilesSizeRemaining -= fileInfo.Length;
                                TempStateLog.NbFilesLeftToDo--;
                                TempStateLog.SourceFilePath = Path.Combine(SourcePath, SourceFile.Path);
                                TempStateLog.TargetFilePath = Path.Combine(DestinationPath, SourceFile.Path);
                                FileLogModel TempFileLog = new FileLogModel(Name, Path.Combine(SourcePath, SourceFile.Path), Path.Combine(DestinationPath, SourceFile.Path), String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                    stopWatch.Elapsed.Hours, stopWatch.Elapsed.Minutes, stopWatch.Elapsed.Seconds,
                                    stopWatch.Elapsed.Milliseconds / 10), fileInfo.Length, DateTime.Now.ToString(new CultureInfo("fr-FR")), result);

                                //LogFiles TempLogs = new LogFiles(TempStateLog, TempFileLog);
                                // Write to logs
                                lock (PauseLock)
                                {
                                    LogViewModel.StateLogUpdate(TempStateLog);
                                }
                                LogViewModel.FileLogUpdate(TempFileLog);
                            }
                            if (fileInfo.Length >= 10000)
                            {
                                lock (SizeLock)
                                {
                                    LockingNumber--;
                                }
                            }

                        }
                        else
                        {
                            CopyFile(SourcePath, DestinationPath, SourceFile, Name, TempStateLog);
                        }
                    }

                });

                Parallel.ForEach(nameSFL, (SourceFile, state) =>
                {
                    lock (PauseLock)
                    {
                        if (DatabaseViewModel.ReturnStatus(Name) == "STOPPED")
                        {
                            state.Break();
                        }
                        else if (DatabaseViewModel.ReturnStatus(Name) == "PAUSED")
                        {
                            Pause = true;
                        }
                        List<string> ListProcess = DatabaseViewModel.ListSoftwares();
                        foreach (string DesiredProcess in ListProcess)
                        {
                            if (Process.GetProcessesByName(DesiredProcess).Length > 0)
                            {
                                Pause = true;
                            }
                        }
                    }
                    while (Pause)
                    {
                        Thread.Sleep(2000);
                        lock (PauseLock)
                        {
                            if (DatabaseViewModel.ReturnStatus(Name) == "RUNNING")
                            {
                                Pause = false;
                            }
                            List<string> ListProcess = DatabaseViewModel.ListSoftwares();
                            foreach (string DesiredProcess in ListProcess)
                            {
                                if (Process.GetProcessesByName(DesiredProcess).Length > 0)
                                {
                                    Pause = true;
                                }
                            }
                        }
                    }
                    if (!Directory.Exists(SourceFile.Path))
                    {
                        lock (PauseLock)
                        {
                            while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                        }
                        string extension = Path.GetExtension(SourceFile.Name);
                        if (ListExtensionCrypt.Contains(extension))
                        {
                            var MyIni = new IniFile("Settings.ini");
                            string EncryptionKey = MyIni.Read("EncryptionKey");
                            int DefaultVolume = Int32.Parse(MyIni.Read("DefaultSize"));
                            FileInfo fileInfo = new FileInfo(Path.Combine(SourcePath, SourceFile.Path));
                            lock (SizeLock)
                            {
                                while (LockingNumber >= 1) { Thread.Sleep(2000); }
                                if (fileInfo.Length >= (1000000 * DefaultVolume))
                                {
                                    LockingNumber++;
                                }
                            }
                            Process p = new Process();
                            p.StartInfo.FileName = "CryptoSoft/CryptoSoft.exe";
                            p.StartInfo.Arguments = $"{Path.Combine(SourcePath, SourceFile.Path)} {Path.Combine(DestinationPath, SourceFile.Path)} {EncryptionKey} COMPLETE";
                            p.StartInfo.CreateNoWindow = true;
                            Stopwatch stopWatch = new Stopwatch();
                            stopWatch.Start();
                            p.Start();
                            p.WaitForExit();
                            stopWatch.Stop();
                            int result = p.ExitCode;

                            lock (LogLock)
                            {
                                TempStateLog.TotalFilesSizeRemaining -= fileInfo.Length;
                                TempStateLog.NbFilesLeftToDo--;
                                TempStateLog.SourceFilePath = Path.Combine(SourcePath, SourceFile.Path);
                                TempStateLog.TargetFilePath = Path.Combine(DestinationPath, SourceFile.Path);
                                FileLogModel TempFileLog = new FileLogModel(Name, Path.Combine(SourcePath, SourceFile.Path), Path.Combine(DestinationPath, SourceFile.Path), String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                    stopWatch.Elapsed.Hours, stopWatch.Elapsed.Minutes, stopWatch.Elapsed.Seconds,
                                    stopWatch.Elapsed.Milliseconds / 10), fileInfo.Length, DateTime.Now.ToString(new CultureInfo("fr-FR")),result);

                                //LogFiles TempLogs = new LogFiles(TempStateLog, TempFileLog);
                                // Write to logs
                                lock (PauseLock)
                                {
                                    LogViewModel.StateLogUpdate(TempStateLog);
                                }
                                LogViewModel.FileLogUpdate(TempFileLog);
                            }
                            if (fileInfo.Length >= 10000)
                            {
                                lock (SizeLock)
                                {
                                    LockingNumber--;
                                }
                            }

                        }
                        else
                        {
                            CopyFile(SourcePath, DestinationPath, SourceFile, Name, TempStateLog);
                        }
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
            List<FileModel> nameSFLP = new List<FileModel>();
            List<string> ListExtensionPrio = DatabaseViewModel.ListExtensions("hpextensions");
            List<string> ListExtensionCrypt = DatabaseViewModel.ListExtensions("extensions");
            long TotalSize = 0;
            Task ThreadSource = new Task(() =>
            {
                foreach (string FileSD in SFileList)
                {
                    FileInfo fileInfo = new FileInfo(FileSD);
                    TotalSize += fileInfo.Length;
                    string TempPath = Path.GetFullPath(FileSD).Substring(Path.GetFullPath(SourcePath).Length).TrimStart(Path.DirectorySeparatorChar);
                    string extension = Path.GetExtension(FileSD);
                    if (ListExtensionPrio.Contains(extension))
                    {
                        nameSFLP.Add(new FileModel(Path.GetFileName(FileSD), TempPath, fileInfo.Length));
                    }
                    else
                    {
                        nameSFL.Add(new FileModel(Path.GetFileName(FileSD), TempPath, fileInfo.Length));
                    }
                }
            });

            Task ThreadDestination = new Task(() =>
            {
                foreach (string FileDD in DFileList)
                {
                    lock (PauseLock)
                    {
                        while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                    }
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
                bool Pause = false; ;
                int Count = nameSFL.Count() + nameSFLP.Count();
                Parallel.ForEach(nameSFLP, (SourceFile, state) =>
                {
                    lock (PauseLock)
                    {
                        if (DatabaseViewModel.ReturnStatus(Name) == "STOPPED")
                        {
                            state.Break();
                        }
                        else if (DatabaseViewModel.ReturnStatus(Name) == "PAUSED")
                        {
                            Pause = true;
                        }
                        List<string> ListProcess = DatabaseViewModel.ListSoftwares();
                        foreach (string DesiredProcess in ListProcess)
                        {
                            if (Process.GetProcessesByName(DesiredProcess).Length > 0)
                            {
                                Pause = true;
                            }
                        }
                    }
                    while (Pause)
                    {
                        Thread.Sleep(2000);
                        lock (PauseLock)
                        {
                            if (DatabaseViewModel.ReturnStatus(Name) == "RUNNING")
                            {
                                Pause = false;
                            }
                            List<string> ListProcess = DatabaseViewModel.ListSoftwares();
                            foreach (string DesiredProcess in ListProcess)
                            {
                                if (Process.GetProcessesByName(DesiredProcess).Length > 0)
                                {
                                    Pause = true;
                                }
                            }
                        }
                    }
                    if (!Directory.Exists(SourceFile.Path))
                    {
                        lock (PauseLock)
                        {
                            while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                        }
                        string extension = Path.GetExtension(SourceFile.Name);
                        if (ListExtensionCrypt.Contains(extension))
                        {
                            var MyIni = new IniFile("Settings.ini");
                            string EncryptionKey = MyIni.Read("EncryptionKey");
                            int DefaultVolume = Int32.Parse(MyIni.Read("DefaultSize"));
                            FileInfo fileInfo = new FileInfo(Path.Combine(SourcePath, SourceFile.Path));
                            lock (SizeLock)
                            {
                                while (LockingNumber >= 1) { Thread.Sleep(2000); }
                                if (fileInfo.Length >= (1000000 * DefaultVolume))
                                {
                                    LockingNumber++;
                                }
                            }
                            Process p = new Process();
                            p.StartInfo.FileName = "CryptoSoft/CryptoSoft.exe";
                            p.StartInfo.Arguments = $"{Path.Combine(SourcePath, SourceFile.Path)} {Path.Combine(DestinationPath, SourceFile.Path)} {EncryptionKey} DIFFERENTIAL";
                            p.StartInfo.CreateNoWindow = true;
                            Stopwatch stopWatch = new Stopwatch();
                            stopWatch.Start();
                            p.Start();
                            p.WaitForExit();
                            stopWatch.Stop();
                            int result = p.ExitCode;
                            if (result != 0)
                            {

                                lock (LogLock)
                                {
                                    TempStateLog.TotalFilesSizeRemaining -= fileInfo.Length;
                                    TempStateLog.NbFilesLeftToDo--;
                                    TempStateLog.SourceFilePath = Path.Combine(SourcePath, SourceFile.Path);
                                    TempStateLog.TargetFilePath = Path.Combine(DestinationPath, SourceFile.Path);
                                    FileLogModel TempFileLog = new FileLogModel(Name, Path.Combine(SourcePath, SourceFile.Path), Path.Combine(DestinationPath, SourceFile.Path), String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                        stopWatch.Elapsed.Hours, stopWatch.Elapsed.Minutes, stopWatch.Elapsed.Seconds,
                                        stopWatch.Elapsed.Milliseconds / 10), fileInfo.Length, DateTime.Now.ToString(new CultureInfo("fr-FR")), result);

                                    //LogFiles TempLogs = new LogFiles(TempStateLog, TempFileLog);
                                    // Write to logs
                                    lock (PauseLock)
                                    {
                                        LogViewModel.StateLogUpdate(TempStateLog);
                                    }
                                    LogViewModel.FileLogUpdate(TempFileLog);
                                }
                            } else
                            {
                                lock (LogLock)
                                {
                                    TempStateLog.TotalFilesSizeRemaining -= fileInfo.Length;
                                    TempStateLog.NbFilesLeftToDo--;
                                    TempStateLog.SourceFilePath = Path.Combine(SourcePath, SourceFile.Path);
                                    TempStateLog.TargetFilePath = Path.Combine(DestinationPath, SourceFile.Path);
                                    // Write to logs
                                    lock (PauseLock)
                                    {
                                        LogViewModel.StateLogUpdate(TempStateLog);
                                    }
                                }
                            }
                            
                            if (fileInfo.Length >= 10000)
                            {
                                lock (SizeLock)
                                {
                                    LockingNumber--;
                                }
                            }

                        }
                        else
                        {

                            FileModel TestDuplicate = nameDFL.FirstOrDefault(o => o.Path == SourceFile.Path);
                            if (TestDuplicate == null || CalculateMD5(Path.Combine(SourcePath, SourceFile.Path)) != CalculateMD5(Path.Combine(DestinationPath, TestDuplicate.Path)))
                            {
                                CopyFile(SourcePath, DestinationPath, SourceFile, Name, TempStateLog);
                            }
                        }
                    }
                });

                Parallel.ForEach(nameSFL, (SourceFile, state) =>
                {
                    lock (PauseLock)
                    {
                        if (DatabaseViewModel.ReturnStatus(Name) == "STOPPED")
                        {
                            state.Break();
                        }
                        else if (DatabaseViewModel.ReturnStatus(Name) == "PAUSED")
                        {
                            Pause = true;
                        }
                        List<string> ListProcess = DatabaseViewModel.ListSoftwares();
                        foreach (string DesiredProcess in ListProcess)
                        {
                            if (Process.GetProcessesByName(DesiredProcess).Length > 0)
                            {
                                Pause = true;
                            }
                        }
                    }
                    while (Pause)
                    {
                        Thread.Sleep(2000);
                        lock (PauseLock)
                        {
                            if (DatabaseViewModel.ReturnStatus(Name) == "RUNNING")
                            {
                                Pause = false;
                            }
                            List<string> ListProcess = DatabaseViewModel.ListSoftwares();
                            foreach (string DesiredProcess in ListProcess)
                            {
                                if (Process.GetProcessesByName(DesiredProcess).Length > 0)
                                {
                                    Pause = true;
                                }
                            }
                        }
                    }
                    if (!Directory.Exists(SourceFile.Path))
                    {
                        lock (PauseLock)
                        {
                            while (DatabaseViewModel.IsPaused(Name)) { Thread.Sleep(2000); }
                        }
                        string extension = Path.GetExtension(SourceFile.Name);
                        if (ListExtensionCrypt.Contains(extension))
                        {
                            var MyIni = new IniFile("Settings.ini");
                            string EncryptionKey = MyIni.Read("EncryptionKey");
                            int DefaultVolume = Int32.Parse(MyIni.Read("DefaultSize"));
                            FileInfo fileInfo = new FileInfo(Path.Combine(SourcePath, SourceFile.Path));
                            lock (SizeLock)
                            {
                                while (LockingNumber >= 1) { Thread.Sleep(2000); }
                                if (fileInfo.Length >= (1000000 * DefaultVolume))
                                {
                                    LockingNumber++;
                                }
                            }
                            Process p = new Process();
                            p.StartInfo.FileName = "CryptoSoft/CryptoSoft.exe";
                            string test1 = Path.Combine(SourcePath, SourceFile.Path);
                            string test2 = Path.Combine(DestinationPath, SourceFile.Path);
                            string test3 = EncryptionKey;
                            p.StartInfo.Arguments = $"{Path.Combine(SourcePath, SourceFile.Path)} {Path.Combine(DestinationPath, SourceFile.Path)} {EncryptionKey} DIFFERENTIAL";
                            p.StartInfo.CreateNoWindow = true;
                            Stopwatch stopWatch = new Stopwatch();
                            stopWatch.Start();
                            p.Start();
                            p.WaitForExit();
                            stopWatch.Stop();
                            int result = p.ExitCode;
                            if (result != 0)
                            {

                                lock (LogLock)
                                {
                                    TempStateLog.TotalFilesSizeRemaining -= fileInfo.Length;
                                    TempStateLog.NbFilesLeftToDo--;
                                    TempStateLog.SourceFilePath = Path.Combine(SourcePath, SourceFile.Path);
                                    TempStateLog.TargetFilePath = Path.Combine(DestinationPath, SourceFile.Path);
                                    FileLogModel TempFileLog = new FileLogModel(Name, Path.Combine(SourcePath, SourceFile.Path), Path.Combine(DestinationPath, SourceFile.Path), String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                        stopWatch.Elapsed.Hours, stopWatch.Elapsed.Minutes, stopWatch.Elapsed.Seconds,
                                        stopWatch.Elapsed.Milliseconds / 10), fileInfo.Length, DateTime.Now.ToString(new CultureInfo("fr-FR")), result);

                                    //LogFiles TempLogs = new LogFiles(TempStateLog, TempFileLog);
                                    // Write to logs
                                    lock (PauseLock)
                                    {
                                        LogViewModel.StateLogUpdate(TempStateLog);
                                    }
                                    LogViewModel.FileLogUpdate(TempFileLog);
                                }
                            }
                            else
                            {
                                lock (LogLock)
                                {
                                    TempStateLog.TotalFilesSizeRemaining -= fileInfo.Length;
                                    TempStateLog.NbFilesLeftToDo--;
                                    TempStateLog.SourceFilePath = Path.Combine(SourcePath, SourceFile.Path);
                                    TempStateLog.TargetFilePath = Path.Combine(DestinationPath, SourceFile.Path);
                                    // Write to logs
                                    lock (PauseLock)
                                    {
                                        LogViewModel.StateLogUpdate(TempStateLog);
                                    }
                                }
                            }

                            if (fileInfo.Length >= 10000)
                            {
                                lock (SizeLock)
                                {
                                    LockingNumber--;
                                }
                            }

                        }
                        else
                        {

                            FileModel TestDuplicate = nameDFL.FirstOrDefault(o => o.Path == SourceFile.Path);
                            if (TestDuplicate == null || CalculateMD5(Path.Combine(SourcePath, SourceFile.Path)) != CalculateMD5(Path.Combine(DestinationPath, TestDuplicate.Path)))
                            {
                                CopyFile(SourcePath, DestinationPath, SourceFile, Name, TempStateLog);
                            }
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

        public static void SetStatus(BackupModel oldbackup)
        {
            lock (PauseLock)
            {
                if (DatabaseViewModel.ReturnStatus(oldbackup.Name) == "PAUSED")
                {
                    DatabaseViewModel.SetStatus(oldbackup, "RUNNING");
                    UpdateStateLog(oldbackup.Name, "RUNNING");
                }
                else if (DatabaseViewModel.ReturnStatus(oldbackup.Name) == "RUNNING")
                {
                    DatabaseViewModel.SetStatus(oldbackup, "PAUSED");
                    UpdateStateLog(oldbackup.Name, "PAUSED");
                }
            }
        }

        public static void SetPause(BackupModel oldbackup)
        {
            lock (PauseLock)
            {
                if (DatabaseViewModel.ReturnStatus(oldbackup.Name) == "PAUSED")
                {
                    DatabaseViewModel.SetStatus(oldbackup, "RUNNING");
                }
            }
        }

        public static void SetResume(BackupModel oldbackup)
        {
            lock (PauseLock)
            {
                if (DatabaseViewModel.ReturnStatus(oldbackup.Name) == "RUNNING")
                {
                    DatabaseViewModel.SetStatus(oldbackup, "PAUSED");
                }
            }
        }

        public static void StopBackup(BackupModel oldbackup)
        {
            lock (PauseLock)
            {
                DatabaseViewModel.SetStatus(oldbackup, "STOPPED");
            }
        }
    }
}