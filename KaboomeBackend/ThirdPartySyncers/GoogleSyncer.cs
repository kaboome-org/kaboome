namespace KaboomeBackend.ThirdPartySyncers
{
    using System.Text.RegularExpressions;
    using Google;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Auth.OAuth2.Flows;
    using Google.Apis.Calendar.v3;
    using Google.Apis.Calendar.v3.Data;
    using Google.Apis.Services;
    using KaboomeBackend.Couch;
    using KaboomeBackend.Models;

    public partial class GoogleSyncer
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
                var (latestSeq, changedEvents) = await this.client.GetEventChanges(kaboomeUsername, this.authSessionCookie, googleCalendarConfig.Since);
                var blacklistEventIds = new Dictionary<string, string>(googleCalendarConfig.BlackListEventIds);
                foreach (var kaboomeEvent in changedEvents)
                {
                    await this.PushEventToGoogle(kaboomeUsername, service, googleCalendarConfig, kaboomeEvent, blacklistEventIds);
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
                        if (!blacklistEventIds.ContainsKey(googleEvent.Id))
                        {
                            await this.PullEventFromGoogle(kaboomeUsername, googleEvent, googleCalendarConfig.GoogleCalendarPath);
                        }
                    }
                    listRequest.PageToken = events.NextPageToken;
                    syncToken = events.NextSyncToken;
                } while (listRequest.PageToken != null);

                googleCalendarConfig.Since = latestSeq;
                googleCalendarConfig.GoogleSyncToken = syncToken;
                googleCalendarConfig.BlackListEventIds = blacklistEventIds.ToList();
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
                if (googleEvent.RecurringEventId != null)
                {
                    var toUpdate = await this.client.GetKaboomeEvent(kaboomeUsername, $"google-{googleEvent.RecurringEventId}");
                    if (toUpdate != null)
                    {
                        toUpdate.ExDates.Add(EventDateTimeToTimestamp(googleEvent.Start));
                        toUpdate.ExDates = toUpdate.ExDates.ToHashSet().ToList();
                        await this.client.WriteKaboomeEvent(kaboomeUsername, $"google-{googleEvent.RecurringEventId}", toUpdate);
                    }
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
                        RRule = googleEvent.Recurrence?.FirstOrDefault(s => s.StartsWith("RRULE", StringComparison.InvariantCultureIgnoreCase)),
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
                    kaboomeEvent.RRule = googleEvent.Recurrence?.FirstOrDefault(s => s.StartsWith("RRULE", StringComparison.InvariantCultureIgnoreCase));
                    await this.client.WriteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, kaboomeEvent);
                }
            }
        }

        private async Task PushEventToGoogle(
            string kaboomeUsername, CalendarService service, GoogleCalendarConfig googleCalendarConfig, KaboomeEvent? kaboomeEvent, Dictionary<string, string> blacklistEventIds)
        {
            if (kaboomeEvent?.ReadWriteExternalEvent?.GoogleCalendarPath == googleCalendarConfig.GoogleCalendarPath)
            {
                var google = kaboomeEvent.ReadWriteExternalEvent.Google;
                var googleCalendarId = googleCalendarConfig.GoogleCalendarPath.GoogleCalendarId;
                kaboomeEvent.ReadWriteExternalEvent.SyncType = SyncType.ReadWrite; // readwrite events can only be readwrite
                await this.PushGoogleEvent(kaboomeUsername, service, kaboomeEvent, kaboomeEvent.ReadWriteExternalEvent);
            }
            if (kaboomeEvent._deleted != true)
            {
                var expectedWriteOnlyEvents = blacklistEventIds.Where(kv => kv.Value == kaboomeEvent._id).Select(kv => kv.Key).ToHashSet();
                var writeOnlyExternalEvents = (kaboomeEvent?.WriteOnlyExternalEvents ?? new()).Select(e => e.Google?.Id ?? "");
                expectedWriteOnlyEvents.ExceptWith(writeOnlyExternalEvents);
                foreach (var deletedWriteOnly in expectedWriteOnlyEvents)
                {
                    try
                    {
                        await service.Events.Delete(googleCalendarConfig.GoogleCalendarPath.GoogleCalendarId, deletedWriteOnly).ExecuteAsync();
                        blacklistEventIds.Remove(deletedWriteOnly);
                    }
                    catch (GoogleApiException g)
                    {

                        if (g.HttpStatusCode != System.Net.HttpStatusCode.Gone)
                        {
                            throw g;
                        }
                    }
                }
            }
            foreach (var writeOnlyExternalEvent in kaboomeEvent?.WriteOnlyExternalEvents ?? new())
            {
                if (writeOnlyExternalEvent.SyncType == SyncType.ReadWrite) // write only events can't be readwrite
                {
                    writeOnlyExternalEvent.SyncType = SyncType.WriteOnlyNoDetail;
                }

                if (writeOnlyExternalEvent?.GoogleCalendarPath == googleCalendarConfig.GoogleCalendarPath)
                {
                    await this.PushGoogleEvent(kaboomeUsername, service, kaboomeEvent, writeOnlyExternalEvent);
                    blacklistEventIds[writeOnlyExternalEvent.Google?.Id ?? ""] = kaboomeEvent._id;
                }
            }
        }

        private async Task PushGoogleEvent(string kaboomeUsername, CalendarService service, KaboomeEvent? kaboomeEvent, ExternalEvent externalEvent)
        {
            var googleCalendarId = externalEvent.GoogleCalendarPath?.GoogleCalendarId;
            var google = externalEvent.Google;
            var googleEventId = google?.Id;
            var syncType = externalEvent.SyncType;
            if (kaboomeEvent._deleted == true)
            {
                // Delete
                try
                {
                    await service.Events.Delete(googleCalendarId, googleEventId).ExecuteAsync();
                }
                catch (GoogleApiException g)
                {

                    if(g.HttpStatusCode == System.Net.HttpStatusCode.Gone || google == null)
                    {
                        return;
                    }
                    throw g;
                }

            }
            else if (google?.Id != null)
            {
                // Update
                if (syncType != SyncType.WriteOnlyNoDetail)
                {
                    google.Summary = kaboomeEvent.Title;
                    google.Description = kaboomeEvent.Description;
                }
                else
                {
                    google.Summary = "Blocked";
                    google.Description = "";
                }
                google.Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp);
                google.End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp);
                google.CreatedRaw = google.CreatedRaw.Replace("Z", ".000Z");

                if (kaboomeEvent.RRule != null)
                {
                    google.Recurrence = new List<string>{
                    kaboomeEvent.RRule,
                    ConvertToExDatesString(kaboomeEvent.ExDates)
                    };
                }

                var refetched = await service.Events.Get(googleCalendarId, google.Id).ExecuteAsync();
                google.ETag = refetched.ETag;
                google.Sequence = refetched.Sequence;
                var updated = await service.Events.Update(google, googleCalendarId, google.Id).ExecuteAsync();
                externalEvent.Google = updated;
                await this.client.WriteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, kaboomeEvent);
            }
            else
            {
                // Insert
                google = new()
                {
                    Start = TimestampToEventDateTime(kaboomeEvent.StartTimestamp),
                    End = TimestampToEventDateTime(kaboomeEvent.EndTimestamp)
                };
                // Update
                if (syncType != SyncType.WriteOnlyNoDetail)
                {
                    google.Summary = kaboomeEvent.Title;
                    google.Description = kaboomeEvent.Description;
                }
                else
                {
                    google.Summary = "Blocked";
                    google.Description = "";
                }
                if (kaboomeEvent.RRule != null)
                {
                    google.Recurrence = new List<string>{
                    kaboomeEvent.RRule,
                    ConvertToExDatesString(kaboomeEvent.ExDates)
                    };
                }
                var inserted = await service.Events.Insert(google, googleCalendarId).ExecuteAsync();

                if (externalEvent.SyncType == SyncType.ReadWrite)
                {
                    // Will be added by pull sync again (with a correct event id generated by google)
                    await this.client.DeleteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, kaboomeEvent._rev);
                }
                else
                {
                    externalEvent.Google = inserted;
                    await this.client.WriteKaboomeEvent(kaboomeUsername, kaboomeEvent._id, kaboomeEvent);
                }
            }
        }

        /// <summary>
        /// See https://www.rfc-editor.org/rfc/rfc5545#section-3.8.5.1
        /// example return: EXDATE:19960402T010000Z,19960403T010000Z,19960404T010000Z
        /// </summary>
        /// <param name="exDates"></param>
        /// <returns>Exdate string</returns>
        private static string ConvertToExDatesString(List<long> exDates)
        {
            if(exDates.Count == 0)
            { return ""; }
            return "EXDATE:" + string.Join(",",
            exDates.Select(ed =>
            {
                var dateWithOutMinusAndColon = MinusAndColonDiscarder().Replace(TimestampToEventDateTime(ed).DateTimeRaw, "");
                return MillisecondsDiscarder().Replace(dateWithOutMinusAndColon, "Z");
            }));
        }

        [GeneratedRegex("\\..+", RegexOptions.Compiled)]
        private static partial Regex MillisecondsDiscarder();
        [GeneratedRegex("[-:]", RegexOptions.Compiled)]
        private static partial Regex MinusAndColonDiscarder();
    }
}
