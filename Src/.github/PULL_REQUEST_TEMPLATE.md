## Summary

- What does this PR change?
- Why is this change needed?

## Related work

- Issue/Task: <!-- link or id -->
- Breaking change: `Yes/No`

## Architecture impact

- [ ] Follows layered boundaries (`Core`, `Data`, `Domain`, `Shared`, `WebApi`)
- [ ] No circular dependencies introduced
- [ ] Business logic kept in `Domain` (controllers remain thin)

## Database / EF Core changes

- [ ] No schema changes
- [ ] Schema changed and `ApplicationDbContext` updated
- [ ] Migration added
- [ ] Migration tested (`dotnet ef database update`)

If schema changed, list impacted entities/tables:

-

## API contract impact

- [ ] No API contract changes
- [ ] Request/response contract changed
- [ ] Endpoint added/modified/removed

If changed, list endpoints/contracts:

-

## Security checklist

- [ ] AuthN/AuthZ behavior reviewed
- [ ] Policy-based authorization applied where required
- [ ] No secrets committed in code/config
- [ ] OAuth/JWT settings validated
- [ ] OTP/security-sensitive data stored safely (hashed/expiring/one-time)

## Notification module checklist (if applicable)

- [ ] Uses `INotificationProcessor` abstraction
- [ ] Medium-specific logic stays in notification channel implementations
- [ ] Provider selection is configuration-driven (`GmailSmtp` / `AzureEmailService`)

## Testing and validation

- [ ] Build passes (`dotnet build`)
- [ ] Manual test completed
- [ ] Unit/integration tests added or updated (if applicable)
- [ ] Existing tests pass

Evidence (logs/screenshots/test output):

-

## Configuration changes

- [ ] No config changes
- [ ] `appsettings` updated
- [ ] Environment/secret store updates required

List required configuration keys (if any):

-

## Best practices check

- [ ] SOLID principles preserved
- [ ] Minimal and focused change set
- [ ] Naming and code style align with repository conventions
- [ ] README/docs updated when setup/behavior changed

## Deployment notes

- Any migration/runtime/dependency or rollout notes:

-
