using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PoCAzureServiceBus.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        const string ServiceBusConnectionString = "";
        static string queueName = "queue_{0}";
        static IQueueClient queueClient;

        private static DateTime tokenExpiresAtUtc = DateTime.MinValue;

        static string tokenValue = string.Empty;
        static string tenantId = "";
        static string clientId = "";
        static string clientSecret = "";
        static string subscriptionId = "";

        private static string resourceGroupName = "";
        const string namespaceName = "";

        /// <summary>
        /// Envia mensagem para o service bus e cria a fila especificada para o identificador caso o mesmo não exista
        /// </summary>
        /// <param name="identificador">id da fila</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Post([FromBody] string identificador)
        {
            try
            {
                queueName = string.Format(queueName, identificador);

                CriarQueueSeNaoExistir().ConfigureAwait(true);

                queueClient = new QueueClient(ServiceBusConnectionString, queueName);

                EnviarMensagem(queueName).GetAwaiter().GetResult();

                return Ok();
            }
            catch(Exception e)
            {
                return StatusCode(500, e);
            }
        }

        private static async Task CriarQueueSeNaoExistir()
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new Exception("Namespace nao existe!");
            }

            var token = await GetToken();

            var creds = new TokenCredentials(token);
            var sbClient = new ServiceBusManagementClient(creds)
            {
                SubscriptionId = subscriptionId,
            };

            var queueParams = new SBQueue
            {
                EnablePartitioning = true
            };
                
            await sbClient.Queues.CreateOrUpdateAsync(resourceGroupName, namespaceName, queueName, queueParams);
        }

        private static async Task<string> GetToken()
        {
            if (tokenExpiresAtUtc < DateTime.UtcNow.AddMinutes(-2))
            {

                var context = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");

                var result = await context.AcquireTokenAsync(
                    "https://management.core.windows.net/",
                    new ClientCredential(clientId, clientSecret)
                );

                if (string.IsNullOrEmpty(result.AccessToken))
                {
                    throw new Exception("Token esta vazio empty!");
                }

                tokenExpiresAtUtc = result.ExpiresOn.UtcDateTime;
                tokenValue = result.AccessToken;
            }

            return tokenValue;
        }

        static async Task EnviarMensagem(string queueName)
        {
            string acao = "ArquivoPesagem";
            string conteudo = "Cliente: xxx, Traco: xxxx";

            await SendMessagesAsync(acao, conteudo);

            await queueClient.CloseAsync();
        }

        static async Task SendMessagesAsync(string acao, string conteudo)
        {
            // Create a new message to send to the queue.
            string messageBody = $"Acao {acao}";
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));

            // Write the body of the message to the console.
            Console.WriteLine($"Sending message: {messageBody}");

            // Send the message to the queue.
            await queueClient.SendAsync(message);
        }
    }
}
