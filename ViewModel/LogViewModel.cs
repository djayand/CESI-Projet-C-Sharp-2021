using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using Projet_ProgSys.Model;
using System.Text.Json;
using System.Data.SQLite;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using Projet_ProgSys.View;
using System.Resources;

namespace Projet_ProgSys.ViewModel
{
    public class LogViewModel
    {
        public LogViewModel()
        {

        }

        // JSON
        static public void StateLogCreation()
        {
            string LogType = LogTypeFinder();
            List<BackupModel> ListOfBackups = DatabaseViewModel.ListBackups();
            List<StateLogModel> ListOfStates = new List<StateLogModel>();
            foreach (var backup in ListOfBackups)
            {
                ListOfStates.Add(new StateLogModel(backup.Name, "", "", "FINISHED", 0, 0, 0, 0));
            }
            if (LogType == "json")
            {
                string fileName = "StateLog.json";
                string jsonString = JsonSerializer.Serialize(ListOfStates, new JsonSerializerOptions { WriteIndented = true });
                using (TextWriter writer = new StreamWriter(fileName))
                {
                    writer.Write(jsonString);
                }
            }
            else
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<StateLogModel>));

                string fileName = "StateLog.xml";
                using (TextWriter writer = new StreamWriter(fileName))
                {
                    serializer.Serialize(writer, ListOfStates);
                }
            }
            
        }

        static public List<StateLogModel> ListState()
        {
            string LogType = LogTypeFinder();
            List<StateLogModel> ListOfStates = new List<StateLogModel>();
            if (LogType == "json")
            {
                string fileName = "StateLog.json";
                string jsonString = File.ReadAllText(fileName);
                ListOfStates = JsonSerializer.Deserialize<List<StateLogModel>>(jsonString);
            }
            else
            {
                string fileName = "StateLog.xml";
                XmlSerializer serializer = new XmlSerializer(typeof(List<StateLogModel>));
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    ListOfStates = (List<StateLogModel>)serializer.Deserialize(fs);
                }
            }
            return ListOfStates;
        }

        static public void StateLogUpdate(StateLogModel LogInfos)
        {
            List<BackupModel> ListOfBackups = DatabaseViewModel.ListBackups();
            List<StateLogModel> ListOfStates = new List<StateLogModel>();
            string LogType = LogTypeFinder();

            if (LogType == "json")
            {
                string fileName = "StateLog.json";
                string jsonString = File.ReadAllText(fileName);
                ListOfStates = JsonSerializer.Deserialize<List<StateLogModel>>(jsonString);

                var index = ListOfStates.FindIndex(o => o.Name == LogInfos.Name);
                ListOfStates[index].SourceFilePath = LogInfos.SourceFilePath;
                ListOfStates[index].TargetFilePath = LogInfos.TargetFilePath;
                ListOfStates[index].State = LogInfos.State;
                ListOfStates[index].TotalFilesToCopy = LogInfos.TotalFilesToCopy;
                ListOfStates[index].TotalFilesSize = LogInfos.TotalFilesSize;
                ListOfStates[index].NbFilesLeftToDo = LogInfos.NbFilesLeftToDo;
                ListOfStates[index].TotalFilesSizeRemaining = LogInfos.TotalFilesSizeRemaining;
                jsonString = JsonSerializer.Serialize(ListOfStates, new JsonSerializerOptions { WriteIndented = true });
                using(TextWriter writer = new StreamWriter(fileName))
                {
                    writer.Write(jsonString);
                }
            }
            else
            {
                // Get file
                string fileName = "StateLog.xml";
                XmlSerializer serializer = new XmlSerializer(typeof(List<StateLogModel>));
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    ListOfStates = (List<StateLogModel>)serializer.Deserialize(fs);
                }

                // Update fields
                var index = ListOfStates.FindIndex(o => o.Name == LogInfos.Name);
                ListOfStates[index].SourceFilePath = LogInfos.SourceFilePath;
                ListOfStates[index].TargetFilePath = LogInfos.TargetFilePath;
                ListOfStates[index].State = LogInfos.State;
                ListOfStates[index].TotalFilesToCopy = LogInfos.TotalFilesToCopy;
                ListOfStates[index].TotalFilesSize = LogInfos.TotalFilesSize;
                ListOfStates[index].NbFilesLeftToDo = LogInfos.NbFilesLeftToDo;
                ListOfStates[index].TotalFilesSizeRemaining = LogInfos.TotalFilesSizeRemaining;
                //  Write updates to file
                using (TextWriter writer = new StreamWriter(fileName))
                {
                    serializer.Serialize(writer, ListOfStates);
                }


            }
                
            
        }

        static public void FileLogUpdate(FileLogModel LogInfos)
        {

            string LogType = LogTypeFinder();

            string dateForLog = LogDate();
            string fileName = "FileLog" + dateForLog + "." + LogType;
            if (!File.Exists(fileName))
            {
                File.WriteAllText(fileName, "");
            }

            if (LogType == "json")
            {

                var newText = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(LogInfos, new JsonSerializerOptions { WriteIndented = true }));

                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite))
                {
                    if (fs.Length >= 1)
                    {
                        fs.Seek(0, SeekOrigin.End);
                    }
                    fs.Write(newText, 0, newText.Length); 
                    string stringtest = "\r\n";
                    fs.Write(Encoding.UTF8.GetBytes(stringtest), 0, 1);
                    fs.SetLength(fs.Position); 
                }
            }
            else
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<FileLogModel>));
                List<FileLogModel> ListOfFiles = new List<FileLogModel>();
                ListOfFiles.Add(LogInfos);
                string newText = "";

                if (!File.Exists(fileName))
                {
                    File.WriteAllText(fileName, "");
                    TextWriter writer = new StreamWriter(fileName);
                    serializer.Serialize(writer, ListOfFiles);
                    writer.Close();
                }
                else
                {
                    using (StringWriter textWriter = new StringWriter())
                    {
                        serializer.Serialize(textWriter, ListOfFiles);
                        newText = textWriter.ToString();

                        string[] lines = newText
                            .Split("\r\n")
                            .Skip(2)
                            .ToArray();

                        newText = string.Join("\r\n", lines);

                    }

                    var encodedText = Encoding.UTF8.GetBytes(newText);

                    using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite))
                    {
                        fs.Seek(-22, SeekOrigin.End);
                        fs.Write(encodedText, 0, encodedText.Length);
                    }
                }
            }
                
            
        }


        static public string LogDate()
        {
            string LDate = DateTime.Now.ToString("ddMMyyyy");
            string LogsDate = LDate;
            return LogsDate;
        }

        static public string LogTypeFinder()
        {
            if (System.IO.File.Exists("json"))
            {
                return "json";
            } 
            else
            {
                return "xml";
            }
        }

        static public void ChangeLogType(string LogType)
        {
            if (System.IO.File.Exists("xml"))
            {
                File.Delete("xml");
                File.Delete("StateLog.xml");
            }

            if (System.IO.File.Exists("json"))
            {
                File.Delete("json");
                File.Delete("StateLog.json");
            }

            var myFile = File.Create(LogType);
            myFile.Close();
            SQLiteConnection m_dbConnection;
            m_dbConnection = new SQLiteConnection("Data Source=EasySaveDB.sqlite;Version=3;");
            StateLogCreation();
        }

        static public void ChangeLogTypeMenu()
        {
            ResourceManager res_mng = new ResourceManager(typeof(Languages.Resource));
            Console.WriteLine(res_mng.GetString("LogFormat"));
            Console.WriteLine("1. JSON");
            Console.WriteLine("2. XML");

            int userInput = int.Parse(Console.ReadLine());

            while (userInput != 1 && userInput != 2)
            {
                Console.WriteLine(res_mng.GetString("LogFormat"));
                userInput = int.Parse(Console.ReadLine());
            }
            switch (userInput)
            {
                case 1:
                    Console.Clear();
                    ChangeLogType("json");
                    break;
                case 2:
                    Console.Clear();
                    ChangeLogType("xml");
                    break;
                default:
                    break;
            }
        }
    }
}
