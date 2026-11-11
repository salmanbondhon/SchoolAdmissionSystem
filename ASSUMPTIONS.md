# Assumptions – School Admission System

This document describes the assumptions and design decisions made during the development of the School Admission System.

## 1. Student Identification

- Students are identified using a unique 17-digit registration number.
- No traditional login, password, or authentication system is implemented because the assignment specifies registration-number-based identification through an HTTP header.
- The registration number is sent using the `X-Student-Id` request header for student-specific operations.
- A student can submit applications for different admission classes.
- A student cannot have more than one active or confirmed application in the same admission class.

## 2. Admission Classes

- Each grade and shift combination is treated as a separate admission class.
- For example, "Class 6 - Morning Shift" and "Class 6 - Day Shift" are separate records.
- Each admission class has a name and a fixed total seat capacity.
- Admission classes are created through the backend API.

## 3. Seat Reservation and Hold Duration

- Reserving a seat creates a temporary hold rather than immediately confirming an application.
- The default hold duration is 600 seconds (10 minutes).
- The hold duration can be configured using the `HOLD_DURATION_SECONDS` environment variable.
- Expired holds no longer occupy available seats.
- A background service checks for expired holds periodically and updates their status automatically.
- Cancelled holds release their reserved seats.

## 4. Seat Capacity and Concurrency

- Confirmed applications and active, unexpired holds count toward class seat occupancy.
- Cancelled and expired holds do not count toward occupied seats.
- PostgreSQL transactions and row-level locking are used to prevent concurrent reservation requests from exceeding the available seat capacity.
- The same PostgreSQL database is expected to be shared by backend instances when running multiple API processes.

## 5. Application Confirmation

- Only the student who owns a hold can confirm or cancel it.
- A hold must still be active and unexpired to be confirmed.
- Repeated confirmation requests for an already confirmed application return the existing confirmation and payment information rather than creating another payment record.
- Cancelled or expired holds cannot be confirmed.

## 6. Payment Processing

- Payment processing is simulated for this assignment.
- No external payment gateway or real financial transaction is integrated.
- A successful confirmation creates a payment record with a transaction reference, amount, status, and payment timestamp.
- The default simulated payment amount is 1000.
- The payment amount can be configured using the `PAYMENT_AMOUNT` environment variable.

## 7. Data Persistence

- PostgreSQL is used as the persistent database.
- Entity Framework Core manages database access and migrations.
- Pending migrations are applied automatically when the backend starts.
- Docker Compose uses a named PostgreSQL volume to preserve database data when containers are stopped or restarted.
- Removing the Docker volume permanently deletes the stored database data.

## 8. Frontend Behaviour

- The frontend uses the student's registration number to retrieve their applications.
- The registration number is saved in browser local storage to make it easier to revisit applications.
- Application information is retrieved from the backend, not stored as the authoritative application record in the browser.
- Admission class information and seat availability are refreshed periodically without requiring a full page reload.

## 9. Administration

- Admission class creation is provided through the backend API.
- The assignment does not specify an administrator authentication or authorization workflow, so a separate admin login system is not implemented.

## 10. Deployment

- Docker Compose runs the PostgreSQL database, ASP.NET Core backend, and React frontend.
- The frontend is served using Nginx.
- The backend listens on port 8080.
- The frontend is accessible on port 5173.
- The PostgreSQL database is accessible on port 5432.

