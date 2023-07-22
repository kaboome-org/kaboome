<template>
  <div class="full-width">
    <q-item v-for="[index, sync] in syncs.entries()" :key="sync">
      <q-btn
        flat
        round
        color="negative"
        icon="delete"
        @click="syncs.splice(index, 1)"
      ></q-btn>
      <q-btn-dropdown :label="sync.Vendor">
        <q-list>
          <q-item
            v-for="vendor in vendors"
            :key="vendor"
            clickable
            v-close-popup
            @click="sync.Vendor = vendor"
          >
            <q-item-section>
              <q-item-label>{{ vendor }}</q-item-label>
            </q-item-section>
          </q-item>
        </q-list>
      </q-btn-dropdown>
      <q-btn-dropdown :label="sync.label" v-if="sync.Vendor != 'kaboome'">
        <q-list>
          <q-item
            v-for="option in optionsForVendor(sync.Vendor)"
            :key="option.label"
            clickable
            v-close-popup
            @click="
              sync.VendorCalendarPathJson = option.VendorCalendarPathJson;
              sync.label = option.label;
            "
          >
            <q-item-section>
              <q-item-label>{{ option.label }}</q-item-label>
            </q-item-section>
          </q-item>
        </q-list>
      </q-btn-dropdown>
      <q-btn-dropdown :label="this.syncTypes[sync.SyncType]">
        <q-list>
          <q-item
            v-for="syncType in Object.entries(this.syncTypes)"
            :key="syncType[0]"
            clickable
            v-close-popup
            @click="sync.SyncType = syncType[0]"
          >
            <q-item-section>
              <q-item-label>{{ syncType[1] }}</q-item-label>
            </q-item-section>
          </q-item>
        </q-list>
      </q-btn-dropdown>
    </q-item>
    <q-btn flat label="+" @click="this.addSync()" />
    <q-btn
      flat
      label="OK"
      @click="this.onSubmit()"
      color="primary"
      v-close-popup
    />
  </div>
</template>
<script>
import { reactive } from "vue";

export default {
  name: "SyncFromConfiguratorModal",
  props: ["syncFromCalendars", "accounts"],
  emits: ["syncSaved"],
  setup() {
    return {
      syncs: [],
      vendors: ["kaboome", "google"],
      syncTypes: {
        2: "no details",
        1: "full details",
      },
      selectedOption: "Choose a calendar",
    };
  },
  created() {
    this.syncs = reactive(
      this.syncFromCalendars.map((s) => {
        return {
          ...s,
          label: JSON.parse(s.VendorCalendarPathJson)?.GoogleCalendarId,
        };
      })
    );
  },
  methods: {
    onSubmit() {
      const syncsCopy = JSON.parse(JSON.stringify(this.syncs));
      syncsCopy.forEach((sc) => {
        delete sc.label;
        sc.SyncType = Number.parseInt(sc.SyncType, 10);
      });
      this.$emit("syncSaved", syncsCopy);
    },
    addSync() {
      this.syncs.push({
        Vendor: "kaboome",
        VendorCalendarPathJson: "null",
        SyncType: 2,
      });
    },
    optionsForVendor(vendor) {
      if (vendor == "kaboome") {
        return [];
      } else {
        return this.accounts
          .filter((a) => a.label.startsWith(vendor))
          .flatMap((a) =>
            a.children.flatMap((c) => {
              if (vendor == "google") {
                return [
                  {
                    label: c.label,
                    VendorCalendarPathJson: JSON.stringify({
                      GoogleAccountId: a.label.substring(6),
                      GoogleCalendarId: c.label,
                    }),
                  },
                ];
              }
              return [];
            })
          );
      }
    },
  },
};
</script>
