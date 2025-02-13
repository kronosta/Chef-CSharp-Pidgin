# Chef-CSharp-Pidgin
This is an extension to the Roslyn C# compiler that allows you to designate certain methods as "Chef methods" which run code in the esolang Chef.

# How to use
- Give the attribute `Kronosta.ChefCSharpPidgin.ChefClass` to the class containing the method, and also give the class the `partial` modifier.
  - You can optionally include a string named argument `Using` for any C# injections your C# code will use, along with method arguments and return type.
    If your method has a return type or argument(s) that require using a namespace, you MUST include it in the `Using` argument.
    The string constant should just be normal C# `using` syntax that you would put at the top of a file.
    You can also bypass this requirement by fully qualifying the type names.
- Give the Chef method(s) the `partial` attribute and don't give them a body.
- Attach the `Kronosta.ChefCSharpPidgin.ChefMethod` attribute to the method, which has the following arguments
  - Positional: `string` (the Chef code, which has a slightly different syntax than normal, but we'll get to that in another section)
  - The following are all optional.
  - `IngredientTransformer`: `string` (a C# expression returning a `IDictionary<string, ValueTuple<int, bool>>`, to assign the ingredients to at the start of the method.
    This can include method parameters.)
  - `MixingBowlTransformer`, `BakingDishTransformer`: `string` (similar to `IngredientTransformer`, but should return an `IDictionary<int, Stack<ValueTuple<int, bool>>>`,
    and affects the mixing bowls and baking dishes respectively.
  - `ReturnTransformer`: `string` (a C# expression returning whatever the return type of the method is, that will be the return value of the method)

There are currently some class attributes that are supposed to generate overloaded unary, binary, and cast operators, properties, and indexers, but these
don't do anything at the moment.

# Chef syntax
Chef syntax is a bit different from normal, since the period is useful for C# interpolation (we'll get to that in a bit).
So, every method command is instead separated by two exclamation points (`!!`). Ingredients are unaffected.

Also, the Cooking Time, Oven Temperature, and Serves sections don't exist. Due to the way the compiler splits auxiliary recipes, these
are likely to be interpreted as a title for a new recipe and cause problems. The comment block will probably work, but only
if it contains a newline. You should probably use the `Suggestion:` command anyway. Also, a blank line splits a section, so
your ingredients, method, and comment sections must not contain any blank lines (this is pretty standard behavior
among Chef implementations). To output, use the `Refrigerate for [number] hours` command rather than Serves.

Also, any arguments to a command, such as an ingredient or mixing bowl number, can enclose a C# expression in curly braces to do string
interpolation. This C# expression can have side effects which is useful for actually doing stuff. The whole thing, including the curly braces,
has a maximum of 300 characters, and semicolons are blocked (to prevent it from being too easy by injecting statements in there).
This 300-character limit thus applies to ingredient names as well.

## Extra commands
- The `Suggestion: [comment]` command comments until the next instance of `!!`, and can be used inside the method section.

## Important variables
For use by the transformers and C# interpolation, some variables are important:
- `Chef__ingredientsR`: Holds the ingredients.
  An `IDictionary<string, ValueTuple<int, bool>>`. The key is the ingredient name. The value is a tuple of the ingredients integer value,
  and a boolean which will be true if the ingredient is liquid (character output) and false otherwise.
- `Chef__mixingBowlsR`: Holds the mixing bowls.
  An `IDictionary<int, Stack<ValueTuple<int, bool>>>`. The key is the mixing bowl index, while the value is a stack of ingredient tuples.
- `Chef__bakingDishesR`: Holds the baking dishes. Same structure as `Chef__mixingBowlsR`.
- `Chef__input`: The `TextReader` for the `Take [ingredient] from refrigerator` command.
- `Chef__output`: The `TextWriter` for the `Refrigerate for [number] hours` command.
- `Chef__recipes`: A `Dictionary` of `Func`s that take in and return a tuple of `Chef__ingredientsR`, `Chef__mixingBowlsR`, and `Chef__bakingDishesR`.
  The auto-compiled recipes automatically copy the mixing bowls and baking dishes (the ingredients are empty and the parameter
  is mostly for custom behavior), but if you add your own recipes,
  this won't be done for you and the `Chef__ingredientsR`, `Chef__mixingBowlsR`, and `Chef__bakingDishesR` variables won't exist.
- `Chef__mainResult`: The tuple returned by the main recipe in `Chef__recipes`, useful in the return transformer.
- `Chef__local`: A `Dictionary<string, object>` holding arbitrary data global across Chef recipes, but local to the C#-level method.
- `Chef__localR`: A `Dictionary<string, object>` holding arbitrary data local to a Chef recipe.
  - Keys in these beginning with "`Chef__`" are reserved.

# API
## Class `Program`
Functions to invoke the compiler.

Fields:
- `[ThreadStatic] public static Compilation JustCompiledCompilation`: The Compilation most recently compiled by the `Compile` methods, specific to the current thread.

Methods:
- `public static (MemoryStream, EmitResult) Compile(string[] args)`: Compiles a program from fake command-line arguments
  - `-A[assembly name]` decides the name of the assembly. Defaults to `Test`.
  - `-R[reference list file path]` decides the references, the path should point to a file containing one path to an assembly on each line. Defaults to `references.txt` in
    the current directory.
- `public static (MemoryStream, EmitResult) Compile(string[] args, List<Action<ChefCompiler>> mods)`: Same as the previous, but runs all the Actions on the compiler to
  change the available commands, add custom measurements and "heaped"/"level" designations, change how ingredients are emitted, the regex for ingredients, etc. Not
  everything is fully moddable but I have put a lot of the functionality in instance variable containing delegates so they can be modified.
- `public static (MemoryStream, EmitResult) CompileSources(List<string> sources, string assemblyName, List<MetadataReference> references)`:
  Compiles the source code directly in memory with the given assembly name and the list of references. Note that there are some assembly references that are automatically used
  because they are required for the compiled Chef code to run.
- `public static (MemoryStream, EmitResult) CompileSources(List<string> sources, string assemblyName, List<MetadataReference> references, List<Action<ChefCompiler>> mods)`:
  The previous but with mods.

## Class `TestSuite<TReturn>`
This represents a list of tests and their results, and can be used to run a ton of Chef/C# code in sequence.

Properties:
- `public IEnumerable<Test> Tests {get; set; }`: The Tests in this test suite.
- `public Dictionary<string, (EmitResult, Exception?, TReturn?)> Results { get; set; }`: The results of all the tests, indexed by ID.
- `public Dictionary<string, string> Logs { get; set; }`: The logs of all the tests, indexed by ID. These logs are generated while an arbitrary TextWriter
  can still be provided, by using a custom TextWriter that forks between a string output and the TextWriter you provide.
- `public Dictionary<string, bool> Successes { get; set; }`: Whether each test was successful, indexed by ID.

Methods:
- `public bool Run(TextWriter? logger)`: Runs all of the tests in the test suite, logging to the provided logger. If all are successful, this
  method will return true, otherwise false.
- `public void ClearResults()`, `ClearLogs`, `ClearSuccesses`, `ClearTests`: Clears the lists/dictionaries of the test suite.
- `public void Clear()`: Clears the results, logs, and successes.
- `public void ClearAll()`: Clears the results, logs, and successes, and also removes all the tests from this test suite.
- `public bool IsValueApplicable(string ID)`: Returns true if the `TReturn?` in the return value tuple of the test with the given ID is safe to use.
  Essentially it checks that the test exists, the compilation was successful, and there were no uncaught exceptions.

### Class `Test`
When running a test, the method `TestMain(TextWriter)` in the class `Program` will be run, and must exist. No other parameters are allowed, and the method must return a TReturn
(the type argument in this classes' enclosing constructed generic class). To pass other data, use the `Input` property. Each test is run in a separate collectible `AssemblyLoadContext` that
is immediately discarded after the test finishes, so they don't bloat your memory with assemblies.

Fields:
- `public TestSuite<TReturn> LatestTestSuite`: The TestSuite<TReturn> that invoked this test.
- `public Compilation Compilation`: The Compilation that compiled this test.

Properties:
- `public List<string> SourceFiles { get; set; }`: The list of source code strings in this test.
- `public List<MetadataReference> MetadataReferences { get; set; }`: The list of assembly references in this test.
- `public string AssemblyName { get; set; }`: The assembly name for this test.
- `public List<Action<ChefCompiler>> Mods { get; set; }`: The mods to apply to this ChefCompiler.
- `public string ID { get; set; }`: A unique ID for this test within the TestSuite<TReturn>
- `public string? Input { get; set; }`: The Console input for this test.
- `public Func<EmitResult, Exception?, TReturn?, Test, TestSuite<TReturn>, bool>? IsSuccessful { get; set; }`: Given the Compilation's EmitResult, the first uncaught Exception
  to occur within the test, the return value of the test if no exceptions occured, the Test itself, and the TestSuite<TReturn> containing the test, return true if
  the test was successful and false otherwise. The whole function can also be null for a test where you just need to see the output.

Methods:
- `public (EmitResult, Exception?, TReturn?) Run(TextWriter? logger)`: Runs the test, optionally writes its output, errors, and compilation stuff to a TextWriter,
  returns the EmitResult of the Compilation, and either an exception thrown by the test or the return value of the test. This won't fill the Dictionaries used
  by the TestSuite.

# Examples
Taken from `StandardTestSuites.cs`.

A simple showcase of the syntax.
```
using Kronosta.ChefCSharpPidgin;
using System.IO;

[ChefClass(Using = """
    using System.IO;
    """
)]
public partial class Program {
    [ChefMethod(
        """
        One Pea.

        Ingredients.
        1 pea
        
        Method.
        Refrigerate!!
        """,
        ReturnTransformer = "\"\""
    )]
    public static partial string TestMain(TextWriter Chef__output);
}
```

All the available commands as of the original release:
```
[Kronosta.ChefCSharpPidgin.ChefClass]
public partial class Program
{
    [Kronosta.ChefCSharpPidgin.ChefMethod(
        """
        Chocolate Pasta.

        Ingredients.
        3 cups chocolate chips
        5 cups pasta dough
        1 dash vanilla extract
        17 cups strawberry ice cream
        5 cups raspberries
        1 cup cream cheese frosting

        Method.
        Take pasta dough from refrigerator!!
        Suggestion: PastaDough = 8!!
        Put chocolate chips into 1st mixing bowl!!
        Suggestion: MB1 = [3]!!
        Clean {(typeof(System.Console).GetMethod({{"WriteLine",new System.Type[]{typeof(string)}).Invoke(null, new object[]{string.Join(", ", Chef__mixingBowlsR[1])})== null ? 3 : 3)}rd mixing bowl!!
        Add strawberry ice cream to 1st mixing bowl!!
        Suggestion: MB1 = [20]!!
        Combine strawberry ice cream into 1st mixing bowl!!
        Suggestion: MB1 = [340]!!
        Remove raspberries from 1st mixing bowl!!
        Suggestion: MB1 = [335]!!
        Divide chocolate chips into 1st mixing bowl!!
        Suggestion: MB1 = [111]!!
        Fold cream cheese frosting into 1st mixing bowl!!
        Suggestion: MB1 = [], CreamCheeseFrosting = 112!!
        Liquefy cream cheese frosting!!
        Add dry ingredients to 1st mixing bowl!!
        Suggestion: MB1 = [33]!!
        Put pasta dough into 1st mixing bowl!!
        Put strawberry ice cream into 1st mixing bowl!!
        Put {"strawb" + $"erry{(char)32}ice cream"} into 1st mixing bowl!!
        Put pasta dough into 1st mixing bowl!!
        Suggestion: MB1 = [33, 8, 17, 17, 8]!!
        Stir chocolate chips into the 1st mixing bowl!!
        Suggestion: MB1 = [33, 8, 8, 17, 17]!!
        Put chocolate chips into the 1st mixing bowl!!
        Suggestion: MB1 = [33, 8, 8, 17, 17, 3]!!
        Stir the 1st mixing bowl for 4 minutes!!
        Suggestion: MB1 = [33, 3, 8, 8, 17, 17]!!
        Put chocolate chips into 2nd mixing bowl!!
        Put chocolate chips into 2nd mixing bowl!!
        Add vanilla extract to 2nd mixing bowl!!
        Put chocolate chips into 2nd mixing bowl!!
        Add vanilla extract to 2nd mixing bowl!!
        Add vanilla extract to 2nd mixing bowl!!
        Put strawberry ice cream into 2nd mixing bowl!!
        Put strawberry ice cream into 2nd mixing bowl!!
        Add vanilla extract to 2nd mixing bowl!!
        Put strawberry ice cream into 2nd mixing bowl!!
        Add vanilla extract to 2nd mixing bowl!!
        Add vanilla extract to 2nd mixing bowl!!
        Put cream cheese frosting into 2nd mixing bowl!!
        Put cream cheese frosting into 2nd mixing bowl!!
        Add vanilla extract to 2nd mixing bowl!!
        Put cream cheese frosting into 2nd mixing bowl!!
        Add vanilla extract to 2nd mixing bowl!!
        Add vanilla extract to 2nd mixing bowl!!
        Suggestion: MB2 = [3, 4, 5, 17, 18, 19, 112, 113, 114]!!
        Mix the 2nd mixing bowl well!!
        Clean {(typeof(System.Console).GetMethod("WriteLine",new System.Type[]{typeof(string)}).Invoke(null, new object[]{"Random mixing bowl: " + string.Join(", ", Chef__mixingBowlsR[2])})== null ? 3 : 3)}rd mixing bowl!!
        Clean 2nd mixing bowl!!
        Clean {(typeof(System.Console).GetMethod("WriteLine",new System.Type[]{typeof(string)}).Invoke(null, new object[]{"Clean mixing bowl: [" + string.Join(", ", Chef__mixingBowlsR[2]) + "]"})== null ? 3 : 3)}rd mixing bowl!!
        Pour contents of the 1st mixing bowl into the 1st baking dish!!
        Suggestion: BD1 = [33, 3, 8, 8, 17, 17]!!
        Clean 1st mixing bowl!!
        Suggestion: MB1 = []!!
        Put cream cheese frosting into 1st mixing bowl!!
        Suggestion: MB1 = ['p']!!
        Rotini the pasta dough!!
            Put pasta dough into the 1st mixing bowl!!
        Twist the pasta dough until rotinied!!
        Suggestion: MB1 = ['p', 8, 7, 6, 5, 4, 3, 2, 1]!!
        Pour contents of the 1st mixing bowl into the 1st baking dish!!
        Suggestion: BD1 = [30, 3, 8, 8, 17, 17, 'p', 8, 7, 6, 5, 4, 3, 2, 1]!!
        Drink the vanilla extract!!
            Suggestion: trust me!!
            Set aside!!
            Pour contents of the 1st mixing bowl into the 1st baking dish!!
            Suggestion: the previous command should never run if Set aside works correctly!!
        Drink until drinked!!
        Refrigerate for 1 hour!!
        Suggestion: 
            output = "1 2 3 4 5 6 7 8 o17 17 8 8 3 33"!!
        """,
        ReturnTransformer = "\"\""
    )]
    public static partial string TestMain(System.IO.TextWriter Chef__output);
}
```

Auxiliary recipe:
```
using Kronosta.ChefCSharpPidgin;
using System.IO;

[ChefClass(Using = """
    using System.IO;
    """
)]
public partial class Program {
    [ChefMethod(
        """
        Sauce Pocket.

        Ingredients.
        5 breadbowls
        2 donuts
        
        Method.
        Put breadbowls into 2nd mixing bowl!!
        Put donuts into 2nd mixing bowl!!
        Pour contents of the 2nd mixing bowl into the 1st baking dish!!
        Suggestion: SaucePocket:MB2 = SaucePocket:BD1 = [5, 2]!!
        Serve with Sauce!!

        Sauce.

        Ingredients.
        19 tomatoes
        1 carrot

        Method.
        Put tomatoes into 1st mixing bowl!!
        Suggestion: Sauce:MB1 = [19]!!
        Add carrot to 2nd mixing bowl!!
        Suggestion: Sauce:MB2 = [5, 3]!!
        Pour contents of the 2nd mixing bowl into the 1st baking dish!!
        Suggestion: Sauce:BD1 = [5, 2, 5, 3]!!
        Refrigerate for 1 hour!!
        Suggestion: Output = "3 5 2 5"!!
        """,
        ReturnTransformer = "\"\""
    )]
    public static partial string TestMain(TextWriter Chef__output);
}
```
