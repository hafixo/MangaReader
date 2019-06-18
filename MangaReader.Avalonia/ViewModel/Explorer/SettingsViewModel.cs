﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MangaReader.Avalonia.ViewModel.Command;
using MangaReader.Core.Account;
using MangaReader.Core.NHibernate;
using MangaReader.Core.Services;
using MangaReader.Core.Services.Config;

namespace MangaReader.Avalonia.ViewModel.Explorer
{
  public class SettingsViewModel : SettingTabViewModel
  {
    public int AutoupdateLibraryInHours
    {
      get => autoupdateLibraryInHours;
      set => RaiseAndSetIfChanged(ref autoupdateLibraryInHours, value);
    }

    private int autoupdateLibraryInHours;

    public bool CheckAppUpdateOnStart
    {
      get => checkAppUpdateOnStart;
      set => RaiseAndSetIfChanged(ref checkAppUpdateOnStart, value);
    }

    private bool checkAppUpdateOnStart;

    public bool MinimizeToTray
    {
      get => minimizeToTray;
      set => RaiseAndSetIfChanged(ref minimizeToTray, value);
    }

    private bool minimizeToTray;

    public Languages Language
    {
      get => language;
      set => RaiseAndSetIfChanged(ref language, value);
    }

    private Languages language;

    public List<Languages> AllowedLanguages => Generic.GetEnumValues<Languages>();

    public IFolderNamingStrategy FolderNamingStrategy
    {
      get => folderNamingStrategy;
      set => RaiseAndSetIfChanged(ref folderNamingStrategy, value);
    }

    private IFolderNamingStrategy folderNamingStrategy;

    public List<IFolderNamingStrategy> FolderNamingStrategies => Core.Services.FolderNamingStrategies.Strategies.ToList();

    public IReadOnlyList<SortSetting> SortSettings => SortSetting.Sorts;

    public SortSetting SortSetting
    {
      get => sortSetting;
      set => RaiseAndSetIfChanged(ref sortSetting, value);
    }

    private SortSetting sortSetting;

    public ProxySettingModel SelectedProxySettingModel
    {
      get
      {
        return selectedProxySettingModel;
      }

      set
      {
        this.RaiseAndSetIfChanged(ref selectedProxySettingModel, value);
        if (selectedProxySettingModel != null)
          proxySettingId = selectedProxySettingModel.Id;
      }
    }

    private ProxySettingModel selectedProxySettingModel;
    private int proxySettingId;

    public IEnumerable<ProxySettingModel> ProxySettingModels
    {
      get => proxySettingModels;
      set => this.RaiseAndSetIfChanged(ref proxySettingModels, value);
    }

    private IEnumerable<ProxySettingModel> proxySettingModels;

    public override async Task OnSelected(ExplorerTabViewModel previousModel)
    {
      await base.OnSelected(previousModel).ConfigureAwait(true);

      if (!ExplorerViewModel.Instance.Tabs.OfType<MangaSettingsViewModel>().Any())
      {
        using (var context = Repository.GetEntityContext("Load manga settings"))
        {
          await ReloadConfig().ConfigureAwait(true);
          var settings = await context.Get<MangaSetting>().ToListAsync().ConfigureAwait(true);
          ExplorerViewModel.Instance.Tabs.AddRange(settings.Select(s => new MangaSettingsViewModel(s)));
          ExplorerViewModel.Instance.Tabs.Add(new ProxySettingSelectorModel());
        }
      }
    }

    public override async Task OnUnselected(ExplorerTabViewModel newModel)
    {
      await base.OnUnselected(newModel).ConfigureAwait(true);

#warning Нужно ресетить только после изменения порядка сортировки.
      foreach (var model in ExplorerViewModel.Instance.Tabs.OfType<LibraryViewModel>())
        model.ResetView();
    }

    public ICommand Save { get; }

    public ICommand UndoChanged { get; }

    private async Task ReloadConfig()
    {
      var appConfig = ConfigStorage.Instance.AppConfig;
      this.CheckAppUpdateOnStart = appConfig.UpdateReader;
      this.MinimizeToTray = appConfig.MinimizeToTray;
      this.AutoupdateLibraryInHours = appConfig.AutoUpdateInHours;
      this.Language = appConfig.Language;

      var viewConfig = ConfigStorage.Instance.ViewConfig;
      this.SortSetting = SortSettings
        .OrderByDescending(s => s.SortDescription.PropertyName == viewConfig.LibraryFilter.SortDescription.PropertyName)
        .FirstOrDefault();

      using (var context = Repository.GetEntityContext())
      {
        var config = await context.Get<DatabaseConfig>().SingleAsync().ConfigureAwait(true);
        this.FolderNamingStrategy = FolderNamingStrategies.FirstOrDefault(s => s.Id == config.FolderNamingStrategy);
        this.ProxySettingModels = await context
          .Get<ProxySetting>()
          .Where(s => s.SettingType != ProxySettingType.Parent)
          .Select(s => new ProxySettingModel(s))
          .ToListAsync()
          .ConfigureAwait(true);
        this.proxySettingId = config.ProxySetting.Id;
        this.SelectedProxySettingModel = this.ProxySettingModels.FirstOrDefault(m => m.Id == proxySettingId);
      }
    }

    private async Task SaveConfig()
    {
      var configStorage = ConfigStorage.Instance;
      var appConfig = configStorage.AppConfig;
      appConfig.UpdateReader = this.CheckAppUpdateOnStart;
      appConfig.MinimizeToTray = this.MinimizeToTray;
      appConfig.AutoUpdateInHours = this.AutoupdateLibraryInHours;
      appConfig.Language = this.Language;

      var viewConfig = configStorage.ViewConfig;
      viewConfig.LibraryFilter.SortDescription = SortSetting.SortDescription;

      configStorage.Save();

      using (var context = Repository.GetEntityContext())
      {
        var config = await context.Get<DatabaseConfig>().SingleAsync().ConfigureAwait(true);
        config.FolderNamingStrategy = FolderNamingStrategy.Id;
        if (proxySettingId != config.ProxySetting.Id)
          config.ProxySetting = await context.Get<ProxySetting>().SingleAsync(s => s.Id == proxySettingId).ConfigureAwait(false);
        await context.Save(config).ConfigureAwait(true);
      }
    }

    public SettingsViewModel()
    {
      this.Name = "Settings";
      this.Priority = 100;
      this.HideTab = false;

      this.Save = new DelegateCommand(SaveConfig, () => true);
      this.UndoChanged = new DelegateCommand(ReloadConfig, () => true);
    }
  }
}
