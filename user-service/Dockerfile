# Base image
FROM node:22.3.0 AS base
WORKDIR /usr/src/app

# Install dependencies
FROM base AS deps
# Copy package.json and package-lock.json first to leverage Docker cache
COPY package.json package-lock.json ./

# Install dependencies including Prisma
RUN npm ci

# Copy Prisma files if Prisma is used
COPY prisma ./prisma/

# Copy tsconfig.json and other necessary config files
COPY tsconfig.json ./

# Copy the .env file (Ensure it's in the context of the build directory)
COPY .env .env

# Generate Prisma client
RUN npx prisma generate

# Build the application
FROM deps AS build
# Copy all source files, including tsconfig.json
COPY . .

# Build the NestJS application
RUN npm run build

# Final runtime image
FROM node:22.3.0 AS final
WORKDIR /usr/src/app
ENV NODE_ENV=production

# Copy necessary files from previous stages
COPY --from=deps /usr/src/app/node_modules ./node_modules
COPY --from=build /usr/src/app/dist ./dist
COPY package.json package-lock.json ./
COPY prisma ./prisma/
COPY tsconfig.json ./
COPY .env .env

# Expose the application port
EXPOSE 1270

# Start the application
CMD ["npm", "start"]
