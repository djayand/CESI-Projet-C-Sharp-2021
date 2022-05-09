using System;
using System.Collections.Generic;
using System.Resources;
using System.Linq;
using System.Data.SQLite;
using Projet_ProgSys.Model;
using System.IO;

namespace Projet_ProgSys.ViewModel
{
    class DatabaseViewModel
    {
        private static SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=EasySaveDB.sqlite;Version=3;");
        static internal readonly object PauseLock = new object();
        public DatabaseViewModel()
        {
        }

        /// <summary>
        /// BACKUP METHODS
        /// EVERYTHING RELATED TO BACKUPS
        /// </summary>

        // PRINT METHODS
        static public void NoBackupsYet()
        {
            ResourceManager res_mng = new ResourceManager(typeof(Languages.Resource));
            Console.WriteLine(res_mng.GetString("NoBackupYet"));
        }

        // DISPLAY METHODS
        static public void DisplayListBackups(List<BackupModel> ListOfBackups)
        {
            if (ListOfBackups.Count == 0)
            {
                NoBackupsYet();
            }
            else
            {
                foreach (var backup in ListOfBackups)
                {
                    DisplayBackup(backup);
                }
            }
        }

        static public void DisplayBackup(BackupModel backupModel)
        {
            Console.WriteLine("Backup: {0},{1},{2},{3},{4}", backupModel.ID, backupModel.Name, backupModel.SourcePath, backupModel.DestinationPath, backupModel.BackupType);
        }

        // GET METHODS
        static public List<BackupModel> ListBackups()
        {
            List<BackupModel> ListOfBackups = new List<BackupModel>();
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                // SQL query
                string sql = "SELECT * FROM saves";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                SQLiteDataReader rdr = command.ExecuteReader();
                // Return list of backups
                var i = 0;
                while (rdr.Read())
                {
                    ListOfBackups.Add(new BackupModel(rdr.GetString(0), rdr.GetString(1), rdr.GetString(2), (BackupModel.BackupTypes)Enum.Parse(typeof(BackupModel.BackupTypes), rdr.GetString(3)), rdr.GetString(4)));
                    i++;
                }
                // Close DB connexion
                m_dbConnection.Close();
            }
            return ListOfBackups;
        }

        static public BackupModel GetBackup()
        {
            BackupModel backupModel = null;
            lock (PauseLock)
            {
                // Get all backups
                List<BackupModel> ListOfBackups = ListBackups();
                DisplayListBackups(ListOfBackups);
                
                while (backupModel == null)
                {
                    // Prompt backup to grab
                    ResourceManager res_mng = new ResourceManager(typeof(Languages.Resource));
                    Console.WriteLine(res_mng.GetString("SelectBackup"));
                    int userInput = Int32.Parse(Console.ReadLine());
                    backupModel = ListOfBackups.FirstOrDefault(o => o.ID == userInput);
                }
                // Return object
            }
            return backupModel;
        }

