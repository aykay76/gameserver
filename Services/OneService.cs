using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gameserver.Data;
using Microsoft.AspNetCore.SignalR;

namespace gameserver.Services
{
    public class OneService : Hub
    {
        private List<Player> Players { get; set; }
        private List<Card> Deck { get; set; }
        private Stack<Card> Discard { get; set; }

        public OneService()
        {
            string[] colours = new string[4] { "Red", "Blue", "Green", "Yellow" };

            Players = new List<Player>();
            Deck = new List<Card>();
            Discard = new Stack<Card>();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Deck.Add(new Card {  Name = "Reverse", Value = 10, Colour = colours[i] });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Deck.Add(new Card {  Name = "Skip", Value = 10, Colour = colours[i] });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Deck.Add(new Card {  Name = "Draw Two", Value = 10, Colour = colours[i] });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                Deck.Add(new Card { Name = "Wild", Value = 10, Colour = "Wild" });
            }
            for (int i = 0; i < 4; i++)
            {
                Deck.Add(new Card { Name = "Draw Four", Value = 10, Colour = "Wild" });
            }
            for (int c = 0; c < 4; c++)
            {
                for (int i = 0; i <= 9; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        Deck.Add(new Card { Name = $"{i}", Value = i, Colour = colours[c] });
                    }
                }
            }

            int count = Deck.Count;
            Random r = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < 1000; i++)
            {
                int j = r.Next(0, count - 1);
                Card c = Deck.ElementAt(j);
                Deck.RemoveAt(j);
                Deck.Add(c);
            }
            Console.WriteLine("Shuffled, ready to go");
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"{Context.ConnectionId} connected");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception e)
        {
            Console.WriteLine($"Disconnected {e?.Message} {Context.ConnectionId}");
            await base.OnDisconnectedAsync(e);
        }

        public async Task JoinGame(string name)
        {
            Player player = new Player();
            player.Name = name;
            player.SetClient(Clients.Caller);
            Players.Add(player);

            await Clients.All.SendAsync("PlayerList", Players);
        }

        public async Task StartGame()
        {
            // deal some cards
            for (int i = 0; i < 6; i++)
            {
                foreach (Player player in Players)
                {
                    Card card = Deck.ElementAt(0);
                    Deck.RemoveAt(0);
                    await player.GetClient().SendAsync("HaveCard", card);
                }
            }

            // turn over first card
            Card discard = Deck.ElementAt(0);
            Deck.RemoveAt(0);
            Discard.Push(discard);
            await Clients.All.SendAsync("CardDiscarded", discard);

            // notify first player their go
        }
    }
}
