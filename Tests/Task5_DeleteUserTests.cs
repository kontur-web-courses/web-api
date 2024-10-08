using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class Task5_DeleteUserTests : UsersApiTestsBase
    {
        [Test]
        public async Task Test1_Code204_WhenAllIsFine()
        {
            var createdUserId = await CreateUser(new
            {
                login = "condenado",
                firstName = "a muerte",
                lastName = "Condenado"
            });

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Delete;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test2_Code404_WhenUserIsUnknown()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Delete;
            request.RequestUri = BuildUsersByIdUri("77777777-6666-6666-6666-777777777777");
            request.Headers.Add("Accept", "*/*");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test3_Code404_WhenUserIdIsTrash()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Delete;
            request.RequestUri = BuildUsersByIdUri("trash");
            request.Headers.Add("Accept", "*/*");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test4_Code404_WhenAlreadyCreatedAndDeleted()
        {
            var createdUserId = await CreateUser(new
            {
                login = "condenado",
                firstName = "a muerte",
                lastName = "Condenado"
            });

            await DeleteUser(createdUserId);

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Delete;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }
    }
}