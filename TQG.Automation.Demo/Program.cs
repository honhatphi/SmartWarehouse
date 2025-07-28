using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using TQG.Automation.Demo.Views;

namespace TQG.Automation.Demo;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static async Task Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var host = CreateHostBuilder().Build();
        host.Start();

        var form = host.Services.GetRequiredService<MainForm>();

        Application.Run(form);

        await host.StopAsync();
        host.Dispose();
    }

    static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<AutomationGateway>();

                services.AddSingleton<GatewayBackgroundService>();

                services.AddHostedService(sp => sp.GetRequiredService<GatewayBackgroundService>());

                services.AddTransient<MainForm>();
            });
    }
}
