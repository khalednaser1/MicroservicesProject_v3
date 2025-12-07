# MicroservicesProject_v2 (BFF / API Gateway) - Версия 2 (соответствует Заданию 5)

Добавлено:
- User Service + Product + Order
- Gateway: `GET /api/profile/{userId}` (агрегация)
- Redis кеш (TTL 30 секунд)
- Polly (retry + fallback)
- JWT (демо)
- Serilog
- Rate limiting (фиксированное окно)
- Метрики: `/metrics` (prometheus-net)

Запуск:
```bash
docker-compose up --build
```

Токен (пример): POST /token с телом {"username":"ivan","password":"x"} -> получить access_token

Пример вызова:
GET /api/profile/100 (с header Authorization: Bearer <token>)
