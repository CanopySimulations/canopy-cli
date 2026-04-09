using System.CommandLine;

namespace Canopy.Cli.Executable.Commands
{
    internal static class CommandExtensions
    {
        public readonly struct OptionWithDefault<T>(Option<T> option, T defaultValue)
        {
            public Option<T> Option { get; } = option;
            public T DefaultValue { get; } = defaultValue;
        }

        public readonly struct ArgumentWithDefault<T>(Argument<T> argument, T defaultValue)
        {
            public Argument<T> Argument { get; } = argument;
            public T DefaultValue { get; } = defaultValue;
        }

        public static T GetValue<T>(this ParseResult parseResult, OptionWithDefault<T> option)
        {
            return parseResult.GetValue(option.Option) ?? option.DefaultValue!;
        }

        public static T GetValue<T>(this ParseResult parseResult, ArgumentWithDefault<T> argument)
        {
            return parseResult.GetValue(argument.Argument) ?? argument.DefaultValue!;
        }

        public static OptionWithDefault<T> AddOption<T>(
            this Command command,
            string name,
            string alias,
            T defaultValue,
            string description)
        {
            var option = new Option<T>(name, alias)
            {
                DefaultValueFactory = _ => defaultValue,
                Description = description,
            };
            command.Options.Add(option);
            return new OptionWithDefault<T>(option, defaultValue);
        }

        public static OptionWithDefault<T> AddOption<T>(
            this Command command,
            string name,
            T defaultValue,
            string description)
        {
            var option = new Option<T>(name)
            {
                DefaultValueFactory = _ => defaultValue,
                Description = description,
            };
            command.Options.Add(option);
            return new OptionWithDefault<T>(option, defaultValue);
        }

        public static Option<T> AddRequiredOption<T>(
            this Command command,
            string name,
            string alias,
            string description)
        {
            var option = new Option<T>(name, alias)
            {
                Required = true,
                Description = description,
            };
            command.Options.Add(option);
            return option;
        }

        public static ArgumentWithDefault<T> AddArgument<T>(
            this Command command,
            string name,
            T defaultValue,
            string description)
        {
            var argument = new Argument<T>(name)
            {
                DefaultValueFactory = _ => defaultValue,
                Description = description,
            };
            command.Arguments.Add(argument);
            return new ArgumentWithDefault<T>(argument, defaultValue);
        }
    }
}

