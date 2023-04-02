<template>
  <q-page class="flex flex-center">
    <q-dialog v-model="eventEditDialogueOpen" no-backdrop-dismiss>
      <div style="width: 100%; min-width: 200px; max-width: 600px">
        <q-form @submit="onSubmit(eventForm)">
          <q-card v-if="eventEditDialogueOpen">
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
                @click="deleteEvent(eventForm)"
              ></q-btn>
            </q-toolbar>
            <q-card-section class="inset-shadow">
              <q-input
                v-model="eventForm.title"
                label="Title"
                autofocus
                standout="bg-blue-grey-1"
                class="q-mb-sm q-input-dark"
                color="black"
                dense
                />
                <div class="row items-center q-my-sm">
                  <q-icon name="schedule" color="primary" size="32px" class="q-mr-sm" />

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
                          <q-popup-proxy cover transition-show="scale" transition-hide="scale" ref="qDateStartProxy">
                            <q-date v-model="eventForm.start" today-btn @update:model-value="$refs.qDateStartProxy.hide()">
                              <div class="row items-center justify-end">
                                <q-btn v-close-popup label="OK" color="primary" flat />
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
                          <q-popup-proxy cover transition-show="scale" transition-hide="scale">
                              <q-time v-model="eventForm.startTime" >
                                  <div class="row items-center justify-end">
                                  <q-btn v-close-popup label="Select" color="primary" flat />
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
                        <q-popup-proxy cover transition-show="scale" transition-hide="scale" >
                            <q-time v-model="eventForm.endTime" >
                                <div class="row items-center justify-end">
                                <q-btn v-close-popup label="Select" color="primary" flat />
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
                      <q-popup-proxy cover transition-show="scale" transition-hide="scale" ref="qDateEndProxy">
                        <q-date v-model="eventForm.end" today-btn @update:model-value="$refs.qDateEndProxy.hide()">
                          <div class="row items-center justify-end">
                            <q-btn v-close-popup label="Select" color="primary" flat />
                          </div>
                        </q-date>
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
                </div>
              </div>

              <div class="row items-center q-mb-md">
                <q-icon name="fmd_good" color="primary" size="32px" class="q-mr-sm"/>
                <div class="col-11">
                  <q-input
                    v-model="eventForm.extendedProps.place"
                    standout="bg-blue-grey-1"
                    label="Place"
                    class="q-input-dark"
                    style="width: auto;"
                    dense
                    />
                </div>
              </div>


              <q-input
                v-model="eventForm.extendedProps.description"
                label="Description"
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
    <div style="width: 100%">
      <FullCalendar
        :options="calendarOptions"
        ref="fullcalendar"
        :key="rerender"
      />
    </div>
  </q-page>
</template>

<script>
import { defineComponent, ref, reactive } from "vue";
import { loginStore } from "../stores/login.js";
import { calendarStore } from "../stores/calendar.js";
import { useLocalStorage } from "@vueuse/core";
import FullCalendar from "@fullcalendar/vue3";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import { date } from 'quasar';

