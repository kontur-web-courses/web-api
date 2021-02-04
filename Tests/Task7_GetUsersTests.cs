using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Tests
{
    [TestFixture]
    public class Task7_GetUsersTests : UsersApiTestsBase
    {
        [Test]
        public void Test1_Code200_WhenFirstPage()
        {
            CreateUniqueUsers(20);

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersWithPagesUri(null, null);
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");

            var paginationString = response.GetRequiredHeader("X-Pagination").SingleOrDefault();
            var pagination = JsonConvert.DeserializeObject<Pagination>(paginationString);

            pagination.PreviousPageLink.Should().BeNull();
            pagination.NextPageLink.Should().NotBeNull();
            pagination.TotalCount.Should().BeGreaterThan(0);
            pagination.PageSize.Should().Be(10);
            pagination.CurrentPage.Should().Be(1);
            pagination.TotalPages.Should().BeGreaterThan(0);
        }

        [Test]
        public void Test2_Code200_WhenSecondPage()
        {
            CreateUniqueUsers(30);

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersWithPagesUri(2, 10);
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");

            var paginationString = response.GetRequiredHeader("X-Pagination").SingleOrDefault();
            var pagination = JsonConvert.DeserializeObject<Pagination>(paginationString);

            pagination.PreviousPageLink.Should().NotBeNull();
            pagination.NextPageLink.Should().NotBeNull();
            pagination.TotalCount.Should().BeGreaterThan(0);
            pagination.PageSize.Should().Be(10);
            pagination.CurrentPage.Should().Be(2);
            pagination.TotalPages.Should().BeGreaterThan(0);
        }

        [Test]
        public void Test3_Code200_WhenPageNumberMinIs1()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersWithPagesUri(0, null);
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");

            var paginationString = response.GetRequiredHeader("X-Pagination").SingleOrDefault();
            var pagination = JsonConvert.DeserializeObject<Pagination>(paginationString);

            pagination.CurrentPage.Should().Be(1);
        }

        [Test]
        public void Test4_Code200_WhenPageSizeMinIs1()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersWithPagesUri(null, 0);
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");

            var paginationString = response.GetRequiredHeader("X-Pagination").SingleOrDefault();
            var pagination = JsonConvert.DeserializeObject<Pagination>(paginationString);

            pagination.PageSize.Should().Be(1);
        }

        [Test]
        public void Test5_Code200_WhenPageSizeMaxIs20()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersWithPagesUri(null, 100);
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");

            var paginationString = response.GetRequiredHeader("X-Pagination").SingleOrDefault();
            var pagination = JsonConvert.DeserializeObject<Pagination>(paginationString);

            pagination.PageSize.Should().Be(20);
        }

        [Test]
        public void Test6_Code200_WhenPageSizeIs1()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersWithPagesUri(null, 1);
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");

            var paginationString = response.GetRequiredHeader("X-Pagination").SingleOrDefault();
            var pagination = JsonConvert.DeserializeObject<Pagination>(paginationString);

            pagination.TotalCount.Should().BeGreaterThan(0);
            pagination.PageSize.Should().Be(1);
            pagination.TotalPages.Should().Be(pagination.TotalCount);
        }

        private void CreateUniqueUsers(int count)
        {
            for (var i = 0; i < count; i++)
            {
                CreateUser(new
                {
                    login = Guid.NewGuid().ToString().Replace("-", "")
                });
            }
        }

        private class Pagination
        {
            public string PreviousPageLink { get; set; }
            public string NextPageLink { get; set; }
            public int? TotalCount { get; set; }
            public int? PageSize { get; set; }
            public int? CurrentPage { get; set; }
            public int? TotalPages { get; set; }
        }
    }
}