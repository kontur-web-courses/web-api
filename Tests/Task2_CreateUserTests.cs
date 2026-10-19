using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class Task2_CreateUserTests : UsersApiTestsBase
    {
        [Test]
        public async Task Test1_Code201_WhenAllIsFine()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = BuildUsersUri();
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                login = "mjackson",
                firstName = "Michael",
                lastName = "Jackson"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");

            var createdUserId = response.ReadContentAsJson().ToString();
            createdUserId.Should().NotBeNullOrEmpty();
            var createdUserUri = response.GetRequiredHeader("Location").SingleOrDefault();
            createdUserUri.Should().NotBeNullOrEmpty();

            CheckUserCreated(createdUserId, createdUserUri, new
            {
                id = createdUserId,
                login = "mjackson",
                fullName = "Jackson Michael",
                gamesPlayed = 0,
                currentGameId = (string)null
            });
        }

        [Test]
        public async Task Test2_Code400_WhenEmptyContent()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = BuildUsersUri();
            request.Headers.Add("Accept", "*/*");
            request.AddEmptyContent("application/json; charset=utf-8");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test3_Code422_WhenEmptyLogin()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = BuildUsersUri();
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                firstName = "Michael",
                lastName = "Jackson"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            var responseContent = response.ReadContentAsJson() as JObject;
            responseContent.Should().NotBeNull();
            responseContent.GetValue("login").Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task Test4_Code422_WhenLoginWithUnallowedChars()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = BuildUsersUri();
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                login = "!jackson!",
                firstName = "Michael",
                lastName = "Jackson"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            var responseContent = response.ReadContentAsJson() as JObject;
            responseContent.Should().NotBeNull();
            responseContent.GetValue("login").Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task Test5_Code201_WithDefaultFirstNameAndLastName()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = BuildUsersUri();
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                login = "anonymous"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");

            var createdUserId = response.ReadContentAsJson().ToString();
            createdUserId.Should().NotBeNullOrEmpty();
            var createdUserUri = response.GetRequiredHeader("Location").SingleOrDefault();
            createdUserUri.Should().NotBeNullOrEmpty();

            CheckUserCreated(createdUserId, createdUserUri, new
            {
                id = createdUserId,
                login = "anonymous",
                fullName = "Doe John",
                gamesPlayed = 0,
                currentGameId = (string)null
            });
        }

        [Test]
        public async Task Test6_Code201_WhenAcceptXml()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = BuildUsersUri();
            request.Headers.Add("Accept", "application/xml");
            request.Content = new
            {
                login = "mjackson",
                firstName = "Michael",
                lastName = "Jackson"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.ShouldHaveHeader("Content-Type", "application/xml; charset=utf-8");

            var responseContent = response.ReadContentAsXml();
            responseContent.Name.LocalName.Should().Be("guid");
        }

        [Test]
        public async Task Test7_Code406_WhenAcceptTextPlain()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = BuildUsersUri();
            request.Headers.Add("Accept", "text/plain");
            request.Content = new
            {
                login = "mjackson",
                firstName = "Michael",
                lastName = "Jackson"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
            response.ShouldNotHaveHeader("Content-Type");
        }
    }
}