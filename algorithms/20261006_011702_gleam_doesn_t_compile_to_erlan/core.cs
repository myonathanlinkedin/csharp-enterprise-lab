using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GleamToErlang
{
    // Simple representation of a Gleam type
    public enum GleamType
    {
        Int,
        Bool,
        String,
        Custom
    }

    // Representation of a function parameter
    public record Parameter(string Name, GleamType Type);

    // Representation of a Gleam function
    public record GleamFunction(string Name, List<Parameter> Params, string Body);

    // Representation of a Gleam module
    public record GleamModule(string Name, List<GleamFunction> Functions);

    // Very small parser for a subset of Gleam syntax
    public static class Parser
    {
        private static readonly Regex FunctionRegex = new(@"^\s*def\s+(\w+)\s*\(([^)]*)\)\s*=\s*(.+)$", RegexOptions.Compiled);

        public static GleamModule Parse(string source)
        {
            var lines = source.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                throw new ArgumentException("Source is empty.");

            // Assume first non-empty line is module name: "module <name>"
            string moduleName = "gleam_module";
            if (lines[0].StartsWith("module "))
            {
                moduleName = lines[0].Substring(7).Trim();
                lines = lines[1..];
            }

            var functions = new List<GleamFunction>();
            foreach (var line in lines)
            {
                var match = FunctionRegex.Match(line);
                if (!match.Success)
                    continue; // ignore non-function lines

                var name = match.Groups[1].Value;
                var paramsPart = match.Groups[2].Value;
                var body = match.Groups[3].Value.Trim();

                var parameters = new List<Parameter>();
                if (!string.IsNullOrWhiteSpace(paramsPart))
                {
                    var paramTokens = paramsPart.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var token in paramTokens)
                    {
                        var parts = token.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length != 2)
                            throw new FormatException($"Invalid parameter format: {token}");
                        var paramName = parts[0].Trim();
                        var typeName = parts[1].Trim();
                        var type = ParseType(typeName);
                        parameters.Add(new Parameter(paramName, type));
                    }
                }

                functions.Add(new GleamFunction(name, parameters, body));
            }

            return new GleamModule(moduleName, functions);
        }

        private static GleamType ParseType(string typeName)
        {
            return typeName switch
            {
                "Int" => GleamType.Int,
                "Bool" => GleamType.Bool,
                "String" => GleamType.String,
                _ => GleamType.Custom
            };
        }
    }

    // Translator from Gleam AST to Erlang source
    public static class Translator
    {
        public static string Translate(GleamModule module)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"-module({module.Name}).");
            sb.AppendLine("-export([");

            for (int i = 0; i < module.Functions.Count; i++)
            {
                var fn = module.Functions[i];
                sb.Append($"    {fn.Name}/{fn.Params.Count}");
                if (i < module.Functions.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("]).");
            sb.AppendLine();

            foreach (var fn in module.Functions)
            {
                var paramNames = string.Join(", ", fn.Params.ConvertAll(p => p.Name));
                var body = TranslateExpression(fn.Body);
                sb.AppendLine($"{fn.Name}({paramNames}) -> {body}.");
            }

            return sb.ToString();
        }

        // Very naive expression translator: replace '=' with '->' and add semicolon
        private static string TranslateExpression(string expr)
        {
            // For this demo, we simply return the expression unchanged
            // In a real implementation, we would parse and translate the expression
            return expr.Trim();
        }
    }
}
