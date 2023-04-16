<template>
  <div class="column fit">
    <div class="col-10">
      <h5 class="q-my-none">Tasks & Habits</h5>
      <draggable
        v-model="tasks"
        tag="ul"
        handle=".handle"
        v-on:change="reorder()"
        itemKey="id"
      >
        <template #item="{ element }">
          <div v-if="!element.extendedProps.isDone || showDone">
            <q-icon name="menu" class="handle"></q-icon>
            <span class="text">
              {{ element.title }}
              <q-icon name="check" v-if="element.extendedProps.isDone"></q-icon>
            </span>
          </div>
        </template>
      </draggable>
    </div>
    <div class="col-2 column justify-end">
      <q-btn v-on:click="reschedule()" class="full-width q-mb-sm">
        Reschedule
      </q-btn>
      <q-toggle v-model="showDone" label="Show Done" />
    </div>
  </div>
</template>
<script>
import { ref } from "vue";
import draggable from "vuedraggable";
import { calendarStore } from "../stores/calendar.js";
import { schedule } from "../schedulers/simpleScheduler";

export default {
  setup() {
    const calendar = calendarStore();
    const tasks = ref([]);

    return {
      calendar,
      tasks,
      showDone: ref(false),
    };
  },
  mounted() {
    this.fetchTasks();
    this.calendar.registerChangesHandler(() => {
      this.fetchTasks();
    });
  },
  methods: {
    fetchTasks() {
      this.calendar.events().then((res) => {
        this.tasks = res
          .filter((r) => r.extendedProps.eventType === "Task")
          .sort((a, b) => a.start - b.start);
      });
    },
    reorder() {
      this.calendar.events().then((res) => {
        schedule(
          this.tasks.filter((r) => !r.extendedProps.isDone),
          res.filter((r) => r.extendedProps.eventType !== "Task"),
          this.calendar
        );
      });
    },
    reschedule() {
      this.calendar.events().then((res) => {
        const tasksToReschedule = res
          .filter(
            (r) =>
              r.extendedProps.eventType === "Task" && !r.extendedProps.isDone
          )
          .sort((a, b) => a.start - b.start);
        schedule(
          tasksToReschedule,
          res.filter((r) => r.extendedProps.eventType !== "Task"),
          this.calendar
        );
      });
    },
  },
  components: { draggable },
};
</script>
