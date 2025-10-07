using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace WebApi.MinimalApi.Models;

public class GetUsersRequest
{
    [DefaultValue(1)]
    [Range(1, int.MaxValue)]
    [SwaggerParameter(Description = "Page number to show. Should be greater than or equal to 1")]
    public int PageNumber { get; set; } = 1;

    [DefaultValue(10)]
    [Range(1, 20)]
    [SwaggerParameter(Description = "Number of items per page. Must be between 1 and 20")]
    public int PageSize { get; set; } = 10;
}