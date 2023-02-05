<template>
  <q-page>
    <SignInForm />
    <q-btn
      color="white"
      text-color="black"
      label="Register instead"
      aria-label="register instead"
      to="/sign-up"
    />
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
