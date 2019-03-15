//
// CProgram - PRG
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Analyzer
{
    class CProgram
    {
        string fileName;
        
        // строки кода
        public List<CLine> Lines;
        public int totalLines;
        public int codeLines;

        // процедуры
        public List<CProcedure> Procedures;

        public bool checkIncorrectVarNames  = true;  //false;
        public bool checkUndefinedVars      = true;  //false;
        public bool checkExceptionStatement = true;  //false;
        public bool checkUnreachableCode    = true;  //false;

        //
        // Конструктор
        //
        public CProgram(string fileName)
        {
            this.fileName = fileName;

            Lines      = new List<CLine>();
            Procedures = new List<CProcedure>();
        }

        //
        // Разбор
        //
        public void Parse()
        {
            ReadLines();
            FindProcedures();
            FindVariables();
            FindAssignments();
        }

        //
        // Проверка корректности имени
        //
        static bool CheckName( string name )
        {
            if ( Regex.Match(name, @"\p{IsCyrillic}|\p{IsCyrillicSupplement}").Success )
                return false;

            return true;
        }

        //
        // Чтение строк кода
        //
        void ReadLines( )
        {
            using ( var fs = new StreamReader(fileName, Encoding.GetEncoding(1251)) )
            {
                string fullLine = "";
                int lineNumber = 0;

                while ( true )
                {
                    string line = fs.ReadLine(); // Читаем строку
                    if ( line == null )
                        break;

                    totalLines++;

                    lineNumber++;

                    line = line.Trim(); // Убираем пробелы

                    int commentPos = line.IndexOf("&&");  // Убираем все что после &&
                    if ( commentPos >= 0 )
                    {
                        line = line.Substring(0, commentPos);
                        line = line.Trim();
                    }

                    // TODO: убрать
                    ///////if ( String.IsNullOrWhiteSpace(line) ) // Пропускаем пустые строки
                    ///////    continue;

                    // Склеиваем перенесенные строки
                    fullLine += line;
                    fullLine = fullLine.TrimEnd(';');

                    if ( String.IsNullOrWhiteSpace(fullLine) ) // Пропускаем пустые строки
                        continue;

                    // если это конечная строка 
                    if ( !line.EndsWith(";") )
                    {
                        // и не комментарий
                        if ( !(fullLine.StartsWith("*") || fullLine.StartsWith("&&") || fullLine.StartsWith("NOTE", StringComparison.CurrentCultureIgnoreCase)) )
                            Lines.Add(new CLine(lineNumber, fullLine)); // добавляем в список строк кода

                        fullLine = "";
                        codeLines++;
                    }
                }
            }
        }

        //
        // Поиск процедур (разбивка содержимого на процедуры)
        //
        // ПРИМЕЧАНИЕ: FUNCTION/PROCEDURE - ENDFUNC/ENDPROC включаются в содержимое процедуры
        //
        void FindProcedures()
        {
            CProcedure proc = null;
            string     class_name = "";

            for ( int i = 0; i < Lines.Count; i++ )
            {
                CLine line = Lines[i];

                // TODO: тут же в этом месте надо разобрать параметры переданные не через PARAMETES/LPARAMETERS, а как func( param1, param2, ... )
                string[] words = line.content.Split(new[] { ',', ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                if ( words.Length > 1 )
                {
                    if ( words.Length > 2 )
                        if ( String.Compare(words[0], "define", true) == 0 && String.Compare(words[1], "class", true) == 0 )
                            class_name = words[2];

                    if ( String.Compare(words[0], "enddefine", true) == 0 )
                        class_name = "";


                    int procWord = (String.Compare(words[0], "hidden", true) == 0 || String.Compare(words[0], "protected", true) == 0) ? 1 : 0;
                    if ( procWord == 1 && words.Length < 3 )
                        continue;

                    // procedure/function
                    if ( String.Compare(words[procWord], "procedure", true) == 0 || String.Compare(words[procWord], "function", true) == 0 )
                    {
                        if ( proc != null ) // значит не обнаружен endproc/endfunc
                        {
                            proc.end_line = i;
                            CLog.Print("{0}: {1}: Отсутствует ENDPROC/ENDFUNC в предшествующей процедуре", fileName, line.number);
                        }

                        proc = new CProcedure(class_name, words[procWord + 1], i, -1);  // конечную строку еще не знаем
                        Procedures.Add(proc);
                    }
                }

                if ( words.Length > 0 )
                {
                    // endproc/endfunc
                    if ( String.Compare(words[0], "endproc", true) == 0 || String.Compare(words[0], "endfunc", true) == 0 )
                    {
                        if ( proc != null )
                        {
                            proc.end_line = i; // конечная строка
                            proc = null;
                        }
                    }
                }
            }

            // Еслм нет описания ни одной процедуры, считаем что файл и есть процедура
            if ( Lines.Count > 0 && Procedures.Count == 0 )
                Procedures.Add(new CProcedure("", Path.GetFileNameWithoutExtension(fileName), 0, Lines.Count - 1));
        }

        //
        // Поиск переменных в процедурах
        //
        void FindVariables()
        {
            foreach ( var proc in Procedures )
            {
                for ( int i = proc.begin_line; i <= proc.end_line; i++ )
                {
                    string line = Lines[i].content;

                    int type = 0;
                    int scope = 0;

                    bool RedefineCheck = false;

                    bool found = false;
                    if ( line.StartsWith("local ", StringComparison.CurrentCultureIgnoreCase) )
                    {
                        type = CVariable.VARIABLE;
                        scope = CVariable.LOCAL;
                        RedefineCheck = true;
                        found = true;
                    }

                    if ( line.StartsWith("private ", StringComparison.CurrentCultureIgnoreCase) )
                    {
                        type = CVariable.VARIABLE;
                        scope = CVariable.PRIVATE;
                        RedefineCheck = true;
                        found = true;
                    }

                    if ( line.StartsWith("public ", StringComparison.CurrentCultureIgnoreCase) )
                    {
                        type = CVariable.VARIABLE;
                        scope = CVariable.GLOBAL;
                        RedefineCheck = true;
                        found = true;
                    }

                    if ( line.StartsWith("lparameter ", StringComparison.CurrentCultureIgnoreCase) || line.StartsWith("lparameters ", StringComparison.CurrentCultureIgnoreCase) )
                    {
                        type = CVariable.PARAMETER;
                        scope = CVariable.LOCAL;
                        found = true;
                    }

                    if ( line.StartsWith("parameter", StringComparison.CurrentCultureIgnoreCase) || line.StartsWith("parameters", StringComparison.CurrentCultureIgnoreCase) )
                    {
                        type = CVariable.PARAMETER;
                        scope = CVariable.PRIVATE;
                        found = true;
                    }

                    if ( line.StartsWith("dimension ", StringComparison.CurrentCultureIgnoreCase) )
                    {
                        type = CVariable.VARIABLE;
                        scope = CVariable.PRIVATE;
                        found = true;
                    }

                    if ( line.StartsWith("declare ", StringComparison.CurrentCultureIgnoreCase) )
                    {
                        // пропускаем DECLARE DLL
                        if ( line.IndexOf(" in ", StringComparison.CurrentCultureIgnoreCase) < 0 )
                        {
                            type = CVariable.VARIABLE;
                            scope = CVariable.PRIVATE;
                            found = true;
                        }
                    }

                    if ( found )
                    {
                        string s = "";
                        bool ignore = false;

                        line = line.Substring(line.IndexOf(' '));

                        // если описан тип, отбрасываем
                        int as_word = line.IndexOf(" as ");
                        if ( as_word > 0 )
                            line = line.Substring(0, as_word);

                        // игнорируем размерности массивов
                        for ( int j = 0; j < line.Length; j++ )
                        {
                            if ( line[j] == '[' || line[j] == '(' )
                                ignore = true;

                            if ( line[j] == ']' || line[j] == ')' )
                            {
                                ignore = false;
                                continue;
                            }

                            if ( !ignore )
                                s += line[j];
                        }
                  
                        string [] words = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for ( int j = 0; j < words.Length; j++ )
                        {
                            if ( checkIncorrectVarNames && !CheckName(words[j]) )
                                CLog.Print("{0}: {1}: Недопустимое имя переменной при объявлении \"{2}\"", fileName, Lines[i].number, words[j]);
                            
                            if ( !proc.AddVariable(Lines[i].number, type, scope, words[j]) )
                            {
                                if ( checkIncorrectVarNames && RedefineCheck )
                                    CLog.Print("{0}: {1}: Повторное объявление переменной \"{2}\"", fileName, Lines[i].number, words[j]);
                            }
                        }
                    }
                }
            }
        }

        //
        // Поиск присвоения значений переменным в процедурах
        //
        public void FindAssignments()
        {
            foreach ( var proc in Procedures )
            {
                for ( int i = proc.begin_line; i <= proc.end_line; i++ )
                {
                    CLine line = Lines[i];

                    // VAR = ...
                    int pos = line.content.IndexOf('=');
                    if ( pos > 0 )
                    {
                        string str = line.content.Substring(0, pos);

                        if ( str.StartsWith("for ", StringComparison.CurrentCultureIgnoreCase) )
                            str = str.Substring(4).Trim();

                        string[] columns = str.Split(new[] { '=', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ( columns.Length == 1 )
                        {
                            str = str.Trim();

                            if ( checkIncorrectVarNames && !CheckName(str) )
                                CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, str);

                            proc.AddAssignment(line.number, str);
                        }
                    }
                    // STORE ... TO ...
                    else if ( line.content.StartsWith("store ", StringComparison.CurrentCultureIgnoreCase) )
                    {
                        string str = line.content.Substring(6);
                        int pos2 = str.IndexOf(" to ", StringComparison.CurrentCultureIgnoreCase);
                        if ( pos2 >= 0 )
                        {
                            str = str.Substring(pos2 + 4).Trim();
                            string[] columns = str.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            for ( int j = 0; j < columns.Length; j++ )
                            {
                                if ( checkIncorrectVarNames && !CheckName(columns[j]) )
                                    CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, columns[j]);

                                proc.AddAssignment(line.number, columns[j]);
                            }
                        }
                    }
                    // FOR EACH ...
                    else if ( line.content.StartsWith("for each ", StringComparison.OrdinalIgnoreCase) )
                    {
                        string [] words = line.content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ( words.Length > 2 )
                        {
                            if ( checkIncorrectVarNames && !CheckName(words[2]) )
                                CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, words[2]);
                            proc.AddAssignment(line.number, words[2]);
                        }
                    }
                    else
                    {
                        // Прочие случаи ...
                        string[] words = line.content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        int len = words.Length;

                        // SELECT * FROM XXX INTO ARRAY YYY
                        if ( len > 6 ) 
                        {
                            if ( String.Compare(words[0], "select", true) == 0 )
                            {
                                if ( String.Compare(words[len - 3], "into", true) == 0 )
                                {
                                    if ( String.Compare(words[len - 2], "array", true) == 0 )
                                    {
                                        if ( checkIncorrectVarNames && !CheckName(words[len - 1]) )
                                            CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, words[len - 1]);

                                        proc.AddAssignment(line.number, words[len - 1]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // 
        // Недопустимые инструкции при блоке обработки исключений
        //
        public void CheckExceptionStatement()
        {
            if ( !checkExceptionStatement )
                return;

            bool tryStart = false;
            foreach ( var line in Lines )
            {
                if ( String.Compare(line.content, "try", true) == 0 )
                    tryStart = true;

                if ( String.Compare(line.content, "endtry", true) == 0 )
                    tryStart = false;

                if ( tryStart )
                {
                    if ( line.content.StartsWith("return", StringComparison.CurrentCultureIgnoreCase) )
                        CLog.Print("{0}: {1}: Недопустимое использование RETURN", fileName, line.number);

                    if ( line.content.StartsWith("retry", StringComparison.CurrentCultureIgnoreCase) )
                        CLog.Print("{0}: {1}: Недопустимое использование RETRY", fileName, line.number);
                }
            }
        }

        //
        // Необъявленные переменные
        //
        public void CheckUndefinedVariables( )
        {
            if ( !checkUndefinedVars )
                return;

            foreach ( var proc in Procedures )
            {
                foreach ( var assignment in proc.Assignments )
                {
                    string name = assignment.name;
                    
                    // Пропускаем присвоения свойствам текущего класса
                    if ( name.StartsWith("this.", StringComparison.CurrentCultureIgnoreCase) || name.StartsWith("this->", StringComparison.CurrentCultureIgnoreCase) )
                        continue;

                    // Пропускаем присвоения свойствам текущей формы
                    if ( name.StartsWith("thisform.", StringComparison.CurrentCultureIgnoreCase) || name.StartsWith("thisform->", StringComparison.CurrentCultureIgnoreCase) )
                        continue;

                    // Для свойcтв объекта берем имя
                    int dotPos = name.IndexOf('.');
                    if ( dotPos < 0 )
                        dotPos = name.IndexOf("->");

                    if ( dotPos > 0 )
                        name = name.Substring(0, dotPos);

                    // TODO: убрать
                    //if ( proc.FindVariable(name) == null )
                    //    CLog.Print("{0}: {1}: Необъявленная переменная \"{2}\"", fileName, assignment.line, name);

                    CVariable var = proc.FindVariable(name);
                    if ( var == null || assignment.line < var.line ) // если переменная не описана или описана после использования
                        CLog.Print("{0}: {1}: Необъявленная переменная \"{2}\"", fileName, assignment.line, name);
                }
            }
        }

        //
        // Недостижимый код
        //
        public void CheckUnreachableCode()
        {
            if ( !checkUnreachableCode )
                return;

            foreach ( var proc in Procedures )
            {
                int cond_count = 0;
                for ( int i = proc.begin_line; i <= proc.end_line; i++ )
                {
                    CLine line = Lines[i];

                    if ( line.content.StartsWith("if ", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("endif", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("for ", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("endfor", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("do while ", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("enddo", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("do case", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("endcase", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("scan", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("endscan", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    //
                    // ПРИМЕЧАНИЕ: 
                    // В данном месте наличие RETURN меджу TRY/ENDTRY не считаем ошибкой
                    // Ошибка на недопустимость использования RETURN проверяется в CheckTryEndtry
                    //
                    if ( line.content.StartsWith("try", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("endtry", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("return", StringComparison.OrdinalIgnoreCase) )
                    {
                        if ( cond_count == 0 && i < proc.end_line )
                        {
                            if ( !(Lines[i + 1].content.StartsWith("endfunc", StringComparison.OrdinalIgnoreCase)
                                || Lines[i + 1].content.StartsWith("endproc", StringComparison.OrdinalIgnoreCase)
                                || Lines[i + 1].content.StartsWith("endwith", StringComparison.OrdinalIgnoreCase)) )
                            {
                                CLog.Print("{0}: {1}: Недостижимый код", fileName, Lines[i + 1].number);
                            }
                        }
                    }             
                }
            }
        }
    }
}
