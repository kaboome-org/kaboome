namespace KaboomeBackend.ThirdPartySyncers
{
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Auth.OAuth2.Flows;
    using Google.Apis.Calendar.v3;
    using Google.Apis.Calendar.v3.Data;
    using Google.Apis.Services;
    using KaboomeBackend.Couch;
    using KaboomeBackend.Models;

    public class GoogleSyncer
    {
        private readonly GoogleAuthorizationCodeFlow flow;
        private readonly AdminCouchClient client;
        private readonly string? authSessionCookie;

        public GoogleSyncer(GoogleAuthorizationCodeFlow flow, AdminCouchClient client, string authSessionCookie)
        {
            this.flow = flow;
            this.client = client;
            this.authSessionCookie = authSessionCookie;
        }
        private static long EventDateTimeToTimestamp(EventDateTime eventDateTime) =>
            DateTimeOffset.Parse(eventDateTime.DateTimeRaw ?? eventDateTime.Date).ToUnixTimeMilliseconds();
        private static EventDateTime TimestampToEventDateTime(long timestamp) => new()
        {
            DateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime,
            TimeZone = "UTC"
        };
        public async Task SyncGoogleAccount(string kaboomeUsername, UserConfigDoc userConfig, UserSecretDoc userSecret)
        {
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = new UserCredential(this.flow,
                       "user", new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                       {
                           RefreshToken = userSecret.GoogleRefreshToken
                       })
            };
            var service = new CalendarService(initializer);
            foreach (var googleCalendarConfig in (userConfig.GoogleCalendarConfigs ?? new()).Where(gcc => gcc.ShouldSync))
            {
                //PUSH
                foreach (var kaboomeEvent in await this.client.GetEventChanges(kaboomeUsername, this.authSessionCookie, googleCalendarConfig.Since))
                {
                    await this.PushEventToGoogle(kaboomeUsername, service, googleCalendarConfig, kaboomeEvent);
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
                        await this.PullEventFromGoogle(kaboomeUsername, googleEvent, googleCalendarConfig.GoogleCalendarPath);
                    }
                    listRequest.PageToken = events.NextPageToken;
                    syncToken = events.NextSyncToken;
                } while (listRequest.PageToken != null);

                googleCalendarConfig.Since = await this.client.GetLatestSeqNumberOfEventDb(kaboomeUsername);
                googleCalendarConfig.GoogleSyncToken = syncToken;
                await this.client.WriteUserConfig(kaboomeUsername, userConfig._id, userConfig);
            }
        }

        private async Task PullEventFromGoogle(string kaboomeUsername, Event? googleEvent, GoogleCalendarPath googleCalendarPath)
        {
            if (googleEvent.Status == "cancelled")
            {
                // Delete
                var toDelete = await this.client.GetKaboomeEvent(kaboomeUsername, $"google-{googleEvent.Id}");
                if (toDelete != null)
                {
                    await this.client.DeleteKaboomeEvent(kaboomeUsername, toDelete._id, toDelete._rev);
                }
            }
            else
            {
                var toUpdateDoc = await this.client.GetKaboomeEvent(kaboomeUsername, $"google-{googleEvent.Id}");
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
                    kaboomeEvent.ReadWriteExternalEvent.GoogleCalendarPath = googleCalendarPath;
                    await this.client.WriteKaboomeEvent(kaboomeUsername, $"google-{googleEvent.Id}", kaboomeEvent);
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
                    await this.client.WriteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, kaboomeEvent);
                }
            }
        }

        private async Task PushEventToGoogle(string kaboomeUsername, CalendarService service, GoogleCalendarConfig googleCalendarConfig, KaboomeEvent? kaboomeEvent)
        {
            if (kaboomeEvent?.ReadWriteExternalEvent?.GoogleCalendarPath == googleCalendarConfig.GoogleCalendarPath)
            {
                var google = kaboomeEvent.ReadWriteExternalEvent.Google;
                var googleCalendarId = googleCalendarConfig.GoogleCalendarPath.GoogleCalendarId;
                await this.PushGoogleEvent(kaboomeUsername, service, kaboomeEvent, google, googleCalendarId);
            }
            foreach (var writeOnlyExternalEvent in kaboomeEvent?.WriteOnlyExternalEvents ?? new())
            {
                if (writeOnlyExternalEvent.Google != null && writeOnlyExternalEvent.GoogleCalendarPath != null)
                {
                    await this.PushGoogleEvent(kaboomeUsername, service, kaboomeEvent, writeOnlyExternalEvent.Google, writeOnlyExternalEvent.GoogleCalendarPath.GoogleCalendarId);
                }
            }
        }

        private async Task PushGoogleEvent(string kaboomeUsername, CalendarService service, KaboomeEvent? kaboomeEvent, Event? google, string googleCalendarId)
        {
            var googleEventId = google?.Id;
            if (kaboomeEvent._deleted == true)
            {
                // Delete
                await service.Events.Delete(googleCalendarId, googleEventId).ExecuteAsync();

            }
            else if (google?.Id != null)
            {
                // Update
                google.Summary = kaboomeEvent.Title;
                google.Description = kaboomeEvent.Description;
                google.Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp);
                google.End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp);
                google.CreatedRaw = google.CreatedRaw.Replace("Z", ".000Z");
                await service.Events.Update(google, googleCalendarId, google.Id).ExecuteAsync();
            }
            else
            {
                // Insert
                google.Summary = kaboomeEvent.Title;
                google.Description = kaboomeEvent.Description;
                google.Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp);
                google.End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp);
                await service.Events.Insert(google, googleCalendarId).ExecuteAsync();
                // Will be added by pull sync again (with a correct event id generated by google)
                await this.client.DeleteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, kaboomeEvent._rev);
            }
        }
    }
}
