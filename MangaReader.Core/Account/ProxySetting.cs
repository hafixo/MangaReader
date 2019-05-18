﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using MangaReader.Core.Entity;
using MangaReader.Core.Services;

namespace MangaReader.Core.Account
{
  [DebuggerDisplay("Type = {SettingType}, Id = {Id}, User = {UserName}")]
  public class ProxySetting : Entity.Entity
  {
    internal static readonly Lazy<IWebProxy> SystemProxy = new Lazy<IWebProxy>(() =>
    {
      var proxy = WebRequest.GetSystemWebProxy();
      proxy.Credentials = CredentialCache.DefaultCredentials;
      return proxy;
    });

    public string Name { get; set; }

    public Uri Address { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

    public ProxySettingType SettingType { get; set; }

    public virtual IWebProxy GetProxy()
    {
      switch (SettingType)
      {
        case ProxySettingType.NoProxy:
          return null;
        case ProxySettingType.System:
          return SystemProxy.Value;
        case ProxySettingType.Manual:
          return new WebProxy(Address, true, null, new NetworkCredential(UserName, Password));
        case ProxySettingType.Parent:
          return MangaSettingCache.Get(MangaSettingCache.RootPluginType).Proxy;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    public override Task BeforeSave(ChangeTrackerArgs args)
    {
      if (!args.IsNewEntity && !args.CanAddEntities)
      {
        var typeState = args.GetPropertyState<ProxySettingType>(nameof(SettingType));
        if (typeState.IsChanged)
          MangaSettingCache.RevalidateSetting(this);
        else
        {
          var userNameState = args.GetPropertyState<string>(nameof(UserName));
          var passwordState = args.GetPropertyState<string>(nameof(Password));
          var addressState = args.GetPropertyState<Uri>(nameof(Address));
          if (userNameState.IsChanged || passwordState.IsChanged || addressState.IsChanged)
          {
            MangaSettingCache.RevalidateSetting(this);
          }
        }
      }

      return base.BeforeSave(args);
    }

    protected ProxySetting()
    {
      this.SettingType = ProxySettingType.NoProxy;
    }

    public ProxySetting(ProxySettingType settingType)
    {
      this.SettingType = settingType;
    }
  }

  public enum ProxySettingType
  {
    NoProxy,
    System,
    Manual,
    Parent
  }
}
