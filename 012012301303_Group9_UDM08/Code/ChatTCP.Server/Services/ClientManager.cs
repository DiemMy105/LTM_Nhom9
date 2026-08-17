using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ChatTCP.Server.Services
{
    /// <summary>
    /// Quản lý vòng đời kết nối và đồng bộ trạng thái hiện diện (Presence State) của Client.
    /// Đảm bảo tính nhất quán dữ liệu giữa In-memory (RAM) và Persistence (Database).
    /// </summary>
    public class ClientManager
    {
        // Thread-safe map lưu trữ các luồng mạng đang hoạt động (Active Sessions)
        private readonly ConcurrentDictionary<int, TcpClient> _onlineClients = new ConcurrentDictionary<int, TcpClient>();

        private readonly DatabaseService _dbService = new DatabaseService();

        /// <summary>
        /// Khởi tạo phiên làm việc khi Client xác thực thành công và đồng bộ trạng thái trực tuyến.
        /// </summary>
        public void AddClient(int userId, TcpClient client)
        {
            // Cấp phát hoặc ghi đè luồng mạng cho định danh tương ứng
            _onlineClients[userId] = client;

            // Cập nhật cờ trạng thái xuống Persistence layer
            if (_dbService.UpdateUserStatus(userId, true))
            {
                Console.WriteLine($"[Presence-Service] Node {userId} gắn kết thành công (ONLINE).");

                // TODO: Dispatch sự kiện cập nhật trạng thái đến các Client khác
            }
        }

        /// <summary>
        /// Thu hồi tài nguyên và chuyển trạng thái ngoại tuyến khi phát hiện mất tín hiệu mạng.
        /// </summary>
        public void RemoveClient(int userId)
        {
            // Gỡ bỏ luồng khỏi bộ nhớ quản lý
            if (_onlineClients.TryRemove(userId, out _))
            {
                // Đồng bộ cờ trạng thái Offline xuống Database
                if (_dbService.UpdateUserStatus(userId, false))
                {
                    Console.WriteLine($"[Presence-Service] Node {userId} đã ngắt kết nối (OFFLINE).");

                    // TODO: Dispatch sự kiện cập nhật trạng thái đến các Client khác
                }
            }
        }

        /// <summary>
        /// Truy xuất Socket của Client đang hoạt động để định tuyến gói tin (Message Routing).
        /// </summary>
        public TcpClient GetClient(int userId)
        {
            _onlineClients.TryGetValue(userId, out TcpClient client);
            return client;
        }

        /// <summary>
        /// Trích xuất danh sách định danh của toàn bộ mạng lưới Client đang duy trì kết nối.
        /// </summary>
        public List<int> GetAllOnlineUserIds()
        {
            return new List<int>(_onlineClients.Keys);
        }
    }
}