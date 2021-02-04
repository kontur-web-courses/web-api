using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http;

namespace Tests
{
    [TestFixture]
    public class Task6_HeadUserByIdTests : UsersApiTestsBase
    {
        [Test]
        public void Test1_Code200_WhenAllIsFine()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Head;
            request.RequestUri = BuildUsersByIdUri("77777777-7777-7777-7777-777777777777");
            request.Headers.Add("Accept", "application/json");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            response.ReadContentAsBytes().Length.Should().Be(0);
        }

        [Test]
        public void Test2_Code404_WhenUserIdIsUnknown()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Head;
            request.RequestUri = BuildUsersByIdUri("77777777-6666-6666-6666-777777777777");
            request.Headers.Add("Accept", "application/json");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }

        [Test]
        public void Test3_Code404_WhenUserIdIsTrash()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Head;
            request.RequestUri = BuildUsersByIdUri("trash");
            request.Headers.Add("Accept", "application/json");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ShouldNotHaveHeader("Content-Type");
        }
    }
}