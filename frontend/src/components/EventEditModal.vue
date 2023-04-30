<template>
  <q-dialog no-backdrop-dismiss>
    <div style="width: 100%; min-width: 200px; max-width: 600px">
      <q-form @submit="onSubmit">
        <q-card>
          <q-toolbar class="bg-primary text-white">
            <q-toolbar-title> Event Editor </q-toolbar-title>
            <q-checkbox
              v-model="eventForm.extendedProps.isDone"
              label="Done"
              color="positive"
              dark
              class="q-mr-md"
            />
            <q-btn
              flat
              round
              color="white"
              icon="delete"
              v-close-popup
              @click="$emit('eventDeleted', eventForm)"
            ></q-btn>
          </q-toolbar>
          <q-card-section class="inset-shadow">
            <q-input
              v-model="eventForm.title"
              label="Title"
              autofocus
              @focus="(evt) => evt.target.select()"
              standout="bg-blue-grey-1"
              class="q-mb-sm q-input-dark"
              color="black"
              dense
            />
            <div class="row items-center q-my-sm">
              <q-icon
                name="schedule"
                color="primary"
                size="32px"
                class="q-mr-sm"
              />

              <div class="col-3 q-mt-md">
                <q-input
                  v-model="eventForm.start"
                  ref="dateStart"
                  label="Event start date"
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
                          v-model="eventForm.start"
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
              <div class="col-2 q-mt-md">
                <q-input
                  v-model="eventForm.startTime"
                  ref="timeStart"
                  label="Event start time"
                  color="blue-6"
                  mask="time"
                  :rules="['time']"
                  class="q-ml-sm q-input-dark"
                  standout="bg-blue-grey-1"
                  dense
                >
                  <template v-slot:append>
                    <q-icon name="access_time" class="cursor-pointer">
                      <q-popup-proxy
                        cover
                        transition-show="scale"
                        transition-hide="scale"
                      >
                        <q-time v-model="eventForm.startTime">
                          <div class="row items-center justify-end">
                            <q-btn
                              v-close-popup
                              label="Select"
                              color="primary"
                              flat
                            />
                          </div>
                        </q-time>
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
              </div>

              <label class="q-mx-sm">to</label>

              <div class="col-2 q-mt-md">
                <q-input
                  v-model="eventForm.endTime"
                  ref="timeEnd"
                  label="Event end time"
                  color="blue-6"
                  mask="time"
                  :rules="['time']"
                  class="q-mr-sm q-input-dark"
                  standout="bg-blue-grey-1"
                  dense
                >
                  <template v-slot:append>
                    <q-icon name="access_time" class="cursor-pointer">
                      <q-popup-proxy
                        cover
                        transition-show="scale"
                        transition-hide="scale"
                      >
                        <q-time v-model="eventForm.endTime">
                          <div class="row items-center justify-end">
                            <q-btn
                              v-close-popup
                              label="Select"
                              color="primary"
                              flat
                            />
                          </div>
                        </q-time>
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
              </div>
              <div class="col-3 q-mt-md">
                <q-input
                  v-model="eventForm.end"
                  ref="dateTimeEnd"
                  label="Event end date"
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
                        ref="qDateEndProxy"
                      >
                        <q-date
                          v-model="eventForm.end"
                          today-btn
                          @update:model-value="$refs.qDateEndProxy.hide()"
                        >
                          <div class="row items-center justify-end">
                            <q-btn
                              v-close-popup
                              label="Select"
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

            <div class="row items-center q-mb-md">
              <q-icon
                name="fmd_good"
                color="primary"
                size="32px"
                class="q-mr-sm"
              />
              <div class="col-11">
                <q-input
                  v-model="eventForm.extendedProps.place"
                  standout="bg-blue-grey-1"
                  label="Place"
                  class="q-input-dark"
                  style="width: auto"
                  dense
                />
              </div>
            </div>

            <q-input
              v-model="eventForm.extendedProps.description"
              label="Description"
              standout="bg-blue-grey-1"
              class="q-mb-md q-input-dark"
            />
            <q-input
              v-model="eventForm.extendedProps.rrule"
              label="Recurrence (DEMO)"
              standout="bg-blue-grey-1"
              autogrow
              class="q-mb-md q-input-dark"
            />
            <q-select
              filled
              v-model="eventForm.extendedProps.eventType"
              :options="eventTypeOptions"
              label="Event type"
              class="q-mb-sm"
            />
            <q-btn
              v-if="eventForm.extendedProps.ReadWriteExternalEvent.Google"
              push
              color="primary"
              label="Expert edit other fields"
            >
              <q-popup-proxy
                cover
                transition-show="scale"
                transition-hide="scale"
              >
                <JsonEditorVue
                  v-model="eventForm.extendedProps.ReadWriteExternalEvent"
                />
              </q-popup-proxy>
            </q-btn>
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
import { reactive } from "vue";
import { date } from "quasar";
import JsonEditorVue from "json-editor-vue";

export default {
  name: "EventEditModal",
  props: ["eventToOpen"],
  emits: ["eventSaved", "eventDeleted"],
  setup() {
    return {
      eventForm: {},
      eventTypeOptions: ["Event", "Task", "Habit"],
    };
  },
  beforeUpdate() {
    this.eventForm = reactive({
      id: this.eventToOpen.id,
      title: this.eventToOpen.title,
      start: date.formatDate(this.eventToOpen.start, "YYYY/MM/DD"),
      startTime: date.formatDate(this.eventToOpen.start, "HH:mm"),
      end: date.formatDate(this.eventToOpen.end, "YYYY/MM/DD"),
      endTime: date.formatDate(this.eventToOpen.end, "HH:mm"),
      extendedProps: {
        description: this.eventToOpen.extendedProps?.description,
        rev: this.eventToOpen.extendedProps?.rev,
        eventType: this.eventToOpen.extendedProps?.eventType,
        isDone: this.eventToOpen.extendedProps?.isDone,
        ReadWriteExternalEvent:
          this.eventToOpen.extendedProps?.ReadWriteExternalEvent,
        rrule: this.eventToOpen.extendedProps?.rrule,
      },
    });
  },
  methods: {
    onSubmit() {
      const eventFormCopy = JSON.parse(JSON.stringify(this.eventForm));
      if (
        typeof eventFormCopy.extendedProps.ReadWriteExternalEvent == "string"
      ) {
        eventFormCopy.extendedProps.ReadWriteExternalEvent = JSON.parse(
          eventFormCopy.extendedProps.ReadWriteExternalEvent
        );
      }
      eventFormCopy.start = this.convertStringDateTimeToDateObject(
        eventFormCopy.start,
        eventFormCopy.startTime
      );
      eventFormCopy.end = this.convertStringDateTimeToDateObject(
        eventFormCopy.end,
        eventFormCopy.endTime
      );
      this.$emit("eventSaved", eventFormCopy);
    },
    convertStringDateTimeToDateObject(d, t) {
      return date.extractDate(d + " " + t, "YYYY/MM/DD HH:mm");
    },
  },
  components: { JsonEditorVue },
};
</script>
