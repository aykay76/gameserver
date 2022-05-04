# Gameserver

It was during COVID lockdown, my kids were bored. I was bored. So I decided to code up a version of a card game called Uno, which we all enjoyed. I hadn't developed a game server where multiple players could play on their own devices before, and I wanted an excuse to learn more about Blazor/WASM. So I decided to code up a game server with a game called One.

The rules of the game are probably quite well known but i'll give a quick summary here. 

1. Each player is dealt an initial hand of 6 cards, the rest of the cards make up the draw deck and the first card is turned over to seed the discard pile.
2. The first player must play a card that has the same number or colour as the card on top of the discard pile. 
3. Play commences in a clockwise fashion, unless a reverse card is played, this reverses play.
4. If the previous player drops a "+2" or "+4" card the next player loses their turn and instead picks up the appropriate number of cards.
5. If a player can't play a valid card they pick up from the deck.
6. The first player to play all their cards wins.

It's pretty simple to learn to play, with elements of strategy and luck.

On to the code...

I will start by saying that I daresay this could be made simpler. Like I say, this was an opportunity to learn more about client side C# code and how to interact with a server. It could have been implemented using a simple Javascript/API model. But where would be the fun in that? 🙂

The gameserver is hosted on a "server" computer that will act as the game server. Each person who wants to play can then go to the server in a browser where they will see the gameserver home page. From there they can "Play One" by clicking on the menu. This will activate the `One.razor` page.

On initialisation the page will create a hub connection back to the server. This is a SignalR connection that allows the client to receive notifications from the server via server sent events:

```cs
        hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/onesignal"))
            .Build();
```

Now the client can send messages to the server and receive updates from the server. To send a message in response to an event on the browser (clicking a button or something) you simply call something like:

```cs
await hubConnection.SendAsync("JoinGame", username);
```

And to receive messages from the server it's a simple case of registering 'event handlers':

```cs
hubConnection.On("GameStarted", () =>
{
    log.Add("Game has started");
    if (log.Count > 5) log.RemoveAt(0);

    started = true;
    StateHasChanged();
});
```

On the server side, a SignalR service is added to the server by adding a singleton service to the dependency injection container:

```cs
services.AddSingleton<OneService>();
```

The service class in question is derived from the `Hub` class which has overrides which you can implement to handle incoming connections from clients etc.:

```cs
public override Task OnConnectedAsync()
{
    Console.WriteLine($"{Context.ConnectionId} connected");
    return base.OnConnectedAsync();
}
```

Additional methods can then be added that correspond to the names in the SendAsync function called from the clients.

Using SignalR clients can be targeted individually for messages, can be added to a group or broadcast messages can be sent to all clients:

```cs
await Groups.AddToGroupAsync(Context.ConnectionId, "Players");
await Clients.Caller.SendAsync("YouAre", player);
await Clients.Group("Players").SendAsync("PlayerList", players);
```

The rest of the documentation is in the code 🙂