        // OTHER CRUD METHODS
        static public void DeleteBackup(BackupModel backupModel)
        {
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();
                // SQL query
                string sql = $"DELETE FROM saves WHERE `Name` LIKE '{backupModel.Name}'";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                command.ExecuteNonQuery();
                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
                // Re-create state log file
                LogViewModel.StateLogCreation();
            }
        }

        static public void DeleteAllBackups()
        {
            lock (PauseLock)
            {
                // Open the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();

                // SQL query
                string sql = $"DELETE FROM saves";

                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);

                // SQL command executer
                command.ExecuteNonQuery();

                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
            }

        }

        static public void CreateBackup(BackupModel backupModel)
        {
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();
                // SQL query
                string sql = $"INSERT INTO saves(Name, SourcePath, DestinationPath, BackupType, Status) VALUES('{backupModel.Name}','{backupModel.SourcePath}','{backupModel.DestinationPath}','{backupModel.BackupType}', 'STOPPED')";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                command.ExecuteNonQuery();
                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
            }
        }

        static public void UpdateBackup(BackupModel backupModel, string oldname)
        {
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();
                // SQL query
                string sql = $"UPDATE saves SET `Name` = '{backupModel.Name}', `SourcePath`= '{backupModel.SourcePath}', `DestinationPath`= '{backupModel.DestinationPath}', `BackupType` = '{backupModel.BackupType}' WHERE `Name` LIKE '{oldname}'";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                command.ExecuteNonQuery();
                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
            }
        }

        static public int NumberBackups()
        {
            int NumberOfBackups;
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                // SQL query
                string sql = "SELECT COUNT(ALL) FROM saves";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                NumberOfBackups = int.Parse(command.ExecuteScalar().ToString());
                // Close DB connexion
                m_dbConnection.Close();
                // Returns int with the number of records
            }
            return NumberOfBackups;
        }

        static public string IsAlreadyInserted(string Name)
        {
            lock (PauseLock)
            {
                if (NumberBackups() != 0)
                {
                    List<BackupModel> ListOfBackups = ListBackups();
                    BackupModel TempBackupModel = ListOfBackups.FirstOrDefault(o => o.Name == Name);
                    while (TempBackupModel != null)
                    {
                        Name = Name + "1";
                        TempBackupModel = ListOfBackups.FirstOrDefault(o => o.Name == Name);
                    }
                }
            }
            return Name;
        }

        static public string ReturnStatus(string Name)
        {
            string Status;
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                // SQL query
                string sql = $"SELECT `Status` FROM saves WHERE `Name` LIKE '{Name}'";
                // SQL command maker
                SQLiteCommand commandstatus = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                Status = commandstatus.ExecuteScalar().ToString();
                // Close DB connexion
                m_dbConnection.Close();
            }
            //GC.Collect();
            return Status;
        }

        static public void SetStatus(BackupModel backupModel, string Status)
        {
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();
                // SQL query
                string sql = $"UPDATE saves SET `Status` = '{Status}' WHERE `Name` LIKE '{backupModel.Name}'";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                command.ExecuteNonQuery();
                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
            }
        }

        static public void SetStatusName(string Name, string Status)
        {
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();
                // SQL query
                string sql = $"UPDATE saves SET `Status` = '{Status}' WHERE `Name` LIKE '{Name}'";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                command.ExecuteNonQuery();
                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
            }
        }

        static public bool IsPaused(string Name)
        {
            lock (PauseLock)
            {
                if (ReturnStatus(Name) == "PAUSED") { return true; }
                else { return false; }
            }
        }

        // VERIFICATION METHODS
        static private string SourcePathVerif()
        {

            ResourceManager res_mng = new ResourceManager(typeof(Languages.Resource));
            Console.WriteLine(res_mng.GetString("SourcePath"));
            string path = Console.ReadLine();
            while (!Directory.Exists(path))
            {
                Console.WriteLine(res_mng.GetString("DirectoryNotFound"));
                Console.WriteLine(res_mng.GetString("NewDirectory"));
                path = Console.ReadLine();
            }
            return path;
        }

        static private string DestiantionPathVerif(string SourcePath)
        {
            ResourceManager res_mng = new ResourceManager(typeof(Languages.Resource));
            Console.WriteLine(res_mng.GetString("DestinationPath"));
            string DestinationPath = Console.ReadLine();
            while (SourcePath == DestinationPath)
            {

                Console.WriteLine(res_mng.GetString("SamePath"));
                Console.WriteLine(res_mng.GetString("NewDirectory"));
                DestinationPath = Console.ReadLine();
            }

            return DestinationPath;
        }

        static private BackupModel.BackupTypes BackupTypeVerif()
        {
            int tempval = 0;
            while (tempval != 1 && tempval != 2 && tempval != 3)
            {
                ResourceManager res_mng = new ResourceManager(typeof(Languages.Resource));
                Console.WriteLine(res_mng.GetString("CompleteCopy"));
                Console.WriteLine(res_mng.GetString("DifferentialCopy"));
                Console.WriteLine(res_mng.GetString("SyncFiles"));
                Console.WriteLine(res_mng.GetString("BackupType"));

                tempval = Int32.Parse(Console.ReadLine());
            }
            return (BackupModel.BackupTypes)(tempval - 1);
        }

        /// <summary>
        /// EXTENSIONS METHODS
        /// EVERYTHING RELATED TO EXTENSIONS
        /// </summary>

        static public List<String> ListExtensions(string table)
        {
            List<String> ListOfExtensions = new List<String>();
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                // SQL query
                string sql = $"SELECT * FROM {table}";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                SQLiteDataReader rdr = command.ExecuteReader();
                // Return list of backups
                while (rdr.Read())
                {
                    ListOfExtensions.Add(rdr.GetString(0));
                }
                // Close DB connexion
                m_dbConnection.Close();
            }
            return ListOfExtensions;
        }

        static public void AddExtension(string extension, string table)
        {
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();
                // SQL query
                string sql = $"INSERT INTO {table}(Extension) VALUES('{extension}')";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                command.ExecuteNonQuery();
                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
            }
        }

        static public void DeleteExtension(string extension, string table)
        {
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();
                // SQL query
                string sql = $"DELETE FROM {table} WHERE `Extension` LIKE '{extension}'";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                command.ExecuteNonQuery();
                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
            }
        }

        static public bool VerifyExtension(string extension, string table)
        {
            List<string> ListOfExtension = ListExtensions(table);
            return ListOfExtension.Contains(extension);
        }

        /// <summary>
        /// SOFTWARES METHODS
        /// EVERYTHING RELATED TO SOFTWARES
        /// </summary>
        /// 
        static public List<String> ListSoftwares()
        {
            List<String> ListOfSoftwares = new List<String>();
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                // SQL query
                string sql = "SELECT * FROM softwares";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                SQLiteDataReader rdr = command.ExecuteReader();
                // Return list of backups
                while (rdr.Read())
                {
                    ListOfSoftwares.Add(rdr.GetString(0));
                }
                // Close DB connexion
                m_dbConnection.Close();
            }
            return ListOfSoftwares;
        }

        static public void AddSoftware(string software)
        {
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();
                // SQL query
                string sql = $"INSERT INTO softwares(Software) VALUES('{software}')";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                command.ExecuteNonQuery();
                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
            }
        }

        static public void DeleteSoftware(string software)
        {
            lock (PauseLock)
            {
                // Opens the database
                m_dbConnection.Open();
                var transaction = m_dbConnection.BeginTransaction();
                // SQL query
                string sql = $"DELETE FROM softwares WHERE `Software` LIKE '{software}'";
                // SQL command maker
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                // SQL command executer
                command.ExecuteNonQuery();
                // Close DB connexion
                transaction.Commit();
                m_dbConnection.Close();
            }
        }

        static public bool VerifySoftware(string software)
        {
            List<string> ListOfSoftwares = ListSoftwares();
            return ListOfSoftwares.Contains(software);
        }

    }
}