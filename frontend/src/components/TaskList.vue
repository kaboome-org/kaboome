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
        this.schedule(
          this.tasks.filter((r) => !r.extendedProps.isDone),
          res.filter((r) => !r.extendedProps.isTask)
        );
      });
    },
    reschedule() {
      this.calendar.events().then((res) => {
        const tasksToReschedule = res
          .filter((r) => r.extendedProps.isTask && !r.extendedProps.isDone)
          .sort((a, b) => a.start - b.start);
        this.schedule(
          tasksToReschedule,
          res.filter((r) => !r.extendedProps.isTask)
        );
      });
    },
    schedule(tasksToReschedule, blockers) {
      const minuteRounder = (t) => {
        const fiveMinutes = 5 * 60 * 1000;
        return t - (t % fiveMinutes) + fiveMinutes;
      };
      let potentialStart = minuteRounder(new Date());
      blockers.sort((a, b) => a.start - b.start);
      let blockerindex = 0;
      tasksToReschedule.forEach((task) => {
        const taskDuration = task.end - task.start;
        while (
          blockerindex < blockers.length &&
          (blockers[blockerindex].end < potentialStart ||
            blockers[blockerindex].start - potentialStart < taskDuration)
        ) {
          if (
            blockerindex < blockers.length &&
            ((blockers[blockerindex].end >= potentialStart &&
              blockers[blockerindex].start <= potentialStart) ||
              (blockers[blockerindex].end >= potentialStart + taskDuration &&
                blockers[blockerindex].start <= potentialStart + taskDuration))
          ) {
            potentialStart = minuteRounder(blockers[blockerindex].end);
          }
          blockerindex++;
        }
        task.start = potentialStart;
        task.end = potentialStart + taskDuration;
        potentialStart = minuteRounder(task.end);
        this.calendar.put(task);
      });
    },
  },
  components: { draggable },
};
</script>
