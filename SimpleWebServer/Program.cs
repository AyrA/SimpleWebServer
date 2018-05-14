using Engine;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SimpleWebServer
{
    class Program
    {
        public const int E_SUCCESS = 0;
        public const int E_ARGS = E_SUCCESS + 1;
        public const int E_HELP = E_ARGS + 1;
        public const int E_COMPILE = E_HELP + 1;

        static int Main(string[] args)
        {
            string Dir = null;
            int Port = -1;
            bool Browser = false;

#if DEBUG
            args = @"/b 55555 C:\Temp\SWS".Split(' ');
#endif

            if (IsHelp(args))
            {
                ShowHelp();
                return E_HELP;
            }
            else
            {
                foreach (var a in args.Where(m => !string.IsNullOrEmpty(m)))
                {
                    if (a.ToLower() == "/b")
                    {
                        if (!Browser)
                        {
                            Browser = true;
                        }
                        else
                        {
                            Logger.Error("/b used multiple times");
                            return E_ARGS;
                        }
                    }
                    else
                    {
                        //First argument is port
                        if (Port == -1)
                        {
                            Port = Tools.IntOrDefault(a, -1);
                            if (Port <= ushort.MinValue || Port >= ushort.MaxValue)
                            {
                                Logger.Error("Invalid Port: {0}. Range: {1}-{2}", a, ushort.MinValue + 1, ushort.MaxValue - 1);
                                return E_ARGS;
                            }
                        }
                        else if (Dir == null)
                        {
                            if (Directory.Exists(a))
                            {
                                Dir = a;
                            }
                            else
                            {
                                Logger.Error("Directory does not exists: {0}", a);
                                return E_ARGS;
                            }
                        }
                        else
                        {
                            Logger.Error("Unknown argument: {0}", a);
                            return E_ARGS;
                        }
                    }
                }
            }
            if (Dir == null)
            {
                Dir = Environment.CurrentDirectory;
            }
            var A = Compiler.Compile(Directory.GetFiles(Dir, "*.cs", SearchOption.AllDirectories));

            if (A.Errors.Length == 0)
            {
                ShowProblems(A.Warings);
                using (var S = new Server(Port, Controller.GetControllers(A.A), Browser))
                {
                    do
                    {
                        Logger.Info("Press [ESC] to exit");
                    }
                    while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                }

                return E_SUCCESS;
            }
            else
            {
                ShowProblems(A.Errors.Concat(A.Warings).ToArray());
            }
            return E_COMPILE;
        }

        private static void ShowProblems(CompilerError[] Err)
        {
            foreach (var E in Err)
            {
                if (E.IsWarning)
                {
                    Logger.Warn("[{0};{1}:{2}] {3} {4}", E.FileName, E.Line, E.Column, E.ErrorNumber, E.ErrorText);
                }
                else
                {
                    Logger.Err("[{0};{1}:{2}] {3} {4}", E.FileName, E.Line, E.Column, E.ErrorNumber, E.ErrorText);
                }
            }
#if DEBUG
            if (Err.Length > 0)
            {
                Console.ReadKey(true);
            }
#endif
        }

        private static void ShowHelp()
        {
            Write(Console.Out, string.Format(@"SimpleWebServer <Port> [Dir] [/B]

Starts a simple C# 'MVC-Like' Webserver

Port  - Required; Port number. Must be first argument. Range: {0}-{1}
Dir   - Optional; Directory of files. Default is current directory.
/b    - Optional; Start browser"
, ushort.MinValue + 1, ushort.MaxValue - 1));
        }

        private static void Write(TextWriter Output, string Text, int LineLength = -1)
        {
            char[] Spaces = Unicode.Get(UnicodeCategory.SpaceSeparator);
            char[] LineBreaks = Unicode.Get(UnicodeCategory.ParagraphSeparator)
                .Concat(Unicode.Get(UnicodeCategory.LineSeparator))
                .Concat(Unicode.Get(UnicodeCategory.Control))
                .ToArray();
            var Lines = Text.Replace("\r\n", "\n").Split(LineBreaks);

            if (LineLength == -1)
            {
                LineLength = Console.BufferWidth;
            }
            if (LineLength < 1)
            {
                throw new ArgumentOutOfRangeException("LineLength");
            }
            foreach (var Line in Lines)
            {
                var LinePos = 0;
                var Words = Line.Split(Spaces);
                foreach (var Word in Words)
                {
                    if (Word.Length > LineLength)
                    {
                        if (LinePos > 0)
                        {
                            Output.WriteLine();
                        }
                        Output.WriteLine(Word.Substring(0, LineLength - 4) + "...");
                        LinePos = 0;
                    }
                    else if (LinePos + Word.Length < LineLength)
                    {
                        Output.Write("{0} ", Word);
                        LinePos += Word.Length + 1;
                    }
                    else
                    {
                        Output.WriteLine();
                        Output.Write("{0} ", Word);
                        LinePos = Word.Length + 1;
                    }
                }
                Output.WriteLine();
            }
        }

        public static bool IsHelp(IEnumerable<string> args)
        {
            var Helps = "--help,-h,-?,/?".Split(',');
            return args != null && args.Any(m => m != null && Helps.Contains(m.ToLower()));
        }
    }
}
