using System.Xml.Serialization;

namespace WebApi.MinimalApi.Models;

[XmlRoot("guid")]
public class UserDtoXmlWrapper
{
    [XmlElement("User")]
    public UserDto UserDto { get; set; }
}