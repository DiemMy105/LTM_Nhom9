using System;
using System.Threading;
using System.Windows.Forms;
using ChatTCP.Client.Forms;
using ChatTCP.Client.Network;
using ChatTCP.Client.Services;

namespace ChatTCP.Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Bắt tất cả lỗi UI đi qua ThreadException handler
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ApplicationConfiguration.Initialize();

            try
            {
             
                var tcpClientManager = new TcpClientManager();
                var authService = new AuthService(tcpClientManager);

                // Chạy LoginForm: Khi đăng nhập thành công, LoginForm sẽ tự mở ClientForm
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

        private static void Application_ThreadException(object? sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Đã xảy ra lỗi:\n\n{e.Exception.Message}",
                "Lỗi ứng dụng",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show(
                $"Đã xảy ra lỗi nghiêm trọng ở luồng nền:\n\n{ex?.Message}",
                "Lỗi hệ thống",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}