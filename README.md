# School Admission System

A full-stack school admission management application built with ASP.NET Core Web API, React, PostgreSQL, and Docker. The system allows students to browse admission classes, reserve seats temporarily, confirm applications, and manage their admission records.

## Features

### Admission Management

- Create admission classes with a name and total seat capacity.
- View all available admission classes.
- View individual class details.
- Track total, confirmed, held, and available seats.
- Support separate records for each grade and shift.

### Student Applications

- Identify students using a 17-digit registration number.
- Reserve an available seat through a temporary hold.
- Confirm or cancel an application.
- View all applications associated with a student.
- Prevent duplicate active or confirmed applications in the same class.
- Restrict confirmation and cancellation to the application owner.

### Seat Management

- Configurable hold duration (default: 600 seconds).
- Automatically expire temporary holds.
- Release seats when holds expire or are cancelled.
- Protect seat capacity during concurrent requests using PostgreSQL transactions and row-level locking.
- Make application confirmation idempotent.

### Payment

- Simulated payment confirmation.
- Generate a transaction reference.
- Store payment amount, status, and payment timestamp.

### Frontend

- Responsive React interface.
- Admission class listing and seat availability.
- Student application management.
- Application status and payment information.
- Automatic seat-count refresh.
- Application data retrieval after browser refresh.

## Technology Stack

| Component         | Technology                    |
| ----------------- | ----------------------------- |
| Backend           | ASP.NET Core Web API (.NET 8) |
| Language          | C#                            |
| Database          | PostgreSQL 17                 |
| ORM               | Entity Framework Core         |
| Database Provider | Npgsql                        |
| Frontend          | React                         |
| Build Tool        | Vite                          |
| Styling           | Bootstrap                     |
| Routing           | React Router                  |
| Web Server        | Nginx                         |
| Containerization  | Docker                        |
| Orchestration     | Docker Compose                |
| Backend Testing   | xUnit, Testcontainers         |

## Project Structure

```text
SchoolAdmissionSystem/
│
├── backend/
│   ├── SchoolAdmission.API/
│   │   ├── Background/
│   │   ├── Controllers/
│   │   ├── Data/
│   │   ├── DTOs/
│   │   ├── Enums/
│   │   ├── Migrations/
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── Dockerfile
│   │   └── Program.cs
│   │
│   └── SchoolAdmission.Tests/
│
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── components/
│   │   ├── pages/
│   │   ├── services/
│   │   └── App.jsx
│   ├── Dockerfile
│   ├── nginx.conf
│   └── package.json
│
├── .env.example
├── .gitignore
├── docker-compose.yml
├── ASSUMPTIONS.md
├── AI_USAGE.md
└── README.md
```

## Requirements

Install the following software:

- Docker Desktop
- Git (for cloning the repository)

Docker Compose is included with Docker Desktop.

No separate installation of PostgreSQL, .NET SDK, or Node.js is required to run the complete application using Docker.

## Run the Application with Docker

### 1. Clone the repository

```bash
git clone PUBGLIC_ITHUB_REPOSITORY_URL
cd SchoolAdmissionSystem
```

Replace `PUBLIC_GITHUB_REPOSITORY_URL` with the actual GitHub repository URL.

### 2. Configure environment variables

Copy the example environment file:

**Windows PowerShell:**

```powershell
Copy-Item .env.example .env
```

**Linux/macOS:**

```bash
cp .env.example .env
```

Review the `.env` file and configure the database credentials if necessary.

### 3. Start the application

```bash
docker compose up --build -d
```

Docker Compose starts:

1. PostgreSQL database.
2. ASP.NET Core backend API.
3. React frontend served by Nginx.

The backend waits for PostgreSQL to become healthy and applies pending Entity Framework Core migrations during startup.

### 4. Access the application

