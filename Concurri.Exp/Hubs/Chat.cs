using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Concurri.Exp.Hubs
{
    [Authorize]
    public class Chat : HubWithPresence
    {
        public Chat(IUserTracker<Chat> userTracker)
            : base(userTracker)
        {
        }

        //public override async Task OnConnectedAsync()
        //{
        //    await Clients.Client(Context.ConnectionId).InvokeAsync("SetUsersOnline", await GetUsersOnline());

        //    await base.OnConnectedAsync();
        //}

        //public override Task OnUsersJoined(UserDetails[] users)
        //{
        //    return Clients.Client(Context.ConnectionId).InvokeAsync("UsersJoined", new[] { users });
        //}

        //public override Task OnUsersLeft(UserDetails[] users)
        //{
        //    return Clients.Client(Context.ConnectionId).InvokeAsync("UsersLeft", new[] { users });
        //}

        //public async Task Send(string message)
        //{
        //    await Clients.All.InvokeAsync("Send", Context.User.Identity.Name, message);
        //}
    }
}
