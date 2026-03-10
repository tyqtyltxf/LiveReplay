using System;
using System.Windows;

namespace LiveReplay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 全局异常处理
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"程序发生错误: {args.Exception.Message}\n\n{args.Exception.StackTrace}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}