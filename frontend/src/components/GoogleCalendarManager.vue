<template>
  <div>
    <q-btn
      color="blue"
      text-color="white"
      label="Authorize a Google Account"
      aria-label="Authorize a Google Account"
      onclick="window.open('/backend/google-signin','_blank','width=700,height=600');"
    />
    <q-tree :nodes="this.googleAccounts" node-key="label" default-expand-all>
      <template v-slot:default-header="prop">
        <div class="row items-center">
          <q-icon :name="prop.node.icon || 'refresh'" class="q-mr-sm" />
          <div class="text-weight-bold text-primary">{{ prop.node.label }}</div>
        </div>
      </template>

      <template v-slot:default-body="prop">
        <div v-if="prop.node.calendarPath">
          <q-btn
            color="blue"
            text-color="white"
            label="Sync now"
            aria-label="Sync now"
            v-on:click="this.syncNow(prop.node.calendarPath)"
          />
        </div>
        <div v-else>
          <q-btn
            color="red"
            text-color="white"
            label="Delete Account"
            aria-label="Delete Account"
            onclick="alert('Not implemented yet.')"
          />
        </div>
      </template>
    </q-tree>
  </div>
</template>

<script>
import { configStore } from "../stores/config.js";
import { ref } from "vue";

export default {
  methods: {
    syncNow: function (googleCalendarPath) {
      fetch("/backend/google-sync-events", {
        method: "POST",
        body: JSON.stringify(googleCalendarPath),
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
      });
    },
    loadGoogleAccounts: function () {
      fetch(`/backend/google-calendar-list`)
        .then((res) => {
          return res.text();
        })
        .then((t) => {
          if (t !== "<h1>You must first authenticate.</h1>") {
            const gaccs = JSON.parse(t);
            this.googleAccounts = [];
            for (const [key, value] of Object.entries(gaccs)) {
              this.googleAccounts.push({
                label: "google-" + key,
                children: value.map((v) => {
                  return {
                    label: v.summary,
                    icon: "calendar_month",
                    calendarPath: {
                      GoogleAccountId: key,
                      GoogleCalendarId: v.summary,
                    },
                  };
                }),
              });
            }
          }
        });
    },
  },
  setup() {
    const config = configStore();
    const gaccs = [];
    return {
      config,
      googleAccounts: ref(gaccs),
    };
  },
  created() {
    this.loadGoogleAccounts();
    this.config.activateSync(localStorage.getItem("user"));
    this.config.configDb
      .changes({
        since: "now",
        live: true,
      })
      .on("change", () => {
        this.loadGoogleAccounts();
      });
  },
};
</script>
