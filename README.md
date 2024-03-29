# kaboome

Kaboome is a revolutionary new time blocking application that aims to improve the way people manage their time and increase productivity. Our unique approach combines gamification with a strong community aspect to help users stay motivated and on track.

You can try a frontend-only version here:
https://kaboome-org.github.io/kaboome/#/

## Installation and Configuration

Kaboome is installed from source and deployed through docker compose.
So first ensure that Docker Compose is installed. 
( For example by following https://docs.docker.com/compose/install/ )
Then clone this repository:
`git clone https://github.com/kaboome-org/kaboome.git`

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

### Deployment

1. Choose a secure password for the admin user and put it into the COUCHDB_ADMIN_PASSWORD variable
2. Decide if you want to use ssl/tls. If no jump to the last step.
3. The nginx that acts as the host for the frontend also acts as a reverse proxy for the backend and the couchdb. It handles all ssl/tls encryption in this scenario. WARNING: If you somehow host this docker compose on multiple machines you should evaluate if secrets could be leaked on transfer between the machines.
4. If you intend to use Let's encrypt certificates and an automatic certificate renew follow https://mindsers.blog/post/https-using-nginx-certbot-docker/
If you already have certificates, mount them as a volume on the frontend service and modify the frontend/nginx/nginx.conf accordingly.
5. Run `docker-compose up -d`. After some seconds try accessing the server at port 8000. http://localhost:8000/- or depending on your server_name in the nginx.conf: https://${server_name}/-

## Development

### Frontend only development

Navigate to the `./frontend` folder and execute `quasar dev`.
This should host a frontend at http://localhost:8000/-/.
If you need a different port change the `devServer.port` in `./frontend/quasar.config.js`.
Consider running `yarn test:cover` and checking the HTML coverage report in `(./frontend/)coverage/index.html`,
when working on unit tests.

### Backend only deployment

The backend needs some CouchDB server for most of its features and as it also uses hooks into the cookie authentification of CouchDB, it needs to be hosted at the same URL, so the users browser sends the cookie.
This is achieved through a reverse proxy nginx.
Both of these systems can be deployed through `docker compose -f .\docker-compose.dev.yml up`.
Now use `dotnet run` inside the `./KaboomeBackend` folder.
This should host the backend at http://localhost:5118/.
If you want to use different ports, you can configure this in `./nginx-dev/nginx.conf`.

Be sure to access the backend through the reverse proxy at http://localhost:8000/backend.

### Full dev deployment

Follow the steps above.
The development nginx is already configured to route requests to the frontend.
Change the `devServer.port` in `./frontend/quasar.config.js.` to 9000 and run `quasar dev`.
The port is left on 8000 in the repo, because we sometimes use a full deployment and at other times a frontend only deployment.
We want these two deployment types to share the same URL so we have the same data in the frontend.
