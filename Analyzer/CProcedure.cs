//
// CProcedure - процедура
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    class CProcedure
    {
        public int    begin_line; // начальная строка (это не физический номер! Это номер строки в строках кода CLine)
        public int    end_line;   // конечная строка  (это не физический номер! Это номер строки в строках кода CLine)
        public string class_name; // принадлежность классу
        public string name;       // имя

        public List<CVariable> Variables; // переменные процедуры

        public List<CAssignment> Assignments; // присвоения переменным

        //
        // Конструктор
        //
        public CProcedure(string class_name, string name, int begin_line, int end_line)
        {
            this.class_name = class_name;
            this.name = name;
            this.begin_line = begin_line;
            this.end_line = end_line;

            Variables = new List<CVariable>();
            Assignments = new List<CAssignment>();
        }

        //
        // Добавить переменную
        //
        public bool AddVariable(int line, int type, int scope, string name)
        {
            if ( FindVariable(name) != null )
                return false; // уже есть такая переменная
            
            Variables.Add(new CVariable(line, type, scope, name));

            return true;
        }

        //
        // Поиск переменной по имени
        //
        public CVariable FindVariable( string name )
        {
            foreach ( var variable in Variables )
                if ( String.Compare(variable.name, name, true) == 0 )
                    return variable;
            return null;
        }
         
        //
        // Добавить присвоение
        //
        public void AddAssignment(int lineNum, string name)
        {
            // игнорируем присвоения внутри with/endwith
            if ( name.StartsWith(".") || name.StartsWith("->") )
                return;
            
            // От массива берем только имя
            int offs = name.IndexOf('[');
            if ( offs < 0 )
                offs = name.IndexOf('(');
            if ( offs > 0 )
                name = name.Substring(0, offs);
            
            Assignments.Add(new CAssignment(lineNum, name));
        }

        //
        // Есть ли присвоение у переменной
        //
        public bool IsVariableAssigned( CVariable var )
        {
            foreach ( var assignment in Assignments )
                if ( String.Compare(assignment.name, var.name, true) == 0 )
                    return true;
            return false;
        }
    }
}
