<template>
  <q-popup-proxy>
    <div class="row no-wrap q-pa-md user-menu">
      <div class="column col-8">
        <div class="text-h6 q-mb-md">Connected calendars</div>
        <GoogleCalendarManager />
      </div>
      <div class="column items-center col-3 offset-1 q-pl-md">
        <q-separator vertical class="full-height" />
        <q-avatar size="90px" icon="account_circle" />

        <div class="text-subtitle1 q-mt-md q-mb-xs">{{ this.login.user }}</div>

        <q-btn color="grey-6" label="Logout" push size="md" v-on:click="logOut()" v-close-popup />
      </div>
    </div>
  </q-popup-proxy>
</template>

<style lang="scss">
  .user-menu{
    width: 90vw;
    @media (min-width: $breakpoint-sm-min){
      width: 80vw;
    }
    @media (min-width: $breakpoint-md-min){
      width: 60vw;
    }
    @media (min-width: $breakpoint-lg-min){
      width: 40vw;
    }
  }
</style>

<script>
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
