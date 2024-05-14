using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tests
{

    [TestFixture]
    public class Task4_PartiallyUpdateUserTests : UsersApiTestsBase
    {
        [Test]
        public async Task Test1_Code204_WhenAllIsFine()
        {
            var createdUserId = await CreateUser(new
            {
                login = "anonymous"
            });

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Patch;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new object[] {
                new {
                    op = "replace",
                    path = "login",
                    value = "Anon"
                },
                new {
                    op = "replace",
                    path = "firstName",
                    value = "Vendetta"
                },
                new {
                    op = "replace",
                    path = "lastName",
                    value = "V"
                }
            }.SerializeToJsonContent("application/json-patch+json");
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
        public async Task Test2_Code404_WhenNoUser()
        {
            var updatingUserId = Guid.NewGuid().ToString();

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Patch;
            request.RequestUri = BuildUsersByIdUri(updatingUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new object[] {
                new {
                    op = "replace",
                    path = "login",
                    value = "Anon"
                },
                new {
                    op = "replace",
                    path = "firstName",
                    value = "Vendetta"
                },
                new {
                    op = "replace",
                    path = "lastName",
                    value = "V"
                }
            }.SerializeToJsonContent("application/json-patch+json");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test3_Code404_WhenUserIdIsTrash()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Patch;
            request.RequestUri = BuildUsersByIdUri("trash");
            request.Headers.Add("Accept", "*/*");
            request.Content = new object[] {
                new {
                    op = "replace",
                    path = "login",
                    value = "Anon"
                },
                new {
                    op = "replace",
                    path = "firstName",
                    value = "Vendetta"
                },
                new {
                    op = "replace",
                    path = "lastName",
                    value = "V"
                }
            }.SerializeToJsonContent("application/json-patch+json");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public async Task Test4_Code400_WhenEmptyContent()
        {
            var updatingUserId = Guid.NewGuid().ToString();

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Patch;
            request.RequestUri = BuildUsersByIdUri(updatingUserId);
            request.Headers.Add("Accept", "*/*");
            request.AddEmptyContent("application/json-patch+json");
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
            request.Method = HttpMethod.Patch;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new object[] {
                new {
                    op = "replace",
                    path = "login",
                    value = ""
                }
            }.SerializeToJsonContent("application/json-patch+json");
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
            request.Method = HttpMethod.Patch;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new object[] {
                new {
                    op = "replace",
                    path = "login",
                    value = "!Anon!"
                }
            }.SerializeToJsonContent("application/json-patch+json");
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
            request.Method = HttpMethod.Patch;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new object[] {
                new {
                    op = "replace",
                    path = "firstName",
                    value = ""
                }
            }.SerializeToJsonContent("application/json-patch+json");
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
            request.Method = HttpMethod.Patch;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            request.Content = new object[] {
                new {
                    op = "replace",
                    path = "lastName",
                    value = ""
                }
            }.SerializeToJsonContent("application/json-patch+json");
            var response = await HttpClient.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            var responseContent = response.ReadContentAsJson() as JObject;
            responseContent.Should().NotBeNull();
            responseContent.GetValue("lastName").Should().NotBeNullOrEmpty();
        }
    }
}