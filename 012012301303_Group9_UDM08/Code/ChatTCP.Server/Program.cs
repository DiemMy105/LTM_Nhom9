using System;
using System.Threading;
using System.Windows.Forms;
using ChatTCP.Server.Forms;

namespace ChatTCP.Server
{
    /// <summary>
    /// Điểm khởi chạy chương trình Server. [TV6]
    /// Chịu trách nhiệm khởi tạo môi trường WinForms, bắt lỗi toàn cục
    /// và mở ServerForm (giao diện điều khiển Server).
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // ---- Bắt các lỗi không xử lý được (Unhandled Exceptions) ----
            // Tránh Server bị crash im lặng khi có lỗi ngoài dự tính
            // (ví dụ: lỗi mất kết nối Database, lỗi Socket...).
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // ---- Khởi tạo cấu hình WinForms ----
            ApplicationConfiguration.Initialize();

            try
            {
                // Chạy giao diện chính của Server
                Application.Run(new ServerForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Server đã gặp lỗi nghiêm trọng và phải đóng:\n\n{ex.Message}",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Bắt lỗi xảy ra trên luồng giao diện (UI Thread).
        /// </summary>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Đã xảy ra lỗi:\n\n{e.Exception.Message}",
                "Lỗi ứng dụng",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            // TODO: [TV6] Ghi lỗi vào Logger.cs khi lớp Logger đã sẵn sàng
            // Logger.Instance.LogError(e.Exception);
        }

        /// <summary>
        /// Bắt lỗi xảy ra trên các luồng nền (background thread),
        /// ví dụ luồng lắng nghe Client trong TcpServer.cs.
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show(
                $"Đã xảy ra lỗi nghiêm trọng ở luồng nền:\n\n{ex?.Message}",
                "Lỗi hệ thống",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            // TODO: [TV6] Ghi lỗi vào Logger.cs khi lớp Logger đã sẵn sàng
            // Logger.Instance.LogError(ex);
        }
    }
}