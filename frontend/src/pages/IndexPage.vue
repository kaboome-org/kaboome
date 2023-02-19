<template>
  <q-page class="flex flex-center">
    <q-dialog v-model="eventEditDialogueOpen" no-backdrop-dismiss>
      <div>
        <q-form @submit="onSubmit(eventForm)">
          <q-card v-if="eventEditDialogueOpen" style="width: 400px">
            <q-toolbar class="bg-primary text-white">
              <q-toolbar-title> Event Editor </q-toolbar-title>
              <q-btn
                flat
                round
                color="white"
                icon="delete"
                v-close-popup
                @click="deleteEvent(eventForm)"
              ></q-btn>
              <q-btn
                flat
                round
                color="white"
                icon="close"
                v-close-popup
              ></q-btn>
            </q-toolbar>
            <q-card-section class="inset-shadow">
              <q-input v-model="eventForm.title" label="Title" autofocus />
              <q-input
                v-model="eventForm.extendedProps.description"
                label="Description"
              />
              <div>
                <q-input
                  v-model="eventForm.start"
                  ref="dateTimeStart"
                  label="Event start date and time"
                  outlined
                  color="blue-6"
                />

                <q-input
                  v-model="eventForm.end"
                  ref="dateTimeEnd"
                  label="Event end date and time"
                  color="blue-6"
                  outlined
                />
              </div>
              <q-checkbox
                v-model="eventForm.extendedProps.isTask"
                label="Task"
              />
              <q-checkbox
                v-model="eventForm.extendedProps.isDone"
                label="Done"
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
import { defineComponent, ref } from "vue";
import { loginStore } from "../stores/login.js";
import { calendarStore } from "../stores/calendar.js";
import FullCalendar from "@fullcalendar/vue3";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";

export default defineComponent({
  name: "IndexPage",
  setup() {
    const login = loginStore();
    const calendar = calendarStore();
    calendar.activateSync(login.user);

    const calendarOptions = {
      plugins: [timeGridPlugin, interactionPlugin],
      initialView: "timeGridWeek",
      firstDay: new Date().getDay() - 1,
      scrollTime: new Date(new Date() - 3600000).toLocaleTimeString(),
      editable: true,
      selectable: true,
      displayEventTime: false, // don't show the time column in list view
      slotDuration: "00:05:00",
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
      zoomLevelIndex: 0,
      zoomLevels: [
        "00:05:00",
        "00:10:00",
        "00:15:00",
        "00:20:00",
        "00:30:00",
        "01:00:00",
      ],
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
      instance.eventForm = {
        id: eventToOpen.id,
        title: eventToOpen.title,
        start: eventToOpen.start,
        end: eventToOpen.end,
        extendedProps: {
          description: eventToOpen.extendedProps?.description,
          rev: eventToOpen.extendedProps?.rev,
          isTask: eventToOpen.extendedProps?.isTask,
          isDone: eventToOpen.extendedProps?.isDone,
          //innerData: eventToOpen.extendedProps.innerData,
        },
      };
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
          isTask: false,
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
      this.calendar.put(eventForm);
    },
  },
  components: { FullCalendar },
});
</script>
