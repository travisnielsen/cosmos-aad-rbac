using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CosmosRbac
{
    class Program
    {
        private static TokenCredential credential;
        private static string databaseId;
        private static string containerId;
        private static string command;
        private static string itemData; 

        static async Task Main(string[] args)
        {
            try
            {
                command = args[0];

                if (command.ToLower() == "create")
                {
                    try
                    {
                        itemData = String.IsNullOrEmpty(args[1]) ? "" : args[1];
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(ex.Message);
                    }

                }

                IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true).Build();

                string endpoint = configuration["endPointUrl"];
                databaseId = configuration["database"];
                containerId = configuration["container"];

                if (string.IsNullOrEmpty(endpoint))
                {
                    throw new ArgumentNullException("Please specify a valid endpoint in the appSettings.json");
                }

                
                if (string.IsNullOrEmpty(configuration["AZURE_CLIENT_ID"]))
                {
                    credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions());
                }
                else
                {
                    string clientId = configuration["AZURE_CLIENT_ID"];
                    string tenantId = configuration["AZURE_TENANT_ID"];
                    string clientSecret = configuration["AZURE_CLIENT_SECRET"];
                    credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                }

                using (CosmosClient client = new CosmosClient(endpoint, credential))
                {
                    switch (command.ToLower())
                    {
                        case "create":
                            await Program.CreateItem(client, itemData);
                            break;
                        case "read":
                            await Program.ReadItems(client);
                            break;
                        default:
                            Console.WriteLine("Not a valid command. Try 'create' or 'read'");
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async Task CreateItem(CosmosClient client, string data)
        {
            Container container = client.GetContainer(databaseId, containerId);
            Person item = JsonConvert.DeserializeObject<Person>(data);
            item.Id = Guid.NewGuid().ToString();
            ItemResponse<Person> response = await container.CreateItemAsync<Person>(item, new PartitionKey(item.Id));
            Console.WriteLine(response.Resource.ToString());
        }

        private static async Task ReadItems(CosmosClient client)
        {
            Container container = client.GetContainer(databaseId, containerId);
            QueryDefinition query = new QueryDefinition("SELECT * FROM c");
            FeedIterator<Person> queryIterator = container.GetItemQueryIterator<Person>(query);
            List<Person> people = new List<Person>();

            while (queryIterator.HasMoreResults)
            {
                FeedResponse<Person> resultSet = await queryIterator.ReadNextAsync();
                foreach (Person person in resultSet)
                {
                    Console.WriteLine(person);
                }
            }

        }

    }

}
