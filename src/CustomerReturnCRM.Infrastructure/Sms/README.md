SMS provider integration notes

- `ISmsProvider` is the provider adapter contract.
- `ISmsProviderResolver` selects a registered provider by `SmsProviderType`.
- `SmsService` exposes provider-neutral application operations.
- `SmsSendingBackgroundService` resolves the configured provider from `Sms:Provider`.
- Development uses `Development` provider.
- Production can use `SmsIr` after credentials are configured through secrets/environment variables.
- Adding another provider only requires a new `ISmsProvider` implementation and registration; existing business logic remains unchanged.
