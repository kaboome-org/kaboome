import { defineStore } from "pinia";

export const configStore = defineStore("config", {
  id: "config",
  state: () => ({
    configDb: null,
    syncing: false,
  }),
  actions: {
    activateSync(user) {
      const configDbName = `kaboome_${user}_config`;
      PouchDB.sync(
        document.location.origin + configDbName,
        configDbName,
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
      this.configDb = new PouchDB(configDbName);
    },
    registerChangesHandler(fun) {
      this.configDb
        .changes({
          since: "now",
          live: true,
          include_docs: true,
        })
        .on("change", () => {
          fun();
        });
    },
    loadGoogleAccounts: async function () {
      const googleAccounts = [];
      await this.configDb.allDocs(
        { include_docs: true, descending: true },
        function (err, doc) {
          doc.rows.forEach((row) => {
            googleAccounts.push({
              label: row.id,
              children: row.doc.GoogleCalendarConfigs.map((v) => {
                return {
                  label: v.GoogleCalendarPath.GoogleCalendarId,
                  icon: "calendar_month",
                  doc: row.doc,
                  shouldSync: v.ShouldSync,
                  syncFromCalendars: v.syncFromCalendars ?? [
                    {
                      calendarPath: {
                        vendor: "kaboome",
                        vendorCalendarPathJson: "null",
                      },
                      syncType: "NONE",
                    },
                  ],
                };
              }),
            });
          });
        }
      );
      return googleAccounts;
    },
  },
});
