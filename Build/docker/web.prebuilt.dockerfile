ARG NGINX_IMAGE=docker.m.daocloud.io/library/nginx:1.27-alpine
FROM ${NGINX_IMAGE}

COPY Build/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY Web/dist /usr/share/nginx/html

EXPOSE 80
