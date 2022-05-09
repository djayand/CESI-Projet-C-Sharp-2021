using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Projet_ProgSys.Model
{
    public class FileLogModel
    {
        public string Path { get; set; }
        public string FileSource { get; set; }
        public string FileTarget { get; set; }
        public string FileTransferTime { get; set; }
        public long FileSize { get; set; }
        public string Timestamp { get; set; }
        public int EncryptionTime { get; set; }

        public FileLogModel(string p, string fs, string ft, string ftt, long fsz, string t, int v4)
        {
            this.Path = p;
            this.FileSource = fs;
            this.FileTarget = ft;
            this.FileTransferTime = ftt;
            this.FileSize = fsz;
            this.Timestamp = t;
            this.EncryptionTime = v4;
        }

        public FileLogModel()
        {
        }
    }

    [XmlType(TypeName = "Backup")]
    public class StateLogModel
    {
        public string Name { get; set; }
        public string SourceFilePath { get; set; }
        public string TargetFilePath { get; set; }
        public string State { get; set; }
        public int TotalFilesToCopy { get; set; }
        public long TotalFilesSize { get; set; }
        [XmlElement("FilesRemaining")]
        public int NbFilesLeftToDo { get; set; }
        [XmlElement("SizeRemaining")]
        public long TotalFilesSizeRemaining { get; set; }

        public StateLogModel(string name, string v1, string v2, string states, int v3, long v4, int v5, long v6)
        {
            this.Name = name;
            this.SourceFilePath = v1;
            this.TargetFilePath = v2;
            this.State = states;
            this.TotalFilesToCopy = v3;
            this.TotalFilesSize = v4;
            this.NbFilesLeftToDo = v5;
            this.TotalFilesSizeRemaining = v6;
        }

        public StateLogModel()
        {
        }
        
    }

    public class StateList
    {
        public List<StateLogModel> StateLogModel { get; set; }
    }

    public class LogFiles
    {
        public LogFiles(StateLogModel logStatesTemp, FileLogModel logFilesTemp)
        {
            LogStatesTemp = logStatesTemp;
            LogFilesTemp = logFilesTemp;
        }

        public StateLogModel LogStatesTemp { get; set; }
        public FileLogModel LogFilesTemp { get; set; }
    }
}
