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
