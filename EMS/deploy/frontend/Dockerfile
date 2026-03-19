FROM node:24-alpine AS build
WORKDIR /workspace/ui

COPY ui/package.json ui/package-lock.json ./
RUN npm ci

COPY ui/ ./
RUN npm run build

FROM cgr.dev/chainguard/nginx@sha256:770994a6d14bfd17a34fcf0503e13f5f7c4cf6327b0ec6636f4607cb7dd91afc

COPY EMS/deploy/frontend/nginx.conf /etc/nginx/nginx.conf
COPY EMS/deploy/frontend/default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /workspace/ui/dist /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 CMD ["/usr/bin/nginx", "-t"]
