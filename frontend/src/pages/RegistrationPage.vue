<template>
  <q-page>
    <RegistrationForm />
  </q-page>
</template>

<script>
import RegistrationForm from "components/RegistrationForm.vue";
import { defineComponent } from "vue";

export default {
  components: {
    RegistrationForm,
  },
};
defineComponent({
  name: "RegistrationPage",
});
</script>
