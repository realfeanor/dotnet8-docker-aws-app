# Build stage
FROM node:18-bullseye AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
ARG REACT_APP_API_URL=http://localhost:5000
ENV REACT_APP_API_URL=$REACT_APP_API_URL
RUN npm run build

# Runtime stage
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
