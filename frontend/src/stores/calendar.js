import { defineStore } from "pinia";

function timestampToDTStart(time) {
  return (
    "DTSTART:" +
    new Date(time).toISOString().replace(/[-:]/g, "").replace(/\..+/, "Z\n")
  );
}

function couchDocToFullcalendarEvent(doc) {
  const rrule = doc.RRule
    ? timestampToDTStart(doc.StartTimestamp) + doc.RRule
    : null;
  return {
    id: doc._id,
    title: doc.Title,
    start: doc.StartTimestamp,
    end: doc.EndTimestamp,
    duration: doc.EndTimestamp - doc.StartTimestamp, // Fullcalendar uses it to compute ends of rrule events
    rrule: rrule, // Important for presentation of recuring events only. (Not accessible when receiving events back)
    exdate:
      (doc.ExDates?.length ?? 0) == 0
        ? null
        : doc.ExDates?.map((d) => new Date(d).toISOString()),
    extendedProps: {
      description: doc.Description,
      ReadWriteExternalEvent: doc.ReadWriteExternalEvent,
      eventType: doc.eventType ?? "Event",
      isDone: doc.isDone ?? false,
      rev: doc._rev,
      rrule: rrule, // see above rrule comment -> Keep it in extendedProps so its accessible
      exdates: doc.ExDates ?? [],
    },
    backgroundColor: doc.isDone ? "#21BA45" : "#1976d2",
  };
}
function fullcalendarEventToCouchDoc(fullcalendarEvent) {
  return {
    _id: fullcalendarEvent.id,
    _rev: fullcalendarEvent.extendedProps?.rev,
    Title: fullcalendarEvent.title,
    StartTimestamp: Number(fullcalendarEvent.start),
    EndTimestamp: Number(fullcalendarEvent.end),
    Description: fullcalendarEvent.extendedProps?.description,
    ReadWriteExternalEvent:
      fullcalendarEvent.extendedProps?.ReadWriteExternalEvent,
    eventType: fullcalendarEvent.extendedProps?.eventType,
    isDone: fullcalendarEvent.extendedProps?.isDone,
    RRule: fullcalendarEvent.extendedProps?.rrule?.split("\n")[1],
    ExDates: fullcalendarEvent.extendedProps?.exdates?.map((d) => Number(d)),
  };
}

export const calendarStore = defineStore("calendar", {
  id: "calendar",
  state: () => ({
    calendarDb: null,
    syncing: false,
  }),
  actions: {
    events: async function (start, end) {
      //Convert to fullcalendar events
      const ret = [];
      await this.calendarDb.allDocs(
        { include_docs: true, descending: true },
        function (err, doc) {
          doc.rows.forEach((row) => {
            ret.push(couchDocToFullcalendarEvent(row.doc));
          });
        }
      );
      return ret;
    },
    registerChangesHandler(fun) {
      this.calendarDb
        .changes({
          since: "now",
          live: true,
          include_docs: true,
        })
        .on("change", () => {
          fun();
        });
    },
    activateSync(user) {
      const calendarDbName = `kaboome_${user}`;
      PouchDB.sync(
        document.location.origin + calendarDbName,
        calendarDbName,
        { live: true },
        () => {
          /* Sync error */
        }
      )
        .on("denied", function () {
          // denied
          this.syncing = false;
        })
        .on("error", function () {
          // error
          this.syncing = false;
        });
      this.syncing = true;
      this.calendarDb = new PouchDB(calendarDbName);
    },
    put(fullcalendarEvent) {
      const toPut = fullcalendarEventToCouchDoc(fullcalendarEvent);
      this.calendarDb.put(toPut);
    },
    delete(fullcalendarEvent) {
      const toPut = fullcalendarEventToCouchDoc(fullcalendarEvent);
      this.calendarDb.remove(toPut);
    },
  },
});
