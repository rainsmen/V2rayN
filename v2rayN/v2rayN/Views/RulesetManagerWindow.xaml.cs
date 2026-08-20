namespace v2rayN.Views;

public partial class RulesetManagerWindow
{
    public RulesetManagerWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.RulesetItems, v => v.lstRulesets.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource, v => v.lstRulesets.SelectedItem).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RulesetAddCmd, v => v.menuRulesetAdd).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RulesetAddCmd, v => v.menuRulesetAdd2).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RulesetRemoveCmd, v => v.menuRulesetRemove).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RulesetRemoveCmd, v => v.menuRulesetRemove2).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SaveCmd, v => v.btnSave).DisposeWith(disposables);
        });
        WindowsUtils.SetDarkBorder(this, AppManager.Instance.Config.UiItem.CurrentTheme);
    }
}
