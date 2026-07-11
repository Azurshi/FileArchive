using AzurArchive.Core;
using AzurArchive.Data;
using AzurArchive.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AzurArchive.Services;

internal class StartupService {
    public async Task BeforeWindowCreate(IServiceProvider provider) {
        DataManager dataManager = provider.GetRequiredService<DataManager>();
        await dataManager.Start("D:\\Workstation\\Data\\AA");
    }
    public async Task AfterWindowCreate(IServiceProvider provider) {
        //EventSystem.PrintBlockedEvents.Add(typeof(StatusEventArgs));

        //await ImportTest(provider);


        //NavigatingEventArgs naviArgs = new(this, PageRoute.None, PageRoute.Overview);
        //EventSystem.Publish(this, naviArgs);
    }
    private void Print(string type, ArchiveProgress data) {
        //var msg = new ProgressMessage("Importing", data.Current, data.Total);
        //EventSystem.Publish(this, new StatusEventArgs(msg, false));
        //if (data.Current == 1 || data.Current % 1000 == 0 || data.Current == data.Total) {
        //    Debug.WriteLine($"{type} {data.Current} / {data.Total}: {data.Message}");
        //}
    }
    private async Task ImportTest(IServiceProvider provider) {
        //var archiver = provider.GetRequiredService<IProgramArchiver>();
        //string rootFolder = "E:\\Games\\Archive\\Sim";
        //foreach (var folderPath in Directory.GetDirectories(rootFolder)) {
        //    string folderName = new DirectoryInfo(folderPath).Name;
        //    await archiver.Add("Sim_" + folderName, folderPath, new Progress<ArchiveProgress>((p) => Print(folderName, p)));
        //}
    }
}
