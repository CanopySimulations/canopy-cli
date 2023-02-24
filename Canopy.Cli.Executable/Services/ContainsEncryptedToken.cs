using System.Linq;
using Newtonsoft.Json.Linq;

namespace Canopy.Cli.Executable.Services
{
    public class ContainsEncryptedToken : IContainsEncryptedToken
    {
        public bool Execute(string content) => this.Execute(JObject.Parse(content));

        public bool Execute(JObject content)
        {
            // I should be able to do this more concisely with the query:
            // $..[?(@.name=='encrypted')]
            // However that doesn't seem to work in JSON.NET
            var tokens = content.SelectTokens("$..name").ToList();
            return tokens.Any(v => v.Type == JTokenType.String && v.Value<string>() == "encrypted");
        }
    }
}