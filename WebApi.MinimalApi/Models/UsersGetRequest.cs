using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UsersGetRequest
{
    [Range(1, 20, ErrorMessage = "Please enter a number between 1 and 20")]
    public int PageSize { get; set; } = 10;

    [Range(1, int.MaxValue, ErrorMessage = "PageSize must be a positive number")]
    public int PageNumber { get; set; } = 1;
}
