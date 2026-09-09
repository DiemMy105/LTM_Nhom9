using System;
using System.IO;
using System.Text.Json;

namespace ChatTCP.Shared.Utils
{
    
    /// Các thông số cấu hình mạng
    public class NetworkSettings
    {
        /// Địa chỉ IP hoặc Domain của Server
        public string ServerIp { get; set; } = "127.0.0.1";
        /// Cổng Port kết nối TCP Socket giữa Client và Server
        public int ServerPort { get; set; } = 8888;
        /// Kích thước bộ đệm (Buffer Size) đọc/ghi TCP Socket (bytes)
        public int BufferSize { get; set; } = 8192;
        /// Thời gian chờ kết nối tối đa (milliseconds)
        public int ConnectTimeoutMs { get; set; } = 5000;
        /// Thời gian chờ gửi dữ liệu (milliseconds) - 0 là vô hạn
        public int SendTimeoutMs { get; set; } = 5000;
        /// Thời gian chờ nhận dữ liệu (milliseconds) - 0 là vô hạn
        public int ReceiveTimeoutMs { get; set; } = 0;
    }
    /// Quản lý cấu hình mạng, đọc/ghi tự động từ file network_config.json
    public static class NetworkConfig
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "network_config.json");

        public static NetworkSettings Settings { get; private set; } = new NetworkSettings();
        /// Địa chỉ IP hiện tại của Server
        public static string ServerIp
        {
            get => Settings.ServerIp;
            set => Settings.ServerIp = value;
        }
        /// Cổng Port hiện tại của Server
        public static int ServerPort
        {
            get => Settings.ServerPort;
            set => Settings.ServerPort = value;
        }
        /// Kích thước bộ đệm Socket
        public static int BufferSize
        {
            get => Settings.BufferSize;
            set => Settings.BufferSize = value;
        }
        /// Timeout kết nối (ms)
        public static int ConnectTimeoutMs
        {
            get => Settings.ConnectTimeoutMs;
            set => Settings.ConnectTimeoutMs = value;
        }

        static NetworkConfig()
        {
            Load();
        }

        /// Tải cấu hình từ file network_config.json
        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var loaded = JsonSerializer.Deserialize<NetworkSettings>(json);
                    if (loaded != null)
                    {
                        Settings = loaded;
                    }
                }
                else
                {
                    // Tạo file cấu hình mặc định nếu chưa tồn tại
                    Save();
                }
            }
            catch
            {
                Settings = new NetworkSettings();
            }
        }
        /// Lưu cấu hình hiện tại vào file network_config.json
        public static void Save()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(Settings, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // Bỏ qua nếu không có quyền ghi file
            }
        }
    }
}
