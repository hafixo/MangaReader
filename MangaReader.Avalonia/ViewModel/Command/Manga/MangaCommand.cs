﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using MangaReader.Avalonia.ViewModel.Command.Library;
using MangaReader.Avalonia.ViewModel.Explorer;
using MangaReader.Core.Manga;
using MangaReader.Core.NHibernate;
using MangaReader.Core.Services;

namespace MangaReader.Avalonia.ViewModel.Command.Manga
{
  public abstract class MultipleMangasBaseCommand : LibraryBaseCommand
  {
    private bool canExecuteNeedSelection;
    protected bool NeedRefresh { get; set; }

    protected bool CanExecuteNeedSelection
    {
      get { return canExecuteNeedSelection; }
      set
      {
        canExecuteNeedSelection = value;
        OnPropertyChanged();
        SubscribeToSelection(canExecuteNeedSelection);
      }
    }

    protected Explorer.LibraryViewModel LibraryModel { get; }

    protected IEnumerable<MangaModel> SelectedModels => LibraryModel.SelectedMangaModels;

    public override bool CanExecute(object parameter)
    {
      return base.CanExecute(parameter) && CanExecuteMangaCommand();
    }

    protected bool CanExecuteMangaCommand()
    {
      return SelectedModels.Any();
    }

    public override async Task Execute(object parameter)
    {
      using (var context = Repository.GetEntityContext($"Manga command '{this.Name}'"))
      {
        var ids = SelectedModels.Select(m => m.Id).ToList();
        var query = await context.Get<IManga>().Where(m => ids.Contains(m.Id)).ToListAsync().ConfigureAwait(true);
        var mangas = query.OrderBy(m => ids.IndexOf(m.Id)).ToList();
        try
        {
          await this.Execute(mangas).ConfigureAwait(true);
        }
        catch (Exception e)
        {
          Log.Exception(e);
        }
        finally
        {
          foreach (var model in SelectedModels)
          {
            model.UpdateProperties(mangas.SingleOrDefault(m => m.Id == model.Id));
          }
        }
      }

      if (NeedRefresh)
      {
        var selected = SelectedModels.ToList();
        LibraryModel.ResetView();
        foreach (var model in selected.Where(s => LibraryModel.FilteredItems.Contains(s) && !LibraryModel.SelectedMangaModels.Contains(s)))
          LibraryModel.SelectedMangaModels.Add(model);
      }

      foreach (var command in LibraryModel.MangaCommands.Where(m => m.GetType() == GetType()).OfType<MultipleMangasBaseCommand>())
        command.OnCanExecuteChanged();
    }

    public abstract Task Execute(IEnumerable<IManga> mangas);

    private void SubscribeToSelection(bool subscribe)
    {
      if (subscribe)
        LibraryModel.SelectedMangaModels.CollectionChanged += SelectedMangaModelsOnCollectionChanged;
      else
        LibraryModel.SelectedMangaModels.CollectionChanged -= SelectedMangaModelsOnCollectionChanged;
      OnCanExecuteChanged();
    }

    private void SelectedMangaModelsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
      OnCanExecuteChanged();
    }

    protected MultipleMangasBaseCommand(Explorer.LibraryViewModel model) : base(model.Library)
    {
      LibraryModel = model;
      this.NeedRefresh = true;
      this.CanExecuteNeedSelection = true;
    }
  }
}
