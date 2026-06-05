FROM node:22-alpine AS build
WORKDIR /app

COPY Web/package.json Web/package-lock.json ./
RUN npm ci

COPY Web/ ./

ARG VITE_API_BASE_URL=/api/v1
ARG VITE_SITE_TITLE=地满滑翔
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_SITE_TITLE=$VITE_SITE_TITLE

RUN npm run build

FROM nginx:1.27-alpine AS final

COPY Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
