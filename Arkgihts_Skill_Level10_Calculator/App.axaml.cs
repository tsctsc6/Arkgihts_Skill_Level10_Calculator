using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Net.Http;
using AngleSharp.Html.Parser;
using Avalonia.Markup.Xaml;
using Arkgihts_Skill_Level10_Calculator.ViewModels;
using Arkgihts_Skill_Level10_Calculator.Views;
using Avalonia.Controls;

namespace Arkgihts_Skill_Level10_Calculator;

public partial class App : Application
{
    public new static App Current => (Application.Current as App)!;
    
    private readonly HttpClient _httpClient;
    private readonly HtmlParser _htmlParser;
    public const string ResourceInfoPath = "resource_info.json";
    
    public MainWindowViewModel MainWindowViewModel { get; }
    public Window MainWindow { get; private set; }
    
    public App()
    {
        _httpClient = new();
        _htmlParser = new();
        MainWindowViewModel = new(_httpClient, _htmlParser);
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = MainWindowViewModel,
            };
            MainWindow = desktop.MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}