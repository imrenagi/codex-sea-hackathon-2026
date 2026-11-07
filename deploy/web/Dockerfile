# The slim variant contains Nginx without the optional dynamic modules.
FROM nginx:stable-alpine-slim

ENV PORT=8080
# Limit template substitution to PORT, preserving Nginx's own variables.
ENV NGINX_ENVSUBST_FILTER=^PORT$

RUN rm -rf /usr/share/nginx/html/*
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY webgl/ /usr/share/nginx/html/

EXPOSE 8080
