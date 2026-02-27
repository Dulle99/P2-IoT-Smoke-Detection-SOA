FROM node:20-alpine

WORKDIR /app

COPY package*.json ./
RUN npm ci --omit=dev

COPY src ./src

ENV PORT=5001
EXPOSE 5001

CMD ["node", "src/index.js"]