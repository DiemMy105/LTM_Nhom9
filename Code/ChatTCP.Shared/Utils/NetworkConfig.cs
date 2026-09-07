using System;
using System.IO;
using System.Text.Json;

namespace ChatTCP.Shared.Utils
{
    /// <summary>
    /// Các thông số cấu hình mạng
    /// </summary>
    public class NetworkSettings
    {
        /// <summary>
        /// Địa chỉ IP hoặc Domain của Server
        /// </summary>
        public string ServerIp { get; set; } = "127.0.0.1";

        /// <summary>
        /// Cổng Port kết nối TCP Socket giữa Client và Server
        /// </summary>
        public int ServerPort { get; set; } = 8888;

        /// <summary>
        /// Kích thước bộ đệm (Buffer Size) đọc/ghi TCP Socket (bytes)
        /// </summary>
        public int BufferSize { get; set; } = 8192;

        /// <summary>
        /// Thời gian chờ kết nối tối đa (milliseconds)
        /// </summary>
        public int ConnectTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Thời gian chờ gửi dữ liệu (milliseconds) - 0 là vô hạn
        /// </summary>
        public int SendTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Thời gian chờ nhận dữ liệu (milliseconds) - 0 là vô hạn
        /// </summary>
        public int ReceiveTimeoutMs { get; set; } = 0;
    }

    /// <summary>
    /// Quản lý cấu hình mạng, đọc/ghi tự động từ file network_config.json
    /// </summary>
    public static class NetworkConfig
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "network_config.json");

        public static NetworkSettings Settings { get; private set; } = new NetworkSettings();

        /// <summary>
        /// Địa chỉ IP hiện tại của Server
        /// </summary>
        public static string ServerIp
        {
            get => Settings.ServerIp;
            set => Settings.ServerIp = value;
        }

        /// <summary>
        /// Cổng Port hiện tại của Server
        /// </summary>
        public static int ServerPort
        {
            get => Settings.ServerPort;
            set => Settings.ServerPort = value;
        }

        /// <summary>
        /// Kích thước bộ đệm Socket
        /// </summary>
        public static int BufferSize
        {
            get => Settings.BufferSize;
            set => Settings.BufferSize = value;
        }

        /// <summary>
        /// Timeout kết nối (ms)
        /// </summary>
        public static int ConnectTimeoutMs
        {
            get => Settings.ConnectTimeoutMs;
            set => Settings.ConnectTimeoutMs = value;
        }

        static NetworkConfig()
        {
            Load();
        }

        /// <summary>
        /// Tải cấu hình từ file network_config.json
        /// </summary>
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

        /// <summary>
        /// Lưu cấu hình hiện tại vào file network_config.json
        /// </summary>
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
