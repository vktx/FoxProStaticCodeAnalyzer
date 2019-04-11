//
// CVariable - переменная
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    class CVariable
    {
        // Тип
        public static readonly int PARAMETER = 1;
        public static readonly int VARIABLE  = 2;
      
        // Видимость
        public static readonly int GLOBAL  = 1;
        public static readonly int PRIVATE = 2;
        public static readonly int LOCAL   = 3;

        public int    line;   // номер строки
        public int    type;   // тип (параметр/переменная)
        public int    scope;  // видимость (глобальная/приватная/локальная)
        public string name;   // имя

        //
        // Конструктор
        //
        public CVariable( int line, int type, int scope, string name )
        {
            this.line = line;
            this.type = type;
            this.scope = scope;
            this.name = name;
        }
    }
}
