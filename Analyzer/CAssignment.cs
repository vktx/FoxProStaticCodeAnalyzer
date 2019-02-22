//
// Присвоение переменной
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    class CAssignment
    {
        public int    line;  // Номер строки
        public string name;  // Переменная

        //
        // Конструктор
        //
        public CAssignment(int line, string name)
        {
            // TODO: перенести этот код отсюда в CProcedure.AddAssignment

            // избавляемся от m.
            if ( name.StartsWith("m.", StringComparison.CurrentCultureIgnoreCase) )
                name = name.Substring(2);
            if ( name.StartsWith("m->", StringComparison.CurrentCultureIgnoreCase ) )
                name = name.Substring(3);

            // избавляемся от &
            if ( name.StartsWith("&") )
                name = name.Substring(1);

            // TODO: тут наверно надо провериться на точку в конце &var.

            this.line = line; // физический номер в файле, начиная с единицы
            this.name = name;
        }
    }
}
