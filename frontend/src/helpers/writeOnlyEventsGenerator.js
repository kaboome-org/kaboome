export function writeOnlyEvents(source, configuration) {
  const ret = [];
  const sourceCalendarPath = JSON.parse(source.VendorCalendarPathJson);
  configuration.forEach((googleAccount) => {
    googleAccount.children.forEach((googleCalendar) => {
      googleCalendar.SyncFromCalendars.forEach((calendarSyncConfig) => {
        const calendarPath = calendarSyncConfig.VendorCalendarPathJson;
        if (source.Vendor == calendarSyncConfig.Vendor) {
          if (
            (source.Vendor == "google" &&
              sourceCalendarPath.GoogleAccountId ==
                calendarPath.GoogleAccountId &&
              sourceCalendarPath.GoogleCalendarId ==
                calendarPath.GoogleCalendarId) ||
            source.Vendor == "kaboome"
          ) {
            ret.push({
              GoogleCalendarPath: {
                GoogleAccountId: googleAccount.label.substring(7),
                GoogleCalendarId: googleCalendar.label,
              },
              SyncType: calendarSyncConfig.SyncType,
              ManuallyEdited: false,
            });
          }
        }
      });
    });
  });
  return ret;
}
export function addOrMergeWriteOnlyEvents(row, gaccs, fullcalendarEvent) {
  const vendor = row.doc._id.split("-")[0];
  const proposedAutomaticWOEvents = writeOnlyEvents(
    {
      Vendor: vendor,
      VendorCalendarPathJson:
        vendor == "kaboome"
          ? "null"
          : JSON.stringify(row.doc.ReadWriteExternalEvent.GoogleCalendarPath),
    },
    gaccs
  );
  const ret = false;
  const WriteOnlyEvents =
    fullcalendarEvent.extendedProps.WriteOnlyExternalEvents;
  if (WriteOnlyEvents.length == 0) {
    fullcalendarEvent.extendedProps.WriteOnlyExternalEvents =
      proposedAutomaticWOEvents;
  } else {
    for (let i = 0; i < WriteOnlyEvents.length; i++) {
      let found = false;
      for (let j = 0; j < proposedAutomaticWOEvents.length; j++) {
        const prop = proposedAutomaticWOEvents[j];
        if (
          prop.GoogleCalendarPath?.GoogleAccountId ==
            WriteOnlyEvents[i].GoogleCalendarPath?.GoogleAccountId &&
          prop.GoogleCalendarPath?.GoogleCalendarId ==
            WriteOnlyEvents[i].GoogleCalendarPath?.GoogleCalendarId
        ) {
          if (!WriteOnlyEvents[i].ManuallyEdited) {
            WriteOnlyEvents[i] = prop;
            ret = true;
          }
          proposedAutomaticWOEvents.splice(j, 1);
          found = true;
          break;
        }
      }
      if (!found) {
        WriteOnlyEvents.splice(i, 1);
        ret = true;
        i--;
      }
    }
    for (const prop of proposedAutomaticWOEvents) {
      WriteOnlyEvents.push(prop);
      ret = true;
    }
    return ret;
  }
}
