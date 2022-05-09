using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Projet_ProgSys.Model
{
    public class BackupModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public BackupTypes BackupType { get; set; }

        public string Status { get; set; }

        public BackupModel()
        {
        }

        public BackupModel(string v1, string v2, string v3, BackupTypes v4, string v5)
        {
            this.Name = v1;
            this.SourcePath = v2;
            this.DestinationPath = v3;
            this.BackupType = v4;
            this.Status = v5;
        }

        public enum BackupTypes
        {
            COMPLETE,
            DIFFERENTIAL,
            SYNC,
        }
    }
}