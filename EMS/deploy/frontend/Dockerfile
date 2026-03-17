FROM nginx:1.29-alpine

COPY deploy/frontend/default.conf /etc/nginx/conf.d/default.conf
COPY deploy/frontend/site /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 CMD wget -qO- http://127.0.0.1:8080/healthz || exit 1
