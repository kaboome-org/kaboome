<template>
  <q-dialog no-backdrop-dismiss>
    <div style="width: 100%; min-width: 200px; max-width: 400px">
      <q-form @submit="onSubmit">
        <q-card>
          <q-toolbar class="bg-primary text-white">
            <q-toolbar-title> Jump to Date </q-toolbar-title>
          </q-toolbar>
          <q-card-section class="inset-shadow">
            <div class="row items-center q-my-sm">
              <q-icon
                name="schedule"
                color="primary"
                size="32px"
                class="q-mr-sm"
              />

              <div class="col-8 q-mt-md">
                <q-input
                  v-model="start"
                  ref="dateStart"
                  label="Jump Date"
                  color="blue-6"
                  mask="date"
                  :rules="['date']"
                  standout="bg-blue-grey-1"
                  dense
                  class="q-input-dark"
                >
                  <template v-slot:append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy
                        cover
                        transition-show="scale"
                        transition-hide="scale"
                        ref="qDateStartProxy"
                      >
                        <q-date
                          v-model="start"
                          today-btn
                          @update:model-value="$refs.qDateStartProxy.hide()"
                        >
                          <div class="row items-center justify-end">
                            <q-btn
                              v-close-popup
                              label="OK"
                              color="primary"
                              flat
                            />
                          </div>
                        </q-date>
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
              </div>
            </div>
          </q-card-section>
          <q-card-actions align="right">
            <q-btn
              flat
              label="OK"
              type="submit"
              color="primary"
              v-close-popup
            />
            <q-btn flat label="Cancel" color="primary" v-close-popup />
          </q-card-actions>
        </q-card>
      </q-form>
    </div>
  </q-dialog>
</template>
<script>
import { ref } from "vue";
import { date } from "quasar";

export default {
  name: "GotoDateModal",
  emits: ["dateSelected"],
  setup() {
    return {
      start: ref(date.formatDate(new Date(), "YYYY/MM/DD")),
    };
  },
  methods: {
    onSubmit() {
      this.$emit("dateSelected", this.start);
    },
  },
};
</script>
