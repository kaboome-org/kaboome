# kaboome

Kaboome is a revolutionary new time blocking application that aims to improve the way people manage their time and increase productivity. Our unique approach combines gamification with a strong community aspect to help users stay motivated and on track.

You can try a frontend-only version here:
https://kaboome-org.github.io/kaboome/#/

## Installation and Configuration

### Preparation: Google Project

1. If you don't have a Google cloud Project already follow the steps in: https://console.cloud.google.com/projectcreate
2. Now go to https://console.cloud.google.com/apis/credentials/consent and configure a consent screen. 
  1. Add your Google Account(s) as Test User(s) 
3. On https://console.cloud.google.com/apis/credentials/oauthclient create a new credential
  1. Application Type: Web Application
  2. Name: Choose your own :) 
  3. Authorized redirect URL: http(s)://localhost:{Port you would like to deploy on}/google-auth Example: http://localhost:8000/backend/google-auth
  4. Download the Json (client_secret_...json) and rename it to client_secret.json and move it into the KaboomeBackend folder
4. On https://console.developers.google.com/apis/api/calendar-json.googleapis.com/overview enable the Google Calendar API for the project.
