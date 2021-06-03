// using System;
// using Canopy.Api.Client;

// namespace Canopy.Cli.Executable.IntegrationTestsRunner
// {
//     public class GetCredentialsFromEnvironmentVariable : IGetCredentialsFromEnvironmentVariable
//     {
//         public IntegrationTestsCommand.Parameters Execute(string key)
//         {
//             var credentials = Environment.GetEnvironmentVariable(key);

//             if (credentials == null)
//             {
//                 throw new RecoverableException($"Environment variable {key} not found.");
//             }

//             var splitCredentials = credentials.Split('|');

//             if (splitCredentials.Length != 5)
//             {
//                 throw new RecoverableException($"Environment variable {key} had an unexpected format.");
//             }

//             var clientId = splitCredentials[0];
//             var clientSecret = splitCredentials[1];
//             var username = splitCredentials[2];
//             var tenantName = splitCredentials[3];
//             var password = splitCredentials[4];

//             return new IntegrationTestsCommand.Parameters(
//                 ConnectionManager.DefaultApiEndpoint,
//                 ClientId: clientId,
//                 ClientSecret: clientSecret,
//                 Username: username,
//                 Company: tenantName,
//                 Password: password,
//                 string.Empty);
//         }
//     }
// }