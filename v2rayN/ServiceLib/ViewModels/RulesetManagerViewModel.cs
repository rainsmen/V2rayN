using ServiceLib.Base;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Services;

namespace ServiceLib.ViewModels;

public partial class RulesetManagerViewModel : MyReactiveObject, ICloseable
{
    public event EventHandler? RequestClose;

    private readonly RoutingItem _routingItem;

    public BulkObservableCollection<Ruleset4Sbox> RulesetItems { get; } = [];

    [Reactive]
    public partial Ruleset4Sbox SelectedSource { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> RulesetAddCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RulesetRemoveCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveCmd { get; }

    public RulesetManagerViewModel(RoutingItem routingItem)
    {
        _config = AppManager.Instance.Config;
        _routingItem = routingItem;

        var canEditRemove = this.WhenAnyValue(
            x => x.SelectedSource,
            selectedSource => selectedSource != null && !selectedSource.tag.IsNullOrEmpty());

        RulesetAddCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await RulesetAddAsync();
        });
        RulesetRemoveCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await RulesetRemoveAsync();
        }, canEditRemove);
        SaveCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SaveRulesetsAsync();
        });

        _ = Init();
    }

    private async Task Init()
    {
        SelectedSource = new();
        await RefreshRulesetItems();
    }

    private async Task RefreshRulesetItems()
    {
        RulesetItems.Clear();
        var items = LoadRulesets();
        RulesetItems.AddRange(items);
        await Task.CompletedTask;
    }

    private List<Ruleset4Sbox> LoadRulesets()
    {
        if (_routingItem.CustomRulesetPath4Singbox.IsNotEmpty())
        {
            var result = EmbedUtils.LoadResource(_routingItem.CustomRulesetPath4Singbox);
            if (result.IsNotEmpty())
            {
                return (JsonUtils.Deserialize<List<Ruleset4Sbox>>(result) ?? [])
                    .Where(t => t.tag != null)
                    .Where(t => t.type != null)
                    .Where(t => t.format != null)
                    .ToList();
            }
        }
        return [];
    }

    private async Task RulesetAddAsync()
    {
        var item = new Ruleset4Sbox
        {
            tag = $"custom-{Guid.NewGuid().ToString("N")[..8]}",
            type = "remote",
            format = "binary",
            url = "",
            download_detour = Global.ProxyTag
        };
        RulesetItems.Add(item);
        SelectedSource = item;
        await Task.CompletedTask;
    }

    private async Task RulesetRemoveAsync()
    {
        if (SelectedSource is null || SelectedSource.tag.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseSelectRules);
            return;
        }
        RulesetItems.Remove(SelectedSource);
        SelectedSource = new();
        await Task.CompletedTask;
    }

    private async Task SaveRulesetsAsync()
    {
        if (_routingItem.CustomRulesetPath4Singbox.IsNullOrEmpty())
        {
            _routingItem.CustomRulesetPath4Singbox = Utils.GetConfigPath($"custom_ruleset_{_routingItem.Id}.json");
        }

        var items = RulesetItems
            .Where(t => t.tag != null && t.type != null && t.format != null)
            .ToList();

        var json = JsonUtils.Serialize(items, true, true);
        try
        {
            await File.WriteAllTextAsync(_routingItem.CustomRulesetPath4Singbox, json);
            await ConfigHandler.SaveRoutingItem(_config, _routingItem);
            NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Logging.SaveLog("RulesetManagerViewModel", ex);
            NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
        }
    }
}
