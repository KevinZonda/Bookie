using KevinZonda.BooCat.Library;
using KevinZonda.BooCat.Library.Models;
using KevinZonda.BooCat.Library.Models.WebAPI;

namespace KevinZonda.BooCat.AspNetCoreWebAPI.Controllers;

public static class BooCatController
{
    private static readonly ProviderDic dic = new ProviderDic();

    public static async Task<IResult> ProviderRequest(string provider, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest((ErrModel)"Not Valid Name");
        var p = dic[provider];
        if (p == null)
            return Results.BadRequest((ErrModel)"Not Valid Provider");
        var (Infos, Err) = await p.SearchBook(name);

        if (Err != null)
            return Results.BadRequest((ErrModel)Err);
        return Results.Ok(Infos);
    }

    public static async Task<IResult> MultipleProviderRequest(string[]? providers, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest((ErrModel)"Not Valid Name");

        if (providers == null || providers.Length == 0)
            return Results.BadRequest((ErrModel)"Not Valid Provider");

        var _dic = new Dictionary<string, Task<(BookInfo[] Infos, Exception? Err)>>();
        foreach (var provider in providers)
        {
            var p = dic[provider];
            if (p == null) continue;
            _dic.Add(provider, p.SearchBook(name));
        }
        await Task.Factory.StartNew(() => Task.WaitAll(_dic.Values.ToArray(), 10000));

        var _resultDic = new Dictionary<string, ResultModel>();
        foreach (var kvp in _dic)
        {
            var rst = kvp.Value;
            if (!rst.IsCompleted)
            {
                _resultDic.Add(kvp.Key, (ResultModel)(ErrModel)"Not completed.");
                continue;
            }
            var (Infos, Err) = rst.Result;
            if (Err != null) _resultDic.Add(kvp.Key, (ResultModel)Err);
            else _resultDic.Add(kvp.Key, (ResultModel)Infos);
        }
        return Results.Ok(_resultDic);
    }
}