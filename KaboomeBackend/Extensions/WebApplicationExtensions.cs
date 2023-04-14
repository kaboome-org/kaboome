namespace KaboomeBackend.Extensions
{
    using System.Text.RegularExpressions;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Auth.OAuth2.Flows;
    using Google.Apis.Calendar.v3;
    using Google.Apis.Calendar.v3.Data;
    using Google.Apis.Services;
    using KaboomeBackend.Couch;
    using KaboomeBackend.Models;
    using Newtonsoft.Json;

    public static partial class WebApplicationExtensions
    {
        private const string RegisterTheUserInCouchDB = "/backend/register";

        private const string GoogleShouldSendItsAuthCodeHere = "/backend/google-auth";
        private const string DeleteGoogleAccount = "/backend/delete-google-account";
        private const string RedirectTheUserIntoTheGoogleAuthFlow = "/backend/google-signin";
        private const string GoogleCalendarList = "/backend/google-calendar-list";
        private const string YouCanCloseThisWindowNow = "<h1>You can close this window now</h1><script>window.close()</script>";
        private const string YouMustFirstAuthenticate = "<h1>You must first authenticate.</h1>";

        public static WebApplication UseRegistrationEndpoint(this WebApplication app)
        {
            _ = app.MapPost(RegisterTheUserInCouchDB, async (AdminCouchClient client, string username, string password) =>
            {
                if (!UsernameRegex().IsMatch(username))
                {
                    return "The username must start with a letter, and can only contain letters, numbers and underscore";
                }
                await client.CreateUserAndDatabases(username, password);
                return YouCanCloseThisWindowNow;
            });
            return app;
        }
        public static WebApplication UseThirdPartyEndpoints(this WebApplication app, Uri couchDbUri)
        {
            app.UseSyncEndpoint(couchDbUri);
            _ = app.MapGet(RedirectTheUserIntoTheGoogleAuthFlow, async (HttpRequest req, HttpResponse res, GoogleAuthorizationCodeFlow flow) =>
            {
                var kaboomeUsername = await AuthHelper.GetAndValidateUsername(req, couchDbUri);
                if (kaboomeUsername == null)
                {
                    return YouMustFirstAuthenticate;
                }
                var redirectUri = BuildRedirectUri(req);
                res.Redirect($"https://accounts.google.com/o/oauth2/v2/auth/oauthchooseaccount?redirect_uri=" +
                    $"{redirectUri}&prompt=consent&response_type=code&client_id={flow.ClientSecrets.ClientId}&scope={string.Join(" ", flow.Scopes)}" +
                    $"&access_type=offline&service=lso&o2v=2&flowName=GeneralOAuthFlow");
                return "";
            });
            _ = app.MapGet(GoogleShouldSendItsAuthCodeHere, async (HttpRequest req, HttpResponse res, GoogleAuthorizationCodeFlow flow, AdminCouchClient client) =>
            {
                var kaboomeUsername = await AuthHelper.GetAndValidateUsername(req, couchDbUri);
                if (kaboomeUsername == null)
                {
                    return YouMustFirstAuthenticate;
                }
                var redirectUri = BuildRedirectUri(req);
                var tokres = await flow.ExchangeCodeForTokenAsync("user", req.Query["code"], redirectUri, CancellationToken.None);
                var initializer = new BaseClientService.Initializer
                {
                    HttpClientInitializer = new UserCredential(flow,
                    "user", new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                    {
                        RefreshToken = tokres.RefreshToken
                    })
                };
                var service = new CalendarService(initializer);
                var accountName = service.CalendarList.List().Execute().Items[0].Id;
                await client.AddUserSecret(kaboomeUsername, $"google-{accountName}", JsonConvert.SerializeObject(new UserSecret { RefreshToken = tokres.RefreshToken }));
                await client.WriteUserConfig(kaboomeUsername, $"google-{accountName}", JsonConvert.SerializeObject(new UserConfigDocIn { Since = null, GoogleSyncToken = null }));

                res.ContentType = "text/html";
                return YouCanCloseThisWindowNow;
            });
            _ = app.MapGet(GoogleCalendarList, async (HttpRequest req, GoogleAuthorizationCodeFlow flow, AdminCouchClient client) =>
            {
                var kaboomeUsername = await AuthHelper.GetAndValidateUsername(req, couchDbUri);
                if (kaboomeUsername == null)
                {
                    return YouMustFirstAuthenticate;
                }
                var usersecrets = await client.GetUserSecrets(kaboomeUsername, "google-");
                var ret = new Dictionary<string, IList<CalendarListEntry>>();
                foreach (var userSecret in usersecrets.Select(u => JsonConvert.DeserializeObject<UserSecret>(u)))
                {
                    var initializer = new BaseClientService.Initializer
                    {
                        HttpClientInitializer = new UserCredential(flow,
                    "user", new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                    {
                        RefreshToken = userSecret.RefreshToken
                    })
                    };
                    var service = new CalendarService(initializer);
                    var calendars = (await service.CalendarList.List().ExecuteAsync()).Items;
                    ret.Add(calendars.FirstOrDefault(c => c.Primary == true, calendars[0]).Id, calendars);
                }
                return JsonConvert.SerializeObject(ret);
            });
            return app;
        }

        private static string BuildRedirectUri(HttpRequest req)
            => $"{req.Headers["X-Forwarded-Proto"]}://{req.Headers["X-Forwarded-Host"]}:{req.Headers["X-Forwarded-Port"]}{GoogleShouldSendItsAuthCodeHere}";

        [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]+$")]
        private static partial Regex UsernameRegex();
    }
}


