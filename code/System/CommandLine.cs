using System.Collections.Generic;

namespace System
{
    public static class CommandLine
    {
        public enum ParseStatus
        {
            Valid = 0,

            Invalid = 1,

            HelpRequested = 2
        }

        private static string[] Examples { get; set; }

        private static string[] Notes { get; set; }

        private static List<Parameter> Parameters { get; set; }

        private static string Synopsis { get; set; }

        private static string[] Syntax { get; set; }

        private static string Version { get; set; }

        public static void AddParameter(string name, string description, Func<bool> isMandatoryFunction, Action<string> assignmentAction)
        {
            if (Parameters == null)
            {
                Parameters = new List<Parameter>();
            }

            Parameters.Add(new Parameter(name, description, isMandatoryFunction, assignmentAction));
        }

        public static ParseResult ParseArguments(string[] args)
        {
            if ((args != null) && (args.Length > 0))
            {
                if (args[0].Equals("/?"))
                {
                    return new ParseResult(ParseStatus.HelpRequested);
                }
            }

            var parseResult = new ParseResult();

            if (Parameters != null)
            {
                var argDictionary = new Dictionary<string, string>();

                if ((args != null) && (args.Length > 0))
                {
                    foreach (var arg in args)
                    {
                        if (!string.IsNullOrEmpty(arg))
                        {
                            if (arg.StartsWith("/"))
                            {
                                var p = arg.IndexOf("=");

                                if (p >= 0)
                                {
                                    var parameterName = NormalizeParameterName(arg.Substring(0, p));
                                    var parameterValue = arg.Substring(p + 1);

                                    if (!string.IsNullOrEmpty(parameterValue))
                                    {
                                        if (!argDictionary.ContainsKey(parameterName)) argDictionary.Add(parameterName, parameterValue);
                                    }
                                }
                                else
                                {
                                    var parameterName = NormalizeParameterName(arg);

                                    if (!argDictionary.ContainsKey(parameterName)) argDictionary.Add(parameterName, null);
                                }
                            }
                            else
                            {
                                parseResult.AddValue(arg);
                            }
                        }
                    }
                }

                foreach (var parameter in Parameters)
                {
                    var parameterName = NormalizeParameterName(parameter.Name);

                    if (argDictionary.TryGetValue(parameterName, out string parameterValue))
                    {
                        try
                        {
                            parameter.AssignmentAction(parameterValue);
                        }
                        catch
                        {
                            parseResult.AddError(ParseError.CreateFromInvalidParameterError(parameter, parameterValue));
                        }
                    }
                    else if (parameter.IsMandatoryFunction())
                    {
                        parseResult.AddError(ParseError.CreateFromMissingParameterError(parameter));
                    }
                }
            }

            return parseResult;
        }

        public static void SetExamples(params string[] examples)
        {
            Examples = examples;
        }

        public static void SetNotes(params string[] notes)
        {
            Notes = notes;
        }

        public static void SetSynopsis(string synopsis)
        {
            Synopsis = synopsis;
        }

        public static void SetSyntax(params string[] syntax)
        {
            Syntax = syntax;
        }

        public static void SetVersion(string version)
        {
            Version = version;
        }

        public static void ShowHelpInConsole()
        {
            Console.WriteLine("SYNOPSIS");

            Console.WriteLine();

            Console.WriteLine(Synopsis);

            if (!string.IsNullOrEmpty(Version))
            {
                Console.WriteLine();

                Console.WriteLine("VERSION");

                Console.WriteLine();

                Console.WriteLine(Version);
            }

            if (Syntax != null)
            {
                Console.WriteLine();

                Console.WriteLine("SYNTAX");

                foreach (var paragraph in Syntax)
                {
                    Console.WriteLine();

                    Console.WriteLine(paragraph);
                }
            }

            Console.WriteLine();

            Console.WriteLine("PARAMETERS");

            Console.WriteLine();

            Console.WriteLine("/?");
            Console.WriteLine("This help text.");

            if (Parameters != null)
            {
                foreach (var parameter in Parameters)
                {
                    Console.WriteLine();

                    Console.WriteLine(parameter.Name);
                    Console.WriteLine(parameter.Description);
                }
            }

            if (Examples != null)
            {
                Console.WriteLine();

                Console.WriteLine("EXAMPLES");

                foreach (var paragraph in Examples)
                {
                    Console.WriteLine();

                    Console.WriteLine(paragraph);
                }
            }

            if (Notes != null)
            {
                Console.WriteLine();

                Console.WriteLine("NOTES");

                foreach (var paragraph in Notes)
                {
                    Console.WriteLine();

                    Console.WriteLine(paragraph);
                }
            }
        }

        public static void ShowParseErrorsInConsole(ParseResult parseResult)
        {
            foreach (var parseError in parseResult.Errors)
            {
                Console.WriteLine(parseError.Message);
            }
        }

        private static string NormalizeParameterName(string parameterName)
        {
            return parameterName.ToUpperInvariant();
        }

        public class Parameter
        {
            public Parameter(string name, string description, Func<bool> isMandatoryFunction, Action<string> assignmentAction)
            {
                this.Name = name;
                this.Description = description;
                this.IsMandatoryFunction = isMandatoryFunction;
                this.AssignmentAction = assignmentAction;
            }

            public Action<string> AssignmentAction { get; private set; }

            public string Description { get; private set; }

            public Func<bool> IsMandatoryFunction { get; private set; }

            public string Name { get; private set; }
        }

        public class ParseError
        {
            public ParseError(Parameter parameter, string message)
            {
                this.Parameter = parameter;
                this.Message = message;
            }

            public string Message { get; private set; }

            public Parameter Parameter { get; private set; }

            public static ParseError CreateFromInvalidParameterError(Parameter parameter, string parameterValue)
            {
                return new ParseError(parameter, $"Invalid value for the \"{parameter.Name}\" parameter.");
            }

            public static ParseError CreateFromMissingParameterError(Parameter parameter)
            {
                return new ParseError(parameter, $"Missing value for the \"{parameter.Name}\" parameter.");
            }
        }

        public class ParseResult
        {
            public ParseResult() : this(ParseStatus.Valid)
            {
            }

            public ParseResult(ParseStatus status)
            {
                this.Status = status;
            }

            public List<ParseError> Errors { get; private set; }

            public ParseStatus Status { get; private set; }

            public List<string> Values { get; private set; }

            public void AddError(ParseError error)
            {
                if (this.Errors == null)
                {
                    this.Errors = new List<ParseError>();
                }

                this.Errors.Add(error);

                this.Status = ParseStatus.Invalid;
            }

            public void AddValue(string value)
            {
                if (this.Values == null)
                {
                    this.Values = new List<string>();
                }

                this.Values.Add(value);
            }
        }
    }
}