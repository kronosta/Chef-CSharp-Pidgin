using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Kronosta.ChefCSharpPidgin
{
    /// <summary>
    /// This class contains standard testing suites for the Chef-C# Pidgin compile process
    /// </summary>
    public static class StandardTestSuites
    {

        /// <summary>
        /// Holds the main test suite, with an output type of string.
        /// </summary>
        public static TestSuite<string> MainStringTests { get; } = new TestSuite<string>
        {
            Tests = new TestSuite<string>.Test[]
            {
                new TestSuite<string>.Test
                {
                    AssemblyName = "Kronosta.ChefCSharpPidgin.Testing.BareMinimum",
                    ID = "BareMinimum",
                    SourceFiles = new List<string> {
                        $$"""
                        using Kronosta.ChefCSharpPidgin;
                        using System.IO;

                        [ChefClass(Using = {{"\"\"\""}}
                            using System.IO;
                            {{"\"\"\""}}
                        )]
                        public partial class Program {
                            [ChefMethod(
                                {{"\"\"\""}}
                                One Pea.

                                Ingredients.
                                1 pea
                                
                                Method.
                                Refrigerate!!
                                {{"\"\"\""}},
                                ReturnTransformer = "\"\""
                            )]
                            public static partial string TestMain(TextWriter Chef__output);
                        }
                        """
                    },
                    IsSuccessful = (emitResult, exception, result, test, suite) =>
                        emitResult.Success && exception == null && result == ""
                },
                new TestSuite<string>.Test
                {
                    AssemblyName = "Kronosta.ChefCSharpPidgin.Testing.AllCommands",
                    ID = "AllCommands",
                    Input = "8\n",
                    SourceFiles = new List<string>
                    {
                        $$"""
                        [Kronosta.ChefCSharpPidgin.ChefClass]
                        public partial class Program
                        {
                            [Kronosta.ChefCSharpPidgin.ChefMethod(
                                {{"\"\"\""}}
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
                                Clean {(typeof(System.Console).GetMethod({{""
                                }}"WriteLine",{{""
                                }}new System.Type[]{typeof(string)}){{""
                                }}.Invoke(null, new object[]{string.Join(", ", Chef__mixingBowlsR[1])}){{""
                                }}== null ? 3 : 3)}rd mixing bowl!!
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
                                Clean {(typeof(System.Console).GetMethod({{""
                                }}"WriteLine",{{""
                                }}new System.Type[]{typeof(string)}){{""
                                }}.Invoke(null, new object[]{"Random mixing bowl: " + {{""
                                }}string.Join(", ", Chef__mixingBowlsR[2])}){{""
                                }}== null ? 3 : 3)}rd mixing bowl!!
                                Clean 2nd mixing bowl!!
                                Clean {(typeof(System.Console).GetMethod({{""
                                }}"WriteLine",{{""
                                }}new System.Type[]{typeof(string)}){{""
                                }}.Invoke(null, new object[]{"Clean mixing bowl: [" + {{""
                                }}string.Join(", ", Chef__mixingBowlsR[2]) + "]"}){{""
                                }}== null ? 3 : 3)}rd mixing bowl!!
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
                                {{"\"\"\""}},
                                ReturnTransformer = "\"\""
                            )]
                            public static partial string TestMain(System.IO.TextWriter Chef__output);
                        }
                        """,
                    },
                    IsSuccessful = (emitResult, exception, result, test, suite) =>
                        suite.Logs[test.ID].Contains("1 2 3 4 5 6 7 8 o17 17 8 8 3 33")
                },
                new TestSuite<string>.Test
                {
                    AssemblyName = "Kronosta.ChefCSharpPidgin.Testing.AuxRecipe",
                    ID = "AuxRecipe",
                    Input = "",
                    SourceFiles = new List<string>
                    {
                        $$"""
                        using Kronosta.ChefCSharpPidgin;
                        using System.IO;

                        [ChefClass(Using = {{"\"\"\""}}
                            using System.IO;
                            {{"\"\"\""}}
                        )]
                        public partial class Program {
                            [ChefMethod(
                                {{"\"\"\""}}
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
                                {{"\"\"\""}},
                                ReturnTransformer = "\"\""
                            )]
                            public static partial string TestMain(TextWriter Chef__output);
                        }
                        """
                    },
                    IsSuccessful = (emitResult, exception, result, test, suite) =>
                        suite.Logs[test.ID].Contains("3 5 2 5")
                }
            }.Concat(
                new List<Tuple<int, int, int, int, int>>
                {
                    Tuple.Create(9, 3, 4, 7, 1),
                    Tuple.Create(9, 0, 0, 1, 1),
                    Tuple.Create(9, 0, 0, 0, 1),
                    Tuple.Create(9, 0, 0, 1, 0),
                    Tuple.Create(9, 0, 0, 0, 0),
                    Tuple.Create(-2, -3, 4, 5, 6)
                }
                .Select(tuple => Tuple.Create(
                    tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5,
                    (tuple.Item1 + "_"
                    + tuple.Item2 + "_"
                    + tuple.Item3 + "_"
                    + tuple.Item4 + "_"
                    + tuple.Item5 + "_").Replace('-', 'N'))
                )
                .Select(tuple =>
                    new TestSuite<string>.Test {
                        AssemblyName = "Kronosta.ChefCSharpPlugin.Testing.Math_" + tuple.Item6,
                        ID = $"Math_" + tuple.Item6,
                        Input = $"{tuple.Item1}\n{tuple.Item2}\n{tuple.Item3}\n{tuple.Item4}\n{tuple.Item5}\n",
                        IsSuccessful = (emitResult, exception, result, test, suite) =>
                        {
                            //Test of failure formatting
                            //if (tuple.Item1 == 9 && tuple.Item4 == 1 && tuple.Item5 == 1) return false;
                            //return false;
                            if (tuple.Item5 == 0)
                                return emitResult.Success && exception != null && exception is DivideByZeroException;
                            else
                                return emitResult.Success && exception == null && result ==
                                    $"({(((tuple.Item1 + tuple.Item2) - tuple.Item3) * tuple.Item4) / tuple.Item5}, False)";
                        },
                        SourceFiles = new List<string> {
                            $$"""
                            using Kronosta.ChefCSharpPidgin;
                            using System.IO;

                            [ChefClass(Using = {{"\"\"\""}}
                                using System.IO;
                                using System;
                                {{"\"\"\""}}
                            )]
                            public partial class Program {
                                [ChefMethod(
                                    {{"\"\"\""}}
                                    Math Pancakes.

                                    Ingredients.
                                    3 tablespoons syrup
                                    5 cups waffle mix
                                    1 egg
                                    1 stick of butter
                                    2 cups blueberries
                                    2 cups blackberries

                                    Method.
                                    Take waffle mix from refrigerator!!
                                    Take egg from refrigerator!!
                                    Take stick of butter from refrigerator!!
                                    Take blueberries from refrigerator!!
                                    Take blackberries from refrigerator!!
                                    Put blackberries into 2nd mixing bowl!!
                                    Put blueberries into 2nd mixing bowl!!
                                    Put stick of butter into 2nd mixing bowl!!
                                    Put egg into 2nd mixing bowl!!
                                    Put waffle mix into 2nd mixing bowl!!
                                    Pour contents of the 2nd mixing bowl into the 1st baking dish!!
                                    Put waffle mix into 1st mixing bowl!!
                                    Add egg to 1st mixing bowl!!
                                    Clean {(typeof(Console).GetMethod({{""
                                    }}"WriteLine",{{""
                                    }}new Type[]{typeof(string)}){{""
                                    }}.Invoke(null, new object[]{string.Join(", ", Chef__mixingBowlsR[1])}){{""
                                    }}== null ? 3 : 3)}rd mixing bowl!!
                                    Remove stick of butter from 1st mixing bowl!!
                                    Clean {(typeof(Console).GetMethod({{""
                                    }}"WriteLine",{{""
                                    }}new Type[]{typeof(string)}){{""
                                    }}.Invoke(null, new object[]{string.Join(", ", Chef__mixingBowlsR[1])}){{""
                                    }}== null ? 3 : 3)}rd mixing bowl!!
                                    Combine blueberries into 1st mixing bowl!!
                                    Clean {(typeof(Console).GetMethod({{""
                                    }}"WriteLine",{{""
                                    }}new Type[]{typeof(string)}){{""
                                    }}.Invoke(null, new object[]{string.Join(", ", Chef__mixingBowlsR[1])}){{""
                                    }}== null ? 3 : 3)}rd mixing bowl!!
                                    Divide blackberries into 1st mixing bowl!!
                                    Clean {(typeof(Console).GetMethod({{""
                                    }}"WriteLine",{{""
                                    }}new Type[]{typeof(string)}){{""
                                    }}.Invoke(null, new object[]{string.Join(", ", Chef__mixingBowlsR[1])}){{""
                                    }}== null ? 3 : 3)}rd mixing bowl!!
                                    Fold syrup into 1st mixing bowl!!
                                    Refrigerate for 1 hour!!
                                    {{"\"\"\""}},
                                    ReturnTransformer = "$\"{Chef__mainResult.Item1[\"syrup\"]}\""
                                )]
                                public static partial string TestMain(TextWriter? Chef__output);
                            }
                            """
                        }
                    }
                )
            )
        };
    }
}
