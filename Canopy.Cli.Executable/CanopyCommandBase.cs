using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable
{
    public abstract class CanopyCommandBase : CommandLineApplication
    {
        public const string ErrorLogFileName = "error.txt";

        protected CanopyApiConfiguration configuration = new CanopyApiConfiguration();
        protected AuthenticatedUser authenticatedUser = null;

		public CanopyCommandBase()
        {
            this.RequiresConnection = true;
            this.RequiresAuthentication = true;

            this.HelpOption("-? | -h | --help");
            this.OnExecute((Func<Task<int>>)this.ExecuteWrapperAsync);
        }

		protected bool RequiresConnection { get; set; }
		protected bool RequiresAuthentication { get; set; }

        private async Task<int> ExecuteWrapperAsync()
        {
            try
            {
                if (this.RequiresConnection)
                {
                    if (!ConnectionManager.Instance.LoadConnectionInformation())
                    {
                        throw new RecoverableException("Not connected. Run `canopy connect --help` for more information.");
                    }
                }

                if(this.RequiresAuthentication)
                {
                    if (!AuthenticationManager.Instance.LoadAuthenticatedUser())
                    {
						throw new RecoverableException("Not authenticated. Run `canopy authenticate --help` for more information.");
					}

                    this.authenticatedUser = await AuthenticationManager.Instance.GetAuthenticatedUser();
                }

                await this.ExecuteAsync();
                return 0;
            }
            catch (RecoverableException t)
            {
                this.DisplayErrorMessage(t);
				return 1;
            }
            catch(HttpRequestException t)
            {
				this.DisplayErrorMessage(t);
				return 1;
			}
            catch (Exception t)
            {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine();
				Console.WriteLine($"An error occurred. See {ErrorLogFileName} for more details.");
				Console.ResetColor();
				this.WriteError(t);
                return 1;
            }
        }

        private void DisplayErrorMessage(Exception error)
        {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine();
			Console.WriteLine(error.Message);
            if(error.InnerException != null)
            {
				Console.WriteLine(error.InnerException.Message);
		    }
			Console.ResetColor();
			this.WriteError(error);
		}

        private void WriteError(Exception error)
        {
            try
            {
				var saveFolder = PlatformUtilities.AppDataFolder();
                var saveFile = Path.Combine(saveFolder, ErrorLogFileName);
                File.WriteAllText(saveFile, error.ToString());
			}
            catch(Exception t)
            {
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine();
				Console.WriteLine("Failed to log error:");
				Console.WriteLine(t);
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine();
				Console.WriteLine("Original error:");
				Console.WriteLine(error);
				Console.ResetColor();
			}
		}

        protected abstract Task ExecuteAsync();
    }
}
