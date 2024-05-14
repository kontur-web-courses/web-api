using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class Task8_GetUsersOptionsTests : UsersApiTestsBase
    {
        [Test]
        public async Task Test1_Code200_AllIsFine()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Options;
            request.RequestUri = BuildUsersUri();
            request.Headers.Add("Accept", "*/*");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldNotHaveHeader("Content-Type");
            var allow = response.GetRequiredHeader("Allow");
            allow.Should().BeEquivalentTo("POST", "GET", "OPTIONS");
        }
    }
}