using System;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using Projet_ProgSys_Graphical;
using Projet_ProgSys.Model;
using Projet_ProgSys.Languages;
using System.Resources;

namespace Projet_ProgSys
{
    internal class Program
    {
        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");
        [STAThread]
        static void Main(string[] args)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                if (!System.IO.File.Exists("EasySaveDB.sqlite"))
                {
                    SQLiteConnection.CreateFile("EasySaveDB.sqlite");
                    SQLiteConnection m_dbConnection;
                    m_dbConnection = new SQLiteConnection("Data Source=EasySaveDB.sqlite;Version=3;");
                    m_dbConnection.Open();

                    // Saves table
                    string sql = "CREATE TABLE saves (`Name` VARCHAR(30) PRIMARY KEY, `SourcePath` VARCHAR(255) NOT NULL, `DestinationPath` VARCHAR(255) NOT NULL, `BackupType` VARCHAR(20) NOT NULL, `Status` VARCHAR(10))";
                    SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();

                    // Extensions table
                    sql = "CREATE TABLE extensions (`Extension` VARCHAR(30) PRIMARY KEY)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();

                    // Extensions table
                    sql = "CREATE TABLE hpextensions (`Extension` VARCHAR(30) PRIMARY KEY)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();

                    // Softwares table
                    sql = "CREATE TABLE softwares (`Software` VARCHAR(30) PRIMARY KEY)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    m_dbConnection.Close();
                }

                if (!System.IO.File.Exists("Settings.ini"))
                {
                    var MyIni = new IniFile("Settings.ini");
                    MyIni.Write("DefaultSize", "100");
                    Random random = new Random();
                    int length = 128;
                    var key = "";
                    for (var i = 0; i < length; i++)
                    {
                        key += ((char)(random.Next(1, 26) + 64)).ToString().ToLower();
                    }
                    MyIni.Write("EncryptionKey", key);
                }

                    if (!System.IO.File.Exists("xml") && !System.IO.File.Exists("json"))
                {
                    var myFile = File.Create("json");
                    myFile.Close();
                    ViewModel.LogViewModel.StateLogCreation();
                }

                Thread t = new Thread(StartMainWindows);
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                mutex.ReleaseMutex();
            }
            else
            {
                ResourceManager res_mng = new ResourceManager(typeof(Resource));
                System.Windows.MessageBox.Show(res_mng.GetString("ERR"));
            }
        }

        public static void StartMainWindows()
        {
            MainWindow start = new MainWindow();
            start.ShowDialog();
        }
    }
} 
