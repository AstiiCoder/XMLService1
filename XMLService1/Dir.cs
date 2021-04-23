using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLService1
{
    class Dir
    {
        public string _DirAlias { get; set; }
        public string _DirPath { get; set; }
        // Сюда добавить имя процедуры, которая обрабатывает загрузку из этой директории

        public Dir(string DirAlias, string DirPath)
        {
            _DirAlias = DirAlias;
            _DirPath = DirPath;
        }

    }
}
