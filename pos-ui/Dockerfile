FROM node:25-alpine
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
EXPOSE 4200
# --poll 2000 is needed for hot-reload to work reliably on Windows Docker Desktop
CMD ["npm", "start", "--", "--host", "0.0.0.0", "--poll", "2000"]
