using System;
using System.Windows.Forms;
using ChatTCP.Server.Forms;

namespace ChatTCP.Server
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new ServerForm());
        }
    }
}