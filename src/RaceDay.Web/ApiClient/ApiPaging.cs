using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.ApiClient;

public static class ApiPaging
{
    public const int MaxPageSize = 100;

    public static async Task<List<T>> CollectAllAsync<T>(Func<int, Task<ApiResult<PagedResult<T>>>> fetchPage, int maxPages = 50)
    {
        var items = new List<T>();
        for (var page = 1; page <= maxPages; page++)
        {
            var result = (await fetchPage(page)).Require();
            items.AddRange(result.Items);
            if (page >= result.TotalPages)
            {
                break;
            }
        }

        return items;
    }
}
