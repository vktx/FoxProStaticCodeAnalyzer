//
// CLog - протокол
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Analyzer
{
    class CLog
    {
        static StreamWriter streamWriter;
        public static int   count;

        //
        // Открытие протокола
        //
        public static void Open( string fileName, bool append = false )
        {
            streamWriter = new StreamWriter(fileName, append);
        }

        //
        // Вывод строки
        //
        public static void Print( string format, params object[] args )
        {
            if ( streamWriter == null )
                return;

            streamWriter.WriteLine(format, args);
            Console.WriteLine(format, args);

            count++;
        }

        //
        // Закрытие
        //
        public static void Close( )
        {
            if ( streamWriter != null )
                streamWriter.Dispose();
        }
    }
}
