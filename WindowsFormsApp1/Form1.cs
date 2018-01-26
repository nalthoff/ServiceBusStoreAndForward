using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using StoreAndForward.Cache;
using Microsoft.Azure.ServiceBus.Core;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private SqlLiteErrorCache _cache = null;
        private readonly SynchronizationContext _syncContext;
        private static System.Threading.Timer _cacheTimer;
        private readonly SvcBusSender _sender;
        private System.Timers.Timer _sendTimer;

        public Form1()
        {
            _cache = new SqlLiteErrorCache();
            InitializeComponent();
            SetUpTimers();
            _syncContext = SynchronizationContext.Current;
            _sender = new SvcBusSender();
            RebuidListBox();
        }

        private void SetUpTimers()
        {
            TimerCallback timerDelegate = SendCachedErrors;
            var dueTime = new TimeSpan(0, 0, 5);
            var interval = new TimeSpan(0, 0, 5);
            _cacheTimer = new System.Threading.Timer(timerDelegate, null, dueTime, interval);

            _sendTimer = new System.Timers.Timer(3000);
            _sendTimer.Elapsed += _sendTimer_Elapsed;
        }

        private object padlock = new object();
        private void SendCachedErrors(object state)
        {
            lock (padlock)
            {
                SendFromLocalCache();
            }
        }

        private async void SendFromLocalCache()
        {
            var allCachedmsgs = await _cache.GetAll();
            if (allCachedmsgs == null || allCachedmsgs.Count == 0)
            {
                return;
            }

            foreach (var itemToSend in allCachedmsgs)
            {
                var result = _sender.SendMessageToQueue(itemToSend.MessageToSend).GetAwaiter().GetResult();
                if (result)
                {
                    await _cache.Remove(new MessageCacheDto() { Id = itemToSend.Id });
                }
            }
            RebuidListBox();
        }

        private void btnSendMessage_ClickAsync(object sender, EventArgs e)
        {
            _sendTimer.Start();
        }

        private void _sendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string messageBody = $"New Message at:{DateTime.Now.ToString()}";

            try
            {
                var result =
                    Task.Run(() => _sender.SendMessageToQueue(messageBody)).Result;

                if (!result)
                {
                    AddMessageToCache(messageBody);
                }
            }
            catch (Exception ex)
            {
                AddMessageToCache(messageBody);
            }
        }

        private void AddMessageToCache(string messageBody)
        {
            var dto = new MessageCacheDto() { MessageToSend = messageBody };
            Task.Run(() => _cache.Add(dto));
            RebuidListBox();
        }

        private void RebuidListBox()
        {
            _syncContext.Post(new SendOrPostCallback(x =>
            {
                List<string> cacheItems = new List<string>();
                var allItems = Task.Run(() => _cache.GetAll()).Result;

                listBox1.Items.Clear();
                foreach (var itm in allItems)
                {
                    listBox1.Items.Add($"{itm.Id} | {itm.MessageToSend}");
                }
                listBox1.Refresh();
            }), null);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        //private void btnDeleteMessage_Click(object sender, EventArgs e)
        //{
        //    if (listBox1.SelectedIndex > -1)
        //    {
        //        var selectedItem = (string)listBox1.SelectedItem;
        //        var itemID = selectedItem.Split('|')[0].Trim();
        //        Task.Run(() => _cache.Remove(new MessageCacheDto() { Id = Guid.Parse(itemID) }));

        //        RebuidListBox();
        //    }
        //}

        private void button1_Click(object sender, EventArgs e)
        {
            RebuidListBox();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _sendTimer.Stop();
        }
    }
}
