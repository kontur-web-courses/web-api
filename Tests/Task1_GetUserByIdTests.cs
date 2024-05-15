using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class Task1_GetUserByIdTests : UsersApiTestsBase
    {
        [Test]
        public async Task Test1_Code200_WhenAllIsFine()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersByIdUri("77777777-7777-7777-7777-777777777777");
            request.Headers.Add("Accept", "application/json");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            response.ShouldHaveJsonContentEquivalentTo(new
            {
                login = "Admin",
                id = "77777777-7777-7777-7777-777777777777",
                fullName = "Halliday James",
                gamesPlayed = 999,
                currentGameId = (string)null
            });
        }

        [Test]
        public async Task Test2_Code404_WhenUserIdIsUnknown()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersByIdUri("77777777-6666-6666-6666-777777777777");
            request.Headers.Add("Accept", "application/json");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test3_Code404_WhenUserIdIsTrash()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersByIdUri("trash");
            request.Headers.Add("Accept", "application/json");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test4_Code200_WhenAcceptXml()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersByIdUri("77777777-7777-7777-7777-777777777777");
            request.Headers.Add("Accept", "application/xml");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/xml; charset=utf-8");
            var xml = response.ReadContentAsXml();
            var ids = xml.ValuesOfElements("Id");
            ids.Should().BeEquivalentTo("77777777-7777-7777-7777-777777777777");
        }

        [Test]
        public async Task Test5_Code406_WhenAcceptTextPlain()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersByIdUri("77777777-7777-7777-7777-777777777777");
            request.Headers.Add("Accept", "text/plain");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
            response.ShouldNotHaveHeader("Content-Type");
        }
    }
}