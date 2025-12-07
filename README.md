# MicroservicesProject_v3 (BFF / API Gateway)

This version upgrades the demo to match the course assignment.
Features added:
- User Service + Product + Order services
- Gateway implements aggregation endpoint: GET /api/profile/{userId}
- Redis caching (TTL 30 seconds)
- Polly retry with exponential backoff + fallback handling
- JWT authentication (simple symmetric key for demo)
- Serilog logging
- Basic Rate Limiting on gateway (fixed window)
- Metrics endpoint (/metrics) provided by prometheus-net for each service

## How to run
Requirements: Docker & Docker Compose.

From the project root:
```bash
docker-compose up --build
```

Services:
- Gateway: http://localhost:6000
  - Token endpoint (demo): POST http://localhost:6000/token with JSON { "username":"ivan", "password":"x" } -> returns access_token
  - Aggregation: GET http://localhost:6000/api/profile/100  (requires Authorization: Bearer <token>)
- Product: http://localhost:6001/api/products
- Order: http://localhost:6002/api/orders?userId=100
- User: http://localhost:6003/api/users/100

Example flow:
1. Get token:
   ```bash
   curl -X POST http://localhost:6000/token -H "Content-Type: application/json" -d '{"username":"ivan","password":"x"}'
   ```
2. Use token:
   ```bash
   curl -H "Authorization: Bearer <token>" http://localhost:6000/api/profile/100
   ```

## What to present to professor
- Show `docker-compose.yml` and that each service is a separate container.
- Explain aggregation: gateway queries user-service, order-service, product-service and composes a single JSON result.
- Mention caching TTL=30s, retry policy (3 retries, exponential backoff), Serilog logging, Rate limiting.
- Show /metrics endpoints if requested.

If you want, I can:
- Prepare a short Russian README for the lab submission.
- Prepare a PowerPoint slide for the presentation.
- Add Swagger UI to all services.

