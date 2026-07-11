# Caddy + web SPA imajı (context: repo kökü — pnpm workspace gerekir).
# Aşama 1: web'i derle (packages/shared workspace bağımlılığıyla).
# Aşama 2: caddy:2-alpine + /srv altına statik build. Caddyfile compose'ta ayrıca
# volume ile bağlanır (canlı düzenleme); imajdaki kopya volume'suz çalışma içindir.

FROM node:22-alpine AS webbuild
WORKDIR /repo
# packageManager alanıyla birebir aynı sürüm (kök package.json: pnpm@11.5.0).
RUN npm install -g pnpm@11.5.0

# Önce manifestler (katman cache verimi): kök + workspace paketleri.
COPY pnpm-workspace.yaml pnpm-lock.yaml package.json ./
COPY packages/shared/package.json packages/shared/package.json
COPY web/package.json web/package.json
RUN pnpm install --frozen-lockfile --filter @finans/web...

COPY packages/shared packages/shared
COPY web web
RUN pnpm --filter @finans/web build

FROM caddy:2-alpine
COPY compose/caddy/Caddyfile /etc/caddy/Caddyfile
COPY --from=webbuild /repo/web/dist /srv
