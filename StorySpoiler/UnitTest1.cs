using NUnit.Compatibility;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;


namespace Story_Spoiler
{
    [TestFixture]
    public class StorySpoilerTests
    {
        private RestClient _client;
        private static string createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("vasido", "Test12345!");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            _client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]

        public void CreateStory_ShouldReturnCreated()
        {
            var story = new
            {
                Title = "New Story",
                Description = "Magical Story",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdStoryId = json.GetProperty("storyId").GetString() ?? string.Empty;

            Assert.That(createdStoryId, Is.Not.Null.And.Not.Empty, "StoryId should be returned in the response.");

            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully created!"));
        }

        [Test, Order(2)]

        public void EditStory_ShouldReturnOk()
        {
            var editedStory = new
            {
                Title = "Edited Story",
                Description = "Even More Magical Story",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(editedStory);

            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]

        public void GetAllStories_ShouldReturnNonEmptyArray()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);

            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var storyspoiler = JsonSerializer.Deserialize<JsonElement>(response.Content);

            Assert.That(storyspoiler.GetArrayLength(), Is.GreaterThan(0), "Story array should not be empty.");
        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);

            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var storyspoiler = JsonSerializer.Deserialize<JsonElement>(response.Content);

            Assert.That(storyspoiler.GetProperty("msg").GetString(), Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]

        public void CreateStory_MissingRequiredFields_ShouldReturnBadRequest()
        {
            var incompleteStory = new
            {
                Title = "",
                Description = "",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(incompleteStory);

            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]

        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            string fakeStoryId = "01234";

            var editedStory = new
            {
                Title = "Edited Story",
                Description = "Trying to edit non-existing story",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{fakeStoryId}", Method.Put);
            request.AddJsonBody(editedStory);

            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            var storyspoiler = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(storyspoiler.GetProperty("msg").GetString(), Is.EqualTo("No spoilers..."));
        }

        [Test, Order(7)]

        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            string fakeStoryId = "01234";

            var request = new RestRequest($"/api/Story/Delete/{fakeStoryId}", Method.Delete);

            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var storyspoiler = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(storyspoiler.GetProperty("msg").GetString(), Is.EqualTo("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _client?.Dispose();
        }
    }
}