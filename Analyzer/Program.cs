using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Analyzer
{
    class Program
    {
        static void Main( string[] args )
        {
            string path = "";
            bool iv, uv, es, uc, unused_vars;
            bool noParams = true;

            iv = uv = es = uc = unused_vars = false;

            if ( args.Length > 0 )
            {
                for ( int i = 0; i < args.Length; ++i )
                {
                    if ( args[i].StartsWith("/") || args[i].StartsWith("-") )
                    {
                        noParams = false;

                        if ( args[i].Substring(1) == "var_names" )
                        {
                            iv = true;
                        }
                        else if ( args[i].Substring(1) == "undef_vars" )
                        {
                            uv = true;
                        }
                        else if ( args[i].Substring(1) == "exc_stmt" )
                        {
                            es = true;
                        }
                        else if ( args[i].Substring(1) == "unr_code" )
                        {
                            uc = true;
                        }
                        else if ( args[i].Substring(1) == "unused_vars" )
                        {
                            unused_vars = true;
                        }
                        else
                        {
                            Console.WriteLine("Неверный параметр:{0}", args[i].Substring(1));
                            return;
                        }
                    }
                    else
                    {
                        path = args[i];
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Analyzer.exe [параметры] <путь>");
                return;
            }


            CLog.Open("Analyzer.log");

            if ( !Directory.Exists(path) )
            {
                Console.WriteLine("Путь \"{0}\" не существует!", path);
                return;
            }

            try
            {
                string[] files = Directory.GetFiles(path, "*.prg", SearchOption.AllDirectories);
                foreach ( string file in files )
                {
                    CProgram prg = new CProgram(file);

                    if ( !noParams )
                    {
                        prg.checkIncorrectVarNames = iv;
                        prg.checkUndefinedVars = uv;
                        prg.checkExceptionStatement = es;
                        prg.checkUnreachableCode = uc;
                        prg.checkUnusedVariables = unused_vars;
                    }

                    prg.Parse();

                    prg.CheckExceptionStatement();
                    prg.CheckUndefinedVariables();
                    prg.CheckUnusedVariables();
                    prg.CheckUnreachableCode();
                }
            }
            catch ( Exception e )
            {
                ConsoleColor c = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ForegroundColor = c;
            }

            if ( CLog.count == 0 )
                CLog.Print("Ошибки не обнаружены.");

            CLog.Close();
        }
    }
}
