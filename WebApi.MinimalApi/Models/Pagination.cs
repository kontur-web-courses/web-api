namespace WebApi.MinimalApi.Models;

public class Pagination
{
    public string? PreviousPageLink { get; set; }
    public string? NextPageLink { get; set; }
    public int? TotalCount { get; set; }
    public int? PageSize { get; set; }
    public int? CurrentPage { get; set; }
    public int? TotalPages { get; set; }
}