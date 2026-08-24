namespace ChatTCP.Shared.Utils
{
    public static class NetworkConfig
    {
        /// Địa chỉ IP mặc định của Server 
        public static string ServerIp { get; set; } = "127.0.0.1";

        /// Cổng Port kết nối TCP Socket giữa Client và Server
        public static int ServerPort { get; set; } = 8888;
    }
}
