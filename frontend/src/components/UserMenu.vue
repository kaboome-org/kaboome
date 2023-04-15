<template>
  <q-menu>
    <div class="row no-wrap q-pa-md">
      <div class="column">
        <div class="text-h6 q-mb-md">Connected calendars</div>
        <GoogleCalendarManager />
      </div>

      <q-separator vertical inset class="q-mx-lg" />

      <div class="column items-center">
        <q-avatar size="90px" icon="account_circle" />

        <div class="text-subtitle1 q-mt-md q-mb-xs">{{ this.login.user }}</div>

        <q-btn color="grey-6" label="Logout" push size="md" v-on:click="logOut()" v-close-popup />
      </div>
    </div>
  </q-menu>
</template>

<script>
import { ref } from 'vue';
import GoogleCalendarManager from "components/GoogleCalendarManager.vue";
import { loginStore } from "../stores/login.js";

export default {
  components: {
    GoogleCalendarManager,
  },
  setup() {
    const login = loginStore();

    return {
      login,
      logOut: () => login.logOut(),
    }
  }
}
</script>
