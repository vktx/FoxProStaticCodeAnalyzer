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
            if ( name.StartsWith("m.", StringComparison.OrdinalIgnoreCase) )
                name = name.Substring(2);
            if ( name.StartsWith("m->", StringComparison.OrdinalIgnoreCase) )
                name = name.Substring(3);

            // обработка макроподстановки
            if (name.StartsWith("&"))
            {
                // избавляемся от & в начале
                name = name.Substring(1);
                // и от . в конце
                int dot = name.IndexOf('.');
                if (dot > 0)
                    name = name.Substring(0, dot);
            }

            this.line = line; // физический номер в файле, начиная с единицы
            this.name = name;
        }
    }
}
