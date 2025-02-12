using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Kronosta.ChefCSharpPidgin
{
    public class Program
    {
        [ThreadStatic]
        public static Compilation JustCompiledCompilation = null;

        /// <summary>
        /// The Main method of this program, when used as an executable
        /// </summary>
        /// <param name="args">The command-line arguments</param>
        public static void Main(string[] args)
        {
            StandardTestSuites.MainStringTests.Run(Console.Error);
        }

        /// <summary>
        /// Compiles a Chef-C# Pidgin program with no mods depending on args.
        /// See Compile(string[], List<Action<ChefCompiler>>) for more details on the args array.
        /// </summary>
        /// <param name="args">The arguments</param>
        /// <returns></returns>
        public static (MemoryStream, EmitResult) Compile(string[] args) =>
            Compile(args, new List<Action<ChefCompiler>>());

        /// <summary>
        /// Compiles a Chef-C# Pidgin program with the given mods, depending on args.<br/>
        /// <br/>
        /// Strings in the args array that start with "-" are options, otherwise they are source files.<br/>
        /// The "-A[assembly-name]" argument specified the assembly name ("Test" by default).<br/>
        /// The "-R[path]" argument specifies the path to a text file containing a list of assembly paths,
        /// each on separate lines. If this is not specified, it looks for "references.txt" in the current directory.
        /// If this file does not exist, a FileNotFoundException will be thrown.
        /// 
        /// </summary>
        /// <param name="args">The argument array</param>
        /// <param name="mods">The mods to pass to the ChefCompiler. These are Actions that will be run on
        /// a mutable instance of ChefCompiler, so you can modify the fields, handlers, and command table.</param>
        /// <returns>
        /// Item1: A memory stream containing the emitted assembly file. You can use this with Assembly.Load(Stream)
        /// or AssemblyLoadContext.LoadFromStream(Stream), or write it to a file for later use.<br/>
        /// Item2: The EmitResult of the compilation, for diagnostic purposes
        /// </returns>
        public static (MemoryStream, EmitResult) Compile(string[] args, List<Action<ChefCompiler>> mods)
        {
            Dictionary<char, List<string>> options = args
                .Where(x => x.StartsWith("-") && x.Length >= 2)
                .Select(x => new KeyValuePair<char, string>(x[1], x.Substring(2)))
                .GroupBy(x => x.Key)
                .Select(x => new KeyValuePair<char, List<string>>(x.Key, x.Select(x2 => x2.Value).ToList()))
                .ToDictionary();
            string assemblyName = options['A'].Count == 0 ? "Test" : options['A'][0];
            string referenceListFile = options['R'].Count == 0 ? "references.txt" : options['R'][0];
            List<MetadataReference> references =
                Utils.NormalizeNewlines(File.ReadAllText(referenceListFile))
                .Split('\n')
                .Select(x => (MetadataReference)AssemblyMetadata.CreateFromFile(x).GetReference())
                .ToList();
            List<string> sourceFiles = args.Where(x => x.Length > 0 && x[0] != '-').ToList();
            List<string> sourceContents = sourceFiles
               .Where(x => Path.Exists(x))
               .Select(x => File.ReadAllText(x))
               .ToList();
            return CompileSources(sourceContents, assemblyName, references, mods);
        }

        /// <summary>
        /// Compiles a Chef-C# Pidgin program from source files, entirely in memory, with no mods.
        /// </summary>
        /// <param name="sources">A list of source texts</param>
        /// <param name="assemblyName">The name of the assembly to compile</param>
        /// <param name="references">A list of references to add to the assembly along with the automatically
        /// added assemblies required for the generated code to function.</param>
        /// <returns>
        /// Item1: A memory stream containing the emitted assembly file. You can use this with Assembly.Load(Stream)
        /// or AssemblyLoadContext.LoadFromStream(Stream), or write it to a file for later use.<br/>
        /// Item2: The EmitResult of the compilation, for diagnostic purposes
        /// </returns>
        public static (MemoryStream, EmitResult) CompileSources(
            List<string> sources,
            string assemblyName,
            List<MetadataReference> references) =>
            CompileSources(sources, assemblyName, references, new List<Action<ChefCompiler>>());

        /// <summary>
        /// Compiles a Chef-C# Pidgin program from source files, entirely in memory, with the given mods.
        /// </summary>
        /// <param name="sources">A list of source texts</param>
        /// <param name="assemblyName">The name of the assembly to compile</param>
        /// <param name="references">A list of references to add to the assembly along with the automatically
        /// added assemblies required for the generated code to function.</param>
        /// <param name="mods">The mods to pass to the ChefCompiler. These are Actions that will be run on
        /// a mutable instance of ChefCompiler, so you can modify the fields, handlers, and command table.</param>
        /// <returns>
        /// Item1: A memory stream containing the emitted assembly file. You can use this with Assembly.Load(Stream)
        /// or AssemblyLoadContext.LoadFromStream(Stream), or write it to a file for later use.<br/>
        /// Item2: The EmitResult of the compilation, for diagnostic purposes
        /// </returns>
        public static (MemoryStream, EmitResult) CompileSources(
            List<string> sources,
            string assemblyName,
            List<MetadataReference> references,
            List<Action<ChefCompiler>> mods)
        {
            List<MetadataReference> requiredReferences = new List<MetadataReference>();
            requiredReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Private.CoreLib").Location));
            requiredReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location));
            requiredReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location));
            requiredReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location));
            requiredReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location));
            List<CSharpSyntaxTree> syntaxTrees = sources
                .Select(x => (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(x))
                .ToList();
            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, null,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(requiredReferences);
            compilation = compilation.AddReferences(references);
            CSharpGeneratorDriver generatorDriver = CSharpGeneratorDriver.Create(new IIncrementalGenerator[]
            {
                new ChefGenerator(mods)
            });
            generatorDriver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation newCompilation, out ImmutableArray<Diagnostic> generatorDiagnostics);
            MemoryStream stream = new MemoryStream();
            EmitResult emitResult = newCompilation.Emit(stream);
            JustCompiledCompilation = newCompilation;
            stream.Seek(0, SeekOrigin.Begin);
            return (stream, emitResult);
        }
    }
}