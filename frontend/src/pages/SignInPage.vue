<template>
  <q-page>
    <div
      style="height: 80vh; display: flex"
      class="items-center justify-center row"
    >
      <q-card class="col-sm-6 col-10 q-pa-md">
        <div class="column justify-center">
          <q-icon
            name="shield"
            size="5em"
            class="q-mx-auto"
            color="blue-grey-4"
          ></q-icon>
          <h1 class="text-center text-h4">Sign In</h1>
        </div>

        <SignInForm />
        <q-btn
          color="white"
          text-color="black"
          class="q-mt-md full-width"
          label="Register instead"
          aria-label="register instead"
          to="/sign-up"
        />
      </q-card>
    </div>
  </q-page>
</template>

<script>
import SignInForm from "src/components/SignInForm.vue";
import { defineComponent } from "vue";

export default {
  components: {
    SignInForm,
  },
};
defineComponent({
  name: "SignInPage",
});
</script>
