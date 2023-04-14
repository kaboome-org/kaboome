namespace KaboomeBackend.Extensions
{
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
        private const string GoogleSyncEvents = "/backend/google-sync-events";
        public static WebApplication UseSyncEndpoint(this WebApplication app, Uri couchDbUri)
        {
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
                    var kaboomeEvent = JsonConvert.DeserializeObject<KaboomeEvent>(changeDoc);
                    if (kaboomeEvent._id.StartsWith(idPrefix, StringComparison.InvariantCulture))
                    {
                        var googleEventId = kaboomeEvent._id.Substring(idPrefix.Length);
                        var google = kaboomeEvent.ReadWriteExternalEvent.Google;
                        if (kaboomeEvent._deleted == true)
                        {
                            // Delete
                            await service.Events.Delete(googleCalendarPath.GoogleCalendarId, googleEventId).ExecuteAsync();

                        }
                        else if (google?.Id != null)
                        {
                            // Update
                            google.Summary = kaboomeEvent.Title;
                            google.Description = kaboomeEvent.Description;
                            google.Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp);
                            google.End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp);
                            google.CreatedRaw = google.CreatedRaw.Replace("Z", ".000Z");
                            await service.Events.Update(google, googleCalendarPath.GoogleCalendarId, google.Id).ExecuteAsync();
                        }
                        else
                        {
                            // Insert
                            google.Summary = kaboomeEvent.Title;
                            google.Description = kaboomeEvent.Description;
                            google.Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp);
                            google.End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp);
                            await service.Events.Insert(google, googleCalendarPath.GoogleCalendarId).ExecuteAsync();
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
                                var toDelete = JsonConvert.DeserializeObject<KaboomeEvent>(toDeleteDoc);
                                await client.DeleteKaboomeEvent(kaboomeUsername, toDelete._id, toDelete._rev);
                            }
                        }
                        else
                        {
                            var toUpdateDoc = await client.GetKaboomeEvent(kaboomeUsername, $"{idPrefix}{googleEvent.Id}");
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
                                await client.WriteKaboomeEvent(kaboomeUsername, $"{idPrefix}{googleEvent.Id}", JsonConvert.SerializeObject(kaboomeEvent));
                            }
                            else
                            {
                                // Update
                                var kaboomeEvent = JsonConvert.DeserializeObject<KaboomeEvent>(toUpdateDoc);
                                kaboomeEvent.Description = googleEvent.Description;
                                kaboomeEvent.Title = googleEvent.Summary;
                                kaboomeEvent.StartTimestamp = EventDateTimeToTimestamp(googleEvent.Start);
                                kaboomeEvent.EndTimestamp = EventDateTimeToTimestamp(googleEvent.End);
                                kaboomeEvent.ReadWriteExternalEvent.Google = googleEvent;
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

        private static long EventDateTimeToTimestamp(EventDateTime eventDateTime) =>
            DateTimeOffset.Parse(eventDateTime.DateTimeRaw ?? eventDateTime.Date).ToUnixTimeMilliseconds();
        private static EventDateTime TimestampToEventDateTime(long timestamp) => new EventDateTime
        {
            DateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime,
            TimeZone = "UTC"
        };
    }
}
