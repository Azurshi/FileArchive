using AzurArchive.Core;
using AzurArchive.Services;
using AzurArchive.ViewModels.Windows;
using AzurArchive.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SQLiteORM;
using System;
using System.Net;
using System.Net.Http;
namespace AzurArchive.Services;

public static class ServiceRegister {
    public static IServiceCollection RegisterServices(this IServiceCollection services) {
        services.AddSingleton<StartupService>();
        services.AddSingleton<IconService>();
        //services.AddSingleton<NavigationStack>();
        //services.AddSingleton<IPageResolver, PageResolver>();
        //services.AddSingleton<IPageRouteRegistry, PageRouteRegistry>();
        return services;
    }
    public static IServiceCollection RegisterShells(this IServiceCollection services) {
        //services.AddSingleton<MainWindow>();
        //services.AddSingleton<NavigationBar>();
        //services.AddSingleton<NavigationBarViewModel>();
        //services.AddSingleton<StatusBar>();
        //services.AddSingleton<StatusBarViewModel>();
        //services.AddSingleton<AppOverlay>();
        //services.AddSingleton<AppOverlayViewModel>();
        return services;

    }
    public static IServiceCollection RegisterOverlay(this IServiceCollection services) {
        //services.AddTransient<CreateArchiveOverlay>();
        //services.AddTransient<GetEstimatedSizeOverlay>();
        //services.AddTransient<DeleteConfirmOverlay>();
        //services.AddTransient<RenameOverlay>();
        return services;

    }
    public static IServiceCollection RegisterOthers(this IServiceCollection services) {
        //UserSetting.AddConverter(new ProgramHeaderInfoJsonConverter());
        //UserSetting.AddConverter(new ProgramColumnInfoJsonConverter());
        return services;

    }
    public static IServiceCollection RegisterWindows(this IServiceCollection services) {
        //services.AddSingleton<OverviewPage>();
        //services.AddSingleton<OverviewPageViewModel>();
        //services.AddSingleton<ProgramEntryPage>();
        //services.AddSingleton<ProgramEntryPageViewModel>();
        //services.AddSingleton<ProgramTreePage>();
        //services.AddSingleton<ProgramTreePageViewModel>();
        services.AddTransient<OperationWindow>();
        services.AddTransient<ExplorerWindow>();
        services.AddTransient<ExplorerWindowContent>();
        services.AddTransient<ExplorerWindowViewModel>();
        services.AddTransient<InfoWindow>();
        return services;

    }
    public static IServiceCollection RegisterHttp(this IServiceCollection services) {
        //services.AddHttpClient()
        //    .ConfigureHttpClientDefaults(b => {
        //        b.ConfigureHttpClient(c => {
        //            c.Timeout = TimeSpan.FromSeconds(15);
        //        });
        //    });
        return services;
    }
}