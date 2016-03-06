using System.IO;
using System.Threading.Tasks;
using AuthorizeCore.Response;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AuthorizeCore.Test
{
    public class AuthorizationTests
    {
        private readonly IAuthorizeNetClient _client;
        
        public AuthorizationTests()
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(".", $"config.json"));

            var Configuration = builder.Build();
            _client = new AuthorizeNetClient(Configuration["apiName"], Configuration["apiKey"], false);
        }
        
        [Fact]
        public async Task Authorize_ShouldReturnSuccess()
        {
            var result = await _client.Authorize();
            Assert.IsType<AuthorizationSuccess>(result);
        }
    }
}