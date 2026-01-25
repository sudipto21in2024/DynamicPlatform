# Running DynamicPlatform with Docker Compose

This project includes a `docker-compose.yml` file to run the entire stack (Backend API and Frontend Studio) locally using Docker.

## Prerequisites

- Docker Desktop installed and running.

## How to Run

1.  Open a terminal in the root directory of the project.
2.  Run the following command:

    ```bash
    docker-compose up --build
    ```

3.  Access the applications:
    - **Platform Studio (Frontend):** [http://localhost:4200](http://localhost:4200)
    - **Platform API (Backend):** [http://localhost:5018/swagger](http://localhost:5018/swagger)

## Structure

- **Database (`db`):** PostgreSQL 15-alpine. Data is stored in a docker volume `postgres_data` mapped to `/var/lib/postgresql/data` in the container.
- **Backend (`api`):** build from `src/Platform.API`. Automatically connects to the `db` service using the `ConnectionStrings__DefaultConnection` environment variable.
- **Frontend (`studio`):** build from `platform-studio`. Runs on container port 80 (Nginx).

## Troubleshooting

- If `localhost:4200` fails to connect to the API, ensure the API container is running and accessible at `http://localhost:5018`.
- To reset the database, you can remove the volume: `docker volume rm dynamicplatform_postgres_data`.
