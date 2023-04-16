<template>
  <div>
    <q-btn
      color="blue"
      text-color="white"
      aria-label="Authorize a Google Account"
      onclick="window.open('/backend/google-signin','_blank','width=700,height=600');"
    >
      Authorize a Google Account
    </q-btn>
    <q-btn
      color="blue"
      text-color="white"
      label="Sync now"
      aria-label="Sync now"
      v-on:click="this.syncNow()"
    />
    <q-tree
      :nodes="this.googleAccounts"
      no-nodes-label=" "
      node-key="label"
      default-expand-all
    >
      <template v-slot:default-header="prop">
        <div class="row items-center">
          <q-icon :name="prop.node.icon || 'refresh'" class="q-mr-sm" />
          <div class="text-weight-bold text-primary">{{ prop.node.label }}</div>
        </div>
      </template>

      <template v-slot:default-body="prop">
        <div v-if="prop.node.doc">
          <q-checkbox
            v-model="prop.node.shouldSync"
            label="Sync this calendar"
            class="q-mr-md"
            v-on:click="this.setShouldSync(prop.node)"
          />
        </div>
        <div v-else>
          <q-btn
            color="red"
            text-color="white"
            label="Delete Account"
            aria-label="Delete Account"
            onclick="alert('TODO: Not implemented yet.')"
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
    syncNow: function () {
      fetch("/backend/third-party-sync-events", {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
      });
    },
    setShouldSync: function (node) {
      node.doc.GoogleCalendarConfigs.find(
        (v) => v.GoogleCalendarPath.GoogleCalendarId == node.label
      ).ShouldSync = node.shouldSync;
      this.config.configDb.put(node.doc);
    },
    loadGoogleAccounts: async function () {
      const instance = this;
      await this.config.configDb.allDocs(
        { include_docs: true, descending: true },
        function (err, doc) {
          instance.googleAccounts = [];
          doc.rows.forEach((row) => {
            instance.googleAccounts.push({
              label: row.id,
              children: row.doc.GoogleCalendarConfigs.map((v) => {
                return {
                  label: v.GoogleCalendarPath.GoogleCalendarId,
                  icon: "calendar_month",
                  doc: row.doc,
                  shouldSync: v.ShouldSync,
                };
              }),
            });
          });
        }
      );
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
    this.config.activateSync(localStorage.getItem("user"));
    this.config.configDb
      .changes({
        since: "now",
        live: true,
      })
      .on("change", () => {
        this.loadGoogleAccounts();
      });
    this.loadGoogleAccounts();
  },
};
</script>
