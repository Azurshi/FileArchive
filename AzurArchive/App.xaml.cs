using AzurArchive.Core;
using AzurArchive.Data;
using AzurArchive.Data.Services;
using AzurArchive.Domain;
using AzurArchive.Services;
using AzurArchive.ViewModels.Items;
using AzurArchive.Views.Windows;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AzurArchive;
public partial class App: Application {
    private readonly IHost AppHost;
    public IServiceProvider Services => AppHost.Services;
    public App() {
        var path = Path.Combine(Path.GetTempPath(), nameof(AzurArchive));
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) => {
                RegisterDependency(services);
            })
            .ConfigureLogging(ConfigureLogging)
            .Build();
        ConfigureLifeCycle(Services);
        InitializeComponent();
    }
    private void ConfigureLifeCycle(IServiceProvider provider) {
        AppLifeCycle.RegisterAppStart(async () => {
            DataManager dataManager = provider.GetRequiredService<DataManager>();
            Directory.CreateDirectory("Data");
            await dataManager.Start("Data");
        });
#if DEBUG
        AppLifeCycle.RegisterAppStart(() => {
            EventSystem.PrintBlockedEvents.Add(typeof(LogEventArgs));
        });
#endif
        AppLifeCycle.RegisterAppStart(() => {
            var appSetting = provider.GetRequiredService<IAppSetting>();
            appSetting.Register(new ItemHeaderInfoJsonConverter());
            appSetting.Register(new ItemColumnInfoJsonConverter());
        });
        AppLifeCycle.RegisterAppStart(async () => {
            await ImportTest(provider);
        });

        AppLifeCycle.RegisterAppClose(async () => {
            var dataManager = provider.GetRequiredService<DataManager>();
            dataManager.Dispose();
            var appSetting = provider.GetRequiredService<IAppSetting>();
            appSetting.Dispose();
        });
    }
    protected override async void OnLaunched(LaunchActivatedEventArgs args) {
        await AppLifeCycle.StartApp();
        var explorer = Services.GetRequiredService<ExplorerWindow>();
        explorer.Activate();
        await AppLifeCycle.AfterFirstWindow();
    }
    private void Print(string type, ArchiveProgress data) {
        //var msg = new ProgressMessage("Importing", data.Current, data.Total);
        //EventSystem.Publish(this, new StatusEventArgs(msg, false));
        Debug.WriteLine($"{type} {data.Current} / {data.Total}: {data.Message}");

        //if (data.Current == 1 || data.Current % 10 == 0 || data.Current == data.Total) {
        //    Debug.WriteLine($"{type} {data.Current} / {data.Total}: {data.Message}");
        //}
    }
    private async Task ImportTest(IServiceProvider provider) {
        var archiver = provider.GetRequiredService<IArchiver>();
        //string rootFolder = "E:\\Games\\Archive\\Pack";
        //await archiver.ImportTopLevel(rootFolder, 4, new Progress<ArchiveProgress>((p) => Print("Importing", p)));

        //foreach (var folderPath in Directory.GetDirectories(rootFolder)) {
        //    string folderName = new DirectoryInfo(folderPath).Name;
        //    await archiver.Import(-1, folderPath, 2, new Progress<ArchiveProgress>((p) => Print(folderName, p)));
        //}
        //string outputFolder = "E:\\Games\\Archive\\Temp4";
        //MemoryCache cache = new(new MemoryCacheOptions() { })
        //await archiver.Restore(1, outputFolder, new Progress<ArchiveProgress>((p) => Print("Importing", p)));
        //await archiver.DeleteFolder(31, new Progress<ArchiveProgress>((p) => Print("Importing", p)));
    }
    private void RegisterDependency(IServiceCollection services) {
        services.RegisterData();
        services.RegisterHttp();
        services.RegisterServices();
        services.RegisterShells();
        services.RegisterWindows();
        services.RegisterOverlay();
        services.RegisterOthers();
    }
    private static void ConfigureLogging(ILoggingBuilder logging) {

    }
}
