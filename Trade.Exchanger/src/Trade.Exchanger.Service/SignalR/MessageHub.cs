using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Trade.Exchanger.Service.StateMachines;

namespace Trade.Exchanger.Service.SignalR
{
    [Authorize]
    public class MessageHub : Hub
    {
        public async Task SendStatusAsync(PurchaseState status)
        {
            if (Clients != null)
            {
                await Clients.User(Context.UserIdentifier)
                       .SendAsync("ReceivePurchaseStatus", status);
            }
        }
    }
}