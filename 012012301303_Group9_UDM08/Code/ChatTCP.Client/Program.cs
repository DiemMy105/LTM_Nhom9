using System;
using System.Threading;
using System.Windows.Forms;
using ChatTCP.Client.Forms;
using ChatTCP.Client.Services;

namespace ChatTCP.Client
{
    /// <summary>
    /// Điểm khởi chạy chương trình Client.
    /// Chịu trách nhiệm khởi tạo môi trường WinForms, bắt lỗi toàn cục
    /// và mở LoginForm (màn hình đăng nhập đầu tiên).
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
            // Tránh Client bị crash im lặng khi có lỗi ngoài dự tính
            // (ví dụ: lỗi mất kết nối Server, lỗi Socket...).
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // ---- Khởi tạo cấu hình WinForms ----
            ApplicationConfiguration.Initialize();

            try
            {
                // TODO: [TV1] Khi TcpClientManager thật đã sẵn sàng, khởi tạo nó ở đây
                // và truyền vào AuthService thay vì để AuthService tự chạy bản demo nội bộ.
                // var tcpClientManager = new TcpClientManager();
                // var authService = new AuthService(tcpClientManager);
                var authService = new AuthService();

                // Chạy giao diện đăng nhập - sau khi đăng nhập thành công,
                // LoginForm tự mở ClientForm (xem LoginForm.OnLoginSucceeded).
                Application.Run(new LoginForm(authService));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ứng dụng đã gặp lỗi nghiêm trọng và phải đóng:\n\n{ex.Message}",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Bắt lỗi xảy ra trên luồng giao diện (UI Thread).
        /// </summary>
        private static void Application_ThreadException(object? sender, ThreadExceptionEventArgs e)
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
        /// ví dụ luồng nhận dữ liệu từ Server trong TcpClientManager.cs.
        /// </summary>
        private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
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