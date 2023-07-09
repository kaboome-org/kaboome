namespace KaboomeBackend.Extensions
{
    using System.Text.RegularExpressions;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Auth.OAuth2.Flows;
    using Google.Apis.Calendar.v3;
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
                var calendarList = service.CalendarList.List().Execute().Items;
                var accountName = calendarList.FirstOrDefault(c => c.Primary == true, calendarList[0]).Id;
                await client.AddUserSecret(kaboomeUsername, $"google-{accountName}", new UserSecretDocIn { GoogleRefreshToken = tokres.RefreshToken });
                await client.WriteUserConfig(kaboomeUsername, $"google-{accountName}", new UserConfigDocIn
                {
                    GoogleCalendarConfigs =
                    calendarList.Select(c => new GoogleCalendarConfig
                    {
                        GoogleCalendarPath = new GoogleCalendarPath
                        {
                            GoogleAccountId = accountName,
                            GoogleCalendarId = c.Id
                        },
                        GoogleSyncToken = null,
                        ShouldSync = false,
                    }).ToList()
                });

                res.ContentType = "text/html";
                return YouCanCloseThisWindowNow;
            });
            return app;
        }

        private static string BuildRedirectUri(HttpRequest req)
            => $"{req.Headers["X-Forwarded-Proto"]}://{req.Headers["X-Forwarded-Host"]}:{req.Headers["X-Forwarded-Port"]}{GoogleShouldSendItsAuthCodeHere}";

        [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]+$")]
        private static partial Regex UsernameRegex();
    }
}


