<template>
  <q-btn v-on:click="reschedule()"> Reschedule </q-btn>
  <q-checkbox v-model="showDone">Show Done</q-checkbox>
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
          .filter((r) => r.extendedProps.isTask)
          .sort((a, b) => a.start - b.start);
      });
    },
    reorder() {
      this.calendar.events().then((res) => {
        schedule(
          this.tasks.filter((r) => !r.extendedProps.isDone),
          res.filter((r) => !r.extendedProps.isTask),
          this.calendar
        );
      });
    },
    reschedule() {
      this.calendar.events().then((res) => {
        const tasksToReschedule = res
          .filter((r) => r.extendedProps.isTask && !r.extendedProps.isDone)
          .sort((a, b) => a.start - b.start);
        schedule(
          tasksToReschedule,
          res.filter((r) => !r.extendedProps.isTask),
          this.calendar
        );
      });
    },
  },
  components: { draggable },
};
</script>
