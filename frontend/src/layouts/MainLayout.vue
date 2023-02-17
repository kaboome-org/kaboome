<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated>
      <q-toolbar>
        <q-btn
          flat
          dense
          round
          icon="menu"
          aria-label="Menu"
          @click="toggleLeftDrawer"
        />

        <q-toolbar-title> Kaboome </q-toolbar-title>
        <div v-if="!this.login.isLoggedIn">
          <q-btn
            color="white"
            text-color="black"
            label="Sign up"
            aria-label="Registration"
            to="/sign-up"
          />
          <q-btn
            color="white"
            text-color="black"
            label="Sign in"
            aria-label="Sign In"
            to="/sign-in"
          />
        </div>
        <div v-else>
          <span>Signed in as {{ this.login.user }}</span>
          <q-btn
            color="white"
            text-color="black"
            label="Log out"
            aria-label="Log out"
            v-on:click="logOut()"
          />
        </div>
      </q-toolbar>
    </q-header>

    <q-drawer v-model="leftDrawerOpen" bordered>
      <q-list>
        <q-item-label header> Configuration </q-item-label>

        <GoogleCalendarManager v-if="this.login.isLoggedIn" />
      </q-list>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script>
import { defineComponent, ref } from "vue";
import GoogleCalendarManager from "components/GoogleCalendarManager.vue";
import { loginStore } from "../stores/login.js";

export default defineComponent({
  name: "MainLayout",

  components: {
    GoogleCalendarManager,
  },
  setup() {
    const leftDrawerOpen = ref(false);
    const login = loginStore();

    return {
      login,
      leftDrawerOpen,
      toggleLeftDrawer() {
        leftDrawerOpen.value = !leftDrawerOpen.value;
      },
      logOut: () => login.logOut(),
    };
  },
});
</script>
