using System;
using System.Collections.Generic;
using System.Text;

namespace Projet_ProgSys.Model
{
    public class FileModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long FileSize { get; set; }

        public FileModel(string v1, string v2, long v3)
        {
            this.Name = v1;
            this.Path = v2;
            this.FileSize = v3;
        }

        
    }
}
