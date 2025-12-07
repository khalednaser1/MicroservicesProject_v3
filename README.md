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
