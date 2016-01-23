﻿using System;
using MangaReader.Services;
using MangaReader.Services.Config;

namespace MangaReader.CLI
{
  public class ConsoleProgress : IProcess
  {
    private double percent;
    private string status;

    public double Percent
    {
      get { return percent; }
      set
      {
        percent = value;
        if (value != 0)
          Report(value);
      }
    }

    public bool IsIndeterminate { get; set; }

    public string Status
    {
      get { return status; }
      set
      {
        if (Equals(status, value))
          return;

        status = value;
        Report(value);
      }
    }

    public Version Version { get; set; }
    public ConvertState State { get; set; }
    public event EventHandler<ConvertState> StateChanged;

    private static void Report(object text)
    {
      Console.WriteLine(text);
    }

    protected virtual void OnStateChanged(ConvertState e)
    {
      Console.WriteLine(e);
      StateChanged?.Invoke(this, e);
    }

    public ConsoleProgress()
    {
      this.Version = AppConfig.Version;
    }
  }
}