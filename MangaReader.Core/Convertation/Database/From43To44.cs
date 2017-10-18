﻿using System;
using System.Linq;
using MangaReader.Core.Convertation.Primitives;
using MangaReader.Core.Services;

namespace MangaReader.Core.Convertation.Database
{
  public class From43To44 : DatabaseConverter
  {
    protected override void ProtectedConvert(IProcess process)
    {
      base.ProtectedConvert(process);

      var settings = NHibernate.Repository.Get<MangaSetting>().ToList();
      foreach (var setting in settings)
      {
        this.RunSql($"update Mangas set Setting = {setting.Id} where Setting is null and Type = \"{setting.Manga}\"");
      }
    }

    public From43To44()
    {
      this.Version = new Version(1, 44, 1);
    }
  }
}