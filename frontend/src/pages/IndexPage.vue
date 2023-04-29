<template>
  <q-btn
    round
    color="yellow-3"
    class="fixed-bottom-right q-mr-xl q-mb-lg"
    aria-label="Add event"
    title="Add event"
    @click="openModal()"
  >
    <q-icon name="add" color="amber-9" />
  </q-btn>
  <q-page class="flex flex-center">
    <EventEditModal
      :eventToOpen="this.eventForm"
      v-model="this.eventEditDialogueOpen"
      @event-deleted="deleteEvent"
      @event-saved="saveEvent"
    >
    </EventEditModal>
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
import { useLocalStorage } from "@vueuse/core";
import FullCalendar from "@fullcalendar/vue3";
import timeGridPlugin from "@fullcalendar/timegrid";
import rrulePlugin from "@fullcalendar/rrule";
import EventEditModal from "src/components/EventEditModal.vue";
import interactionPlugin from "@fullcalendar/interaction";
import { date } from "quasar";

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
      plugins: [timeGridPlugin, interactionPlugin, rrulePlugin],
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
      eventForm: {},
      eventEditDialogueOpen: ref(false),
      login,
      calendarOptions,
      rerender: ref(false),
      zoomLevelIndex,
      zoomLevels,
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
      left: "zoomout,zoomin",
    };
    const syncTimer = setInterval(() => {
      if (instance.login.user) {
        try {
          instance.syncNow();
        } catch (error) {
          console.warn(error);
        }
      }
    }, 15000);
    function editEvent(changedEvent) {
      instance.calendar.put(changedEvent);
    }
    this.calendarOptions.eventResize = (eventResizeInfo) => {
      editEvent(eventResizeInfo.event);
    };
    this.calendarOptions.eventDrop = (eventDropInfo) => {
      editEvent(eventDropInfo.event);
    };
    this.calendarOptions.select = (selectionInfo) => {
      this.openModal({
        id: "kaboome-" + Number(new Date()),
        title: "New Event",
        start: selectionInfo.start,
        end: selectionInfo.end,
        extendedProps: {
          description: "",
          eventType: "Event",
          isDone: false,
          ReadWriteExternalEvent: {},
        },
      });
    };
    this.calendarOptions.eventClick = (arg) => {
      this.openModal(arg.event);
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
    openModal(eventToOpen) {
      if (!eventToOpen) {
        let currentDate = new Date();
        eventToOpen = {
          id: "kaboome-" + Number(new Date()),
          title: "New Event",
          start: currentDate,
          end: date.addToDate(currentDate, { minutes: 30 }),
          extendedProps: {
            description: "",
            eventType: "Event",
            isDone: false,
            ReadWriteExternalEvent: {},
          },
        };
      }

      this.eventForm = eventToOpen;
      this.eventEditDialogueOpen = true;
    },
    deleteEvent(eventForm) {
      this.calendar.delete(eventForm);
    },
    saveEvent(eventForm) {
      this.calendar.put(eventForm);
    },
    syncNow: function () {
      fetch("/backend/third-party-sync-events", {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
      });
    },
  },
  components: { FullCalendar, EventEditModal },
});
</script>
