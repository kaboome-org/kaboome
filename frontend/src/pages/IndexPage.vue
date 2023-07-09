<template>
  <q-btn
    round
    color="yellow-3"
    class="fixed-bottom-right q-mr-xl q-mb-lg"
    aria-label="Add event"
    title="Add event"
    @click="openEventEditModal()"
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
    <GotoDateModal
      v-model="this.gotoDateModalOpen"
      @dateSelected="gotoDate"
    ></GotoDateModal>
    <RecurringEventsEditModal
      :previousEvent="this.previousEvent"
      :proposedChangedEvent="this.proposedChangedEvent"
      v-model="this.recurringEventsEditModalOpen"
      @event-deleted="this.calendar.delete"
      @event-saved="this.calendar.put"
      @cancel="this.$refs.fullcalendar.calendar.refetchEvents()"
    ></RecurringEventsEditModal>
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
import { configStore } from "../stores/config";
import { backendStore } from "../stores/backend";
import { useLocalStorage } from "@vueuse/core";
import FullCalendar from "@fullcalendar/vue3";
import timeGridPlugin from "@fullcalendar/timegrid";
import rrulePlugin from "@fullcalendar/rrule";
import EventEditModal from "src/components/EventEditModal.vue";
import GotoDateModal from "src/components/GotoDateModal.vue";
import RecurringEventsEditModal from "src/components/RecurringEventsEditModal.vue";
import interactionPlugin from "@fullcalendar/interaction";
import { date } from "quasar";
import { writeOnlyEvents } from "../helpers/writeOnlyEventsGenerator";

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
    const config = configStore();
    config.activateSync(login.user);
    const gaccs = [];
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
      googleAccounts: ref(gaccs),
      config,
      eventForm: {},
      previousEvent: {},
      proposedChangedEvent: {},
      eventEditDialogueOpen: ref(false),
      recurringEventsEditModalOpen: ref(false),
      gotoDateModalOpen: ref(false),
      login,
      calendarOptions,
      rerender: ref(false),
      zoomLevelIndex,
      zoomLevels,
      backend: backendStore(),
    };
  },
  mounted() {
    const instance = this;
    this.calendar.registerChangesHandler(() => {
      instance.$refs.fullcalendar.calendar.refetchEvents();
      setTimeout(() => {
        instance.backend.syncNow();
      }, 500);
    });
    const title =
      instance.$refs.fullcalendar.calendar.el.getElementsByClassName(
        "fc-toolbar-title"
      )[0];
    title.style.cursor = "pointer";
    title.onclick = () => {
      instance.gotoDateModalOpen = true;
    };
  },
  created() {
    const instance = this;
    const googleAccountLoader = () => {
      instance.config.loadGoogleAccounts().then((res) => {
        const previouslyEmpty =
          instance.googleAccounts == null ||
          instance.googleAccounts.length == 0;
        instance.googleAccounts = res;
        if (previouslyEmpty && res.length > 0) {
          instance.$refs.fullcalendar.calendar.refetchEvents();
        }
      });
    };
    this.config.registerChangesHandler(googleAccountLoader);
    googleAccountLoader();
    this.calendarOptions.events = async function (
      info,
      successCallback,
      failureCallback
    ) {
      successCallback(await instance.calendar.events(instance.googleAccounts));
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
    this.backend.syncNow();
    const syncTimer = setInterval(() => {
      if (instance.login.user) {
        try {
          instance.backend.syncNow();
        } catch (error) {
          console.warn(error);
        }
      }
    }, 15000);
    function editEvent(changedEvent) {
      instance.calendar.put(changedEvent);
    }
    this.calendarOptions.eventResize = (eventResizeInfo) => {
      if (eventResizeInfo.event.extendedProps?.rrule) {
        this.openRecurringEventsEditModal(
          eventResizeInfo.event,
          eventResizeInfo.oldEvent
        );
      } else {
        editEvent(eventResizeInfo.event);
      }
    };
    this.calendarOptions.eventDrop = (eventDropInfo) => {
      if (eventDropInfo.event.extendedProps?.rrule) {
        this.openRecurringEventsEditModal(
          eventDropInfo.event,
          eventDropInfo.oldEvent
        );
      } else {
        editEvent(eventDropInfo.event);
      }
    };
    this.calendarOptions.select = (selectionInfo) => {
      this.openEventEditModal({
        id: "kaboome-" + Number(new Date()),
        title: "New Event",
        start: selectionInfo.start,
        end: selectionInfo.end,
        extendedProps: {
          description: "",
          eventType: "Event",
          isDone: false,
          ReadWriteExternalEvent: {},
          WriteOnlyExternalEvents: writeOnlyEvents(
            {
              Vendor: "kaboome",
              VendorCalendarPathJson: "null",
            },
            this.googleAccounts
          ),
        },
      });
    };
    this.calendarOptions.eventClick = (arg) => {
      this.openEventEditModal(arg.event);
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
    openEventEditModal(eventToOpen) {
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
            WriteOnlyExternalEvents: writeOnlyEvents(
              {
                Vendor: "kaboome",
                VendorCalendarPathJson: "null",
              },
              this.googleAccounts
            ),
          },
        };
      }
      this.eventForm = eventToOpen;
      this.eventEditDialogueOpen = true;
    },
    openRecurringEventsEditModal(proposedChangedEvent, previousEvent) {
      this.proposedChangedEvent = proposedChangedEvent;
      this.previousEvent = previousEvent;
      this.recurringEventsEditModalOpen = true;
    },
    deleteEvent(eventForm) {
      if (eventForm.extendedProps?.rrule) {
        eventForm.deleted = true;
        this.openRecurringEventsEditModal(eventForm, this.eventForm);
      } else {
        this.calendar.delete(eventForm);
      }
    },
    saveEvent(eventForm) {
      if (
        eventForm.extendedProps?.rrule &&
        eventForm.extendedProps.rev &&
        this.eventForm.extendedProps?.rrule
      ) {
        this.openRecurringEventsEditModal(eventForm, this.eventForm);
      } else {
        this.calendar.put(eventForm);
      }
    },
    gotoDate: function (start) {
      this.$refs.fullcalendar.calendar.gotoDate(start.replaceAll("/", "-"));
    },
  },
  components: {
    FullCalendar,
    EventEditModal,
    RecurringEventsEditModal,
    GotoDateModal,
  },
});
</script>
