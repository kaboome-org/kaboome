<template>
  <div class="full-width">
    <q-btn
      color="blue"
      text-color="white"
      aria-label="Authorize a Google Account"
      onclick="window.open('/backend/google-signin','_blank','width=700,height=600');"
      class="q-mb-md"
    >
      Authorize a Google Account
    </q-btn>
    <q-tree
      :nodes="this.googleAccounts"
      no-nodes-label=" "
      node-key="label"
      default-expand-all
    >
      <template v-slot:default-header="prop">
        <div class="row items-center full-width">
          <div
            class="text-weight-bold text-primary ellipsis"
            style="width: 80%"
            :title="prop.node.label"
          >
            <q-icon
              v-if="prop.node.icon"
              :name="prop.node.icon"
              class="q-pr-sm"
            />{{ prop.node.label }}
          </div>
          <div v-if="!prop.node.doc" class="q-ml-auto">
            <q-btn
              round
              flat
              icon="delete"
              text-color="red"
              title="Delete Account"
              aria-label="Delete Account"
              onclick="alert('TODO: Not implemented yet.')"
            />
          </div>
        </div>
      </template>

      <template v-slot:default-body="prop">
        <div v-if="prop.node.doc">
          <q-toggle
            v-model="prop.node.shouldSync"
            color="green"
            v-on:click="this.setShouldSync(prop.node)"
            label="Sync this calendar"
          />
          <q-btn
            push
            color="primary"
            label="Configure syncs from other calendars"
          >
            <q-popup-proxy
              cover
              transition-show="scale"
              transition-hide="scale"
              v-on:before-show="
                this.openSyncFromCalendars = prop.node.SyncFromCalendars
              "
            >
              <SyncFromConfiguratorModal
                :syncFromCalendars="this.openSyncFromCalendars"
                :accounts="this.googleAccounts"
                @sync-saved="(e) => this.setSyncFromCalendars(prop.node, e)"
              />
            </q-popup-proxy>
          </q-btn>
        </div>
      </template>
    </q-tree>
  </div>
</template>

<script>
import { configStore } from "../stores/config.js";
import { ref } from "vue";
import SyncFromConfiguratorModal from "src/components/SyncFromConfiguratorModal.vue";

export default {
  methods: {
    setShouldSync: function (node) {
      node.doc.GoogleCalendarConfigs.find(
        (v) => v.GoogleCalendarPath.GoogleCalendarId == node.label
      ).ShouldSync = node.shouldSync;
      this.config.configDb.put(node.doc);
    },
    setSyncFromCalendars: function (node, e) {
      const target = node.doc.GoogleCalendarConfigs.find(
        (v) => v.GoogleCalendarPath.GoogleCalendarId == node.label
      );
      target.SyncFromCalendars = e;

      this.config.configDb.put(node.doc);
    },
  },
  setup() {
    const config = configStore();
    const gaccs = [];
    return {
      config,
      googleAccounts: ref(gaccs),
      openSyncFromCalendars: [],
    };
  },
  created() {
    this.config.activateSync(localStorage.getItem("user"));
    const instance = this;
    const googleAccountLoader = () => {
      instance.config.loadGoogleAccounts().then((res) => {
        instance.googleAccounts = res;
      });
    };
    this.config.registerChangesHandler(googleAccountLoader);
    googleAccountLoader();
  },
  components: { SyncFromConfiguratorModal },
};
</script>
