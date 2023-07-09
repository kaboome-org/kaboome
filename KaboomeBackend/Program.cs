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
builder.Services.AddSingleton((_) => kaboomeOptions);
// Add services to the container.
builder.Services.AddSingleton((_) => new AdminCouchClient(kaboomeOptions));
builder.Services.AddSingleton(
    (_) => new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecretsStream = new FileStream("client_secret.json", FileMode.Open),
        Scopes = new List<string> { "https://www.googleapis.com/auth/calendar" }
    }));
builder.Services.AddControllers();

var app = builder.Build();

app.UseRegistrationEndpoint();
app.UseThirdPartyEndpoints(kaboomeOptions.CouchDbUri);
app.MapControllers();
app.UseHttpsRedirection();

app.Run();
