using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotateBackupSetting
{
    class BackupSetting
    {
        public BsonValue _id { get; set; }
//        public string recordId { get; set; }
        public bool disable { get; set; }
        public bool keepMain { get; set; }
        public bool keepFirstDir { get; set; }
        public string Command { get; set; }
        public string Remark { get; set; }
        public bool isDirectory { get; set; }
        public int maxPath { get; set; }
        public string mainPath { get; set; }
        public string Path1 { get; set; }
        public string Path2 { get; set; }
        public string Path3 { get; set; }
        public string Path4 { get; set; }
        public string Path5 { get; set; }
        public string Path6 { get; set; }
        public string Path7 { get; set; }
        public string Path8 { get; set; }
        public string Path9 { get; set; }
        public string Path10 { get; set; }
        public string Path11 { get; set; }
        public string Path12 { get; set; }
        public string Path13 { get; set; }
        public string Path14 { get; set; }
        public string preCommand { get; set; }
        public string postCommand { get; set; }

        public bool keepPath1 { get; set; }
        public bool keepPath2 { get; set; }
        public bool keepPath3 { get; set; }
        public bool keepPath4 { get; set; }
        public bool keepPath5 { get; set; }
        public bool keepPath6 { get; set; }
        public bool keepPath7 { get; set; }
        public bool keepPath8 { get; set; }
        public bool keepPath9 { get; set; }
        public bool keepPath10 { get; set; }

        public bool keepPath11 { get; set; }
        public bool keepPath12 { get; set; }
        public bool keepPath13 { get; set; }
        public bool keepPath14 { get; set; }

    }
}
