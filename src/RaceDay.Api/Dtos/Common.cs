using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Dtos;

/// <summary>Paging parameters accepted by every endpoint that returns a collection.</summary>
public class PagingQuery
{
    /// <summary>Page number, starting at 1.</summary>
    /// <example>1</example>
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Items per page, from 1 to 100.</summary>
    /// <example>20</example>
    [FromQuery(Name = "pageSize")]
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

/// <summary>One page of a collection, with the total so a client can render pagination.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class PagingExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PagingQuery paging, CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T> { Items = items, Page = paging.Page, PageSize = paging.PageSize, TotalCount = total };
    }
}
