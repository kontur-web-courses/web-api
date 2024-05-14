using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class Task3_UpdateUserTests : UsersApiTestsBase
    {
        [Test]
        public async Task Test1_Code204_WhenAllIsFine()
        {
            var createdUserId = await CreateUser(new
            {
                login = "anonymous"
            });

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                login = "Anon",
                firstName = "Vendetta",
                lastName = "V"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.ShouldNotHaveHeader("Content-Type");

            CheckUser(createdUserId, new
            {
                id = createdUserId,
                login = "Anon",
                fullName = "V Vendetta",
                gamesPlayed = 0,
                currentGameId = (string)null
            });
        }

        [Test]
        public async Task Test2_Code201_WhenNoUser()
        {
            var updatingUserId = Guid.NewGuid().ToString();

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = BuildUsersByIdUri(updatingUserId);
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
        public async Task Test3_Code400_WhenUserIdIsTrash()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = BuildUsersByIdUri("trash");
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                login = "Anon",
                firstName = "Vendetta",
                lastName = "V"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test4_Code400_WhenEmptyContent()
        {
            var updatingUserId = Guid.NewGuid().ToString();

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = BuildUsersByIdUri(updatingUserId);
            request.Headers.Add("Accept", "*/*");
            request.AddEmptyContent("application/json; charset=utf-8");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test5_Code422_WhenEmptyLogin()
        {
            var createdUserId = await CreateUser(new
            {
                login = "anonymous"
            });

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                firstName = "Vendetta",
                lastName = "V"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            var responseContent = response.ReadContentAsJson() as JObject;
            responseContent.Should().NotBeNull();
            responseContent.GetValue("login").Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task Test6_Code422_WhenLoginWithUnallowedChars()
        {
            var createdUserId = await CreateUser(new
            {
                login = "anonymous"
            });

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                login = "!Anon!",
                firstName = "Vendetta",
                lastName = "V"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            var responseContent = response.ReadContentAsJson() as JObject;
            responseContent.Should().NotBeNull();
            responseContent.GetValue("login").Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task Test7_Code422_WhenEmptyFirstName()
        {
            var createdUserId = await CreateUser(new
            {
                login = "anonymous"
            });

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                login = "Anon",
                lastName = "V"
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            var responseContent = response.ReadContentAsJson() as JObject;
            responseContent.Should().NotBeNull();
            responseContent.GetValue("firstName").Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task Test8_Code422_WhenEmptyLastName()
        {
            var createdUserId = await CreateUser(new
            {
                login = "anonymous"
            });

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new
            {
                login = "Anon",
                firstName = "Vendetta",
            }.SerializeToJsonContent();
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            var responseContent = response.ReadContentAsJson() as JObject;
            responseContent.Should().NotBeNull();
            responseContent.GetValue("lastName").Should().NotBeNullOrEmpty();
        }
    }
}