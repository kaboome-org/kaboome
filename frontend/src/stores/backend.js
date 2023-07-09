import { defineStore } from "pinia";

export const backendStore = defineStore("backend", {
  id: "backend",
  state: () => {
    const timestamp = Number(new Date());
    return {
      lastBackendSyncStarted: timestamp,
      lastBackendSyncEnded: timestamp + 1,
    };
  },
  actions: {
    syncNow(isRetry = false) {
      const timestamp = Number(new Date());
      const minimumWaitBetweenSyncs = 5000;
      const retryWhenNotFinishedSuccessfully = 60000;
      if (
        timestamp - this.lastBackendSyncEnded >
        retryWhenNotFinishedSuccessfully
      ) {
        //Something went wrong -> reallow sync
        this.lastBackendSyncEnded = timestamp;
      }
      if (
        timestamp - this.lastBackendSyncStarted > minimumWaitBetweenSyncs &&
        this.lastBackendSyncEnded > this.lastBackendSyncStarted
      ) {
        this.lastBackendSyncStarted = timestamp;
        fetch("/backend/third-party-sync-events", {
          method: "POST",
          headers: {
            Accept: "application/json",
            "Content-Type": "application/json",
          },
        })
          .then((res) => res.text())
          .then((t) => {
            if (t === '"OK"') {
              this.lastBackendSyncEnded = Number(new Date());
            } else {
              console.warn(t);
            }
          });
      } else if (!isRetry) {
        setTimeout(() => {
          this.syncNow(true);
        }, minimumWaitBetweenSyncs);
      }
    },
  },
});
