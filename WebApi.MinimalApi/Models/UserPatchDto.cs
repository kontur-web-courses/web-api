namespace WebApi.MinimalApi.Models;

public class UserPatchDto
{
    public string? Login { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}