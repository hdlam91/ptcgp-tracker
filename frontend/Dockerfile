# The build output is plain static files, so build once on the machine doing the build instead of
# emulating every target architecture (arm64 under QEMU is many times slower). Only the small nginx
# stage below is per-platform.
FROM --platform=$BUILDPLATFORM node:22-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
