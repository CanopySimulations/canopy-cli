using Newtonsoft.Json.Linq;
using Xunit;

namespace Canopy.Cli.Executable.Services
{
    public class ContainsEncryptedTokenTests
    {
        private readonly ContainsEncryptedToken target = new();

        [Fact]
        public void WhenEmptyObjectItShouldReturnTrue()
        {
            var content = new JObject();
            Assert.False(target.Execute(content));
            Assert.False(target.Execute(content.ToString()));
        }

        [Fact]
        public void WhenEncryptedDataExistsItShouldReturnTrue()
        {
            var content = JObject.Parse(@"
            {
                a: {
                    b: [
                        {
                            c: {
                                'name': 'encrypted'
                            }
                        }
                    ]
                }
            }");

            Assert.True(target.Execute(content));
            Assert.True(target.Execute(content.ToString()));
        }

        [Fact]
        public void WhenNoEncryptedDataExistsItShouldReturnFalse()
        {
            var content = JObject.Parse(@"
            {
                a: {
                    b: [
                        {
                            c: {
                                'name': 'other'
                            }
                        }
                    ]
                }
            }");

            Assert.False(target.Execute(content));
            Assert.False(target.Execute(content.ToString()));
        }
    }
}