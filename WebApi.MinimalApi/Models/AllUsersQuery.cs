using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.MinimalApi.Models;

public class AllUsersQuery
{
    [FromQuery]
    [DefaultValue(1)]
    [Range(1, int.MaxValue)]
    public int? PageNumber { get; set; }

    [FromQuery]
    [DefaultValue(10)]
    [Range(1, 20)]
    public int? PageSize { get; set; }

    public void Deconstruct(out int pageNumber, out int pageSize)
    {
        pageNumber = PageNumber.Value;
        pageSize = PageSize.Value;
    }
}