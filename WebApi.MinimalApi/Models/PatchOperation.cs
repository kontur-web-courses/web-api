namespace WebApi.MinimalApi.Models;

public class PatchOperation
{
    public string op { get; set; } = ""; 
    public string path { get; set; } = ""; 
    public string? value { get; set; }
}