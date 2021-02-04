using FluentAssertions;
using System;
using System.Net;
using System.Net.Http;
using System.Web;

namespace Tests
{
    public abstract class UsersApiTestsBase
    {
        protected readonly HttpClient httpClient = new HttpClient();

        protected Uri BuildUsersByIdUri(string userId)
        {
            var uriBuilder = new UriBuilder(Configuration.BaseUrl);
            uriBuilder.Path = $"/api/users/{HttpUtility.UrlEncode(userId)}";
            return uriBuilder.Uri;
        }

        protected Uri BuildUsersUri()
        {
            var uriBuilder = new UriBuilder(Configuration.BaseUrl);
            uriBuilder.Path = $"/api/users";
            return uriBuilder.Uri;
        }

        protected Uri BuildUsersWithPagesUri(int? pageNumber, int? pageSize)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (pageNumber.HasValue)
                query.Add("pageNumber", pageNumber.Value.ToString());
            if (pageSize.HasValue)
                query.Add("pageSize", pageSize.Value.ToString());

            var uriBuilder = new UriBuilder(Configuration.BaseUrl);
            uriBuilder.Path = $"/api/users";
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }

        protected void CheckUserCreated(string createdUserId, string createdUserUri, object expectedUser)
        {
            // Проверка, что идентификатор созданного пользователя возвращается в теле ответа
            CheckUser(createdUserId, expectedUser);

            // Проверка, что ссылка на созданного пользователя возвращается в заголовке Location
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(createdUserUri);
            request.Headers.Add("Accept", "application/json");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            response.ShouldHaveJsonContentEquivalentTo(expectedUser);
        }

        protected void CheckUser(string userId, object expectedUser)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = BuildUsersByIdUri(userId);
            request.Headers.Add("Accept", "application/json");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");
            response.ShouldHaveJsonContentEquivalentTo(expectedUser);
        }

        protected string CreateUser(object user)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = BuildUsersUri();
            request.Headers.Add("Accept", "*/*");
            request.Content = user.SerializeToJsonContent();
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.ShouldHaveHeader("Content-Type", "application/json; charset=utf-8");

            var createdUserId = response.ReadContentAsJson().ToString();
            createdUserId.Should().NotBeNullOrEmpty();
            return createdUserId;
        }

        protected void DeleteUser(string userId)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Delete;
            request.RequestUri = BuildUsersByIdUri(userId);
            request.Headers.Add("Accept", "*/*");
            var response = httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.ShouldNotHaveHeader("Content-Type");
        }
    }
}