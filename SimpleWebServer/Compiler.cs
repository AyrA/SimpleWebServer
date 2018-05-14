using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SimpleWebServer
{
    public static class Compiler
    {
        public struct CompileResult
        {
            public Assembly A;
            public CompilerError[] Warings;
            public CompilerError[] Errors;
        }

        /// <summary>
        /// Resolves dependencies in a script
        /// </summary>
        /// <param name="FileName">File name</param>
        /// <returns>List of dependencies</returns>
        private static string[] ResolveDependencies(string FileName)
        {
            return File.ReadAllLines(FileName)
                .Where(m => m.Trim().StartsWith("//#include "))
                .Select(m => Environment.ExpandEnvironmentVariables(m.Substring(11).Trim()))
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Compiles Controllers
        /// </summary>
        /// <param name="Scripts">Script Files</param>
        /// <param name="Optimize">Optimize Code. This makes debugging harder</param>
        /// <returns>Compiler Errors</returns>
        public static CompileResult Compile(string[] Scripts, bool Optimize = true)
        {
            string[] Deps = Scripts.SelectMany(m => ResolveDependencies(m)).Distinct().ToArray();
            return Compile(Scripts, Deps, Optimize);
        }

        /// <summary>
        /// Compiles one or many Source Files into a binary Script
        /// </summary>
        /// <param name="SourceFiles">Source Files</param>
        /// <param name="References">References Assemblies</param>
        /// <param name="Optimize">Optimize Code. This makes debugging harder</param>
        /// <returns>Compiler Errors</returns>
        private static CompileResult Compile(string[] SourceFiles, string[] References, bool Optimize)
        {
            var codeProvider = new CSharpCodeProvider();
            var compilerParams = new CompilerParameters();
            compilerParams.CompilerOptions = "/target:library" + (Optimize ? " /optimize" : "");
            compilerParams.GenerateExecutable = false;
            compilerParams.GenerateInMemory = true;
            compilerParams.IncludeDebugInformation = !Optimize;
            compilerParams.ReferencedAssemblies.Add("mscorlib.dll");
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("System.Linq.dll");
            compilerParams.ReferencedAssemblies.Add("Newtonsoft.JSON.dll");
            compilerParams.ReferencedAssemblies.Add("Engine.dll");
            if (References != null && References.Length > 0)
            {
                compilerParams.ReferencedAssemblies.AddRange(References);
            }
            var Result = codeProvider.CompileAssemblyFromFile(compilerParams, SourceFiles);

            var Ret = new CompileResult()
            {
                Warings = Result.Errors.OfType<CompilerError>().Where(m => m.IsWarning).ToArray(),
                Errors = Result.Errors.OfType<CompilerError>().Where(m => !m.IsWarning).ToArray()
            };

            if (Ret.Errors.Length == 0)
            {
                Ret.A = Result.CompiledAssembly;
            }

            //Return Warnings and errors
            return Ret;
        }
    }
}
