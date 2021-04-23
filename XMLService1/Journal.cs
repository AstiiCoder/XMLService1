using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLService1
{
    class Journal
    {
        public string _MsgText { get; set; }
        public DateTime _MsgDateTime { get; set; }
        public byte _TypeRow { get; set; }

        public Journal(DateTime MsgDate, string MsgText)
        {
            _MsgText = MsgText;
            _MsgDateTime = MsgDate;
            if (MsgText.Contains("Ошибка")) _TypeRow = 1;
            else _TypeRow = 0;
        }

        public string _DateTimeStr
        {
            get
            {
                return _MsgDateTime.ToString("dd.MM.yyyy hh:mm:ss");
            }
        }
    }
}
