using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Kronosta.ChefCSharpPidgin
{
    public class ChefCompiler
    {
        public delegate void IngredientEmitterFunc(
            ChefCompiler compiler, 
            string line,
            Regex regex,
            StringBuilder builder,
            string recipeName,
            bool isMain,
            Dictionary<string, object> extraParams);
        public delegate Tuple<string, bool, string> MeasurementHandler(
            ChefCompiler compiler,
            Dictionary<string, List<string>> args,
            Tuple<string, bool, string> startingValue_wet_heapedLevel,
            Dictionary<string, object> extraParams);
        public delegate Tuple<string, bool> HeapedLevelHandler(
            ChefCompiler compiler,
            Dictionary<string, List<string>> args,
            Tuple<string, bool> startingValue_wet,
            Dictionary<string, object> extraParams);
        public delegate string CustomIngredientEmissionHandler(
            ChefCompiler compiler,
            string startingValue,
            string heapedLeve,
            string measurement,
            string name,
            Dictionary<string, object> prevExtraParams,
            Dictionary<string, object> extraParams);

        public class CommandHandler
        {
            public delegate bool Predicate(
                ChefCompiler compiler,
                string[] commands,
                int commandIndex,
                Dictionary<string, object> extraParams );
            public delegate string Emitter(
                ChefCompiler compiler,
                string[] commands,
                int commandIndex,
                Dictionary<string, object> extraParams );

            public String Name;
            public Predicate ShouldRunEmit;
            public Emitter Emit;

            public CommandHandler(String name, Predicate shouldRunEmit, Emitter emit)
            {
                Name = name;
                ShouldRunEmit = shouldRunEmit;
                Emit = emit;
            }

        }
        public List<string> args { get; set; }
        public static Regex IngredientRegexTypical { get; set; } =
            new Regex("^\\s*((.*?)\\s+)?" +
                      "((heaped|level|@<[^>]*>)\\s+)?" +
                      "((g|kg|pinch|pinches|ml|l|dash|dashes|cup|cups|teaspoons?|tablespoons?|@<[^>]*>)\\s+)?" +
                      "(.*)$");
        public Regex IngredientRegex { get; set; } = IngredientRegexTypical;
        public static IngredientEmitterFunc IngredientEmitterTypical { get; set; } =
            (chefCompiler, line, regex, builder, recipeName, isMain, extraParams) =>
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    string startingValue = match.Groups[2].Value;
                    string heapedLevel = match.Groups[4].Value;
                    string measurement = match.Groups[6].Value;
                    string name = match.Groups[7].Value;
                    bool wet = false;
                    CustomIngredientEmissionHandler? custom = null;
                    var extraHandlerParams = new Dictionary<string, object>();
                    if (measurement == "ml" ||
                        measurement == "l" ||
                        measurement == "dash" ||
                        measurement == "dashes") wet = true;
                    else if (measurement.StartsWith("@"))
                    {
                        var handlerInfo = GetCustomizedSyntax(measurement);
                        (startingValue, wet, heapedLevel) =
                            chefCompiler.CustomMeasurementHandlers[handlerInfo.Item1](
                                chefCompiler,
                                handlerInfo.Item2,
                                Tuple.Create(startingValue, wet, heapedLevel),
                                extraHandlerParams);
                    }
                    if (heapedLevel == "heaped" || heapedLevel == "level")
                        wet = false;
                    else if (heapedLevel.StartsWith("@"))
                    {
                        var handlerInfo = GetCustomizedSyntax(heapedLevel);
                        (startingValue, wet) =
                            chefCompiler.CustomHeapedLevelHandlers[handlerInfo.Item1](
                                chefCompiler,
                                handlerInfo.Item2,
                                Tuple.Create(startingValue, wet),
                                extraHandlerParams);
                    }
                    if (extraHandlerParams.ContainsKey("Chef__customEmit") &&
                        extraHandlerParams["Chef__customEmit"] is CustomIngredientEmissionHandler)
                        custom = (CustomIngredientEmissionHandler)extraHandlerParams["Chef__customEmit"];
                    if (custom != null)
                    {
                        var newExtraHandlerParams = new Dictionary<string, object>();
                        builder.AppendLine(custom(chefCompiler, startingValue, heapedLevel, measurement, name, extraHandlerParams, newExtraHandlerParams));
                    }
                    else
                    {
                        builder.AppendLine($"""
                        Chef__ingredientsUnderlayR[$"{name}"] = System.ValueTuple.Create<int, bool>(int.Parse($"{startingValue}"), {("" + wet).ToLower()});
                        """);
                    }
                }
            };
        public IngredientEmitterFunc IngredientEmitter { get; set; } = IngredientEmitterTypical;
        public IDictionary<string, MeasurementHandler> CustomMeasurementHandlers { get; set; } =
            new Dictionary<string, MeasurementHandler>();
        public IDictionary<string, HeapedLevelHandler> CustomHeapedLevelHandlers { get; set; } =
            new Dictionary<string, HeapedLevelHandler>();
        public IEnumerable<CommandHandler> CommandHandlers { get; set; } =
            new List<CommandHandler>
            {
                new CommandHandler(
                    "Chef__empty",
                    (compiler, commands, commandIndex, extraParams) =>
                        commands[commandIndex] == "",
                    (compiler, commands, commandIndex, extraParams) => ""
                ),
                new CommandHandler(
                    "Chef__suggestion",
                    (compiler, commands, commandIndex, extraParams) =>
                        commands[commandIndex].StartsWith("Suggestion:"),
                    (compiler, commands, commandIndex, extraParams) => ""
                ),
                new CommandHandler(
                    "Chef__takeFromRefrigerator",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Take ([^;]{0,300}) from (the )?refrigerator$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Take ([^;]{0,300}) from (the )?refrigerator$");
                        return $$"""
                        if (Chef__input != null)
                        {
                            int Chef__local_inputInt = 0;
                            if (int.TryParse(Chef__input.ReadLine(), out int Chef__local_inputInt2))
                                Chef__local_inputInt = Chef__local_inputInt2;
                            Chef__ingredientsR[$"{{match.Groups[1].Value}}"] =
                                new System.ValueTuple<int, bool>(Chef__local_inputInt, false);
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__putInto",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Put (the )?([^;]{0,300}) into (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Put (the )?([^;]{0,300}) into (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$");
                        return $$"""
                        {
                            int Chef__local_n = int.Parse($"{{(match.Groups[4].Value == "" ? "1" : match.Groups[5].Value)}}");
                            string Chef__local_ingredient = $"{{match.Groups[2].Value}}";
                            if (!Chef__mixingBowlsR.ContainsKey(Chef__local_n))
                                Chef__mixingBowlsR[Chef__local_n] = new System.Collections.Generic.List<System.ValueTuple<int, bool>>();
                            if (Chef__ingredientsR.ContainsKey(Chef__local_ingredient))
                                Chef__mixingBowlsR[Chef__local_n].Add(Chef__ingredientsR[Chef__local_ingredient]);
                            else
                                Chef__mixingBowlsR[Chef__local_n].Add(new System.ValueTuple<int, bool>(0, false));
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__foldInto",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Fold (the )?([^;]{0,300}) into (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Fold (the )?([^;]{0,300}) into (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$");
                        return $$"""
                        {
                            int Chef__local_n = int.Parse($"{{(match.Groups[4].Value == "" ? "1" : match.Groups[5].Value)}}");
                            string Chef__local_ingredient = $"{{match.Groups[2].Value}}";
                            if (Chef__mixingBowlsR.ContainsKey(Chef__local_n) && Chef__mixingBowlsR[Chef__local_n].Count > 0)
                            {
                                Chef__ingredientsR[Chef__local_ingredient] =
                                    Chef__mixingBowlsR[Chef__local_n][Chef__mixingBowlsR[Chef__local_n].Count - 1];
                                Chef__mixingBowlsR[Chef__local_n].RemoveAt(Chef__mixingBowlsR[Chef__local_n].Count - 1);
                            }
                            else
                            {
                                Chef__ingredientsR[Chef__local_ingredient] = new System.ValueTuple<int, bool>(0, false);
                            }
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__addDryIngredients",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Add dry ingredients( to (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl)?$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Add dry ingredients( to (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl)?$");
                        return $$"""
                        {
                            int Chef__local_n = int.Parse($"{{(match.Groups[3].Value == "" ? "1" : match.Groups[4].Value)}}");
                            int Chef__local_total = 0;
                            foreach (var pair in Chef__ingredientsR)
                                if (!pair.Value.Item2)
                                    Chef__local_total += pair.Value.Item1;
                            if (!Chef__mixingBowlsR.ContainsKey(Chef__local_n))
                                Chef__mixingBowlsR[Chef__local_n] = new System.Collections.Generic.List<System.ValueTuple<int, bool>>();
                            Chef__mixingBowlsR[Chef__local_n].Add(System.ValueTuple.Create(Chef__local_total, false));
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__mathStackOperation",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^(Add|Remove|Combine|Divide) (the )?([^;]{0,300}?)( (to|from|into) (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl)?$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^(Add|Remove|Combine|Divide) (the )?([^;]{0,300}?)( (to|from|into) (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl)?$");
                        string op = match.Groups[1].Value switch
                        {
                            "Add" => "+",
                            "Remove" => "-",
                            "Combine" => "*",
                            "Divide" => "/",
                            _ => "+"
                        };
                        return $$"""
                        {
                            int Chef__local_n = int.Parse($"{{(match.Groups[8].Value == "" ? "1" : match.Groups[8].Value)}}");
                            string Chef__local_ingredient = $"{{match.Groups[3].Value}}";
                            if (!Chef__mixingBowlsR.ContainsKey(Chef__local_n))
                                Chef__mixingBowlsR[Chef__local_n] = new System.Collections.Generic.List<System.ValueTuple<int, bool>>();
                            if (Chef__mixingBowlsR[Chef__local_n].Count == 0)
                                Chef__mixingBowlsR[Chef__local_n].Add(new System.ValueTuple<int, bool>(0, false));
                            if (Chef__ingredientsR.ContainsKey(Chef__local_ingredient))
                            {
                                Chef__mixingBowlsR[Chef__local_n][Chef__mixingBowlsR[Chef__local_n].Count - 1] =
                                    new System.ValueTuple<int, bool>(
                                        Chef__mixingBowlsR[Chef__local_n][Chef__mixingBowlsR[Chef__local_n].Count - 1].Item1
                                            {{op}} Chef__ingredientsR[Chef__local_ingredient].Item1,
                                        Chef__mixingBowlsR[Chef__local_n][Chef__mixingBowlsR[Chef__local_n].Count - 1].Item2
                                    );
                            }
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__liquefyIngredient",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Liqu[ei]fy (the )?([^;]{0,300})$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Liqu[ei]fy (the )?([^;]{0,300})$");
                        return $$"""
                        {
                            string Chef__local_ingredient = $"{{match.Groups[2].Value}}";
                            if (Chef__ingredientsR.ContainsKey(Chef__local_ingredient))
                                Chef__ingredientsR[Chef__local_ingredient] =
                                    new System.ValueTuple<int, bool>(Chef__ingredientsR[Chef__local_ingredient].Item1, true);
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__liquefyContents",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Liqu[ei]fy contents of (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Liqu[ei]fy contents of (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$");
                        return $$"""
                        {
                            int Chef__local_n = int.Parse($"{{(match.Groups[2].Value == "" ? "1" : match.Groups[3].Value)}}");
                            if (Chef__mixingBowlsR.ContainsKey(Chef__local_n))
                            {
                                var Chef__local_bowl = Chef__mixingBowlsR[Chef__local_n];
                                for (int Chef__local_i = 0; Chef__local_i < Chef__local_bowl.Count; Chef__local_i++)
                                    Chef__local_bowl[Chef__local_i] = new System.ValueTuple(Chef__local_bowl[Chef__local_i].Item1, true);
                            }
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__stirMinutes",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Stir( (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl)? for ([^;]{0,300}) minutes$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Stir( (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl)? for ([^;]{0,300}) minutes$");
                        return $$"""
                        {
                            int Chef__local_n = int.Parse($"{{(match.Groups[3].Value == "" ? "1" : match.Groups[4].Value)}}");
                            int Chef__local_depth = int.Parse($"{{match.Groups[6].Value}}");
                            if (Chef__mixingBowlsR.ContainsKey(Chef__local_n) && Chef__mixingBowlsR[Chef__local_n].Count > Chef__local_depth)
                            {
                                var Chef__local_bowl = Chef__mixingBowlsR[Chef__local_n];
                                System.ValueTuple<int, bool> Chef__local_bowlTop = Chef__local_bowl[Chef__local_bowl.Count - 1];
                                Chef__local_bowl.RemoveAt(Chef__local_bowl.Count - 1);
                                Chef__local_bowl.Insert(Chef__local_bowl.Count - Chef__local_depth, Chef__local_bowlTop);
                            }
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__stirIngredient",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Stir (the )?([^;]{0,300}) into (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Stir (the )?([^;]{0,300}) into (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$");
                        return $$"""
                        {
                            int Chef__local_n = int.Parse($"{{(match.Groups[4].Value == "" ? "1" : match.Groups[5].Value)}}");
                            string Chef__local_ingredient = $"{{match.Groups[2].Value}}";
                            if (Chef__mixingBowlsR.ContainsKey(Chef__local_n) &&
                                Chef__mixingBowlsR[Chef__local_n].Count > Chef__ingredientsR[Chef__local_ingredient].Item1 &&
                                Chef__ingredientsR.ContainsKey(Chef__local_ingredient))
                            {
                                var Chef__local_bowl = Chef__mixingBowlsR[Chef__local_n];
                                System.ValueTuple<int, bool> Chef__local_bowlTop = Chef__local_bowl[Chef__local_bowl.Count - 1];
                                Chef__local_bowl.RemoveAt(Chef__local_bowl.Count - 1);
                                Chef__local_bowl.Insert(Chef__local_bowl.Count - Chef__ingredientsR[Chef__local_ingredient].Item1, Chef__local_bowlTop);
                            }
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__mixWell",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Mix( (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl)? well$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Mix( (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl)? well$");
                        return $$"""
                        {
                            int Chef__local_n = int.Parse($"{{(match.Groups[3].Value == "" ? "1" : match.Groups[4].Value)}}");
                            System.Random Chef__local_rng = new System.Random((int)System.DateTime.Now.Ticks);
                            if (Chef__mixingBowlsR.ContainsKey(Chef__local_n))
                            {
                                var Chef__local_bowl = Chef__mixingBowlsR[Chef__local_n];
                                int Chef__local_n2 = Chef__local_bowl.Count;  
                                while (Chef__local_n2 > 1) {  
                                    Chef__local_n2--;  
                                    int Chef__local_k = Chef__local_rng.Next(Chef__local_n2 + 1);  
                                    System.ValueTuple<int, bool> Chef__local_value = Chef__local_bowl[Chef__local_k];  
                                    Chef__local_bowl[Chef__local_k] = Chef__local_bowl[Chef__local_n2];  
                                    Chef__local_bowl[Chef__local_n2] = Chef__local_value;  
                                }  
                            }
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__clean",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Clean (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Clean (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl$");
                        return $$"""
                        {
                            Chef__mixingBowlsR[int.Parse($"{{(match.Groups[2].Value == "" ? "1" : match.Groups[3].Value)}}")]
                                = new System.Collections.Generic.List<System.ValueTuple<int, bool>>();
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__pourContents",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Pour contents of (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl into (the )?(([^;]{0,300})(st|nd|rd|th))? baking dish$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Pour contents of (the )?(([^;]{0,300})(st|nd|rd|th))? mixing bowl into (the )?(([^;]{0,300})(st|nd|rd|th))? baking dish$");
                        return $$"""
                        {
                            int Chef__local_n = int.Parse($"{{(match.Groups[2].Value == "" ? "1" : match.Groups[3].Value)}}");
                            int Chef__local_p = int.Parse($"{{(match.Groups[6].Value == "" ? "1" : match.Groups[7].Value)}}");
                            if (!Chef__mixingBowlsR.ContainsKey(Chef__local_n))
                                Chef__mixingBowlsR[Chef__local_n] = new System.Collections.Generic.List<System.ValueTuple<int, bool>>();
                            if (!Chef__bakingDishesR.ContainsKey(Chef__local_p))
                                Chef__bakingDishesR[Chef__local_p] = new System.Collections.Generic.List<System.ValueTuple<int, bool>>();
                            Chef__bakingDishesR[Chef__local_p].AddRange(Chef__mixingBowlsR[Chef__local_n]);
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__setAside",
                    (compiler, commands, commandIndex, extraParams) =>
                        commands[commandIndex] == "Set aside",
                    (compiler, commands, commandIndex, extraParams) => "break;"
                ),
                new CommandHandler(
                    "Chef__refrigerateHours",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Refrigerate for ([^;]{0,300}) hours?$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Refrigerate for ([^;]{0,300}) hours?$");
                        return $$"""
                        if (Chef__output != null)
                        {
                            int Chef__local_hours = int.Parse($"{{match.Groups[1].Value}}");
                            for (int Chef__local_i = 1; Chef__local_i <= Chef__local_hours; Chef__local_i++)
                                if (Chef__bakingDishesR.ContainsKey(Chef__local_i))
                                    for (int Chef__local_j = Chef__bakingDishesR[Chef__local_i].Count - 1; Chef__local_j >= 0; Chef__local_j--)
                                        if (Chef__bakingDishesR[Chef__local_i][Chef__local_j].Item2)
                                            Chef__output.Write((char)Chef__bakingDishesR[Chef__local_i][Chef__local_j].Item1);
                                        else
                                            Chef__output.Write(Chef__bakingDishesR[Chef__local_i][Chef__local_j].Item1 + " ");
                        }
                        return System.ValueTuple.Create(Chef__ingredientsR, Chef__mixingBowlsR, Chef__bakingDishesR);
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__refrigerate",
                    (compiler, commands, commandIndex, extraParams) =>
                        commands[commandIndex] == "Refrigerate",
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        return $$"""
                        return System.ValueTuple.Create(Chef__ingredientsR, Chef__mixingBowlsR, Chef__bakingDishesR);
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__serveWith",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^Serve with ([^;]{0,300})$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^Serve with ([^;]{0,300})$");
                        return $$"""
                        {
                            string Chef__local_recipe = $"{{match.Groups[1].Value}}";
                            if (Chef__recipes.ContainsKey(Chef__local_recipe))
                            {
                                var Chef__local_result = Chef__recipes[Chef__local_recipe](System.ValueTuple.Create(
                                    Chef__ingredientsR,
                                    System.Linq.Enumerable.ToDictionary(Chef__mixingBowlsR),
                                    System.Linq.Enumerable.ToDictionary(Chef__bakingDishesR)));
                                Chef__local["Chef__mostRecentAuxRecipeResult"] = Chef__local_result;
                                if (Chef__local_result.Item2.ContainsKey(1))
                                {
                                    if (!Chef__mixingBowlsR.ContainsKey(1))
                                        Chef__mixingBowlsR[1] = new System.Collections.Generic.List<System.ValueTuple<int, bool>>();
                                    Chef__mixingBowlsR[1].AddRange(Chef__local_result.Item2[1]);
                                }
                            }
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__verbIngredientUntilVerbed",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^[a-zA-Z]+? (the )?([^;]{1,300})? until [a-zA-Z]+ed$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^[a-zA-Z]+? (the )?([^;]{1,300})? until [a-zA-Z]+ed$");
                        return $$"""
                        {
                            string Chef__local_ingredient = $"{{match.Groups[2].Value}}";
                            if (Chef__ingredientsR.ContainsKey(Chef__local_ingredient))
                                Chef__ingredientsR[Chef__local_ingredient] =
                                    System.ValueTuple.Create(
                                        Chef__ingredientsR[Chef__local_ingredient].Item1 - 1,
                                        Chef__ingredientsR[Chef__local_ingredient].Item2);    
                        }
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__verbUntilVerbed",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^[a-zA-Z]+ until [a-zA-Z]+ed$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^[a-zA-Z]+ until [a-zA-Z]+ed$");
                        return $$"""
                        }
                        """;
                    }
                ),
                new CommandHandler(
                    "Chef__verb",
                    (compiler, commands, commandIndex, extraParams) =>
                        Regex.IsMatch(commands[commandIndex], "^[a-zA-Z]+ (the )?([^;]{0,300})$"),
                    (compiler, commands, commandIndex, extraParams) =>
                    {
                        Match match = Regex.Match(commands[commandIndex], "^[a-zA-Z]+ (the )?([^;]{0,300})$");
                        return $$"""
                        while (Chef__ingredientsR.ContainsKey($"{{match.Groups[2].Value}}") && Chef__ingredientsR[$"{{match.Groups[2].Value}}"].Item1 != 0)
                        {
                        """;
                    }
                ),
            };

        public ChefCompiler(List<string> args) {
            this.args = args;
        }

        public string Compile(
            IMethodSymbol methodSymbol,
            string chefCode,
            string ingredientTransformer,
            string mixingBowlTransformer,
            string bakingDishTransformer,
            string returnTransformer)
        {
            StringBuilder builder = new StringBuilder();
            chefCode = Utils.NormalizeNewlines(chefCode);
            ImmutableArray<IParameterSymbol> parameters = methodSymbol.Parameters;
            bool hasIngredientsParameter = parameters.Any(param =>
                param.Name == "Chef__ingredients" &&
                param.Type.IsType(typeof(IDictionary<string, ValueTuple<int, bool>>)));
            bool hasMixingBowlsParameter = parameters.Any(param =>
                param.Name == "Chef__mixingBowls" &&
                param.Type.IsType(typeof(IDictionary<int, Stack<ValueTuple<int, bool>>>)));
            bool hasBakingDishesParameter =
                parameters.Any(param =>
                param.Name == "Chef__bakingDishes" &&
                param.Type.IsType(typeof(IDictionary<int, Stack<ValueTuple<int, bool>>>)));
            bool hasInputParameter =
                parameters.Any(param =>
                param.Name == "Chef__input" &&
                param.Type.IsType(typeof(TextReader)));
            bool hasOutputParameter =
                parameters.Any(param =>
                param.Name == "Chef__output" &&
                param.Type.IsType(typeof(TextWriter)));
            if (!hasIngredientsParameter)
                builder.AppendLine("""
                    System.Collections.Generic.IDictionary<string, System.ValueTuple<int, bool>> Chef__ingredients =
                        new System.Collections.Generic.Dictionary<string, System.ValueTuple<int, bool>>();
                    """);
            if (!hasMixingBowlsParameter)
                builder.AppendLine("""
                    System.Collections.Generic.IDictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>> Chef__mixingBowls =
                        new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>>();
                    """);
            if (!hasBakingDishesParameter)
                builder.AppendLine("""
                    System.Collections.Generic.IDictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>> Chef__bakingDishes =
                        new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>>();
                    """);
            if (!hasInputParameter)
                builder.AppendLine("""
                    System.IO.TextReader Chef__input = System.Console.In;
                    """);
            if (!hasOutputParameter)
                builder.AppendLine("""
                    System.IO.TextWriter Chef__output = System.Console.Out;
                    """);
            builder.AppendLine("""
                var Chef__local = new System.Collections.Generic.Dictionary<string, object>();
                """);
            if (ingredientTransformer != "")
                builder.AppendLine($"Chef__ingredients = {ingredientTransformer};");
            if (mixingBowlTransformer != "")
                builder.AppendLine($"Chef__mixingBowls = {mixingBowlTransformer};");
            if (bakingDishTransformer != "")
                builder.AppendLine($"Chef__bakingDishes = {bakingDishTransformer};");
            List<List<string>> recipes =
                chefCode
                .Split("\n\n")
                .Aggregate(
                    new List<List<string>>(),
                    (acc, x) =>
                    {
                        if (Regex.IsMatch(x, "^\\s*[-,:;A-Za-z0-9 ]+\\.\\s*$"))
                        {
                            acc.Add(new List<string> { x });
                            return acc;
                        }
                        acc[acc.Count - 1].Add(x);
                        return acc;
                    }
                );
            string mainRecipeName = recipes[0][0].Substring(0, recipes[0][0].Length - 1);
            string recipeDictionaryType = """
                System.Collections.Generic.#Dictionary<string, System.Func<
                    System.ValueTuple<
                        System.Collections.Generic.IDictionary<string, System.ValueTuple<int, bool>>,
                        System.Collections.Generic.IDictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>>,
                        System.Collections.Generic.IDictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>>
                    >,
                    System.ValueTuple<
                        System.Collections.Generic.IDictionary<string, System.ValueTuple<int, bool>>,
                        System.Collections.Generic.IDictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>>,
                        System.Collections.Generic.IDictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>>
                    >
                >>
                """;
            builder.AppendLine($$"""
                {{recipeDictionaryType.Replace('#', 'I')}} Chef__recipes = new {{recipeDictionaryType.Replace("#", "")}}();
                """);
            foreach (var recipe in recipes)
            {
                string recipeName = recipe[0].Trim().Substring(0, recipe[0].Trim().Length - 1);
                builder.AppendLine($$"""
                    Chef__recipes["{{recipeName}}"] = Chef__copyState => {
                        System.Collections.Generic.IDictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>> Chef__mixingBowlsR =
                            System.Linq.Enumerable.ToDictionary(Chef__copyState.Item2);
                        System.Collections.Generic.IDictionary<int, System.Collections.Generic.List<System.ValueTuple<int, bool>>> Chef__bakingDishesR =
                            System.Linq.Enumerable.ToDictionary(Chef__copyState.Item3);
                        var Chef__localR = new System.Collections.Generic.Dictionary<string, object>();
                    """);
                string ingredientsList = recipe
                    .Select(x => x.Trim())
                    .Where(x => x.StartsWith("Ingredients."))
                    .Append("")
                    .First();
                builder.AppendLine(
                    Utils.Indent(
                        EmitIngredients(ingredientsList, hasIngredientsParameter, recipeName, recipeName == mainRecipeName)));
                string methodList = recipe
                    .Select(x => x.Trim())
                    .Where(x => x.StartsWith("Method."))
                    .Append("")
                    .First();
                builder.AppendLine(
                    Utils.Indent(
                        EmitMethod(methodList, recipeName, recipeName == mainRecipeName)));
                builder.AppendLine(ChefGenerator.Indent + "return System.ValueTuple.Create(Chef__ingredientsR, Chef__mixingBowlsR, Chef__bakingDishesR);");
                builder.AppendLine("""
                    };
                    """);
            }
            builder.AppendLine($$"""
                var Chef__mainResult =
                    Chef__recipes["{{mainRecipeName}}"](System.ValueTuple.Create(Chef__ingredients, Chef__mixingBowls, Chef__bakingDishes));
                """);
            if (returnTransformer != "")
                builder.AppendLine($"return {returnTransformer};");
            return builder.ToString();
        }

        public string EmitIngredients(string ingredients, bool hasIngredientsParameter, string recipeName, bool isMain)
        {
            StringBuilder builder = new StringBuilder();
            if (!hasIngredientsParameter) builder.AppendLine($$"""
                System.Collections.Generic.IDictionary<string, System.ValueTuple<int, bool>> Chef__ingredientsR =
                    new System.Collections.Generic.Dictionary<string, System.ValueTuple<int, bool>>();
                """);
            if (ingredients != "")
            {
                builder.AppendLine($$"""
                {
                    System.Collections.Generic.IDictionary<string, System.ValueTuple<int, bool>> Chef__ingredientsUnderlayR =
                        new System.Collections.Generic.Dictionary<string, System.ValueTuple<int, bool>>();
                """);
                foreach (string line in ingredients.Split("\n").Skip(1))
                {
                    var extraParams = new Dictionary<string, object>();
                    IngredientEmitter(this, line, IngredientRegex, builder, recipeName, isMain, extraParams);
                }
                builder.AppendLine($$"""
                    foreach (var pair in Chef__ingredients)
                        Chef__ingredientsUnderlayR[pair.Key] = pair.Value;
                    Chef__ingredientsR = Chef__ingredientsUnderlayR;
                }
                """);
            }
            return builder.ToString();
        }

        public string EmitMethod(string methodList, string recipeName, bool isMain)
        {
            if (methodList == "") return "";
            StringBuilder builder = new StringBuilder();
            string[] commands = methodList
                .Trim()
                .Substring(8) //Remove "Method.\n"
                .Split("!!")
                .Select(x => x.Trim())
                .ToArray();
            IEnumerable<string> emittedCommands =
                Enumerable.Range(0, commands.Length)
                .Select(i => (i, new Dictionary<string, object>()))
                .Select(i => 
                    $"/* {commands[i.Item1].Replace("*/", "*_/")} */\n" +
                    CommandHandlers.Append(
                        new CommandHandler(
                            "Chef__dummy_nonMatch",
                            (compiler, commands, commandIndex, extraParams) => true,
                            (compiler, commands, commandIndex, extraParams) => $"throw new System.Exception(@\"Invalid command: {commands[commandIndex].Replace("\"", "\"\"")}\");"
                        )
                    )
                    .First(
                        handler => handler.ShouldRunEmit(this, commands, i.Item1, i.Item2)
                    ).Emit(this, commands, i.Item1, i.Item2)
                );
            foreach (var code in emittedCommands)
            {
                builder.AppendLine(code);
            }
            return builder.ToString();
        }


        public static Tuple<string, Dictionary<string, List<string>>?> GetCustomizedSyntax(string syntax)
        {
            if (!Regex.IsMatch(syntax, "@<[^>]*>"))
                throw new ArgumentException("Customized syntax must be enclosed in @< and >.");
            if (syntax.Contains("%"))
            {
                string handlerName = syntax.Substring(2, syntax.IndexOf("%"));
                Dictionary<string, List<string>> args =
                syntax
                    .Substring(syntax.IndexOf("%") + 1)
                    .Split('%')
                    .Select(x =>
                        x.Contains("=")
                        ? new KeyValuePair<string, string>(
                            x.Substring(0, x.IndexOf("=")),
                            x.Substring(x.IndexOf("=") + 1)
                        )
                        : new KeyValuePair<string, string>(x, "")
                    )
                    .GroupBy(x => x.Key)
                    .Select(x =>
                        new KeyValuePair<string, List<string>>(
                            x.Key,
                            x.Select(x2 => x2.Value).ToList()
                        )
                    )
                    .ToDictionary();
                return Tuple.Create<string, Dictionary<string, List<string>>?>
                    (handlerName, args);
            }
            else
            {
                return Tuple.Create<string, Dictionary<string, List<string>>?>
                    (syntax.Substring(2, syntax.Length - 3), null);
            }
        }
    }
}
