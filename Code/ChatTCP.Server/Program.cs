using System;
using System.Threading;
using System.Windows.Forms;
using ChatTCP.Server.Forms;


namespace ChatTCP.Server
{
    /// Điểm khởi chạy chương trình Server.
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // Khởi tạo cấu hình WinForms
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
        /// Bắt lỗi xảy ra trên luồng giao diện (UI Thread).
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Đã xảy ra lỗi:\n\n{e.Exception.Message}",
                "Lỗi ứng dụng",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
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



