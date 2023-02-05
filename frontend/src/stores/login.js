import { defineStore } from "pinia";
import { useLocalStorage } from "@vueuse/core";

const isLoggedIn = "isLoggedIn";
const user = "user";
// Check cookie
fetch("/_session")
  .then((res) => {
    return res.json();
  })
  .then((j) => {
    if (j?.userCtx?.name == null) {
      localStorage.removeItem(isLoggedIn);
      localStorage.removeItem(user);
    }
  });

export const loginStore = defineStore({
  id: "login",
  state: () => {
    return {
      isLoggedIn: useLocalStorage(isLoggedIn, false),
      user: useLocalStorage(user, null),
    };
  },
  actions: {
    logIn(username, password, successCallback, failCallback) {
      fetch("/_session", {
        headers: {
          "content-type": "application/x-www-form-urlencoded;charset=UTF-8",
        },
        body: `name=${username}&password=${password}`,
        method: "POST",
      })
        .then((res) => {
          return res.json();
        })
        .then((t) => {
          if (t.ok === true) {
            this.user = username;
            this.isLoggedIn = true;
            successCallback();
          } else {
            failCallback(t.reason);
          }
        });
    },
    logOut() {
      fetch("/_session", {
        headers: {
          "content-type": "application/x-www-form-urlencoded;charset=UTF-8",
        },
        body: "username=_&password=_",
        method: "DELETE",
      });
      this.user = null;
      this.isLoggedIn = false;
    },
  },
});
