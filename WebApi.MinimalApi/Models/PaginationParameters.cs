using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class PaginationParameters
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;
    
    [Range(1, 20)]
    public int PageSize { get; set; } = 10;
}