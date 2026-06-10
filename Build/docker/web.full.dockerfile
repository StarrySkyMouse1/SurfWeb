# 须在首个 FROM 之前声明，供多阶段 FROM 使用（BuildKit 要求）
ARG NODE_IMAGE=docker.m.daocloud.io/library/node:22-alpine
ARG NGINX_IMAGE=docker.m.daocloud.io/library/nginx:1.27-alpine

FROM ${NODE_IMAGE} AS build
WORKDIR /app

COPY Web/package.json Web/package-lock.json ./
RUN npm ci

COPY Web/ ./

ARG VITE_API_BASE_URL=/api/v1
ARG VITE_SITE_TITLE=地满滑翔
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_SITE_TITLE=$VITE_SITE_TITLE

RUN npm run build

FROM ${NGINX_IMAGE} AS final

COPY Build/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
