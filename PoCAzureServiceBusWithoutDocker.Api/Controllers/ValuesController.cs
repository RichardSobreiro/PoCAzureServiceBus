using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace PoCAzureServiceBusWithoutDocker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        const string ServiceBusConnectionString = "Endpoint=sb://pontocarga.servicebus.windows.net/;SharedAccessKeyName=ExpedicaoWeb;SharedAccessKey=N62t5AUF+Fq55hX/yvaFNE2HCyCge7HRhRscG4B3FFE=";
        static string queueName = "queue_{0}";
        static IQueueClient queueClient;
        private static DateTime tokenExpiresAtUtc = DateTime.MinValue;
        IConfigurationRoot settingsCache;
        static string tokenValue = string.Empty;
        static AppOptions appOptions;
        static string mensagemErro = "";

        private static string resourceGroupName = "PontoCargaServiceBus";
        const string namespaceName = "pontocarga";

        /// <summary>
        /// Envia mensagem para o service bus e cria a fila especificada para o identificador caso o mesmo não exista
        /// </summary>
        /// <param name="identificador">id da fila</param>
        /// <returns></returns>
        [HttpPost]
        public void Post([FromBody] string identificador)
        {
            ConfiguracaoInicial();
            CriarQueueSeNaoExistir().ConfigureAwait(true);

            queueName = string.Format(queueName, identificador);

            queueClient = new QueueClient(ServiceBusConnectionString, queueName);

            EnviarMensagem(queueName).GetAwaiter().GetResult();
        }

        void ConfiguracaoInicial()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", true, true);

            settingsCache = builder.Build();
            appOptions = new AppOptions();
            settingsCache.Bind(appOptions);
        }

        private static async Task CriarQueueSeNaoExistir()
        {
            try
            {
                if (string.IsNullOrEmpty(namespaceName))
                {
                    throw new Exception("Namespace nao existe!");
                }

                var token = await GetToken();

                var creds = new TokenCredentials(token);
                var sbClient = new ServiceBusManagementClient(creds)
                {
                    SubscriptionId = appOptions.SubscriptionId,
                };

                var queueParams = new SBQueue
                {
                    EnablePartitioning = true
                };

                await sbClient.Queues.CreateOrUpdateAsync(resourceGroupName, namespaceName, queueName, queueParams);
            }
            catch (Exception e)
            {
                throw new Exception("Nao foi possivel criar fila a queue...");
            }
        }

        private static async Task<string> GetToken()
        {
            try
            {
                if (tokenExpiresAtUtc < DateTime.UtcNow.AddMinutes(-2))
                {
                    var tenantId = appOptions.TenantId;
                    var clientId = appOptions.ClientId;
                    var clientSecret = appOptions.ClientSecret;

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
            catch (Exception e)
            {
                throw new Exception("Nao foi possivel obter um token...");
                throw e;
            }
        }

        static async Task EnviarMensagem(string queueName)
        {
            string acao = "ArquivoPesagem";
            string conteudo = "Cliente: Fonseca gordinho, Traco: Bengay";

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
