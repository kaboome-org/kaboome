using Google.Apis.Auth.OAuth2.Flows;
using KaboomeBackend.Couch;
using KaboomeBackend.Extensions;
using KaboomeBackend.Options;

var builder = WebApplication.CreateBuilder(args);
var section = builder.Configuration.GetSection(nameof(KaboomeOptions));
var kaboomeOptions = section.Get<KaboomeOptions>();
if (kaboomeOptions == null)
{
    throw new Exception("Missing Kaboome Options in Configuration");
}
// Add services to the container.
builder.Services.AddSingleton((_) => new AdminCouchClient(kaboomeOptions));
builder.Services.AddSingleton(
    (_) => new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecretsStream = new FileStream("client_secret.json", FileMode.Open),
        Scopes = new List<string> { "https://www.googleapis.com/auth/calendar" }
    }));

var app = builder.Build();

app.UseRegistrationEndpoint();
app.UseGoogleAuthEndpoints(kaboomeOptions.CouchDbUri);

app.UseHttpsRedirection();

app.Run();
