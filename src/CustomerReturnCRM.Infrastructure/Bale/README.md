# Bale integration

Bale is implemented as a messaging channel, separate from the existing SMS provider abstraction.

## Flow

1. An authenticated business user calls `POST /api/bale/businesses/{businessId}/customers/{customerId}/connect`.
2. BEMOONI creates a random, one-time, 15-minute token and stores only its SHA-256 hash.
3. The API returns a Bale deep link using `Bale:BotUsername`.
4. The link can be sent to the customer's mobile through the existing SMS service.
5. The customer opens the Bale bot and sends `/start <token>`.
6. `POST /api/bale/webhook` validates the optional webhook secret, resolves the token, and stores the customer's Bale user/chat identifiers.
7. Future messages can be sent with `IBaleService.SendMessageAsync` using the stored chat id.

## Configuration

```json
"Bale": {
  "BotToken": "",
  "BotUsername": "",
  "WebhookSecret": ""
}
```

Never commit a real bot token or webhook secret. Use environment variables, user secrets, or the production secret store.

## Database

`BaleCustomerIdentity` and `BaleConnectToken` are part of the EF model through the navigation properties on `Customer`. A database migration must be generated before deploying this branch to an environment that uses an existing database.
