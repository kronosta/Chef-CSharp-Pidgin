using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Kronosta.ChefCSharpPidgin
{
    /// <summary>
    /// Holds a series of automated tests of the Chef-C# Pidgin compiler, and can run them and
    /// get various diagnostic information.
    /// </summary>
    /// <typeparam name="TReturn">The return type of the test, besides the normal compiler diagnostics and possible exception</typeparam>
    public class TestSuite<TReturn>
    {
        /// <summary>
        /// Holds a single test of the Chef-C# Pidgin compiler
        /// </summary>
        public class Test
        {
            /// <summary>
            /// The TestSuite&lt;TReturn&gt; containing this test
            /// </summary>
            public TestSuite<TReturn> LatestTestSuite = null;

            /// <summary>
            /// A list of source texts. These should be the contents of the files, not file paths.
            /// </summary>
            public List<string> SourceFiles { get; set; } = new List<string>();

            /// <summary>
            /// A list of additional metadata references beyond those that normally get added.
            /// </summary>
            public List<MetadataReference> MetadataReferences { get; set; } = new List<MetadataReference>();

            /// <summary>
            /// The assembly name for the assembly generated in memory that contains the compiled version
            /// of the code provided in SourceFiles. Don't count on reusing this since it will be garbage collected,
            /// though it should be unique to prevent conflicts while some assemblies may not yet be garbage collected.
            /// </summary>
            public string AssemblyName { get; set; } = "Test";

            /// <summary>
            /// A list of mods to add to the compiler. (The actions get run on a mutable compiler instance
            /// so you can modify fields, handlers, and the command list).
            /// </summary>
            public List<Action<ChefCompiler>> Mods { get; set; } = new List<Action<ChefCompiler>>();

            /// <summary>
            /// A unique ID for this test within the TestSuite&lt;TReturn&gt;. Used in output formatting and as the key
            /// to the various dictionaries populated by TestSuite&lt;Return&gt;.Run()
            /// </summary>
            public string ID { get; set; } = "Test";

            /// <summary>
            /// If not null, this will be input to the test method instead of standard input
            /// (by using Console.SetIn(TextReader) and reverting it back once the test is done).
            /// Note that this affects everything until the test method completes or you call Console.SetIn(TextReader)
            /// with something else.
            /// </summary>
            public string? Input { get; set; } = null;

            /// <summary>
            /// A function to determine whether the test is successful. If null, the test won't be considered
            /// when checking if all tests are successful, and won't be populated to TestSuite&lt;TReturn&gt;.Successes.
            /// <br/><br/>
            /// Parameters: See TestSuite&lt;TReturn&gt;.Test.Run for more info<br/>
            /// Also passed the Test and TestSuite containing this
            /// <br/><br/>
            /// Should return true if the test was successful, otherwise false
            /// </summary>
            public Func<EmitResult, Exception?, TReturn?, Test, TestSuite<TReturn>, bool>? IsSuccessful { get; set; } = null;

            /// <summary>
            /// The compilation that compiled this Test instance. You can use this to obtain the syntax trees
            /// used to compile the test.
            /// </summary>
            public Compilation Compilation = null;


            /// <summary>
            /// Runs this test
            /// </summary>
            /// <param name="logger">
            /// A TextWriter to output information to, or null if you just want
            /// to run the test.
            /// </param>
            /// <returns>
            /// <list type="number">
            ///     <item>
            ///         <term>EmitResult</term>
            ///         <description>The EmitResult obtained from the CSharpCompilation after emitting</description>
            ///     </item>
            ///     <item>
            ///         <term>Exception?</term>
            ///         <description>
            ///         The first uncaught exception to be thrown by the test (which ends the test prematurely).
            ///         If no uncaught exceptions were thrown, this will be null.
            ///         </description>
            ///     </item>
            ///     <item>
            ///         <term>TReturn?</term>
            ///         <description>
            ///         The return value of the test, if successful. If EmitResult.Success is true and
            ///         the Exception is null, this is okay to use, otherwise it contains a dummy value
            ///         that should not be depended on.
            ///         </description>
            ///     </item>
            /// </list><br/>
            /// </returns>
            public (EmitResult, Exception?, TReturn?) Run(TextWriter? logger)
            {
                (MemoryStream stream, EmitResult emitResult) = Program.CompileSources(SourceFiles, AssemblyName, MetadataReferences, Mods);
                Compilation = Program.JustCompiledCompilation;
                if (logger != null)
                {
                    if (emitResult.Success)
                        logger.WriteLine($"[{ID}] SUCCESSFULLY COMPILED");
                    else
                        logger.WriteLine($"[{ID}] FAILED TO COMPILE");
                    foreach (Diagnostic diagnostic in emitResult.Diagnostics)
                    {
                        logger.WriteLine($"[{ID}: Compilation Diagnostics] {diagnostic.ToString()}");
                    }
                }
                if (!emitResult.Success)
                    return (emitResult, null, default(TReturn));
                AssemblyLoadContext alcontext = new AssemblyLoadContext("Kronosta.ChefCSharpPidgin.Testing." + ID, true);
                Assembly assembly = alcontext.LoadFromStream(stream);
                Exception? exception = null;
                TReturn? resultString = default(TReturn);
                try
                {
                    MethodInfo? methodInfo = assembly.GetType("Program")?.GetMethod("TestMain", new Type[] {typeof(TextWriter)});
                    if (methodInfo == null)
                    {
                        if (logger != null)
                            logger.WriteLine($"[{ID}] No method Program.TestMain(TextWriter).");
                        return (emitResult, new Exception("%%%FAKE: NoMethod"), default(TReturn));
                    }
                    if (methodInfo.ReturnType != typeof(TReturn))
                    {
                        if (logger != null)
                            logger.WriteLine($"[{ID}] Program.TestMain(TextWriter) does not return the correct type.");
                        return (emitResult, new Exception("%%%FAKE: IncorrectReturn"), default(TReturn));
                    }
                    if (!methodInfo.IsStatic)
                    {
                        if (logger != null)
                            logger.WriteLine($"[{ID}] Program.TestMain(TextWriter) must be static.");
                        return (emitResult, new Exception("%%%FAKE: NotStatic"), default(TReturn));
                    }
                    TextReader consoleIn = Console.In;
                    if (logger != null)
                        logger.WriteLine($"[{ID}] @BEGIN TEST LOGGER OUTPUT");
                    if (Input != null)
                        Console.SetIn(new StringReader(Input));
                    resultString = (TReturn?)methodInfo.Invoke(null, new object?[] { logger });
                    if (Input != null)
                        Console.SetIn(consoleIn);
                    if (logger != null)
                    {
                        logger.WriteLine($"[{ID}] @END   TEST LOGGER OUTPUT");
                        logger.WriteLine($"[{ID}] RESULT: {resultString}");
                    }
                }
                catch (TargetInvocationException e)
                {
                    
                    exception = e.InnerException;
                    if (logger != null)
                        logger.WriteLine($"[{ID}] EXCEPTION: ${exception.ToString()}");
                }
                return (emitResult, exception, resultString);
            }
        }

        private IEnumerable<Test> _Tests = null;

        /// <summary>
        /// The list of tests to run in this TestSuite
        /// </summary>
        public IEnumerable<Test> Tests
        {
            get { return _Tests; }
            set
            { 
                foreach (Test test in value)
                    test.LatestTestSuite = this;
                _Tests = value;
            }
        }

        /// <summary>
        /// The results of Test.Run for all tests. Key is the Test.ID of the test.
        /// </summary>
        public Dictionary<string, (EmitResult, Exception?, TReturn?)> Results { get; set; } = 
            new Dictionary<string, (EmitResult, Exception?, TReturn?)>();

        /// <summary>
        /// The output logs of each test. Key is the Test.ID of the test.
        /// </summary>
        public Dictionary<string, string> Logs { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// For all tests that have a non-null value for Test.IsSuccessful, stores the results of Test.IsSuccessful.
        /// </summary>
        public Dictionary<string, bool> Successes { get; set; } = new Dictionary<string, bool>();

        /// <summary>
        /// Runs all tests from Test, in order.
        /// </summary>
        /// <param name="logger">A TextWriter to output information to</param>
        /// <returns>True if all tests with a non-null value for IsSuccessful were successful, false otherwise.</returns>
        public bool Run(TextWriter? logger)
        {
            foreach (var test in Tests)
            {
                StringWriter stringSaver = new StringWriter();
                (EmitResult emitResult, Exception? exception, TReturn? result) = test.Run(
                    new InternalForkingTextWriter(
                        new List<TextWriter> { logger, stringSaver },
                        Encoding.UTF8
                    )
                );
                Results[test.ID] = (emitResult, exception, result);
                Logs[test.ID] = stringSaver.ToString();
                if (test.IsSuccessful != null)
                {
                    Successes[test.ID] = test.IsSuccessful(emitResult, exception, result, test, this);
                }
            }
            if (logger != null)
            {
                logger.Write("\n\n\n");
                LogSuccesses(logger);
                logger.Write("\n\n\n");
            }
            return AllSuccess(logger);
        }

        /// <summary>
        /// Logs the success and failure status of all entries in TestSuite&lt;TReturn&gt;.Successes
        /// </summary>
        /// <param name="logger">A logger to output to. Must not be null.</param>
        public void LogSuccesses(TextWriter logger)
        {
            logger.WriteLine("===========================================================================|");
            foreach (var successResult in Successes)
            {
                logger.WriteLine("[" + successResult.Key + "]" +
                    string.Join("", Enumerable.Range(0, 66 - successResult.Key.Length).Select(x => " ")) +
                    (successResult.Value ? "SUCCESS|" : "FAILURE@@@"));
            }
            logger.WriteLine("===========================================================================|");
        }

        /// <summary>
        /// Checks if all entries in TestSuite&lt;TReturn&gt;.Successes represent a success.
        /// </summary>
        /// <param name="logger">A logger to output information to, or null to not output anywhere</param>
        /// <returns>True if all entries in TestSuite&lt;TReturn&gt;.Successes represent a success, false otherwise</returns>
        public bool AllSuccess(TextWriter? logger)
        {
            List<string> failures = new List<string>();
            foreach (var successResult in Successes)
                if (!successResult.Value)
                    failures.Add(successResult.Key);
            if (logger != null)
            {
                if (failures.Count == 0)
                    logger.WriteLine("All tests successful.");
                else
                    logger.WriteLine($"The following tests failed: {string.Join(", ", failures)}");
            }
            return failures.Count == 0;
        }

        /// <summary>
        /// Clears the Results dictionary
        /// </summary>
        public void ClearResults()
        {
            Results.Clear();
        }

        /// <summary>
        /// Clears the Logs dictionary
        /// </summary>
        public void ClearLogs()
        {
            Logs.Clear();
        }

        /// <summary>
        /// Clears the Successes dictionary
        /// </summary>
        public void ClearSuccesses()
        {
            Successes.Clear();
        }

        /// <summary>
        /// Clears all output dictionaries
        /// </summary>
        public void Clear()
        {
            ClearResults();
            ClearLogs();
            ClearSuccesses();
        }

        /// <summary>
        /// Clears the test IEnumerable (by setting it to an empty array)
        /// </summary>
        public void ClearTests()
        {
            Tests = new Test[0];
        }

        /// <summary>
        /// Clears both the output dictionaries and the test IEnumerable
        /// </summary>
        public void ClearAll()
        {
            Clear();
            ClearTests();
        }

        /// <summary>
        /// For an entry in TestSuite&lt;TReturn&gt;.Results, checks if the TReturn? value (Item3) is safe to use.
        /// </summary>
        /// <param name="ID">The ID of the test to check the output of</param>
        /// <returns>True if the TReturn? value is safe to use, false otherwise</returns>
        public bool IsValueApplicable(string ID)
        {
            if (!Results.ContainsKey(ID)) return false;
            return Results[ID].Item1.Success && Results[ID].Item2 == null;
        }
    }
}
