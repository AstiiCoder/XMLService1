using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLService1
{
    class XMLFile
    {
        public string _FileName { get; set; }
        public DateTime _FileDate { get; set; }
        public int _FileStatus { get; set; }
        public static List<string> FileList = new List<string>();

        public XMLFile(string FileName, DateTime FileDate, int FileStatus)
            {
                _FileName = FileName;
                _FileDate = FileDate;
                _FileStatus = FileStatus;
                FileList.Add(this._FileName);
            }

        public string _DateTimeStr
        {
            get
            {
                return _FileDate.ToString("dd.MM.yyyy hh:mm:ss");
            }
        }

        public static bool IsAlreadyExist(string FileName)
        {
            bool IfExist = false;
            foreach (string item in FileList)
            {
                if (item == FileName)
                {
                    IfExist = true;
                    break;
                }                          
            }
            return IfExist;    
        }

    }
}
