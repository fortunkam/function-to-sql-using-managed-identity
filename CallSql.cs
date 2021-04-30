using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data.Common;
using Azure.Core;
using Azure.Identity;

namespace Memoryleek
{
    public static class CallSql
    {
        [FunctionName("CallSql")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "tcp:<sqlservername>.database.windows.net,1433",
                InitialCatalog = "<sqldbname>",
                TrustServerCertificate = false,
                Encrypt = true,
            };

            await using var sqlConnection = new SqlConnection(connectionStringBuilder.ConnectionString)
            {
                AccessToken = await GetAzureSqlAccessToken()
            };

            await sqlConnection.OpenAsync();
            var sqlCommand = new SqlCommand("SELECT GETDATE()",sqlConnection);
            var currentTime = await sqlCommand.ExecuteScalarAsync(new System.Threading.CancellationToken());


            return new OkObjectResult(currentTime);
        }

        private static async Task<string> GetAzureSqlAccessToken()
        {
            // See https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/services-support-managed-identities#azure-sql
            var tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net//.default" });

            //NOTE: if you want to use a User Assigned Managed Identity, the ManagedIdentityCredential takes the client id as a parameter
            var tokenRequestResult = await new ManagedIdentityCredential().GetTokenAsync(tokenRequestContext);

            return tokenRequestResult.Token;
        }
    }
}