| Service                   | URL                                                            |
| ------------------------- | -------------------------------------------------------------- |
| React Frontend            | [http://localhost:5173](http://localhost:5173)                 |
| Backend API               | [http://localhost:8080](http://localhost:8080)                 |
| Swagger API Documentation | [http://localhost:8080/swagger](http://localhost:8080/swagger) |
| PostgreSQL                | localhost:5432                                                 |

### 5. Check container status

```bash
docker compose ps
```

### 6. View application logs

```bash
docker compose logs -f
```

Backend logs:

```bash
docker compose logs -f backend
```

### 7. Stop the application

```bash
docker compose down
```

To stop the application and remove its database volume (this permanently deletes the Docker-managed database data):

```bash
docker compose down -v
```

## Environment Variables

The application supports the following configuration variables:

| Variable              | Description                  | Default                                        |
| --------------------- | ---------------------------- | ---------------------------------------------- |
| POSTGRES_DB           | PostgreSQL database name     | SchoolAdmissionDb                              |
| POSTGRES_USER         | PostgreSQL username          | schooladmin                                    |
| POSTGRES_PASSWORD     | PostgreSQL password          | SchoolDevPassword123                           |
| HOLD_DURATION_SECONDS | Temporary seat hold duration | 600                                            |
| PAYMENT_AMOUNT        | Simulated payment amount     | 1000                                           |
| VITE_API_BASE_URL     | Frontend API base URL        | [http://localhost:8080](http://localhost:8080) |

The frontend API URL is supplied during the Docker build because Vite embeds environment variables into the generated frontend bundle.

**Security note:** The default credentials are for local development and demonstration only. Change them before deploying to a public environment. Never commit your actual `.env` file.

## API Endpoints

All student-specific endpoints require the following HTTP header:

```http
X-Student-Id: 17_DIGIT_REGISTRATION_NUMBER
```

### Admission Classes

| Method | Endpoint             | Description                    |
| ------ | -------------------- | ------------------------------ |
| POST   | `/classes`           | Create an admission class      |
| GET    | `/classes`           | Retrieve all admission classes |
| GET    | `/classes/{classId}` | Retrieve a specific class      |

Example request:

```json
{
  "name": "Class 7 - Morning Shift",
  "totalSeats": 35
}
```

### Admission Holds

| Method | Endpoint                   | Description                                 |
| ------ | -------------------------- | ------------------------------------------- |
| POST   | `/classes/{classId}/holds` | Create a temporary seat hold                |
| GET    | `/holds/{holdId}`          | Retrieve hold information                   |
| DELETE | `/holds/{holdId}`          | Cancel a temporary hold                     |
| POST   | `/holds/{holdId}/confirm`  | Confirm an application                      |
| GET    | `/me/holds`                | Retrieve the current student's applications |

## Seat Capacity and Concurrency

The application uses PostgreSQL transactions and row-level locking to protect admission class capacity.

When multiple requests attempt to reserve seats simultaneously:

1. The backend starts a database transaction.
2. It locks the relevant admission class row.
3. It checks existing confirmed and active, unexpired holds.
4. It verifies available capacity.
5. It creates the hold if a seat is available.
6. It commits the transaction.

This approach prevents concurrent reservation requests from exceeding the class capacity when requests are processed against the same PostgreSQL database.

A background service periodically marks expired holds as expired, releasing their seats.

## Running Backend Tests

The backend tests use xUnit and Testcontainers.

Docker Desktop must be running because the integration tests start a PostgreSQL test container.

From the project root:

```bash
cd backend
dotnet test
```

The tests cover important scenarios, including:

- Duplicate application prevention.
- Expired hold handling.
- Concurrent reservation capacity.
- Idempotent confirmation.
- Application ownership protection.
- Cancellation and seat release.

## Frontend Development (Optional)

For frontend development without Docker:

```bash
cd frontend
npm install
```

Create a `.env` file:

```env
VITE_API_BASE_URL=http://localhost:8080
```

Start the development server:

```bash
npm run dev
```

Frontend development URL:

[http://localhost:5173](http://localhost:5173)

## Backend Development (Optional)

Install the .NET 8 SDK and ensure PostgreSQL is running.

Set the database connection string in PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=SchoolAdmissionDb;Username=schooladmin;Password=SchoolDevPassword123"
```

Run the backend:

```bash
cd backend/SchoolAdmission.API
dotnet run --urls "http://localhost:8080"
```

Swagger documentation:

[http://localhost:8080/swagger](http://localhost:8080/swagger)

## Important Notes

- Student identification uses a 17-digit registration number instead of a traditional login system.
- Payment processing is simulated; no real payment gateway is integrated.
- Admission class records must be created through the API.
- Application records are stored in PostgreSQL.
- The PostgreSQL Docker volume preserves database data when containers are stopped or restarted.
- Swagger is enabled in the Docker development configuration.

## License

This project was developed as an academic assignment.
