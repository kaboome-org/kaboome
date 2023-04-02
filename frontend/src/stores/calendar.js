import { defineStore } from "pinia";

function couchDocToFullcalendarEvent(doc) {
  return {
    id: doc._id,
    title: doc.Title,
    start: doc.StartTimestamp,
    end: doc.EndTimestamp,
    extendedProps: {
      description: doc.Description,
      innerData: doc.innerData,
      eventType: doc.eventType ?? "Event",
      isDone: doc.isDone ?? false,
      rev: doc._rev,
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
    innerData: fullcalendarEvent.extendedProps?.innerData,
    eventType: fullcalendarEvent.extendedProps?.eventType,
    isDone: fullcalendarEvent.extendedProps?.isDone,
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
        () => {/* Sync error */}
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
