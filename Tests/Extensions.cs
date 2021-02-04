using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace Tests
{
    public static class Extensions
    {
        public static HttpResponseMessage Send(this HttpClient httpClient, HttpRequestMessage request)
        {
            return httpClient.SendAsync(request).Result;
        }

        public static string[] GetRequiredHeader(this HttpResponseMessage response, string headerName)
        {
            var hasResponseHeader = response.Headers.TryGetValues(headerName, out var responseHeaderValues);
            var hasContentHeader = response.Content.Headers.TryGetValues(headerName, out var contentHeaderValues);

            if (hasResponseHeader && hasContentHeader)
                Assert.Fail($"Should have only one '{headerName}' header");

            if (hasResponseHeader)
            {
                return responseHeaderValues.ToArray();
            }
            else if (hasContentHeader)
            {
                return contentHeaderValues.ToArray();
            }
            Assert.Fail($"Should have '{headerName}' header");
            return null;
        }

        public static void ShouldHaveHeader(this HttpResponseMessage response, string headerName, string headerValue)
        {
            var actualHeaderValue = GetRequiredHeader(response, headerName);
            actualHeaderValue.Should().BeEquivalentTo(headerValue);
        }

        public static void ShouldNotHaveHeader(this HttpResponseMessage response, string headerName)
        {
            var hasResponseHeader = response.Headers.TryGetValues(headerName, out var _);
            var hasContentHeader = response.Content.Headers.TryGetValues(headerName, out var _);
            var hasHeader = hasResponseHeader || hasContentHeader;

            hasHeader.Should().BeFalse();
        }

        public static byte[] ReadContentAsBytes(this HttpResponseMessage response)
        {
            var content = response.Content.ReadAsByteArrayAsync().Result;
            return content;
        }

        public static JToken ReadContentAsJson(this HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            return JToken.Parse(content);
        }

        public static void ShouldHaveJsonContentEquivalentTo(this HttpResponseMessage response, object expected)
        {
            var content = response.ReadContentAsJson();
            content.Should().BeEquivalentTo(JToken.FromObject(expected));
        }

        public static XElement ReadContentAsXml(this HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            return XElement.Parse(content);
        }

        public static IEnumerable<string> ValuesOfElements(this XElement xElement, string localName)
        {
            return xElement.Elements()
                .Where(e => e.Name.LocalName == localName)
                .Select(e => e.Value);
        }

        public static void ShouldHaveXmlContentEquivalentTo(this HttpResponseMessage response, string expectedXml)
        {
            var content = response.ReadContentAsXml();
            var expected = XElement.Parse(expectedXml);

            XNode.DeepEquals(content, expected).Should().BeTrue();
        }

        public static ByteArrayContent SerializeToJsonContent(this object obj,
            string contentType = "application/json")
        {
            string json = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return content;
        }

        public static void AddEmptyContent(this HttpRequestMessage request, string contentType)
        {
            request.Content = new ByteArrayContent(new byte[0]);
            request.Content.Headers.Add("Content-Type", contentType);
        }
    }
}