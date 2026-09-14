# ---------- Dependencies ----------
FROM node:20-alpine AS deps

WORKDIR /app

COPY src/CustomerReturnCRM.Web/package*.json ./

RUN npm ci


# ---------- Build ----------
FROM node:20-alpine AS build

WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY src/CustomerReturnCRM.Web/ ./

ENV NEXT_PUBLIC_API_BASE_URL=http://localhost:8080

RUN npm run build


# ---------- Runtime ----------
FROM node:20-alpine AS runtime

WORKDIR /app

ENV NODE_ENV=production
ENV HOSTNAME=0.0.0.0
ENV PORT=3000

COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static

EXPOSE 3000

CMD ["node", "server.js"]
