using System;
using Xunit;

namespace Canopy.Cli.Executable
{
    public class GuardTests
    {
        [Fact]
        public void WhenArgumentTestPasses_ItShouldNotThrow()
        {
            Guard.Argument(true);
            Guard.Argument(true, "Hello {0}", "World");
        }

        [Fact]
        public void WhenArgumentTestFails_ItShouldThrowWithMessage()
        {
            Assert.Throws<ArgumentException>(
                () => Guard.Argument(false));

            string? message = null;
            try
            {
                Guard.Argument(false, "Hello {0}", "World");
                Assert.False(true, "This should not run.");
            }
            catch(ArgumentException t)
            {
                message = t.Message;
            }

            Assert.Equal("Hello World", message);
        }
        
        [Fact]
        public void WhenOperationTestPasses_ItShouldNotThrow()
        {
            Guard.Operation(true);
            Guard.Operation(true, "Hello {0}", "World");
        }

        [Fact]
        public void WhenOperationTestFails_ItShouldThrowWithMessage()
        {
            Assert.Throws<InvalidOperationException>(
                () => Guard.Operation(false));

            string? message = null;
            try
            {
                Guard.Operation(false, "Hello {0}", "World");
                Assert.False(true, "This should not run.");
            }
            catch(InvalidOperationException t)
            {
                message = t.Message;
            }

            Assert.Equal("Hello World", message);
        }
    }
}