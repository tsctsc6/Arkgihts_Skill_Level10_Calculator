using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Arkgihts_Skill_Level10_Calculator.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using RustSharp;

namespace Arkgihts_Skill_Level10_Calculator.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private FrozenDictionary<string, Material> _materialList;
    private readonly HttpClient _httpClient;
    private readonly HtmlParser _htmlParser;

    public ObservableCollection<string> OperatorList { get; } = [];
    
    [ObservableProperty]
    private string _selectedOperator = string.Empty;
    
    [ObservableProperty]
    private int _selectedSkillIndex;
    
    public ObservableCollection<KeyValuePair<string, int>> NeedComposition { get; } = [];
    
    public ObservableCollection<KeyValuePair<string, int>> LackRarity2 { get; }= [];
    
    public MainWindowViewModel(HttpClient httpClient, HtmlParser htmlParser)
    {
        _httpClient = httpClient;
        _htmlParser = htmlParser;
        LoadResourceInfo();
    }

    [MemberNotNull(nameof(_materialList))]
    private void LoadResourceInfo()
    {
        OperatorList.Clear();
        _materialList = Array.Empty<Material>().ToFrozenDictionary(m => m.Name);
        
        if (!File.Exists(App.ResourceInfoPath)) return;
        var resourceInfo = JsonSerializer.Deserialize<ResourceInfo>(File.ReadAllText(App.ResourceInfoPath),
            App.Current.JsonSerializerOptions);
        if (resourceInfo == null) return;

        Array.ForEach(resourceInfo.OperatorList, s => OperatorList.Add(s));
        _materialList = resourceInfo.MaterialList.ToFrozenDictionary(m => m.Name);
    }
    
    [RelayCommand]
    private async Task LoadDepotFromClipboardAsync()
    {
        var clipboard = App.Current.MainWindow.Clipboard;
        if (clipboard == null) return;
        var content = await clipboard.GetTextAsync();
        if (string.IsNullOrEmpty(content)) return;
        Depot? depot;
        try
        {
            depot = JsonSerializer.Deserialize<Depot>(content, App.Current.JsonSerializerOptions);
        }
        catch (Exception)
        {
            await MessageBoxManager.GetMessageBoxStandard("Info", "Failed")
                .ShowWindowDialogAsync(App.Current.MainWindow);
            return;
        }
        if (depot == null) return;
        await MessageBoxManager.GetMessageBoxStandard("Info", "Success")
            .ShowWindowDialogAsync(App.Current.MainWindow);
    }

    [RelayCommand]
    private async Task GetResourceInfoAsync()
    {
        var getOperatorListTask = GetOperatorListAsync();
        var getMaterialListTask = GetMaterialListAsync();
        await Task.WhenAll(getOperatorListTask, getMaterialListTask);
        
        var getOperatorListTaskResult = await getOperatorListTask;
        switch (getOperatorListTaskResult)
        {
            case OkResult<IEnumerable<string>, string> okResult:
                OperatorList.Clear();
                foreach (var s in okResult.Value)
                {
                    OperatorList.Add(s);
                }
                break;
            case ErrResult<IEnumerable<string>, string> errResult:
                await MessageBoxManager.GetMessageBoxStandard("Error", errResult.Value)
                    .ShowWindowDialogAsync(App.Current.MainWindow);
                break;
            default: return;
        }
        
        var getMaterialListTaskResult = await getMaterialListTask;
        switch (getMaterialListTaskResult)
        {
            case OkResult<FrozenDictionary<string, Material>, string> okResult:
                _materialList = okResult.Value;
                break;
            case ErrResult<FrozenDictionary<string, Material>, string> errResult:
                await MessageBoxManager.GetMessageBoxStandard("Error", errResult.Value)
                    .ShowWindowDialogAsync(App.Current.MainWindow);
                break;
            default: return;
        }
        
        if (getOperatorListTaskResult.IsErr || getMaterialListTaskResult.IsErr) return;
        
        await File.WriteAllTextAsync(App.ResourceInfoPath, JsonSerializer.Serialize(new ResourceInfo()
        {
            OperatorList = OperatorList.ToArray(),
            MaterialList = _materialList.Select(kv => kv.Value).ToArray(),
        }, App.Current.JsonSerializerOptions));
    }
    
    [RelayCommand]
    private async Task CalculateSkillMaterialAsync()
    {
        await Task.Delay(1000);
    }
    
    private async Task<Result<KeyValuePair<string, int>[,,], string>> GetOperatorSkillInfoAsync()
    {
        var uri = $"https://prts.wiki/w/{SelectedOperator}";
        var resp = await _httpClient.GetAsync(uri);
        if (!resp.IsSuccessStatusCode) return Result.Err($"{uri}, StatusCode: {resp.StatusCode.ToString()}");
        using var document = _htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var table = document.QuerySelector("span#技能升级材料")?.ParentElement?
            .NextElementSibling?.NextElementSibling?
            .QuerySelector("tbody")?.Children.Skip(9);
        if (table == null) return Result.Err("无法定位技能升级材料");
        var skillInfo = new KeyValuePair<string, int>[3, 3, 2];
        foreach (var (i, tr) in table.Index())
        {
            var d = Math.DivRem(i, 4, out var r);
            if (r == 0) continue;
            var td = tr.QuerySelector("td");
            if (td == null) continue;
            foreach (var (j, div) in td.Children.Index())
            {
                if (j == 0) continue;
                var name = div.QuerySelector("a")?.Attributes["title"]?.Value;
                if (string.IsNullOrEmpty(name)) continue;
                if (!int.TryParse(div.QuerySelector("span")?.TextContent, out var count)) continue;
                skillInfo[d, r - 1, j - 1] = new KeyValuePair<string, int>(name, count);
            }
        }
        return Result.Ok(skillInfo);
    }
    
    private async Task<Result<IEnumerable<string>, string>> GetOperatorListAsync()
    {
        const string uri = "https://prts.wiki/w/干员一览";
        var resp = await _httpClient.GetAsync(uri);
        if (!resp.IsSuccessStatusCode) return Result.Err($"{uri}, StatusCode: {resp.StatusCode.ToString()}");
        using var document = _htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var operatorData = document.QuerySelector("div#filter-data")?.Children;
        var operatorList = operatorData?.Select(e => e.Attributes["data-zh"]?.Value).Where(n => n is not null)
                   .Select(n => n!) ?? [];
        return Result.Ok(operatorList);
    }
    
    private async Task<Result<FrozenDictionary<string, Material>, string>> GetMaterialListAsync()
    {
        const string uri = "https://prts.wiki/w/道具一览";
        var resp = await _httpClient.GetAsync(uri);
        if (!resp.IsSuccessStatusCode) return Result.Err($"{uri}, StatusCode: {resp.StatusCode.ToString()}");
        using var document = _htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var materialData = document.QuerySelector("div.mw-parser-output")?.Children
            .Where(e =>
            {
                if (!int.TryParse(e.Attributes["data-rarity"]?.Value, out var rarity)) return false;
                if (rarity < 2) return false;
                if (!(e.Attributes["data-category"]?.Value.Contains("分类:材料") ?? false)) return false;
                return (e.Attributes["data-category"]?.Value.Contains("分类:加工站产物") ?? false) ||
                       e.Attributes["data-obtain_approach"]?.Value == "常规关卡掉落";
            })
            .Select(e => new Material
            {
                Name = e.Attributes["data-name"]?.Value ?? string.Empty,
                Rarity = int.Parse(e.Attributes["data-rarity"]?.Value ?? string.Empty),
            }).ToArray();
        if (materialData is null) return Result.Err("无法定位道具信息");
        var compositionResults = await Task.WhenAll(materialData.Where(m => m.Rarity > 2).Select(GetCompositionAsync));
        if (compositionResults.Any(c => c.IsErr))
        {
            var errorMessages = compositionResults.Where(c => c.IsErr)
                .Select(c => (c as ErrResult<string, string>)!.Value).ToArray();
            return Result.Err($"获取材料合成表时发生问题:{Environment.NewLine}{string.Join(Environment.NewLine, errorMessages)}");
        }
        return Result.Ok(materialData.ToFrozenDictionary(m => m.Name));
    }

    private async Task<Result<string, string>> GetCompositionAsync(Material material)
    {
        var uri = $"https://prts.wiki/w/{material.Name}";
        var resp = await _httpClient.GetAsync(uri);
        if (!resp.IsSuccessStatusCode) return Result.Err($"{uri}, StatusCode: {resp.StatusCode.ToString()}");
        using var document = _htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var composition = document.QuerySelector("span#加工站")?.ParentElement?.NextElementSibling?
            .QuerySelector("tbody > tr:nth-child(2) > td > div")?.Children;
        if (composition is null) return Result.Err($"{uri}, 无法定位道具合成信息");
        material.Composition = [];
        var pairs = new List<KeyValuePair<string, int>>(3);
        foreach (var compositionItem in composition)
        {
            if (!int.TryParse(compositionItem.QuerySelector("span")?.InnerHtml, out var count)) continue;
            pairs.Add(new(
                compositionItem.QuerySelector("a")?.Attributes["title"]?.Value ?? string.Empty, count
            ));
        }
        material.Composition = pairs.ToArray();
        return Result.Ok(material.Name);
    }
}