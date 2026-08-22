using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ChatTCP.Server.Services
{
    public class ClientManager
    {
        private readonly ConcurrentDictionary<int, TcpClient> _onlineClients = new ConcurrentDictionary<int, TcpClient>();
        private readonly DatabaseService _dbService = new DatabaseService();

        public void AddClient(int userId, TcpClient client)
        {
            _onlineClients[userId] = client;

            if (_dbService.UpdateUserStatus(userId, true))
            {
                Console.WriteLine($"[Presence-Service] Node {userId} gắn kết thành công (ONLINE).");
            }
        }

        public void RemoveClient(int userId)
        {
            if (_onlineClients.TryRemove(userId, out _))
            {
                if (_dbService.UpdateUserStatus(userId, false))
                {
                    Console.WriteLine($"[Presence-Service] Node {userId} đã ngắt kết nối (OFFLINE).");
                }
            }
        }

        public TcpClient GetClient(int userId)
        {
            _onlineClients.TryGetValue(userId, out TcpClient client);
            return client;
        }

        public List<int> GetAllOnlineUserIds()
        {
            return new List<int>(_onlineClients.Keys);
        }
    }
}