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
        () => console.log("Sync error")
      )
        .on("denied", function () {
          console.log("denied");
          this.syncing = false;
        })
        .on("error", function () {
          console.log("error");
          this.syncing = false;
        });
      this.syncing = true;
      this.configDb = new PouchDB(configDbName);
    },
  },
});
