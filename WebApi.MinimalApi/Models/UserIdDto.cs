using System.Xml.Serialization;

namespace WebApi.MinimalApi.Models;

[XmlRoot("guid")]
public class UserIdDto
{
    public Guid Id { get; set; }
}
