# Email Classification :>>

Before running this project, ensure you have the following installed:

- [Docker](https://www.docker.com/get-started) and Docker Compose
- [.NET SDK](https://dotnet.microsoft.com/download) (for development)
- Git

## Installation & Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd email-classification
```

### 2. Setup HTTPS Certificate

For secure HTTPS communication, generate a development certificate:

```bash
dotnet dev-certs https -ep ./certs/email-classification.pfx -p "yourpassword"
```

**Important**: Ensure the `certs` folder exists in your project root directory. If not, create it:

```bash
mkdir certs
```

### 3. Environment Configuration

Create a `.env` file in the project root with your configuration variables:

```env
# Backend Environment settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=https://+:8081
CERT_PATH=/https/email-classification.pfx
CERT_PASSWORD=yourpassword
BACKEND_PORT=
## DB
DB_HOST=
DB_PORT=
DB_NAME=
DB_USER=
DB_PASSWORD=
## Elastic
ELASTIC_URL=http://elastic:9200
ELASTIC_INDEX=
## Google Auth
GOOGLE_CLIENT_ID=
GOOGLE_PROJECT_ID=
GOOGLE_AUTH_URI=https://accounts.google.com/o/oauth2/auth
GOOGLE_ENDPOINT_API=https://www.googleapis.com/gmail/v1/users/me
GOOGLE_TOKEN_URI=https://oauth2.googleapis.com/token
GOOGLE_CERT_URL=https://www.googleapis.com/oauth2/v1/certs
GOOGLE_CLIENT_SECRET=
GOOGLE_CALLBACK_PATH=
## Jwt
JWT_ISSUER=
JWT_AUDIENCE=
JWT_KEY=
JWT_EXPIRY_MINUTES=
## Aes Encrypt
AES_KEY=
## AI API
CLASSIFICATION_API_ENDPOINT=

# ElasticSearch
ELASTICSEARCH_HOST=http://elastic:9200
ELASTIC_PORT=

# Docker image & container names
EMAIL_API_IMAGE=
EMAIL_API_CONTAINER=
POSTGRES_CONTAINER=
ELASTICSEARCH_CONTAINER=

```

### 4. Run the Application

Start the application using Docker Compose:

```bash
docker-compose up --build
```

## API Documentation

Here :>>>

[![View in Postman](https://img.shields.io/badge/View%20in-Postman-orange?logo=postman)](https://documenter.getpostman.com/view/42825667/2sB2x6kXLB)
