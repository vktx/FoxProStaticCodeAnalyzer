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

        public bool checkIncorrectVarNames  = true;
        public bool checkUndefinedVars      = true;
        public bool checkExceptionStatement = true;
        public bool checkUnreachableCode    = true;
        public bool checkUnusedVariables    = true;

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
        //
        //
        static bool StartWith( string s, params string[] words )
        {
            foreach (string w in words)
                if (s.StartsWith(w, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        //
        //
        //
        static int IndexOf(string s, params string[] words)
        {
            foreach (string w in words)
            {
                int i = s.IndexOf(w, StringComparison.OrdinalIgnoreCase);
                if ( i >= 0 )
                    return i;
            }
            return -1;
        }

        //
        //
        //
        static string FormatVarName( string name )
        {
            string s = "";
            bool ignore = false;

            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '[' || name[i] == '(')
                    ignore = true;

                if (name[i] == ']' || name[i] == ')')
                {
                    ignore = false;
                    continue;
                }

                if (!ignore)
                    s += name[i];
            }

            return s;
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

                    // Склеиваем перенесенные строки
                    fullLine += line;
                    fullLine = fullLine.TrimEnd(';');

                    // Добавим пробел для разделения перенесенных строк
                    if ( line.EndsWith(";") )
                        fullLine += " ";

                    if ( String.IsNullOrWhiteSpace(fullLine) ) // Пропускаем пустые строки
                        continue;

                    // Если это конечная строка (не содержит ';' в конце)
                    if ( !line.EndsWith(";") )
                    {
                        // и не комментарий
                        if ( !(fullLine.StartsWith("*") || fullLine.StartsWith("&&") || fullLine.StartsWith("NOTE", StringComparison.OrdinalIgnoreCase)) )
                            Lines.Add(new CLine(lineNumber, fullLine)); // добавляем в список строк кода

                        fullLine = "";
                        codeLines++;
                    }
                }
            }

            /*
            // DEBUG
            for (int i = 0; i < Lines.Count; i++)
                CLog.Print(Lines[i].content);
            */
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
                        if ( String.Compare(words[0], "DEFINE", true) == 0 && String.Compare(words[1], "class", true) == 0 )
                            class_name = words[2];

                    if ( String.Compare(words[0], "ENDDEFINE", true) == 0 )
                        class_name = "";


                    int procWord = (String.Compare(words[0], "HIDDEN", true) == 0 || String.Compare(words[0], "PROTECTED", true) == 0) ? 1 : 0;
                    if ( procWord == 1 && words.Length < 3 )
                        continue;

                    // procedure/function
                    if ( String.Compare(words[procWord], "PROCEDURE", true) == 0 || String.Compare(words[procWord], "FUNCTION", true) == 0 )
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
                    if ( String.Compare(words[0], "ENDPROC", true) == 0 || String.Compare(words[0], "endfunc", true) == 0 )
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
                    if ( line.StartsWith("LOCAL ", StringComparison.OrdinalIgnoreCase) )
                    {
                        type = CVariable.VARIABLE;
                        scope = CVariable.LOCAL;
                        RedefineCheck = true;
                        found = true;
                    }

                    if ( line.StartsWith("PRIVATE ", StringComparison.OrdinalIgnoreCase) )
                    {
                        type = CVariable.VARIABLE;
                        scope = CVariable.PRIVATE;
                        RedefineCheck = true;
                        found = true;
                    }

                    if ( line.StartsWith("PUBLIC ", StringComparison.OrdinalIgnoreCase) )
                    {
                        type = CVariable.VARIABLE;
                        scope = CVariable.GLOBAL;
                        RedefineCheck = true;
                        found = true;
                    }

                    if ( line.StartsWith("LPARAMETER ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("LPARAMETERS ", StringComparison.OrdinalIgnoreCase) )
                    {
                        type = CVariable.PARAMETER;
                        scope = CVariable.LOCAL;
                        found = true;
                    }

                    if ( line.StartsWith("PARAMETER", StringComparison.OrdinalIgnoreCase) || line.StartsWith("PARAMETERS", StringComparison.OrdinalIgnoreCase) )
                    {
                        type = CVariable.PARAMETER;
                        scope = CVariable.PRIVATE;
                        found = true;
                    }

                    if ( line.StartsWith("DIMENSION ", StringComparison.OrdinalIgnoreCase) )
                    {
                        type = CVariable.VARIABLE;
                        scope = CVariable.PRIVATE;
                        found = true;
                    }

                    if ( line.StartsWith("DECLARE ", StringComparison.OrdinalIgnoreCase) )
                    {
                        // пропускаем DECLARE DLL
                        if ( line.IndexOf(" IN ", StringComparison.OrdinalIgnoreCase) < 0 )
                        {
                            type = CVariable.VARIABLE;
                            scope = CVariable.PRIVATE;
                            found = true;
                        }
                    }

                    if ( found )
                    {
                        string s = "";

                        line = line.Substring(line.IndexOf(' '));

                        // обрабатываем LOCAL ARRAY, PUBLIC ARRAY
                        int array_word = line.IndexOf("ARRAY ", StringComparison.OrdinalIgnoreCase);
                        if ( array_word != -1 )
                            line = line.Substring(array_word + 6);

                        // если описан тип, отбрасываем
                        int as_word = line.IndexOf(" AS ", StringComparison.OrdinalIgnoreCase);
                        if ( as_word > 0 )
                            line = line.Substring(0, as_word);

                        // игнорируем размерности массивов
                        s = FormatVarName(line);

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
            // процедуры, создающие массивы (если они не объявлены)
            string[] aprocedures =
            {
                "ACOPY",
                "ACLASS",
                "ADATABASES",
                "ADBOBJECTS",
                "ADIR",
                "ADLLS",
                "ADOCKSTATE",
                "AELEMENT",
                "AERROR",
                "AEVENTS",
                "AFIELDS",
                "AFONT",
                "AGETCLASS",
                "AGETFILEVERSION",
                "AINSTANCE",
                "ALANGUAGE",
                "ALINES",
                "AMEMBERS",
                "AMOUSEOBJ",
                "ANETRESOURCES",
                "APRINTERS",
                "APROCINFO",
                "ASELOBJ",
                "ASESSIONS",
                "ASQLHANDLES",
                "ASTACKINFO",
                "ATAGINFO",
                "AUSED",
                "AVCXCLASSES"
            };
            
            foreach ( var proc in Procedures )
            {
                for ( int i = proc.begin_line; i <= proc.end_line; i++ )
                {
                    CLine line = Lines[i];
                    int offset = 0;

                    // VARIABLE = ... (явное присвоение)
                    int pos = line.content.IndexOf('=');
                    if ( pos > 0 )
                    {
                        string str = line.content.Substring(0, pos);

                        if ( !StartWith(str, "IF") )
                        {
                            // FOR ...
                            if (str.StartsWith("FOR ", StringComparison.OrdinalIgnoreCase))
                                str = str.Substring(4).Trim();

                            str = FormatVarName(str);

                            string[] columns = str.Split(new[] { '=', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (columns.Length == 1)
                            {
                                str = str.Trim();

                                if (checkIncorrectVarNames && !CheckName(str))
                                    CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, str);

                                proc.AddAssignment(line.number, str);
                            }
                        }
                    }

                    // STORE ... TO ...
                    if ( line.content.StartsWith("STORE ", StringComparison.OrdinalIgnoreCase) )
                    {
                        string str = line.content.Substring(6);
                        int pos2 = str.IndexOf(" TO ", StringComparison.OrdinalIgnoreCase);
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
                    // CATCH TO ...
                    else if ( line.content.StartsWith("CATCH TO ", StringComparison.OrdinalIgnoreCase) )
                    {
                        string str = line.content.Substring(9).Trim();
                        if ( str != "" )
                        {
                            if ( checkIncorrectVarNames && !CheckName(str) )
                                CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, str);

                            proc.AddAssignment(line.number, str);
                        }
                    }
                    // FOR EACH ...
                    else if ( line.content.StartsWith("FOR EACH ", StringComparison.OrdinalIgnoreCase) )
                    {
                        string [] words = line.content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ( words.Length > 2 )
                        {
                            if ( checkIncorrectVarNames && !CheckName(words[2]) )
                                CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, words[2]);
                            proc.AddAssignment(line.number, words[2]);
                        }
                    }
                    // CALCULATE ... TO ...
                    else if (line.content.StartsWith("CALCULATE ", StringComparison.OrdinalIgnoreCase))
                    {
                        string str = line.content.Substring(10);
                        int pos2 = str.IndexOf(" TO ", StringComparison.OrdinalIgnoreCase);
                        if (pos2 >= 0)
                        {
                            str = str.Substring(pos2 + 4).Trim();

                            int len = str.IndexOf(" IN ", StringComparison.OrdinalIgnoreCase);
                            if (len > 0)
                                str = str.Substring(0, len);

                            pos2 = str.IndexOf("ARRAY ", StringComparison.OrdinalIgnoreCase);
                            if (pos2 >= 0)
                                str = str.Substring(pos2 + 6).Trim();

                            string[] columns = str.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int j = 0; j < columns.Length; j++)
                            {
                                if (checkIncorrectVarNames && !CheckName(columns[j]))
                                    CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, columns[j]);

                                proc.AddAssignment(line.number, columns[j]);
                            }
                        }
                    }
                    // DO FORM ... NAME ... TO ...
                    else if (line.content.StartsWith("DO FORM ", StringComparison.OrdinalIgnoreCase))
                    {
                        string str = line.content.Substring(8);
                        int pos2 = str.IndexOf(" NAME ", StringComparison.OrdinalIgnoreCase);
                        if (pos2 >= 0)
                        {
                            str = str.Substring(pos2 + 6).Trim();
                            string[] columns = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            int len = columns.Length;
                            if ( len > 0 )
                            {
                                if (checkIncorrectVarNames && !CheckName(columns[0]))
                                    CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, columns[0]);
                                proc.AddAssignment(line.number, columns[0]);
                            }
                        }

                        pos2 = str.IndexOf(" TO ", StringComparison.OrdinalIgnoreCase);
                        if (pos2 >= 0)
                        {
                            str = str.Substring(pos2 + 4).Trim();
                            string[] columns = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            int len = columns.Length;
                            if (len > 0)
                            {
                                if (checkIncorrectVarNames && !CheckName(columns[0]))
                                    CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, columns[0]);
                                proc.AddAssignment(line.number, columns[0]);
                            }
                        }
                    }
                    // ON ERROR VAR = VALUE
                    else if (line.content.StartsWith("ON ERROR ", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] words = line.content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((words.Length > 4) && (words[3] == "=") )
                        {
                            if (checkIncorrectVarNames && !CheckName(words[2]))
                                CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, words[2]);
                            proc.AddAssignment(line.number, words[2]);
                        }
                    }
                    // A... procedures
                    else if ( (offset = IndexOf(line.content, aprocedures)) >= 0 )
                    {
                        string str = line.content.Substring(offset);
                        int num = str.StartsWith("ACOPY", StringComparison.OrdinalIgnoreCase) ? 2 : 1; // для ACOPY проверяем 2-й параметр
                        string[] words = str.Split(new[] { ' ', '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if ( words.Length > 1 )
                        {
                            if ( checkIncorrectVarNames && !CheckName(words[num]) )
                                CLog.Print("{0}: {1}: Недопустимое имя переменной при присваивании \"{2}\"", fileName, line.number, words[num]);
                            proc.AddAssignment(line.number, words[num]);
                        }
                    }
                    // SELECT ... FROM XXX INTO ARRAY YYY
                    else if ( line.content.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase) )
                    {
                        string[] words = line.content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        int len = words.Length;

                        if ( len > 6 ) 
                        {
                            if ( String.Compare(words[0], "SELECT", true) == 0 )
                            {
                                if ( String.Compare(words[len - 3], "INTO", true) == 0 )
                                {
                                    if ( String.Compare(words[len - 2], "ARRAY", true) == 0 )
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
                if ( String.Compare(line.content, "TRY", true) == 0 )
                    tryStart = true;

                if ( String.Compare(line.content, "ENDTRY", true) == 0 )
                    tryStart = false;

                if ( tryStart )
                {
                    if ( line.content.StartsWith("RETURN", StringComparison.OrdinalIgnoreCase) )
                        CLog.Print("{0}: {1}: Недопустимое использование RETURN", fileName, line.number);

                    if ( line.content.StartsWith("RETRY", StringComparison.OrdinalIgnoreCase) )
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
                    if ( name.StartsWith("THIS.", StringComparison.OrdinalIgnoreCase) || name.StartsWith("THIS->", StringComparison.OrdinalIgnoreCase) )
                        continue;

                    // Пропускаем присвоения свойствам текущей формы
                    if ( name.StartsWith("THISFORM.", StringComparison.OrdinalIgnoreCase) || name.StartsWith("THISFORM->", StringComparison.OrdinalIgnoreCase) )
                        continue;

                    // Для свойcтв объекта берем имя
                    int dotPos = name.IndexOf('.');
                    if ( dotPos < 0 )
                        dotPos = name.IndexOf("->");

                    if ( dotPos > 0 )
                        name = name.Substring(0, dotPos);

                    CVariable var = proc.FindVariable(name);
                    if ( var == null || assignment.line < var.line ) // если переменная не объявлена или объявлена после использования
                        CLog.Print("{0}: {1}: Необъявленная переменная \"{2}\"", fileName, assignment.line, name);
                }
            }
        }

        //
        // Поиск неиспользованных переменных
        //
        public void CheckUnusedVariables( )
        {
            if ( !checkUnusedVariables )
                return;
            
            foreach ( var proc in Procedures )
            {
                foreach ( var variable in proc.Variables )
                    if ( variable.type == CVariable.VARIABLE ) // параметры не проверяем
                        if ( !proc.IsVariableAssigned(variable) )
                            CLog.Print("{0}: {1}: Неиспользованная переменная \"{2}\"", fileName, variable.line, variable.name);
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

                    if ( line.content.StartsWith("IF ", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("ENDIF", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("FOR ", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("ENDFOR", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("DO WHILE ", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("ENDDO", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("DO CASE", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("ENDCASE", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("SCAN", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("ENDSCAN", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    //
                    // ПРИМЕЧАНИЕ: 
                    // В данном месте наличие RETURN меджу TRY/ENDTRY не считаем ошибкой
                    // Ошибка на недопустимость использования RETURN проверяется в CheckTryEndtry
                    //
                    if ( line.content.StartsWith("TRY", StringComparison.OrdinalIgnoreCase) )
                        cond_count++;
                    if ( line.content.StartsWith("ENDTRY", StringComparison.OrdinalIgnoreCase) )
                        cond_count--;

                    if ( line.content.StartsWith("RETURN", StringComparison.OrdinalIgnoreCase) )
                    {
                        if ( cond_count == 0 && i < proc.end_line )
                        {
                            if ( !(Lines[i + 1].content.StartsWith("ENDFUNC", StringComparison.OrdinalIgnoreCase)
                                || Lines[i + 1].content.StartsWith("ENDPROC", StringComparison.OrdinalIgnoreCase)
                                || Lines[i + 1].content.StartsWith("ENDWITH", StringComparison.OrdinalIgnoreCase)) )
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
