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

        <q-toolbar-title>
          Kaboome
        </q-toolbar-title>
        <q-btn
            flat
            round
            size="md"
            class="justify-end"
            href="https://github.com/kaboome-org/kaboome/"
            target="_blank"
            title="View the source code on github"
          >
            <svg viewBox="0 0 16 16" height="18">
              <path
                fill-rule="evenodd"
                d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z"
              ></path>
            </svg>
          </q-btn>
          <q-btn
            flat
            round
            size="md"
            class="q-mr-md"
            href="https://discord.gg/rWHyX4kgPr"
            target="_blank"
            title="Visit us on our discord server"
          >
            <svg viewBox="0 0 127.14 96.36" height="18">
              <path
                d="M107.7,8.07A105.15,105.15,0,0,0,81.47,0a72.06,72.06,0,0,0-3.36,6.83A97.68,97.68,0,0,0,49,6.83,72.37,72.37,0,0,0,45.64,0,105.89,105.89,0,0,0,19.39,8.09C2.79,32.65-1.71,56.6.54,80.21h0A105.73,105.73,0,0,0,32.71,96.36,77.7,77.7,0,0,0,39.6,85.25a68.42,68.42,0,0,1-10.85-5.18c.91-.66,1.8-1.34,2.66-2a75.57,75.57,0,0,0,64.32,0c.87.71,1.76,1.39,2.66,2a68.68,68.68,0,0,1-10.87,5.19,77,77,0,0,0,6.89,11.1A105.25,105.25,0,0,0,126.6,80.22h0C129.24,52.84,122.09,29.11,107.7,8.07ZM42.45,65.69C36.18,65.69,31,60,31,53s5-12.74,11.43-12.74S54,46,53.89,53,48.84,65.69,42.45,65.69Zm42.24,0C78.41,65.69,73.25,60,73.25,53s5-12.74,11.44-12.74S96.23,46,96.12,53,91.08,65.69,84.69,65.69Z"
              />
            </svg>
          </q-btn>
        <div v-if="!this.login.isLoggedIn">
          <q-btn
            outline
            color="white"

            label="Sign up"
            aria-label="Registration"
            to="/sign-up"
            class="q-mr-md"
          />
          <q-btn
            color="white"
            text-color="primary"
            label="Sign in"
            aria-label="Sign In"
            to="/sign-in"
            class="q-mr-md"
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
        <q-btn
          flat
          dense
          round
          icon="task_alt"
          aria-label="Menu"
          @click="toggleRightDrawer"
        />
      </q-toolbar>
    </q-header>

    <q-drawer v-model="leftDrawerOpen" bordered>
      <q-list>
        <q-item-label header> Configuration </q-item-label>

        <GoogleCalendarManager v-if="this.login.isLoggedIn" />
      </q-list>
    </q-drawer>
    <q-drawer v-model="rightDrawerOpen" bordered side="right">
      <q-list>
        <q-item-label header> Tasks & Habits </q-item-label>
        <TaskList />
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
import TaskList from "src/components/TaskList.vue";

export default defineComponent({
  name: "MainLayout",

  components: {
    GoogleCalendarManager,
    TaskList,
  },
  setup() {
    const leftDrawerOpen = ref(false);
    const rightDrawerOpen = ref(false);
    const login = loginStore();

    return {
      login,
      leftDrawerOpen,
      rightDrawerOpen,
      toggleLeftDrawer() {
        leftDrawerOpen.value = !leftDrawerOpen.value;
      },
      toggleRightDrawer() {
        rightDrawerOpen.value = !rightDrawerOpen.value;
      },
      logOut: () => login.logOut(),
    };
  },
});
</script>
