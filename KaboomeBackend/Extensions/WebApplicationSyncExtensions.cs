namespace KaboomeBackend.Extensions
{
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Auth.OAuth2.Flows;
    using Google.Apis.Calendar.v3;
    using Google.Apis.Calendar.v3.Data;
    using Google.Apis.Services;
    using KaboomeBackend.Couch;
    using KaboomeBackend.Models;
    using Microsoft.AspNetCore.DataProtection.KeyManagement;
    using Newtonsoft.Json;

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
                        await SyncGoogleAccount(kaboomeUsername,userConfig,userSecret,flow,client);
                    }
                }
                return "OK";
            });
            return app;
        }
        private static async Task SyncGoogleAccount(string kaboomeUsername, UserConfigDoc userConfig, UserSecretDoc userSecret, GoogleAuthorizationCodeFlow flow, AdminCouchClient client)
        {
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = new UserCredential(flow,
                       "user", new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                       {
                           RefreshToken = userSecret.GoogleRefreshToken
                       })
            };
            var service = new CalendarService(initializer);
            foreach (var googleCalendarConfig in (userConfig.GoogleCalendarConfigs ?? new()).Where(gcc => gcc.ShouldSync))
            {
                //PUSH
                foreach (var kaboomeEvent in await client.GetEventChanges(kaboomeUsername, googleCalendarConfig.Since))
                {
                    await PushEventToGoogle(kaboomeUsername, client, service, googleCalendarConfig, kaboomeEvent);
                }
                //PULL
                var listRequest = service.Events.List(googleCalendarConfig.GoogleCalendarPath.GoogleCalendarId);
                listRequest.SyncToken = googleCalendarConfig.GoogleSyncToken;
                var syncToken = googleCalendarConfig.GoogleSyncToken;
                listRequest.ShowDeleted = true;
                do
                {
                    var events = await listRequest.ExecuteAsync();
                    foreach (var googleEvent in events.Items)
                    {
                        await PullEventFromGoogle(kaboomeUsername, client, googleEvent);
                    }
                    listRequest.PageToken = events.NextPageToken;
                    syncToken = events.NextSyncToken;
                } while (listRequest.PageToken != null);

                googleCalendarConfig.Since = await client.GetLatestSeqNumberOfEventDb(kaboomeUsername);
                googleCalendarConfig.GoogleSyncToken = syncToken;
                await client.WriteUserConfig(kaboomeUsername, userConfig._id, userConfig);
            }
        }

        private static async Task PullEventFromGoogle(string kaboomeUsername, AdminCouchClient client, Event? googleEvent)
        {
            if (googleEvent.Status == "cancelled")
            {
                // Delete
                var toDelete = await client.GetKaboomeEvent(kaboomeUsername, $"google-{googleEvent.Id}");
                if (toDelete != null)
                {
                    await client.DeleteKaboomeEvent(kaboomeUsername, toDelete._id, toDelete._rev);
                }
            }
            else
            {
                var toUpdateDoc = await client.GetKaboomeEvent(kaboomeUsername, $"google-{googleEvent.Id}");
                if (toUpdateDoc == null)
                {
                    // Insert
                    var kaboomeEvent = new KaboomeEventIn
                    {
                        Description = googleEvent.Description,
                        Title = googleEvent.Summary,
                        StartTimestamp = EventDateTimeToTimestamp(googleEvent.Start),
                        EndTimestamp = EventDateTimeToTimestamp(googleEvent.End),
                    };
                    kaboomeEvent.ReadWriteExternalEvent.Google = googleEvent;
                    await client.WriteKaboomeEvent(kaboomeUsername, $"google-{googleEvent.Id}", kaboomeEvent);
                }
                else
                {
                    // Update
                    var kaboomeEvent = toUpdateDoc;
                    kaboomeEvent.Description = googleEvent.Description;
                    kaboomeEvent.Title = googleEvent.Summary;
                    kaboomeEvent.StartTimestamp = EventDateTimeToTimestamp(googleEvent.Start);
                    kaboomeEvent.EndTimestamp = EventDateTimeToTimestamp(googleEvent.End);
                    kaboomeEvent.ReadWriteExternalEvent.Google = googleEvent;
                    await client.WriteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, kaboomeEvent);
                }
            }
        }

        private static async Task PushEventToGoogle(string kaboomeUsername, AdminCouchClient client, CalendarService service, GoogleCalendarConfig googleCalendarConfig, KaboomeEvent? kaboomeEvent)
        {
            if (kaboomeEvent?.ReadWriteExternalEvent?.GoogleCalendarPath == googleCalendarConfig.GoogleCalendarPath)
            {
                var googleEventId = kaboomeEvent._id["google-".Length..];
                var google = kaboomeEvent.ReadWriteExternalEvent.Google;
                if (kaboomeEvent._deleted == true)
                {
                    // Delete
                    await service.Events.Delete(googleCalendarConfig.GoogleCalendarPath.GoogleCalendarId, googleEventId).ExecuteAsync();

                }
                else if (google?.Id != null)
                {
                    // Update
                    google.Summary = kaboomeEvent.Title;
                    google.Description = kaboomeEvent.Description;
                    google.Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp);
                    google.End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp);
                    google.CreatedRaw = google.CreatedRaw.Replace("Z", ".000Z");
                    await service.Events.Update(google, googleCalendarConfig.GoogleCalendarPath.GoogleCalendarId, google.Id).ExecuteAsync();
                }
                else
                {
                    // Insert
                    google.Summary = kaboomeEvent.Title;
                    google.Description = kaboomeEvent.Description;
                    google.Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp);
                    google.End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp);
                    await service.Events.Insert(google, googleCalendarConfig.GoogleCalendarPath.GoogleCalendarId).ExecuteAsync();
                    // Will be added by pull sync again (with a correct event id generated by google)
                    await client.DeleteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, kaboomeEvent._rev);
                }
            }
        }

        private static long EventDateTimeToTimestamp(EventDateTime eventDateTime) =>
            DateTimeOffset.Parse(eventDateTime.DateTimeRaw ?? eventDateTime.Date).ToUnixTimeMilliseconds();
        private static EventDateTime TimestampToEventDateTime(long timestamp) => new EventDateTime
        {
            DateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime,
            TimeZone = "UTC"
        };
    }
}
