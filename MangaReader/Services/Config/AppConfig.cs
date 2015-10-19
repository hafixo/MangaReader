﻿using System;
using System.IO;
using System.Reflection;

namespace MangaReader.Services.Config
{
  public class AppConfig
  {
    /// <summary>
    /// Язык манги.
    /// </summary>
    public Languages Language { get; set; }

    /// <summary>
    /// Автообновление программы.
    /// </summary>
    public bool UpdateReader { get; set; }

    /// <summary>
    /// Сворачивать в трей.
    /// </summary>
    public bool MinimizeToTray { get; set; }

    /// <summary>
    /// Частота автообновления библиотеки в часах.
    /// </summary>
    public int AutoUpdateInHours { get; set; }

    /// <summary>
    /// Время последнего обновления.
    /// </summary>
    internal DateTime LastUpdate { get; set; }

    /// <summary>
    /// Версия приложения.
    /// </summary>
    internal static Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

    /// <summary>
    /// Папка загрузки.
    /// </summary>
    internal static string DownloadFolder { get { return Path.Combine(ConfigStorage.WorkFolder, "Download"); } }

    /// <summary>
    /// Префикс папки томов.
    /// </summary>
    internal static string VolumePrefix = "Volume ";

    /// <summary>
    /// Префикс папки глав.
    /// </summary>
    internal static string ChapterPrefix = "Chapter ";

    public AppConfig()
    {
      this.Language = Languages.English;
      this.UpdateReader = true;
      this.MinimizeToTray = false;
      this.AutoUpdateInHours = 0;
      this.LastUpdate = DateTime.Now;
    }
  }

  /// <summary>
  /// Доступные языки.
  /// </summary>
  public enum Languages
  {
    English,
    Russian,
    Japanese
  }
}