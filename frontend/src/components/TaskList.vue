<template>
  <draggable v-model="tasks" tag="ul" handle=".handle">
    <template #item="{ element }">
      <div>
        <q-icon name="menu" class="handle"></q-icon>
        <span class="text">{{ element.title }} </span>
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
    };
  },
  mounted() {
    this.calendar.events().then((res) => {
      this.tasks = res.filter((r) => r.extendedProps.isTask);
    });
  },
  components: { draggable },
};
</script>
