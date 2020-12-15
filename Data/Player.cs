using System;
using Microsoft.AspNetCore.SignalR;

namespace gameserver.Data
{
    public class Player
    {
        public string Name { get; set; }
        public int Score { get; set; }

        private IClientProxy client;

        public void SetClient(IClientProxy c)
        {
            client = c;
        }

        public IClientProxy GetClient()
        {
            return client;
        }
    }
}
