# Serverless Order API

Безсерверне API для управління замовленнями, розгорнуте в AWS.

## Технології та сервіси
Бекенд: ASP.NET Core MVC / AWS Lambda
Маршрутизація: Amazon API Gateway
Сховище даних: Amazon S3 (збереження у `.json`)
Черга повідомлень: Amazon SQS

## Робоче API (Invoke URL)
 `https://0gl409ri5b.execute-api.eu-central-1.amazonaws.com`

## Ендпоінти
Створити замовлення: `POST /api/orders`
Тіло: `{"customerName": "Ім'я", "product": "Товар", "quantity": 1}`
Отримати всі: `GET /api/orders`
Знайти за ID: `GET /api/orders/{id}`

Скріншоти виконання і сервісів в окремій папці проекту.

