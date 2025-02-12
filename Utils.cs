using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kronosta.ChefCSharpPidgin
{
    /// <summary>
    /// Various utility functions for use throughout the compiler and generator
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Encloses the string in a partial class declaration that matches the given INamedTypeSymbol.
        /// </summary>
        /// <param name="baseStr">The string to enclose</param>
        /// <param name="type">The symbol to get the class attributes and type parameters</param>
        /// <param name="indent">What to insert before each line of baseStr</param>
        /// <returns>baseStr enclosed in the class declaration</returns>
        public static string EncloseInPartialType(string baseStr, INamedTypeSymbol type, string indent)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(SyntaxFacts.GetText(type.DeclaredAccessibility));
            builder.Append(" partial ");
            if (type.IsValueType)
                builder.Append("struct ");
            else
                builder.Append("class ");
            builder.Append(type.Name);
            if (type.Arity > 0)
            {
                builder.Append("<");
                foreach (var arg in type.TypeParameters)
                {
                    builder.Append(arg.Name);
                    builder.Append(", ");
                }
                builder.Remove(builder.Length - 3, 2);
                builder.Append(">");
            }
            builder.Append("\n{\n");
            builder.Append(Indent(baseStr, indent));
            builder.Append("\n}");
            return builder.ToString();
        }

        /// <summary>
        /// Encloses the string in a partial class declaration that matches the given INamedTypeSymbol.
        /// Indents with ChefGenerator.Indent.
        /// </summary>
        /// <param name="baseStr">The string to enclose</param>
        /// <param name="type">The symbol to get the class modifiers and type parameters</param>
        /// <returns>baseStr enclosed in the class declaration</returns>
        public static string EncloseInPartialType(string baseStr, INamedTypeSymbol type) =>
            EncloseInPartialType(baseStr, type, ChefGenerator.Indent);

        /// <summary>
        /// Encloses the string in a partial method declaration that matches the given IMethodSymbol
        /// <param name="baseStr">The string to enclose</param>
        /// <param name="methodSymbol">The symbol to get the method modifiers, parameters, and type parameters</param>
        /// <param name="indent">What to insert before each line of baseStr</param>
        /// <returns>baseStr enclosed in the class declaration</returns>
        public static string EncloseInPartialMethod(string baseStr, IMethodSymbol methodSymbol, string indent)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(SyntaxFacts.GetText(methodSymbol.DeclaredAccessibility));
            if (methodSymbol.IsStatic) builder.Append(" static");
            if (methodSymbol.IsVirtual) builder.Append(" virtual");
            if (methodSymbol.IsAbstract) builder.Append(" abstract");
            if (methodSymbol.IsOverride) builder.Append(" override");
            if (methodSymbol.IsSealed) builder.Append(" sealed");
            if (methodSymbol.IsAsync) builder.Append(" async");
            builder.Append(" partial ");
            if (methodSymbol.DeclaringSyntaxReferences.Length > 0)
            {
                MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxReferences[0].GetSyntax();
                builder.Append(methodDeclaration.ReturnType.GetText().ToString());
                builder.Append(" ");
                builder.Append(methodDeclaration.Identifier.ToString());
                builder.Append(methodDeclaration.TypeParameterList?.GetText()?.ToString() ?? "");
                builder.Append(" ");
                builder.Append(methodDeclaration.ParameterList.GetText().ToString());
            }
            else
            {
                throw new InvalidOperationException("The IMethodSymbol has no declarations.");
            }
            builder.Append("\n{\n");
            builder.Append(Indent(baseStr, indent));
            builder.Append("\n}");
            return builder.ToString();
        }

        /// <summary>
        /// Encloses the string in a partial method declaration that matches the given IMethodSymbol
        /// Indents with ChefGenerator.Indent.
        /// <param name="baseStr">The string to enclose</param>
        /// <param name="methodSymbol">The symbol to get the method modifiers, parameters, and type parameters</param>
        /// <returns>baseStr enclosed in the class declaration</returns>
        public static string EncloseInPartialMethod(string baseStr, IMethodSymbol methodSymbol) =>
            EncloseInPartialMethod(baseStr, methodSymbol, ChefGenerator.Indent);

        /// <summary>
        /// Changes all three typical newlines style (LF, CR, CR-LF) to all be LF or '\n'.
        /// </summary>
        /// <param name="baseStr">The string to operate on</param>
        /// <returns>The transformed string</returns>
        public static string NormalizeNewlines(string baseStr) =>
            baseStr.Replace("\r\n", "\n").Replace("\r", "\n");

        /// <summary>
        /// Inserts a string before each line of another, intended for indentation
        /// </summary>
        /// <param name="baseStr">The string to indent</param>
        /// <param name="indent">The line prefix (which is probably all whitespace, but doesn't have to be)</param>
        /// <returns>The indented string</returns>
        public static string Indent(string baseStr, string indent) =>
            string.Join("\n",
                NormalizeNewlines(baseStr)
                .Split('\n')
                .Select(line => indent + line)
            );

        /// <summary>
        /// Indents with ChefGenerator.Indent.
        /// </summary>
        /// <param name="baseStr">The string to indent</param>
        /// <returns>The indented string</returns>
        public static string Indent(string baseStr) =>
            Indent(baseStr, ChefGenerator.Indent);

        /// <summary>
        /// Checks if the ITypeSymbol represents a string
        /// </summary>
        /// <param name="typeSymbol">The type symbol to check</param>
        /// <returns>True if typeSymbol represents the string type, false otherwise</returns>
        public static bool IsString(this ITypeSymbol typeSymbol) =>
            typeSymbol.GetFullMetadataName() == "System.String" ||
            typeSymbol.GetFullMetadataName() == "string";

        /// <summary>
        /// Checks if the ITypeSymbol represents an int
        /// </summary>
        /// <param name="typeSymbol">The type symbol to check</param>
        /// <returns>True if typeSymbol represents the int type, false otherwise</returns>
        public static bool IsInt32(this ITypeSymbol typeSymbol) =>
            typeSymbol.GetFullMetadataName() == "System.Int32" ||
            typeSymbol.GetFullMetadataName() == "int";

        /// <summary>
        /// Checks if the ITypeSymbol represents a bool
        /// </summary>
        /// <param name="typeSymbol">The type symbol to check</param>
        /// <returns>True if typeSymbol represents the bool type, false otherwise</returns>
        public static bool IsBoolean(this ITypeSymbol typeSymbol) =>
            typeSymbol.GetFullMetadataName() == "System.Boolean" ||
            typeSymbol.GetFullMetadataName() == "bool";

        /// <summary>
        /// Checks if the ITypeSymbol represents a Type
        /// </summary>
        /// <param name="typeSymbol">The type symbol</param>
        /// <param name="type">The type</param>
        /// <returns></returns>
        public static bool IsType(this ITypeSymbol typeSymbol, Type type)
        {
            if (type.IsArray)
            {
                if (typeSymbol is not IArrayTypeSymbol) return false;
                return ((IArrayTypeSymbol)typeSymbol).ElementType.IsType(type.GetElementType());
            }
            if (type.IsPointer)
            {
                if (typeSymbol is not IPointerTypeSymbol) return false;
                return ((IPointerTypeSymbol)typeSymbol).PointedAtType.IsType(type.GetElementType());
            }
            if (type.IsConstructedGenericType)
            {
                if (typeSymbol is not INamedTypeSymbol) return false;
                if (((INamedTypeSymbol)typeSymbol).Arity != type.GenericTypeArguments.Length) return false;
                if (((INamedTypeSymbol)typeSymbol).GetFullMetadataName() != type.GetFullMetadataName()) return false;
                for (int i = 0; i < type.GenericTypeArguments.Length; i++)
                    if (!((INamedTypeSymbol)type).TypeArguments[i].IsType(type.GenericTypeArguments[i])) return false;
                return true;
            }
            return typeSymbol.GetFullMetadataName() == type.GetFullMetadataName();
        }

        /// <summary>
        /// Gets the namespace-qualified name of a type with inner classes separated by '+'
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The namespace-qualified name</returns>
        public static string GetFullMetadataName(this Type type)
        {
            StringBuilder builder = new StringBuilder(type.Name);
            Type? containingType = type.DeclaringType;
            Type outermostType = type;
            while (containingType != null)
            {
                builder.Insert(0, containingType.Name + "+");
                if (containingType.DeclaringType == null) outermostType = containingType;
                containingType = containingType.DeclaringType;
            }
            builder.Insert(0, (outermostType.Namespace ?? "") + ".");
            return builder.ToString();
        }

        //Taken from https://stackoverflow.com/questions/27105909/get-fully-qualified-metadata-name-in-roslyn
        #region Get fully qualified metadata name
        /// <summary>
        /// Gets the fully-qualified metadata name of an ISymbol.
        /// Taken from https://stackoverflow.com/questions/27105909/get-fully-qualified-metadata-name-in-roslyn
        /// </summary>
        /// <param name="s">The symbol</param>
        /// <returns>The fully-qualified metadata name</returns>
        public static string GetFullMetadataName(this ISymbol s)
        {
            if (s == null || IsRootNamespace(s))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(s.MetadataName);
            var last = s;

            s = s.ContainingSymbol;

            while (!IsRootNamespace(s))
            {
                if (s is ITypeSymbol && last is ITypeSymbol)
                {
                    sb.Insert(0, '+');
                }
                else
                {
                    sb.Insert(0, '.');
                }

                sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                //sb.Insert(0, s.MetadataName);
                s = s.ContainingSymbol;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Checks if an ISymbol represents the root namespace.
        /// A helper function for GetFullMetadataName(ISymbol).
        /// Taken from https://stackoverflow.com/questions/27105909/get-fully-qualified-metadata-name-in-roslyn
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>True if symbol represents the root namespace, false otherwise</returns>
        private static bool IsRootNamespace(ISymbol symbol)
        {
            INamespaceSymbol s = null;
            return ((s = symbol as INamespaceSymbol) != null) && s.IsGlobalNamespace;
        }
        #endregion
    }
}
