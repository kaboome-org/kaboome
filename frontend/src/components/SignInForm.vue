<template>
  <div>
    <q-form @submit="submitForm">
      <q-input v-model="username" label="Username" type="text"></q-input>
      <q-input v-model="password" label="Password" type="password"></q-input>
      <q-btn type="submit" label="Sign in"></q-btn>
    </q-form>
  </div>
  <q-dialog v-model="alert">
    <q-card>
      <q-card-section>
        <div class="text-h6">Alert</div>
      </q-card-section>

      <q-card-section class="q-pt-none">{{ this.alertText }} </q-card-section>

      <q-card-actions align="right">
        <q-btn flat label="OK" color="primary" v-close-popup />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script>
import { ref } from "vue";
import { loginStore } from "../stores/login.js";

export default {
  setup() {
    return {
      username: ref(""),
      password: ref(""),
      alert: ref(false),
      alertText: "",
    };
  },
  methods: {
    submitForm() {
      const login = loginStore();

      login.logOut();
      login.logIn(
        this.username,
        this.password,
        () => this.$router.push("/"),
        (reason) => {
          this.alertText = reason;
          this.alert = true;
        }
      );
    },
  },
};
</script>
