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
            catch (Exception t)
            {
                Utilities.HandleError(t);
                return 1;
            }
        }


        protected abstract Task ExecuteAsync();
    }
}
