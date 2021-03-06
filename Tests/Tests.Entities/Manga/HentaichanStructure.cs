﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Hentaichan;
using MangaReader.Core.Manga;
using MangaReader.Core.NHibernate;
using MangaReader.Core.Services.Config;
using NUnit.Framework;

namespace Tests.Entities.Manga
{
  [TestFixture]
  public class HentaichanStructure : TestClass
  {
    [Test]
    public async Task AddHentaichanMultiPages()
    {
      var manga = await GetManga("https://henchan.pro/manga/14212-love-and-devil-glava-25.html").ConfigureAwait(false);
      Assert.AreEqual(25, manga.Chapters.Count);
      Assert.IsTrue(manga.HasChapters);
    }

    [Test]
    public async Task AddHentaichanOneChapter()
    {
      var manga = await GetManga("https://henchan.pro/manga/15131-chuui-horeru-to-yakui-kara.html").ConfigureAwait(false);
      Assert.AreEqual(1, manga.Chapters.Count);
      Assert.IsTrue(manga.HasChapters);
    }

    [Test]
    public async Task AddHentaichanSubdomain()
    {
      var manga = await GetManga("https://henchan.pro/manga/23083-ponpharse-tokubetsu-hen-chast-1.html").ConfigureAwait(false);
      Assert.AreEqual(2, manga.Chapters.Count);
      Assert.IsTrue(manga.HasChapters);
      Assert.AreEqual(1, manga.Chapters.First().Number);
    }

    [Test]
    public async Task ParsingDoublesInChapterName()
    {
      var manga = await GetManga(MangaInfos.Henchan.TwistedIntent.Uri).ConfigureAwait(false);
      var mangaChapters = manga.Chapters.ToList();
      Assert.AreEqual(3, mangaChapters.Count);
      Assert.IsTrue(manga.HasChapters);
      Assert.AreEqual(1, mangaChapters[0].Number);
      Assert.AreEqual(2.1, mangaChapters[1].Number);
      Assert.AreEqual(2.2, mangaChapters[2].Number);
    }


    [Test]
    public async Task HentaichanNameParsing()
    {
      // Спецсимвол \
      await TestNameParsing("https://henchan.pro/manga/14504-lets-play-lovegames-shall-we-glava-1.html",
        "Let's Play Lovegames, Shall We?").ConfigureAwait(false);

      // Спецсимвол # и одна глава
      await TestNameParsing("https://henchan.pro/manga/15109-exhibitionist-renko-chan.html",
        "#Exhibitionist Renko-chan").ConfigureAwait(false);

      // Символ звездочки *
      await TestNameParsing("https://henchan.pro/manga/15131-chuui-horeru-to-yakui-kara.html",
        "*Chuui* Horeru to Yakui kara").ConfigureAwait(false);

      // Символ /
      await TestNameParsing("https://henchan.pro/manga/10535-blush-dc.-glava-1.html",
        "/Blush-DC.").ConfigureAwait(false);

      // На всякий случай
      await TestNameParsing("https://henchan.pro/manga/23083-ponpharse-tokubetsu-hen-chast-1.html",
        "Ponpharse - Tokubetsu Hen").ConfigureAwait(false);

      // Манга требующая регистрации для просмотра
      await TestNameParsing("https://henchan.pro/manga/14212-love-and-devil-glava-25.html",
        "Love and Devil").ConfigureAwait(false);
    }

    private async Task TestNameParsing(string uri, string name)
    {
      ConfigStorage.Instance.AppConfig.Language = Languages.English;
      var manga = await GetManga(uri).ConfigureAwait(false);
      Assert.AreEqual(name, manga.Name);
    }

    private async Task<Hentaichan.Hentaichan> GetManga(string url)
    {
      await Login().ConfigureAwait(false);
      var manga = await Mangas.CreateFromWeb(new Uri(url)).ConfigureAwait(false) as Hentaichan.Hentaichan;
      await Unlogin().ConfigureAwait(false);
      return manga;
    }

    private async Task Login()
    {
      using (var context = Repository.GetEntityContext())
      {
        var userId = "235332";
        var setting = ConfigStorage.GetPlugin<Hentaichan.Hentaichan>().GetSettings();
        var login = setting.Login as Hentaichan.HentaichanLogin;
        if (login.UserId != userId)
        {
          login.UserId = userId;
          login.PasswordHash = "0578caacc02411f8c9a1a0af31b3befa";
          login.IsLogined = true;
          await context.Save(setting).ConfigureAwait(false);
        }
      }
    }

    private async Task Unlogin()
    {
      using (var context = Repository.GetEntityContext())
      {
        var setting = ConfigStorage.GetPlugin<Hentaichan.Hentaichan>().GetSettings();
        var login = setting.Login as Hentaichan.HentaichanLogin;
        login.UserId = "";
        login.PasswordHash = "";
        login.IsLogined = false;
        await context.Save(setting).ConfigureAwait(false);
      }
    }
  }
}
