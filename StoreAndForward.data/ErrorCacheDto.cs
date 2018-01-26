using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace StoreAndForward.Cache
{
    public class MessageCacheDto
    {
        
        public MessageCacheDto()
        {
            this.Id = Guid.NewGuid();
        }

        [PrimaryKey]
        public Guid Id { get; set; }

        public string MessageToSend { get; set; }
    }
}
