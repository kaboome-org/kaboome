<template>
  <q-dialog no-backdrop-dismiss>
    <div style="width: 100%; min-width: 200px; max-width: 400px">
      <q-card>
        <q-toolbar class="bg-primary text-white">
          <q-toolbar-title> Edit recurring event </q-toolbar-title>
        </q-toolbar>
        <q-card-section>
          <q-btn
            label="This event"
            @click="this.thisEvent()"
            color="primary"
            v-close-popup
          />
        </q-card-section>
        <q-card-section>
          <q-btn
            label="This and following events"
            color="primary"
            @click="this.thisAndFollowingEvents()"
            v-close-popup
          />
        </q-card-section>
        <q-card-section>
          <q-btn
            label="All events"
            color="primary"
            v-close-popup
            @click="this.allEvents()"
          />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn
            flat
            label="Cancel"
            color="primary"
            @click="this.cancel()"
            v-close-popup
          />
        </q-card-actions>
      </q-card>
    </div>
  </q-dialog>
</template>
<script>
import { RRule } from "rrule";

export default {
  name: "RecurrinEventsEditModal",
  props: ["previousEvent", "proposedChangedEvent"],
  emits: ["eventSaved", "eventDeleted", "cancel"],
  methods: {
    thisEvent() {
      // previousEvent exRule
      const rrule = RRule.fromString(this.previousEvent.extendedProps.rrule);
      const excludeDate = this.previousEvent.start; // The date of the occurrence you want to exclude

      const previousEventCopy = JSON.parse(JSON.stringify(this.previousEvent));
      const exdates = previousEventCopy.extendedProps.exdates;
      exdates.push(excludeDate);
      previousEventCopy.start = rrule.options.dtstart;
      previousEventCopy.end = new Date(
        rrule.options.dtstart.getTime() +
          (this.previousEvent.end - this.previousEvent.start)
      );
      previousEventCopy.extendedProps.exdates = exdates;
      this.$emit("eventSaved", previousEventCopy);
      // new non rrule event
      if (!this.proposedChangedEvent.deleted) {
        const proposedChangedEventCopy = JSON.parse(
          JSON.stringify(this.proposedChangedEvent)
        );
        proposedChangedEventCopy.start = new Date(
          proposedChangedEventCopy.start
        );
        proposedChangedEventCopy.end = new Date(proposedChangedEventCopy.end);
        if (proposedChangedEventCopy.id.startsWith("kaboome")) {
          proposedChangedEventCopy.id = "kaboome-" + Number(new Date());
        } else if (proposedChangedEventCopy.id.startsWith("google")) {
          proposedChangedEventCopy.id =
            proposedChangedEventCopy.id + Number(new Date());
          proposedChangedEventCopy.extendedProps.ReadWriteExternalEvent.Google.id =
            null;
        }
        proposedChangedEventCopy.extendedProps.rrule = null;
        proposedChangedEventCopy.extendedProps.rev = null;
        this.$emit("eventSaved", proposedChangedEventCopy);
      }
    },
    thisAndFollowingEvents() {
      // previousEvent end rrule
      const rrule = RRule.fromString(this.previousEvent.extendedProps.rrule);
      const updatedRRule = new RRule({
        ...rrule.options,
        until: new Date(this.previousEvent.start - 1000),
      }).toString();
      const previousEventCopy = JSON.parse(JSON.stringify(this.previousEvent));
      previousEventCopy.start = rrule.options.dtstart;
      previousEventCopy.end = new Date(
        rrule.options.dtstart.getTime() +
          (this.previousEvent.end - this.previousEvent.start)
      );
      previousEventCopy.extendedProps.rrule = updatedRRule;
      this.$emit("eventSaved", previousEventCopy);
      // Start new rrule event
      if (!this.proposedChangedEvent.deleted) {
        const proposedChangedEventCopy = JSON.parse(
          JSON.stringify(this.proposedChangedEvent)
        );
        proposedChangedEventCopy.start = new Date(
          proposedChangedEventCopy.start
        );
        proposedChangedEventCopy.end = new Date(proposedChangedEventCopy.end);
        if (proposedChangedEventCopy.id.startsWith("kaboome")) {
          proposedChangedEventCopy.id = "kaboome-" + Number(new Date());
        } else if (proposedChangedEventCopy.id.startsWith("google")) {
          proposedChangedEventCopy.id =
            proposedChangedEventCopy.id + Number(new Date());
          proposedChangedEventCopy.extendedProps.ReadWriteExternalEvent.Google.id =
            null;
        }
        proposedChangedEventCopy.extendedProps.rev = null;
        this.$emit("eventSaved", proposedChangedEventCopy);
      }
    },
    allEvents() {
      if (this.proposedChangedEvent.deleted) {
        this.$emit("eventDeleted", this.proposedChangedEvent);
      } else {
        this.$emit("eventSaved", this.proposedChangedEvent);
      }
    },
    cancel() {
      this.$emit("cancel");
    },
  },
  components: {},
};
</script>
