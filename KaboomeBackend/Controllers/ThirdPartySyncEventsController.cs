namespace KaboomeBackend.Controllers
{
    using System;
    using Google;
    using Google.Apis.Auth.OAuth2.Flows;
    using KaboomeBackend.Couch;
    using KaboomeBackend.Options;
    using KaboomeBackend.ThirdPartySyncers;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("/backend/third-party-sync-events")]
    public class ThirdPartySyncEventsController : ControllerBase
    {
        private readonly GoogleAuthorizationCodeFlow flow;
        private readonly AdminCouchClient client;
        private readonly Uri couchDbUri;
        private const string YouCanCloseThisWindowNow = "<h1>You can close this window now</h1><script>window.close()</script>";
        private const string YouMustFirstAuthenticate = "<h1>You must first authenticate.</h1>";
        public ThirdPartySyncEventsController(GoogleAuthorizationCodeFlow flow, AdminCouchClient client, KaboomeOptions kaboome)
        {
            this.flow = flow;
            this.client = client;
            this.couchDbUri = kaboome.CouchDbUri;
        }
        [HttpPost]
        public async Task<ActionResult<string>> Sync()
        {
            var req = this.Request;
            var kaboomeUsername = await AuthHelper.GetAndValidateUsername(req, this.couchDbUri);
            if (kaboomeUsername == null)
            {
                return this.Unauthorized(YouMustFirstAuthenticate);
            }

            var usersecrets = await this.client.GetUserSecrets(kaboomeUsername);
            foreach (var userSecret in usersecrets)
            {
                var userConfig = await this.client.GetUserConfig(kaboomeUsername, userSecret._id);
                var accountType = userSecret._id.Split('-')[0];
                if (accountType == "google")
                {
                    var googleSyncer = new GoogleSyncer(this.flow, this.client, req.Cookies["AuthSession"]);
                    try
                    {
                        await googleSyncer.SyncGoogleAccount(kaboomeUsername, userConfig, userSecret);
                    }
                    catch (GoogleApiException gex)
                    {
                        if (gex.Error.Message == "Rate Limit Exceeded")
                        {
                            return this.Forbid("Rate Limit Exceeded");
                        }
                        throw gex;
                    }
                }
            }
            return this.Ok("OK");
        }
    }
}
