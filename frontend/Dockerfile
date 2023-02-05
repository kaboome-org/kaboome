FROM node:18-alpine as build-stage
RUN yarn global add @quasar/cli
WORKDIR /app
COPY ./ .
RUN yarn install
RUN quasar build

FROM nginx:stable-alpine
RUN mkdir /app
COPY --from=build-stage /app/dist/spa /app
COPY nginx/nginx.conf /etc/nginx/nginx.conf
