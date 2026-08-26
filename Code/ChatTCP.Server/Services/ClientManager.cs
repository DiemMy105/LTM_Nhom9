using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ChatTCP.Server.Network;
using ChatTCP.Shared.Models;
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Server.Services
{
    public class ClientManager
    {
        private readonly ConcurrentDictionary<int, ClientConnection> _clientsByUserId =
            new ConcurrentDictionary<int, ClientConnection>();

        private readonly ConcurrentDictionary<string, ClientConnection> _clientsByUsername =
            new ConcurrentDictionary<string, ClientConnection>(StringComparer.OrdinalIgnoreCase);

        public event Action<ClientConnection>? ClientAdded;
        public event Action<ClientConnection>? ClientRemoved;

        public void AddClient(ClientConnection client)
        {
            if (client == null) return;

            if (client.UserId > 0)
            {
                _clientsByUserId[client.UserId] = client;
            }

            if (!string.IsNullOrWhiteSpace(client.Username))
            {
                _clientsByUsername[client.Username] = client;
            }

            ClientAdded?.Invoke(client);
        }

        public void RemoveClient(ClientConnection client)
        {
            if (client == null) return;

            if (client.UserId > 0)
            {
                _clientsByUserId.TryRemove(client.UserId, out _);
            }

            if (!string.IsNullOrWhiteSpace(client.Username))
            {
                _clientsByUsername.TryRemove(client.Username, out _);
            }

            ClientRemoved?.Invoke(client);
        }

        public void RemoveClient(int userId)
        {
            if (_clientsByUserId.TryRemove(userId, out var client))
            {
                if (!string.IsNullOrWhiteSpace(client.Username))
                {
                    _clientsByUsername.TryRemove(client.Username, out _);
                }

                ClientRemoved?.Invoke(client);
            }
        }

        public ClientConnection? GetClient(int userId)
        {
            _clientsByUserId.TryGetValue(userId, out var client);
            return client;
        }

        public ClientConnection? GetClient(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            _clientsByUsername.TryGetValue(username, out var client);
            return client;
        }

        public bool ForwardMessageToClient(int receiverId, Message message)
        {
            var client = GetClient(receiverId);
            if (client != null && client.IsConnected)
            {
                client.SendMessage(message);
                return true;
            }
            return false;
        }

        public bool ForwardMessageToClient(string username, Message message)
        {
            var client = GetClient(username);
            if (client != null && client.IsConnected)
            {
                client.SendMessage(message);
                return true;
            }
            return false;
        }

        public void Broadcast(Message message, int? excludeUserId = null)
        {
            foreach (var kvp in _clientsByUserId)
            {
                if (excludeUserId.HasValue && kvp.Key == excludeUserId.Value)
                    continue;

                if (kvp.Value.IsConnected)
                {
                    kvp.Value.SendMessage(message);
                }
            }
        }

        public void DisconnectClient(string username)
        {
            var client = GetClient(username);
            if (client != null)
            {
                RemoveClient(client);
                client.Disconnect();
            }
        }

        public void DisconnectClient(int userId)
        {
            var client = GetClient(userId);
            if (client != null)
            {
                RemoveClient(client);
                client.Disconnect();
            }
        }

        public List<ClientConnection> GetAllClients()
        {
            return _clientsByUserId.Values.Where(c => c.IsConnected).ToList();
        }

        public int Count => _clientsByUserId.Count;
    }
}