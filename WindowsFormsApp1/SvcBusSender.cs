using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace WindowsFormsApp1
{
    public class SvcBusSender
    {
        private IMessageSender primarymessageSender;
        private IMessageSender secondaryMessageSender;

        public async Task<bool> SendMessageToQueue(string messageToSend)
        {
            var isSent = await SendToPrimary(messageToSend);
            if (!isSent)
            {
                isSent = await SendToSecondary(messageToSend);
            }
            return isSent;

        }

        private async Task<bool> SendToSecondary(string messageToSend)
        {
            var client = GetSecondaryClient();
            return await SendMessageToClient(messageToSend, client);
        }

        private async Task<bool> SendToPrimary(string messageToSend)
        {
            var client = GetPrimaryClient();
            return await SendMessageToClient(messageToSend, client);
        }

        private static async Task<bool> SendMessageToClient(string messageToSend, IMessageSender client)
        {
            var message = new Message(Encoding.UTF8.GetBytes(messageToSend))
            {
                MessageId = Guid.NewGuid().ToString()
            };
            try
            {
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        private IMessageSender GetPrimaryClient()
        {
            if (primarymessageSender == null)
            {
                var connectionString = ConfigurationManager.AppSettings["svcBusPrimary"];
                var qName = ConfigurationManager.AppSettings["svcBusQueueName"];
                primarymessageSender = GetMessageSender(connectionString, qName);
            }
            return primarymessageSender;
        }

        private IMessageSender GetSecondaryClient()
        {
            if (secondaryMessageSender == null)
            {
                var connectionString = ConfigurationManager.AppSettings["svcBusSecondary"];
                var qName = ConfigurationManager.AppSettings["svcBusQueueName"];
                secondaryMessageSender = GetMessageSender(connectionString, qName);
            }
            return secondaryMessageSender;
        }

        private IMessageSender GetMessageSender(string connectionString, string queueName)
        {
            var tsMinBackoff = new TimeSpan(0, 0, 1);
            var tsMaxBackoff = new TimeSpan(0, 0, 5);
            var maxRetryCount = 5;
            var retry = new RetryExponential(tsMinBackoff, tsMaxBackoff, maxRetryCount);
            return new MessageSender(connectionString, queueName, retry);
        }
    }
}
