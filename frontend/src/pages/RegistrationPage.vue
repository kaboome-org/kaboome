<template>
  <q-page>
    <div style="height: 80vh; display: flex;" class="items-center justify-center row">
        <q-card class="col-sm-6 col-10 q-pa-md">
          <div class="column justify-center">
            <q-icon name="assignment_ind" size="5em" class="q-mx-auto" color="blue-grey-4"></q-icon>
            <h1 class="text-center text-h4">Sign Up</h1>
          </div>

          <RegistrationForm />
        </q-card>
      </div>
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
