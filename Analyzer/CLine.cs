//
// CLine - строка кода
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    class CLine
    {
        public int number;      // Номер
        public string content;  // Содержимое

        //
        // Конструктор
        //
        public CLine(int number, string content)
        {
            this.number = number; // физический номер в файле, начиная с единицы
            this.content = content;
        }
    }
}
