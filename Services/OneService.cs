using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gameserver.Data;
using Microsoft.AspNetCore.SignalR;

// TODO: always assuming there are cards in the deck, if the deck is ever empty we need to reshuffle the discard pile and make that the deck
// TODO: if start with one player enter solitaire mode - either timed challenge or least moves to empty hand wins

namespace gameserver.Services
{
    public class OneService : Hub
    {
        private List<Player> players;
        private List<Card> deck;
        private Stack<Card> discardPile;
        private int currentPlayer;
        private bool started = false;
        private bool forward = true;
        private int scores = 0;

        public OneService()
        {
            players = new List<Player>();
            discardPile = new Stack<Card>();

            InitialiseDeck();
        }

        private void InitialiseDeck()
        {
            string[] colours = new string[4] { "Red", "Blue", "Green", "Yellow" };

            deck = new List<Card>();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    deck.Add(new Card {  Name = "R", Value = 10, Colour = colours[i] });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    deck.Add(new Card {  Name = "S", Value = 10, Colour = colours[i] });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    deck.Add(new Card {  Name = "+2", Value = 10, Colour = colours[i] });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                deck.Add(new Card { Name = "W", Value = 10, Colour = "Wild" });
            }
            for (int i = 0; i < 4; i++)
            {
                deck.Add(new Card { Name = "+4", Value = 10, Colour = "Wild" });
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

            // simple deck shuffle, for a large number of times, remove a random card and add it to the back of the deck
            int count = deck.Count;
            Random r = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < 1000; i++)
            {
                int j = r.Next(0, count - 1);
                Card c = deck.ElementAt(j);
                deck.RemoveAt(j);
                deck.Add(c);
            }
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
            Console.WriteLine($"{name} joined the fun");

            Player player = new Player();
            player.Name = name;
            player.SetClient(Clients.Caller);
            players.Add(player);

            await Groups.AddToGroupAsync(Context.ConnectionId, "Players");

            await Clients.Caller.SendAsync("YouAre", player);

            await Clients.Group("Players").SendAsync("PlayerList", players);
        }

        public async Task StartGame()
        {
            if (started) return;
            else started = true;

            scores = 0;
            currentPlayer = 0;

            await Clients.Group("Players").SendAsync("GameStarted");

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

            // turn over first card - if it's a wildcard, choose a random colour?
            // Card discard = deck.ElementAt(0);
            // deck.RemoveAt(0);
            // discardPile.Push(discard);
            // await Clients.Group("Players").SendAsync("CardDiscarded", discard, discard.Colour);

            // notify first player their go
            await Clients.Group("Players").SendAsync("ActivePlayer", players[currentPlayer]);
        }

        private void NextPlayer()
        {
            if (forward)
            {
                currentPlayer = (currentPlayer + 1) % players.Count;
            }
            else
            {
                currentPlayer--;
                if (currentPlayer < 0) currentPlayer = players.Count - 1;
            }
        }

        public async Task PickupCard()
        {
            // send the top card from the deck to the player
            Card top = deck.ElementAt(0);
            deck.RemoveAt(0);
            await players[currentPlayer].GetClient().SendAsync("HaveCard", top);

            // next player go
            // TODO: not necessarily, it could be that the current player could play the card they just picked up
            NextPlayer();
            await Clients.Group("Players").SendAsync("ActivePlayer", players[currentPlayer]);
        }

        public async Task PlayCard(Card card, string colourOverride, bool lastCard)
        {
            if (lastCard)
            {
                await Clients.Group("Players").SendAsync("Winner", players[currentPlayer]);
            }
            else
            {
                // tell everyone the card was discarded
                discardPile.Push(card);
                await Clients.Group("Players").SendAsync("CardDiscarded", card, colourOverride);

                if (card.Name == "S")
                {
                    NextPlayer();
                }
                else if (card.Name == "R")
                {
                    forward = !forward;
                }

                // next player go
                NextPlayer();

                if (card.Name == "+2")
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Card top = deck.ElementAt(0);
                        deck.RemoveAt(0);
                        await players[currentPlayer].GetClient().SendAsync("HaveCard", top);
                    }
                }
                if (card.Name == "+4")
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Card top = deck.ElementAt(0);
                        deck.RemoveAt(0);
                        await players[currentPlayer].GetClient().SendAsync("HaveCard", top);
                    }
                }
                
                await Clients.Group("Players").SendAsync("ActivePlayer", players[currentPlayer]);
            }
        }

        public async Task Score(Player player, int score)
        {
            var match = players.Where(p => p.Name == player.Name).First();
            if (match != null)
            {
                match.Score += score;
            }

            scores++;

            if (scores == players.Count)
            {                
                await Clients.Group("Players").SendAsync("PlayerList", players);

                await ResetGame();
            }
        }

        public async Task ResetGame()
        {
            discardPile = new Stack<Card>();
            started = false;

            InitialiseDeck();

            await Clients.Group("Players").SendAsync("ResetGame");
        }
    }
}
