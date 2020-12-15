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
        private List<Player> players;
        private List<Card> deck;
        private Stack<Card> discardPile;
        private int currentPlayer;

        public OneService()
        {
            string[] colours = new string[4] { "Red", "Blue", "Green", "Yellow" };

            players = new List<Player>();
            deck = new List<Card>();
            discardPile = new Stack<Card>();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    deck.Add(new Card {  Name = "Reverse", Value = 10, Colour = colours[i] });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    deck.Add(new Card {  Name = "Skip", Value = 10, Colour = colours[i] });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    deck.Add(new Card {  Name = "Draw Two", Value = 10, Colour = colours[i] });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                deck.Add(new Card { Name = "Wild", Value = 10, Colour = "Wild" });
            }
            for (int i = 0; i < 4; i++)
            {
                deck.Add(new Card { Name = "Draw Four", Value = 10, Colour = "Wild" });
            }
            for (int c = 0; c < 4; c++)
            {
                for (int i = 0; i <= 9; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        deck.Add(new Card { Name = $"{i}", Value = i, Colour = colours[c] });
                    }
                }
            }

            int count = deck.Count;
            Random r = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < 1000; i++)
            {
                int j = r.Next(0, count - 1);
                Card c = deck.ElementAt(j);
                deck.RemoveAt(j);
                deck.Add(c);
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
            players.Add(player);

            currentPlayer = 0;

            await Clients.All.SendAsync("PlayerList", players);
        }

        public async Task StartGame()
        {
            // deal some cards
            for (int i = 0; i < 6; i++)
            {
                foreach (Player player in players)
                {
                    Card card = deck.ElementAt(0);
                    deck.RemoveAt(0);
                    await player.GetClient().SendAsync("HaveCard", card);
                }
            }

            // turn over first card
            Card discard = deck.ElementAt(0);
            deck.RemoveAt(0);
            discardPile.Push(discard);
            await Clients.All.SendAsync("CardDiscarded", discard);

            // notify first player their go
            await players[currentPlayer].GetClient().SendAsync("YourTurn");
        }

        public async Task PickupCard()
        {
            // send the top card from the deck to the player
            Card top = deck.ElementAt(0);
            deck.RemoveAt(0);
            await players[currentPlayer].GetClient().SendAsync("HaveCard", top);

            // next player go
            currentPlayer = (currentPlayer + 1) % players.Count;
            await players[currentPlayer].GetClient().SendAsync("YourTurn");
        }

        public async Task PlayCard(Card card)
        {
            // tell everyone the card was discarded
            discardPile.Push(card);
            await Clients.All.SendAsync("CardDiscarded", card);

            // next player go
            currentPlayer = (currentPlayer + 1) % players.Count;
            await players[currentPlayer].GetClient().SendAsync("YourTurn");
        }
    }
}
