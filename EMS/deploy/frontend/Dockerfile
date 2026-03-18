FROM cgr.dev/chainguard/nginx:latest

COPY deploy/frontend/nginx.conf /etc/nginx/nginx.conf
COPY deploy/frontend/default.conf /etc/nginx/conf.d/default.conf
COPY deploy/frontend/site /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 CMD ["/usr/bin/nginx", "-t"]
