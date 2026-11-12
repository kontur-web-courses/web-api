namespace WebApi.MinimalApi.Models;

public class UsersGetDto
{
    private int pageNumber = 1;
    private int pageSize = 10;

    public int PageNumber
    {
        get => pageNumber;
        set => pageNumber = Math.Max(pageNumber, value);
    }

    public int PageSize
    {
        get => pageSize;
        set => pageSize = Math.Min(Math.Max(1, value), 20);
    }
}