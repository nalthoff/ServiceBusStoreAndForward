using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using StoreAndForward.Cache;
using System;
using System.IO;
using System.Text;

namespace StoreAndForward
{
    class Program
    {
        static bool connected = false;
        //static string serviceBusConnectionString = "Endpoint=sb://paylocitystoreandforward.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=qt7KwCNCbpNs81RyKBLOfrRALwtq/Uu0QM7SWyftWbY=";
        //static string QueueName = "storeandforward";

        static string instructions = $"Instructions: \r\n Enter 'a' to add an item to the cache."+
            " \r\n Enter 'g' to retrieve all items. "+
            "\r\n Enter 'c' to clear the cache. "+
            "\r\n Enter 'f' to remove the first item in the cache."+
            "/r/n Enter 'bus' to toggle sending to Service bus." +
            " \r\n Enter the ID of an item to remove it.";

        static void Main(string[] args)
        {
            SetupConfig();
            var cache = new SqlLiteErrorCache();
            Console.WriteLine("*** Starting up *** ");
            Console.WriteLine($"Connected to Service bus:{connected}");
            Console.WriteLine($"Number of items in the cache:{cache.Count()}");

            Console.WriteLine(instructions);

            var input = Console.ReadLine();

            while (input.ToLower() != "q")
            {
                switch (input)
                {
                    case "a":
                        {
                            var itm = new MessageCacheDto
                            {
                                MessageToSend = $"Current Date / Time is {DateTime.Now.ToString()}"
                            };

                            Console.WriteLine($"Adding a new item to the cache. ID:{itm.Id} - msg {itm.MessageToSend}");

                            cache.Add(itm).GetAwaiter().GetResult();
                            break;
                        }
                    case "g":
                        {
                            Console.WriteLine("Writing all items in the Cache");
                            var allItems = cache.GetAll().GetAwaiter().GetResult();

                            foreach (var itm in allItems)
                            {
                                Console.WriteLine($"ID:{itm.Id} - msg {itm.MessageToSend}");
                            }
                            break;
                        }
                    case "c":
                        {
                            Console.WriteLine("Removing all items from the cache");
                            cache.Clear().GetAwaiter().GetResult();
                            break;
                        }
                    case "f":
                        {
                            var allItems = cache.GetAll().GetAwaiter().GetResult();
                            if (allItems != null && allItems.Count > 0)
                            {
                                cache.Remove(allItems[0]).GetAwaiter().GetResult();
                            }
                            break;
                        }
                    case "bus":
                        {
                            connected = !connected;
                            Console.WriteLine($"Connected to Service bus:{connected}");
                            break;
                        }
                    case "send":
                        {
                            var messageBody = "Curent Time is:" + DateTime.Now.ToString();
                            Console.WriteLine($"Sending message to Service bus:{messageBody}");
                            var sbConnection = Configuration["svcBusPrimary"];
                            var queueName = Configuration["svcBusQueueName"];
                            IMessageSender messageSender = new MessageSender(sbConnection, queueName);
                            var message = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(messageBody));
                            //client.SendAsync(message);
                            var tskSend = messageSender.SendAsync(message).ConfigureAwait(false);
                            break;
                        }
                    default:
                        {

                            if (Guid.TryParse(input, out Guid itemID))
                            {
                                Console.WriteLine($"Deleting item:{input}");

                                var itemToRemove = new MessageCacheDto()
                                {
                                    Id = itemID
                                };
                                cache.Remove(itemToRemove).GetAwaiter().GetResult();
                                Console.WriteLine("Item Removed");
                            }
                            else
                            {
                                Console.WriteLine("invalid input.");
                                Console.WriteLine(instructions);
                            }
                            break;
                        }
                }
                Console.WriteLine($"Number of items in the cache:{cache.Count()}");
                input = Console.ReadLine();
            }

            Console.WriteLine("Exiting...");
            return;
        }

        private static IConfigurationRoot Configuration;

        private static void SetupConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }
    }
}
