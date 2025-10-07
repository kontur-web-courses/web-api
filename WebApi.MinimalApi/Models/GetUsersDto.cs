using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.MinimalApi.Models;

public class GetUsersDto
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    [FromQuery]
    public int pageNumber
    {
        get => _pageNumber;
        set
        {
            _pageNumber = value;
            if (value <= 0)
                _pageNumber = 1;
        }
    }

    [FromQuery]
    public int pageSize
    {
        get => _pageSize;
        set
        {
            _pageSize = value;
            if (value <= 0)
                _pageSize = 1;   
            if (value > 20)
                _pageSize = 20;
        }
    }
}