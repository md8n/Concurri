using System;
using System.Collections.Generic;
using System.Threading.Tasks;
//using Microsoft.AspNetCore.SignalR;

namespace Concurri.Exp
{
    public interface IUserTracker<out THub>
    {
        Task<IEnumerable<UserDetails>> UsersOnline();
        //Task AddUser(HubConnectionContext connection, UserDetails userDetails);
        //Task RemoveUser(HubConnectionContext connection);

        event Action<UserDetails[]> UsersJoined;
        event Action<UserDetails[]> UsersLeft;
    }
}
