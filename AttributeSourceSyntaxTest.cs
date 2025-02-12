#if ALWAYS_OFF
// This file is for testing syntax errors
using System;
using System.Diagnostics;
namespace Kronosta.ChefCSharpPidgin
{
    [Conditional("COMPILE_TIME_ONLY")]
    [System.AttributeUsage(System.AttributeTargets.Class
    /**/                 | System.AttributeTargets.Struct
    /**/                 | System.AttributeTargets.Interface)]
    internal sealed class ChefClassAttribute : Attribute
    {
        public string[] GlobalChefCompilerArgs;
        public string Using;

        public ChefClassAttribute()
        {
            GlobalChefCompilerArgs = new string[0];
            Using = "";
        }
    }

    [Conditional("COMPILE_TIME_ONLY")]
    [System.AttributeUsage(System.AttributeTargets.Method)]
    internal sealed class ChefMethodAttribute : Attribute
    {
        private string ChefCode;
        public string IngredientTransformer;
        public string MixingBowlTransformer;
        public string BakingDishTransformer;
        public string ReturnTransformer;
        public string[] ChefCompilerArgs;

        public ChefMethodAttribute(string code)
        {
            ChefCode = code;
            IngredientTransformer = "";
            MixingBowlTransformer = "";
            BakingDishTransformer = "";
            ReturnTransformer = "";
            ChefCompilerArgs = new string[0];
        }
    }

    [Conditional("COMPILE_TIME_ONLY")]
    [System.AttributeUsage(System.AttributeTargets.Class
    /**/                 | System.AttributeTargets.Struct
    /**/                 | System.AttributeTargets.Interface,
    /**/                 AllowMultiple = true)]
    internal sealed class ChefUnaryOperatorAttribute : Attribute
    {
        private string Operator;
        private string RightType;
        private string ReturnType;
        private string ChefCode;
        public string IngredientTransformer;
        public string MixingBowlTransformer;
        public string BakingDishTransformer;
        public string ReturnTransformer;
        public string[] ChefCompilerArgs;

        public ChefUnaryOperatorAttribute(string op, string rightType, string returnType, string code)
        {
            Operator = op;
            RightType = rightType;
            ReturnType = returnType;
            ChefCode = code;
            IngredientTransformer = "";
            MixingBowlTransformer = "";
            BakingDishTransformer = "";
            ReturnTransformer = "";
            ChefCompilerArgs = new string[0];
        }
    }

    [Conditional("COMPILE_TIME_ONLY")]
    [System.AttributeUsage(System.AttributeTargets.Class
    /**/                 | System.AttributeTargets.Struct
    /**/                 | System.AttributeTargets.Interface,
    /**/                 AllowMultiple = true)]
    internal sealed class ChefBinaryOperatorAttribute : Attribute
    {
        private string Operator;
        private string LeftType;
        private string RightType;
        private string ReturnType;
        private string ChefCode;
        public string IngredientTransformer;
        public string MixingBowlTransformer;
        public string BakingDishTransformer;
        public string ReturnTransformer;
        public string[] ChefCompilerArgs;


        public ChefBinaryOperatorAttribute(
            string op, string leftType, string rightType, string returnType, string code)
        {
            Operator = op;
            LeftType = leftType;
            RightType = rightType;
            ReturnType = returnType;
            ChefCode = code;
            IngredientTransformer = "";
            MixingBowlTransformer = "";
            BakingDishTransformer = "";
            ReturnTransformer = "";
            ChefCompilerArgs = new string[0];
        }
    }

    [Conditional("COMPILE_TIME_ONLY")]
    [System.AttributeUsage(System.AttributeTargets.Class
    /**/                 | System.AttributeTargets.Struct
    /**/                 | System.AttributeTargets.Interface,
    /**/                 AllowMultiple = true)]
    internal sealed class ChefCastOperatorAttribute : Attribute
    {
        private bool Explicit;
        private string ParamType;
        private string ReturnType;
        private string ChefCode;
        public string IngredientTransformer;
        public string MixingBowlTransformer;
        public string BakingDishTransformer;
        public string ReturnTransformer;
        public string[] ChefCompilerArgs;

        public ChefCastOperatorAttribute(bool isExplicit, string paramType, string returnType, string chefCode)
        {
            Explicit = isExplicit;
            ParamType = paramType;
            ReturnType = returnType;
            ChefCode = chefCode;
            IngredientTransformer = "";
            MixingBowlTransformer = "";
            BakingDishTransformer = "";
            ReturnTransformer = "";
            ChefCompilerArgs = new string[0];
        }
    }

    [Conditional("COMPILE_TIME_ONLY")]
    [System.AttributeUsage(System.AttributeTargets.Class
    /**/                 | System.AttributeTargets.Struct
    /**/                 | System.AttributeTargets.Interface,
    /**/                 AllowMultiple = true)]
    internal sealed class ChefPropertyAttribute : Attribute
    {
        private string Name;
        private string Type;
        private string GetChefCode;
        private string SetChefCode;
        public string GetIngredientTransformer;
        public string GetMixingBowlTransformer;
        public string GetBakingDishTransformer;
        public string ReturnTransformer;
        public string SetIngredientTransformer;
        public string SetMixingBowlTransformer;
        public string SetBakingDishTransformer;
        public string GetterModifier;
        public string SetterModifier;
        public string[] GetChefCompilerArgs;
        public string[] SetChefCompilerArgs;

        public ChefPropertyAttribute(string name, string type, string getChefCode, string setChefCode)
        {
            Name = name;
            Type = type;
            GetChefCode = getChefCode;
            SetChefCode = setChefCode;
            GetIngredientTransformer = "";
            GetMixingBowlTransformer = "";
            GetBakingDishTransformer = "";
            ReturnTransformer = "";
            SetIngredientTransformer = "";
            SetMixingBowlTransformer = "";
            SetBakingDishTransformer = "";
            GetterModifier = "";
            SetterModifier = "";
            GetChefCompilerArgs = new string[0];
            SetChefCompilerArgs = new string[0];
        }
    }

    [Conditional("COMPILE_TIME_ONLY")]
    [System.AttributeUsage(System.AttributeTargets.Class
    /**/                 | System.AttributeTargets.Struct
    /**/                 | System.AttributeTargets.Interface,
    /**/                 AllowMultiple = true)]
    internal sealed class ChefIndexerAttribute : Attribute
    {
        private string Params;
        private string ReturnType;
        private string GetChefCode;
        private string SetChefCode;
        public string GetIngredientTransformer;
        public string GetMixingBowlTransformer;
        public string GetBakingDishTransformer;
        public string ReturnTransformer;
        public string SetIngredientTransformer;
        public string SetMixingBowlTransformer;
        public string SetBakingDishTransformer;
        public string GetterModifier;
        public string SetterModifier;
        public string[] GetChefCompilerArgs;
        public string[] SetChefCompilerArgs;

        public ChefIndexerAttribute(string args, string returnType, string getChefCode, string setChefCode)
        {
            Params = args;
            ReturnType = returnType;
            GetChefCode = getChefCode;
            SetChefCode = setChefCode;
            GetIngredientTransformer = "";
            GetMixingBowlTransformer = "";
            GetBakingDishTransformer = "";
            ReturnTransformer = "";
            SetIngredientTransformer = "";
            SetMixingBowlTransformer = "";
            SetBakingDishTransformer = "";
            GetterModifier = "";
            SetterModifier = "";
            GetChefCompilerArgs = new string[0];
            SetChefCompilerArgs = new string[0];
        }
    }
}
#endif