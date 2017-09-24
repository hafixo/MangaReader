﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MangaReader.Core.Exception;
using MangaReader.Core.NHibernate;
using MangaReader.Core.Properties;
using MangaReader.Core.Services;
using MangaReader.Core.Services.Config;

namespace MangaReader.Core.Manga
{
  [DebuggerDisplay("{Name}, Id = {Id}, Uri = {Uri}")]
  public abstract class Mangas : Entity.Entity, INotifyPropertyChanged, IManga
  {
    #region Свойства

    /// <summary>
    /// Название манги.
    /// </summary>
    public string Name
    {
      get { return this.IsNameChanged ? this.LocalName : this.ServerName; }
      set
      {
        if (this.IsNameChanged)
          this.LocalName = value;
        else
          this.ServerName = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(Folder));
      }
    }

    public string LocalName
    {
      get { return localName ?? ServerName; }
      set { localName = value; }
    }

    private string localName;

    public string ServerName { get; set; }

    public bool IsNameChanged
    {
      get { return isNameChanged; }
      set
      {
        isNameChanged = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Folder));
      }
    }

    private bool isNameChanged = false;

    public virtual string Url
    {
      get { return Uri?.ToString(); }
      set { Uri = value == null ? null : new Uri(value); }
    }

    /// <summary>
    /// Ссылка на мангу.
    /// </summary>
    [XmlIgnore]
    public virtual Uri Uri
    {
      get { return this.uri; }
      set
      {
        UpdateUri(value);
        this.uri = value;
      }
    }

    private Uri uri;

    /// <summary>
    /// Статус манги.
    /// </summary>
    public virtual string Status
    {
      get { return status; }
      set
      {
        status = value;
        OnPropertyChanged();
      }
    }

    public bool? NeedCompress
    {
      get { return needCompress; }
      set
      {
        needCompress = value;
        OnPropertyChanged();
      }
    }

    private bool? needCompress = null;

    protected virtual IPlugin Plugin
    {
      get
      {
        var plugin = ConfigStorage.Plugins.SingleOrDefault(p => p.MangaType == this.GetType());
        if (plugin == null)
          throw new MangaReaderException(string.Format("Plugin for {0} manga type not found.", this.GetType()));
        return plugin;
      }
    }

    public virtual ISiteParser Parser
    {
      get
      {
        if (Mapping.Initialized)
          return Plugin.GetParser();
        throw new MangaReaderException("Mappings not initialized.");
      }
    }

    /// <summary>
    /// Настройки манги.
    /// </summary>
    public virtual MangaSetting Setting
    {
      get
      {
        if (Mapping.Initialized)
          return Plugin.GetSettings();
        throw new MangaReaderException("Mappings not initialized.");
      }
    }

    /// <summary>
    /// История манги.
    /// </summary>
    [XmlIgnore]
    public virtual IEnumerable<MangaHistory> Histories
    {
      get { return histories; }
    }

    public void AddHistory(Uri message)
    {
      AddHistory(new[] { new MangaHistory(message) });
    }

    public void AddHistory(IEnumerable<Uri> messages)
    {
      AddHistory(messages.Select(m => new MangaHistory(m)));
    }

    public void AddHistory(IEnumerable<MangaHistory> history)
    {
      lock (histories)
      {
        var list = history.Where(message => histories.All(h => h.Uri != message.Uri)).ToList();
        foreach (var mangaHistory in list)
        {
          histories.Add(mangaHistory);
        }
      }
    }

    public void ClearHistory()
    {
      lock (histories)
      {
        histories.Clear();
      }
    }


    [XmlIgnore]
    public virtual ICollection<Volume> Volumes { get; set; }

    [XmlIgnore]
    public virtual ICollection<Volume> ActiveVolumes { get; set; }

    [XmlIgnore]
    public virtual ICollection<Chapter> Chapters { get; set; }

    [XmlIgnore]
    public virtual ICollection<Chapter> ActiveChapters { get; set; }

    [XmlIgnore]
    public virtual ICollection<MangaPage> Pages { get; set; }

    [XmlIgnore]
    public virtual ICollection<MangaPage> ActivePages { get; set; }

    /// <summary>
    /// Нужно ли обновлять мангу.
    /// </summary>
    public virtual bool NeedUpdate
    {
      get { return needUpdate; }
      set
      {
        needUpdate = value;
        OnPropertyChanged();
      }
    }

    private bool needUpdate = true;

    public virtual List<Compression.CompressionMode> AllowedCompressionModes { get { return allowedCompressionModes; } }

    private static List<Compression.CompressionMode> allowedCompressionModes =
      new List<Compression.CompressionMode>(Enum.GetValues(typeof(Compression.CompressionMode)).Cast<Compression.CompressionMode>());

    public virtual Compression.CompressionMode? CompressionMode { get; set; }

    private string status;
    private ICollection<MangaHistory> histories;
    private byte[] cover;

    /// <summary>
    /// Статус корректности манги.
    /// </summary>
    public virtual bool IsValid()
    {
      return !string.IsNullOrWhiteSpace(this.Name);
    }

    /// <summary>
    /// Статус перевода.
    /// </summary>
    public virtual bool IsCompleted { get; set; }

    /// <summary>
    /// Признак только страниц, даже без глав.
    /// </summary>
    public bool OnlyPages { get { return !this.HasVolumes && !this.HasChapters; } }

    /// <summary>
    /// Признак наличия глав.
    /// </summary>
    public virtual bool HasChapters { get; set; }

    /// <summary>
    /// Признак наличия томов.
    /// </summary>
    public virtual bool HasVolumes { get; set; }

    #endregion

    #region DownloadProgressChanged

    /// <summary>
    /// Статус загрузки.
    /// </summary>
    public bool IsDownloaded
    {
      get
      {
        var isVolumesDownloaded = this.ActiveVolumes != null && this.ActiveVolumes.Any() &&
                                this.ActiveVolumes.All(v => v.IsDownloaded);
        var isChaptersDownloaded = this.ActiveChapters != null && this.ActiveChapters.Any() && this.ActiveChapters.All(v => v.IsDownloaded);
        var isPagesDownloaded = this.ActivePages != null && this.ActivePages.Any() && this.ActivePages.All(v => v.IsDownloaded);
        return isVolumesDownloaded || isChaptersDownloaded || isPagesDownloaded;
      }
    }

    /// <summary>
    /// Процент загрузки манги.
    /// </summary>
    public double Downloaded
    {
      get
      {
        var volumes = (this.ActiveVolumes != null && this.ActiveVolumes.Any()) ? this.ActiveVolumes.Average(v => v.Downloaded) : double.NaN;
        var chapters = (this.ActiveChapters != null && this.ActiveChapters.Any()) ? this.ActiveChapters.Average(ch => ch.Downloaded) : double.NaN;
        var pages = (this.ActivePages != null && this.ActivePages.Any()) ? this.ActivePages.Average(ch => ch.Downloaded) : 0;
        return double.IsNaN(volumes) ? (double.IsNaN(chapters) ? pages : chapters) : volumes;
      }
      set { }
    }

    public string Folder { get; set; }

    public byte[] Cover
    {
      get { return cover; }
      set
      {
        cover = value;
        OnPropertyChanged();
      }
    }

    public event EventHandler<IManga> DownloadProgressChanged;

    protected void OnDownloadProgressChanged(IManga manga)
    {
      DownloadProgressChanged?.Invoke(this, manga);
    }

    /// <summary>
    /// Обновить содержимое манги.
    /// </summary>
    /// <remarks>Каждая конкретная манга сама забьет коллекцию Volumes\Chapters\Pages.</remarks>
    public virtual void UpdateContent()
    {
      if (this.Pages == null)
        throw new ArgumentNullException("Pages");

      if (this.Chapters == null)
        throw new ArgumentNullException("Chapters");

      if (this.Volumes == null)
        throw new ArgumentNullException("Volumes");

      this.Volumes.Clear();
      this.Chapters.Clear();
      this.Pages.Clear();

      Parser.UpdateContent(this);

      foreach (var page in Pages)
        page.DownloadProgressChanged += (sender, args) => this.OnDownloadProgressChanged(this);
      foreach (var chapter in Chapters)
        chapter.DownloadProgressChanged += (sender, args) => this.OnDownloadProgressChanged(this);
      foreach (var volume in Volumes)
        volume.DownloadProgressChanged += (sender, args) => this.OnDownloadProgressChanged(this);
    }

    public async Task Download(string mangaFolder = null)
    {
      if (!this.NeedUpdate)
        return;

      this.Refresh();
      Cover = Parser.GetPreviews(this).FirstOrDefault();
      this.Save();

      if (mangaFolder == null)
        mangaFolder = this.GetAbsoulteFolderPath();

      this.UpdateContent();

      this.ActiveVolumes = this.Volumes;
      this.ActiveChapters = this.Chapters;
      this.ActivePages = this.Pages;
      if (this.Setting.OnlyUpdate)
      {
        var histories = this.Histories.ToList();

        Func<MangaPage, bool> pagesFilter = p => histories.All(m => m.Uri != p.Uri);
        Func<Chapter, bool> chaptersFilter = ch => histories.All(m => m.Uri != ch.Uri) || ch.Pages.Any(pagesFilter);
        Func<Volume, bool> volumesFilter = v => v.Container.Any(chaptersFilter);

        this.ActivePages = this.ActivePages.Where(pagesFilter).ToList();
        this.ActiveChapters = this.ActiveChapters.Where(chaptersFilter).ToList();
        this.ActiveVolumes = this.ActiveVolumes.Where(volumesFilter).ToList();

        histories.Clear();
      }

      if (!this.ActiveChapters.Any() && !this.ActiveVolumes.Any() && !this.ActivePages.Any())
        return;

      Log.AddFormat("Download start '{0}'.", this.Name);

      // Формируем путь к главе вида Папка_манги\Том_001\Глава_0001
      try
      {
        NetworkSpeed.Clear();
        var plugin = Plugin;
        var tasks = this.ActiveVolumes.Select(
            v =>
            {
              v.DownloadProgressChanged += (sender, args) => this.OnPropertyChanged(nameof(Downloaded));
              v.OnlyUpdate = this.Setting.OnlyUpdate;
              return v.Download(mangaFolder).ContinueWith(t =>
              {
                if (t.Exception != null)
                  Log.Exception(t.Exception, v.Uri.ToString());

                if (plugin.HistoryType == HistoryType.Chapter)
                  this.AddHistory(v.ActiveChapters.Where(c => c.IsDownloaded).Select(ch => ch.Uri));

                if (plugin.HistoryType == HistoryType.Page)
                  this.AddHistory(v.ActiveChapters.SelectMany(ch => ch.ActivePages).Where(p => p.IsDownloaded).Select(p => p.Uri));
              });
            });
        var chTasks = this.ActiveChapters.Select(
          ch =>
          {
            ch.DownloadProgressChanged += (sender, args) => this.OnPropertyChanged(nameof(Downloaded));
            ch.OnlyUpdate = this.Setting.OnlyUpdate;
            return ch.Download(mangaFolder).ContinueWith(t =>
            {
              if (t.Exception != null)
                Log.Exception(t.Exception, ch.Uri.ToString());

              if (ch.IsDownloaded && plugin.HistoryType == HistoryType.Chapter)
                this.AddHistory(ch.Uri);

              if (plugin.HistoryType == HistoryType.Page)
                this.AddHistory(ch.ActivePages.Where(c => c.IsDownloaded).Select(p => p.Uri));
            });
          });
        var pTasks = this.ActivePages.Select(
          p =>
          {
            p.DownloadProgressChanged += (sender, args) => this.OnPropertyChanged(nameof(Downloaded));
            return p.Download(mangaFolder).ContinueWith(t =>
            {
              if (t.Exception != null)
                Log.Exception(t.Exception, $"Не удалось скачать изображение {p.ImageLink} со страницы {p.Uri}");
              if (p.IsDownloaded && plugin.HistoryType == HistoryType.Page)
                this.AddHistory(p.Uri);
            });
          });
        await Task.WhenAll(tasks.Concat(chTasks).Concat(pTasks).ToArray());
        this.Save();
        NetworkSpeed.Clear();
        Log.AddFormat("Download end '{0}'.", this.Name);
      }

      catch (AggregateException ae)
      {
        foreach (var ex in ae.InnerExceptions)
          Log.Exception(ex);
      }
      catch (System.Exception ex)
      {
        Log.Exception(ex);
      }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Методы

    /// <summary>
    /// Обновить информацию о манге - название, главы, обложка.
    /// </summary>
    public virtual void Refresh()
    {
      Parser.UpdateNameAndStatus(this);
      Parser.UpdateContentType(this);
      OnPropertyChanged(nameof(IsCompleted));
    }

    /// <summary>
    /// Упаковка манги.
    /// </summary>
    public void Compress()
    {
      Log.Info(Strings.Mangas_Compress_Started + this.Name);
      var folder = this.GetAbsoulteFolderPath();
      switch (this.CompressionMode)
      {
        case Compression.CompressionMode.Manga:
          Compression.CompressManga(folder);
          break;
        case Compression.CompressionMode.Volume:
          Compression.CompressVolumes(folder);
          break;
        case Compression.CompressionMode.Chapter:
          Compression.CompressChapters(folder);
          break;
        case null:
          throw new InvalidEnumArgumentException("CompressionMode is null", -1, typeof(Compression.CompressionMode));
        default:
          throw new InvalidEnumArgumentException(nameof(CompressionMode), (int)this.CompressionMode, typeof(Compression.CompressionMode));
      }
      Log.Info(Strings.Mangas_Compress_Completed);
    }

    protected override void BeforeSave(object[] currentState, object[] previousState, string[] propertyNames)
    {
      var mangaFolder = DirectoryHelpers.MakeValidPath(this.Name.Replace(Path.DirectorySeparatorChar, '.'));
      Folder = DirectoryHelpers.MakeValidPath(Path.Combine(this.Setting.Folder, mangaFolder));
      currentState[propertyNames.ToList().IndexOf(nameof(Folder))] = Folder;

      if (CompressionMode == null)
      {
        CompressionMode = this.GetDefaultCompression();
        currentState[propertyNames.ToList().IndexOf(nameof(CompressionMode))] = CompressionMode;
      }

      if (Repository.Get<IManga>().Any(m => m.Id != this.Id && m.Folder == this.Folder))
        throw new SaveValidationException($"Другая манга уже использует папку {this.Folder}.", this);

      if (previousState != null)
      {
        var dirName = previousState[propertyNames.ToList().IndexOf(nameof(Folder))] as string;
        var newValue = this.GetAbsoulteFolderPath();
        var oldValue = DirectoryHelpers.GetAbsoulteFolderPath(dirName);
        if (oldValue != null && !DirectoryHelpers.Equals(newValue, oldValue) && Directory.Exists(oldValue))
        {
          if (Directory.Exists(newValue))
            throw new MangaDirectoryExists("Папка уже существует.", newValue, this);

          // Копируем папку на новый адрес при изменении имени.
          DirectoryHelpers.MoveDirectory(oldValue, newValue);
        }
      }

      base.BeforeSave(currentState, previousState, propertyNames);
    }

    public override void Save()
    {
      if (!this.IsValid())
        throw new SaveValidationException("Нельзя сохранять невалидную сущность", this);

      base.Save();
    }

    public override string ToString()
    {
      return this.Name;
    }

    private void UpdateUri(Uri value)
    {
      if (this.uri != null && !Equals(this.uri, value))
      {
        foreach (var history in this.Histories)
        {
          var historyUri = new UriBuilder(history.Uri) { Scheme = value.Scheme, Host = value.Host, Port = -1 };
          historyUri.Path = historyUri.Path.Replace(this.uri.AbsolutePath, value.AbsolutePath);
          history.Uri = historyUri.Uri;
        }
      }
    }

    /// <summary>
    /// Создать мангу по ссылке.
    /// </summary>
    /// <param name="uri">Ссылка на мангу.</param>
    /// <returns>Манга.</returns>
    /// <remarks>Не сохранена в базе, требует заполнения полей.</remarks>
    public static IManga Create(Uri uri)
    {
      IManga manga = null;

      var setting = Repository.Get<MangaSetting>()
        .ToList()
        .SingleOrDefault(s => s.MangaSettingUris.Any(u => u.Host == uri.Host));
      if (setting != null)
      {
        var plugin = ConfigStorage.Plugins.SingleOrDefault(p => Equals(p.GetSettings(), setting));
        if (plugin != null)
          manga = Activator.CreateInstance(plugin.MangaType) as IManga;
      }

      if (manga != null)
      {
        var parseResult = manga.Parser.ParseUri(uri);
        manga.Uri = parseResult.CanBeParsed ? parseResult.MangaUri : uri;
      }

      return manga;
    }

    /// <summary>
    /// Создать мангу по ссылке, загрузив необходимую информацию с сайта.
    /// </summary>
    /// <param name="uri">Ссылка на мангу.</param>
    /// <returns>Манга.</returns>
    /// <remarks>Сохранена в базе, если была создана валидная манга.</remarks>
    public static IManga CreateFromWeb(Uri uri)
    {
      var manga = Create(uri);
      if (manga != null)
      {
        // Только для местной реализации - вызвать Created\Refresh.
        if (manga is Mangas mangas)
          mangas.Created(uri);

        if (manga.IsValid())
          manga.Save();
      }

      return manga;
    }

    protected virtual void Created(Uri url)
    {
      this.Refresh();
    }

    protected void AddHistoryReadedUris<T>(T source, Uri url) where T : IEnumerable<IDownloadable>
    {
      this.AddHistory(source.TakeWhile(c => c.Uri.AbsolutePath != url.AbsolutePath).Select(ch => ch.Uri));
    }

    protected Mangas()
    {
      this.histories = new List<MangaHistory>();
      this.Chapters = new List<Chapter>();
      this.Volumes = new List<Volume>();
      this.Pages = new List<MangaPage>();
    }

    #endregion
  }
}
