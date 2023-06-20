export function writeOnlyEvents(source, configuration) {
  const ret = [];
  const sourceCalendarPath = JSON.parse(source.vendorCalendarPathJson);
  configuration.forEach((googleAccount) => {
    googleAccount.children.forEach((googleCalendar) => {
      googleCalendar.syncFromCalendars.forEach((calendarSyncConfig) => {
        const calendarPath = calendarSyncConfig.calendarPath;
        if (source.vendor == calendarPath.vendor) {
          if (
            (source.vendor == "google" &&
              sourceCalendarPath.GoogleAccountId ==
                calendarPath.GoogleAccountId &&
              sourceCalendarPath.GoogleCalendarId ==
                calendarPath.GoogleCalendarId) ||
            source.vendor == "kaboome"
          ) {
            debugger;
            ret.push({
              GoogleCalendarPath: {
                GoogleAccountId: googleAccount.label.substring(7),
                GoogleCalendarId: googleCalendar.label,
              },
              SyncType: calendarSyncConfig.syncType,
            });
          }
        }
      });
    });
  });
  return ret;
}