const zoomLevels = [
  "00:05:00",
  "00:10:00",
  "00:15:00",
  "00:20:00",
  "00:30:00",
  "01:00:00",
];
export default defineComponent({
  name: "IndexPage",
  setup() {
    const login = loginStore();
    const calendar = calendarStore();
    calendar.activateSync(login.user);
    const zoomLevelIndex = useLocalStorage("zoomLevelIndex", 0);

    const calendarOptions = {
      plugins: [timeGridPlugin, interactionPlugin],
      initialView: "timeGridWeek",
      firstDay: new Date().getDay() - 1,
      scrollTime: new Date(new Date() - 3600000).toLocaleTimeString(),
      editable: true,
      selectable: true,
      displayEventTime: false, // don't show the time column in list view
      slotDuration: zoomLevels[zoomLevelIndex.value],
      slotLabelInterval: "01:00:00",
      nowIndicator: true,
      allDaySlot: false,
      height: "90vh",
    };
    return {
      calendar,
      eventForm: null,
      eventEditDialogueOpen: ref(false),
      login,
      calendarOptions,
      rerender: ref(false),
      zoomLevelIndex,
      zoomLevels,
      eventTypeOptions: ["Event", "Task", "Habit"],
    };
  },
  mounted() {
    const instance = this;
    this.calendar.registerChangesHandler(() => {
      instance.$refs.fullcalendar.calendar.refetchEvents();
    });
  },
  created() {
    const instance = this;
    this.calendarOptions.events = async function (
      info,
      successCallback,
      failureCallback
    ) {
      successCallback(
        await instance.calendar.events(info.start.valueOf(), info.end.valueOf())
      );
    };
    this.calendarOptions.customButtons = {
      zoomout: {
        text: "-",
        click: function () {
          if (instance.zoomLevelIndex < instance.zoomLevels.length - 1) {
            instance.zoomLevelIndex++;
            instance.calendarOptions.slotDuration =
              instance.zoomLevels[instance.zoomLevelIndex];
            instance.rerender = !instance.rerender;
          }
        },
      },
      zoomin: {
        text: "+",
        click: function () {
          if (instance.zoomLevelIndex > 0) {
            instance.zoomLevelIndex--;
            instance.calendarOptions.slotDuration =
              instance.zoomLevels[instance.zoomLevelIndex];
            instance.rerender = !instance.rerender;
          }
        },
      },
    };
    this.calendarOptions.headerToolbar = {
      right: "prev,next today",
      center: "title",
      left: "zoomout zoomin",
    };
    function editEvent(changedEvent) {
      instance.calendar.put(changedEvent);
    }
    function openModal(eventToOpen) {
      instance.eventForm = reactive({
        id: eventToOpen.id,
        title: eventToOpen.title,
        start: date.formatDate(eventToOpen.start, 'YYYY/MM/DD'),
        startTime: date.formatDate(eventToOpen.start, 'HH:mm'),
        end: date.formatDate(eventToOpen.end, 'YYYY/MM/DD'),
        endTime:  date.formatDate(eventToOpen.end, 'HH:mm'),
        extendedProps: {
          description: eventToOpen.extendedProps?.description,
          rev: eventToOpen.extendedProps?.rev,
          eventType: eventToOpen.extendedProps?.eventType,
          isDone: eventToOpen.extendedProps?.isDone,
          //innerData: eventToOpen.extendedProps.innerData,
        },
      });
      instance.eventEditDialogueOpen = true;
    }
    this.calendarOptions.eventResize = (eventResizeInfo) => {
      editEvent(eventResizeInfo.event);
    };
    this.calendarOptions.eventDrop = (eventDropInfo) => {
      editEvent(eventDropInfo.event);
    };
    this.calendarOptions.select = (selectionInfo) => {
      openModal({
        id: "kaboome-" + Number(new Date()),
        title: "New Event",
        start: selectionInfo.start,
        end: selectionInfo.end,
        extendedProps: {
          description: "",
          eventType: "Event",
          isDone: false,
          //innerData: eventToOpen.extendedProps.innerData,
        },
      });
    };
    this.calendarOptions.eventClick = (arg) => {
      openModal(arg.event);
    };
  },
  computed: {
    scrollerPopupStyle280() {
      if (this.$q.screen.lt.sm) {
        return {
          width: "100vw",
          height: "100vh",
        };
      } else {
        return {
          maxHeight: "400px",
          height: "400px",
          width: "280px",
        };
      }
    },
  },
  methods: {
    deleteEvent(eventForm) {
      this.calendar.delete(eventForm);
    },
    onSubmit(eventForm) {
      eventForm.start = this.convertStringDateTimeToDateObject(eventForm.start, eventForm.startTime);
      eventForm.end = this.convertStringDateTimeToDateObject(eventForm.end, eventForm.endTime);

      this.calendar.put(eventForm);
    },
    convertStringDateTimeToDateObject(d, t){
      return date.extractDate(d + " " + t, 'YYYY/MM/DD HH:mm');
    }
  },
  components: { FullCalendar },
});
</script>
