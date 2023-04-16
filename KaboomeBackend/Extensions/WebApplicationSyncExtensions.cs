namespace KaboomeBackend.Extensions
{
    using Google.Apis.Auth.OAuth2.Flows;
    using KaboomeBackend.Couch;
    using KaboomeBackend.ThirdPartySyncers;

    public static partial class WebApplicationExtensions
    {
        private const string ThirdPartySyncEvents = "/backend/third-party-sync-events";
        public static WebApplication UseSyncEndpoint(this WebApplication app, Uri couchDbUri)
        {
            _ = app.MapPost(ThirdPartySyncEvents, async (HttpRequest req, GoogleAuthorizationCodeFlow flow, AdminCouchClient client) =>
            {
                var kaboomeUsername = await AuthHelper.GetAndValidateUsername(req, couchDbUri);
                if (kaboomeUsername == null)
                {
                    return YouMustFirstAuthenticate;
                }

                var usersecrets = await client.GetUserSecrets(kaboomeUsername);
                foreach (var userSecret in usersecrets)
                {
                    var userConfig = await client.GetUserConfig(kaboomeUsername, userSecret._id);
                    var accountType = userSecret._id.Split('-')[0];
                    if (accountType == "google")
                    {
                        var googleSyncer = new GoogleSyncer(flow, client, req.Cookies["AuthSession"]);
                        await googleSyncer.SyncGoogleAccount(kaboomeUsername, userConfig, userSecret);
                    }
                }
                return "OK";
            });
            return app;
        }
    }
}
