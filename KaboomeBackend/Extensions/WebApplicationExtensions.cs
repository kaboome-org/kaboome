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
        private const string GoogleSyncEvents = "/backend/google-sync-events";
        private const string YouCanCloseThisWindowNow = "<h1>You can close this window now</h1><script>window.close()</script>";
        private const string YouMustFirstAuthenticate = "<h1>You must first authenticate.</h1>";

        private static long EventDateTimeToTimestamp(EventDateTime eventDateTime) =>
            DateTimeOffset.Parse(eventDateTime.DateTimeRaw ?? eventDateTime.Date).ToUnixTimeMilliseconds();
        private static EventDateTime TimestampToEventDateTime(long timestamp) => new EventDateTime
        {
            DateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime,
            TimeZone = "UTC"
        };

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
        public static WebApplication UseGoogleAuthEndpoints(this WebApplication app, Uri couchDbUri)
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
                    ret.Add(calendars[0].Id, calendars);
                }
                return JsonConvert.SerializeObject(ret);
            });
            _ = app.MapPost(GoogleSyncEvents, async (HttpRequest req, GoogleAuthorizationCodeFlow flow, AdminCouchClient client, GoogleCalendarPath googleCalendarPath) =>
            {
                var kaboomeUsername = await AuthHelper.GetAndValidateUsername(req, couchDbUri);
                if (kaboomeUsername == null)
                {
                    return YouMustFirstAuthenticate;
                }

                var key = $"google-{googleCalendarPath.GoogleAccountId}";
                var usersecrets = await client.GetUserSecrets(kaboomeUsername, key);
                var userSecret = JsonConvert.DeserializeObject<UserSecret>(usersecrets.First());
                var userConfigDoc = await client.GetUserConfig(kaboomeUsername, key);
                var userConfig = JsonConvert.DeserializeObject<UserConfigDoc>(userConfigDoc);

                var initializer = new BaseClientService.Initializer
                {
                    HttpClientInitializer = new UserCredential(flow,
                "user", new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                {
                    RefreshToken = userSecret.RefreshToken
                })
                };
                var service = new CalendarService(initializer);
                var idPrefix = $"{JsonConvert.SerializeObject(googleCalendarPath)}-";
                //PUSH
                foreach (var changeDoc in await client.GetEventChanges(kaboomeUsername, userConfig.Since))
                {
                    // Deserialize
                    var kaboomeEvent = JsonConvert.DeserializeObject<KaboomeEvent<Event>>(changeDoc);
                    if (kaboomeEvent._id.StartsWith(idPrefix, StringComparison.InvariantCulture))
                    {
                        var googleEventId = kaboomeEvent._id.Substring(idPrefix.Length);
                        if (kaboomeEvent._deleted == true)
                        {
                            // Delete
                            await service.Events.Delete(googleCalendarPath.GoogleCalendarId, googleEventId).ExecuteAsync();

                        }
                        else if(kaboomeEvent.innerData?.Id != null)
                        {
                            // Update
                            kaboomeEvent.innerData.Summary = kaboomeEvent.Title;
                            kaboomeEvent.innerData.Description = kaboomeEvent.Description;
                            kaboomeEvent.innerData.Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp);
                            kaboomeEvent.innerData.End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp);
                            kaboomeEvent.innerData.CreatedRaw = kaboomeEvent.innerData.CreatedRaw.Replace("Z", ".000Z");
                            await service.Events.Update(kaboomeEvent.innerData, googleCalendarPath.GoogleCalendarId, kaboomeEvent.innerData.Id).ExecuteAsync();
                        }
                        else
                        {
                            // Insert
                            kaboomeEvent.innerData.Summary = kaboomeEvent.Title;
                            kaboomeEvent.innerData.Description = kaboomeEvent.Description;
                            kaboomeEvent.innerData.Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp);
                            kaboomeEvent.innerData.End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp);
                            await service.Events.Insert(kaboomeEvent.innerData, googleCalendarPath.GoogleCalendarId).ExecuteAsync();
                            // Will be added by pull sync again (with a correct event id generated by google)
                            await client.DeleteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, kaboomeEvent._rev);
                        }
                    }
                }
                //PULL
                var listRequest = service.Events.List(googleCalendarPath.GoogleCalendarId);
                listRequest.SyncToken = userConfig.GoogleSyncToken;
                var syncToken = userConfig.GoogleSyncToken;
                listRequest.ShowDeleted = true;
                do
                {
                    var events = await listRequest.ExecuteAsync();
                    foreach (var googleEvent in events.Items)
                    {
                        if (googleEvent.Status == "cancelled")
                        {
                            // Delete
                            var toDeleteDoc = await client.GetKaboomeEvent(kaboomeUsername, $"{idPrefix}{googleEvent.Id}");
                            if (toDeleteDoc != null)
                            {
                                var toDelete = JsonConvert.DeserializeObject<KaboomeEvent<Event>>(toDeleteDoc);
                                await client.DeleteKaboomeEvent(kaboomeUsername, toDelete._id, toDelete._rev);
                            }
                        }
                        else
                        {
                            var toUpdateDoc = await client.GetKaboomeEvent(kaboomeUsername, $"{idPrefix}{googleEvent.Id}");
                            if (toUpdateDoc == null)
                            {
                                // Insert
                                var kaboomeEvent = new KaboomeEventIn<Event>
                                {
                                    Description = googleEvent.Description,
                                    Title = googleEvent.Summary,
                                    StartTimestamp = EventDateTimeToTimestamp(googleEvent.Start),
                                    EndTimestamp = EventDateTimeToTimestamp(googleEvent.End),
                                    innerData = googleEvent
                                };
                                await client.WriteKaboomeEvent(kaboomeUsername, $"{idPrefix}{googleEvent.Id}", JsonConvert.SerializeObject(kaboomeEvent));
                            }
                            else
                            {
                                // Update
                                var kaboomeEvent = JsonConvert.DeserializeObject<KaboomeEvent<Event>>(toUpdateDoc);
                                kaboomeEvent.Description = googleEvent.Description;
                                kaboomeEvent.Title = googleEvent.Summary;
                                kaboomeEvent.StartTimestamp = EventDateTimeToTimestamp(googleEvent.Start);
                                kaboomeEvent.EndTimestamp = EventDateTimeToTimestamp(googleEvent.End);
                                kaboomeEvent.innerData = googleEvent;
                                await client.WriteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, JsonConvert.SerializeObject(kaboomeEvent));
                            }
                        }
                    }
                    listRequest.PageToken = events.NextPageToken;
                    syncToken = events.NextSyncToken;
                } while (listRequest.PageToken != null);

                userConfig.Since = await client.GetLatestSeqNumberOfEventDb(kaboomeUsername);
                userConfig.GoogleSyncToken = syncToken;
                var configDoc = JsonConvert.SerializeObject(userConfig);
                await client.WriteUserConfig(kaboomeUsername, key, configDoc);

                return configDoc;
            });
            return app;
        }

        private static string BuildRedirectUri(HttpRequest req)
            => $"{req.Headers["X-Forwarded-Proto"]}://{req.Headers["X-Forwarded-Host"]}:{req.Headers["X-Forwarded-Port"]}{GoogleShouldSendItsAuthCodeHere}";

        [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]+$")]
        private static partial Regex UsernameRegex();
    }
}


