using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;

namespace Tests
{

    [TestFixture]
    public class Task5_DeleteUserTests : UsersApiTestsBase
    {
        [Test]
        public void Test1_Code204_WhenAllIsFine()
        {
            var createdUserId = CreateUser(new
            {
                login = "condenado",
                firstName = "a muerte",
                lastName = "Condenado"
            });

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Delete;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public void Test2_Code404_WhenUserIsUnknown()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Delete;
            request.RequestUri = BuildUsersByIdUri("77777777-6666-6666-6666-777777777777");
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public void Test3_Code404_WhenUserIdIsTrash()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Delete;
            request.RequestUri = BuildUsersByIdUri("trash");
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public void Test4_Code404_WhenAlreadyCreatedAndDeleted()
        {
            var createdUserId = CreateUser(new
            {
                login = "condenado",
                firstName = "a muerte",
                lastName = "Condenado"
            });

            DeleteUser(createdUserId);

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Delete;
            request.RequestUri = BuildUsersByIdUri(createdUserId);
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }
    }
}