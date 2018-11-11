﻿using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Reflection;
using System.Drawing.Imaging;

namespace MangaReader.Core.Services
{
  public class ImageFile
  {
    public virtual byte[] Body { get; set; }

    public virtual string Hash
    {
      get
      {
        if (string.IsNullOrWhiteSpace(this.hash))
          this.hash = this.GetHashCode();
        return this.hash;
      }
      set { this.hash = value; }
    }

    private string hash = string.Empty;

    protected internal virtual bool Exist
    {
      get { return this.Body != null && this.Body.LongLength > 0; }
    }

    protected internal virtual string Extension
    {
      get
      {
        if (this.Exist && string.IsNullOrWhiteSpace(this.extension))
        {
          var created = Image.FromStream(new MemoryStream(this.Body));
          var parsed = ImageFileExtension(created.RawFormat, "jpg");
          this.extension = parsed.ToLower();
        }

        return this.extension;
      }
      set { this.extension = value; }
    }

    private string extension = string.Empty;

    public virtual string Path { get; set; }

    public virtual new string GetHashCode()
    {
      using (var md5 = MD5.Create())
      {
        return BitConverter.ToString(md5.ComputeHash(md5.ComputeHash(this.Body))).Replace("-", "");
      }
    }

    /// <summary>
    /// Сохранить файл на диск.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    public virtual async Task Save(string path)
    {
      using (FileStream sourceStream = new FileStream(path,
        FileMode.Create, FileAccess.Write, FileShare.None,
        bufferSize: 4096, useAsync: true))
      {
        await sourceStream.WriteAsync(this.Body, 0, this.Body.Length);
      }

      this.Path = path;
    }

    /// <summary>
    /// Удалить файл с диска.
    /// </summary>
    public virtual void Delete()
    {
      if (string.IsNullOrWhiteSpace(this.Path) || !File.Exists(this.Path))
      {
        Log.AddFormat("ImageFile not found. Path - '{0}', Hash - '{1}'.", this.Path, this.Hash);
        return;
      }

      File.Delete(this.Path);
    }

    // Code reworked from https://referencesource.microsoft.com/#System.Drawing/commonui/System/Drawing/Advanced/ImageFormatConverter.cs
    private static string ImageFileExtension(ImageFormat value, string defaultValue)
    {
      var props = typeof(ImageFormat).GetProperties(BindingFlags.Static | BindingFlags.Public);
      foreach (var p in props)
      {
        if (p.GetValue(null, null).Equals(value))
          return p.Name;
      }

      return defaultValue;
    }
  }
}
