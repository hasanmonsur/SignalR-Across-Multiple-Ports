using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MultiPortSignalR.Data;

var builder = WebApplication.CreateBuilder(args);

// Connect to PostgreSQL to get ports
var portDbConn = "Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=postgres";
builder.Services.AddDbContext<PortContext>(options =>
    options.UseNpgsql(portDbConn));

// Build temporary service provider to query DB
using var tempProvider = builder.Services.BuildServiceProvider();

using var portDb = tempProvider.GetRequiredService<PortContext>();

var portList = await portDb.AppPorts.Select(p => p.Port).ToListAsync();

if (!portList.Any())
    portList.Add(5000); // fallback

// Configure Kestrel to listen on all retrieved ports
builder.WebHost.ConfigureKestrel(options =>
{
    foreach (var port in portList.Distinct())
    {
        options.ListenAnyIP(port);
    }
});

// Add SignalR and app services
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapHub<ChatHub>("/chathub");
Console.WriteLine("? SignalR Server is running at http://localhost:5050/chathub");

//app.MapGet("/", () => $"SignalR running on ports: {string.Join(", ", portList)}");

app.Run();


public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"?? Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"? Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string user, string message)
    {
        Console.WriteLine($"?? Message received from {user}: {message}");
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
