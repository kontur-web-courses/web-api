namespace WebApi.MinimalApi.Models;

public class GetAllUsersDto
{
    private int pageNumber = 1;
    private int pageSize = 10;

    public int PageNumber
    {
        get => pageNumber;
        set => pageNumber = Math.Max(value, 1);
    }

    public int PageSize
    {
        get => pageSize;
        set => pageSize = Math.Clamp(value, 1, 20);
    }
}