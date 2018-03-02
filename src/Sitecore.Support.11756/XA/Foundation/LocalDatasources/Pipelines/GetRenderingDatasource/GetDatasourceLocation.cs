using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.Text;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using Sitecore.XA.Foundation.TokenResolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sitecore.Support.XA.Foundation.LocalDatasources.Pipelines.GetRenderingDatasource
{
  public class GetDatasourceLocation
  {
    public void Process(GetRenderingDatasourceArgs args)
    {
      Assert.IsNotNull((object)args, nameof(args));
      foreach (string query in new ListString(args.RenderingItem["Datasource Location"]))
      {
        if (query.StartsWith("query:", StringComparison.InvariantCulture))
        {
          this.AddRootsFromQuery(query, args);
        }
        else
        {
          string path = query;
          if (query.StartsWith("./", StringComparison.InvariantCulture) && !string.IsNullOrEmpty(args.ContextItemPath))
            path = args.ContextItemPath + query.Remove(0, 1);
          Item obj = args.ContentDatabase.GetItem(path);
          if (obj != null)
            args.DatasourceRoots.AddUnique<Item>(obj, (Func<Item, object>)(item => (object)item.ID));
        }
      }
    }

    protected virtual void AddRootsFromQuery(string query, GetRenderingDatasourceArgs args)
    {
      Assert.ArgumentNotNull((object)args, nameof(args));
      Assert.ArgumentNotNullOrEmpty(query, nameof(query));
      Item contextItem = args.ContentDatabase.GetItem(args.ContextItemPath);
      query = TokenResolver.Resolve(query, contextItem, false, true);
      if (query == string.Empty)
        return;
      Item[] objArray = (Item[])null;
      if (query.StartsWith("./", StringComparison.InvariantCulture) && !string.IsNullOrEmpty(args.ContextItemPath))
      {
        Item obj = contextItem;
        if (obj != null)
          objArray = obj.Axes.SelectItems(this.GetWorkingQuery(query));
      }
      else
        objArray = args.ContentDatabase.SelectItems(this.GetWorkingQuery(query));
      if (objArray == null)
        return;
      foreach (Item obj in objArray)
        args.DatasourceRoots.AddUnique<Item>(obj, (Func<Item, object>)(item => (object)item.ID));
    }

    protected virtual string GetWorkingQuery(string query)
    {
      if (query.IndexOf("//", StringComparison.InvariantCulture) < 0 && query.IndexOf('[') < 0 && query.IndexOf('@') < 0 || (query.ToLower().Contains("ancestor-or-self") || query.ToLower().Contains("descendant-or-self")))
        return query;
      return EscapePath(query, true);
    }

    #region FIX 11756
    private string EscapePath(string path, bool escapeSpaces)
    {
      string[] strArray = path.Split('/');
      for (int index = 0; index < strArray.Length; ++index)
      {
        string input = strArray[index];
        if (input.Contains("$"))
          strArray[index] = input.ToLowerInvariant();
        if (escapeSpaces && (input.Contains(" ") || input.Contains("-")) && !Regex.IsMatch(input, "\\[@@(.)*=.*\\]", RegexOptions.Compiled))
        {
          if (input.Contains("|"))
          {
            input = input.Replace("|", string.Empty);
            strArray[index] = "#" + input.Trim('#') + "#|";
          }
          else
          {
            strArray[index] = "#" + input.Trim('#') + "#";
          }
        }
      }
      return ((IEnumerable<string>)strArray).DefaultIfEmpty<string>("").Aggregate<string>((Func<string, string, string>)((a, b) => a + "/" + b));
    }
   #endregion
  }
}