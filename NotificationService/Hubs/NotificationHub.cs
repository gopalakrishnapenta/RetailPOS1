using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinStoreGroup(int storeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Store_{storeId}");
        }

        public async Task SendNotification(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", user, message);
        }

        public override async Task OnConnectedAsync()
        {
            var storeId = Context.User?.FindFirst("StoreId")?.Value;
            if (!string.IsNullOrEmpty(storeId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Store_{storeId}");
            }

            await base.OnConnectedAsync();
        }
    }
}
