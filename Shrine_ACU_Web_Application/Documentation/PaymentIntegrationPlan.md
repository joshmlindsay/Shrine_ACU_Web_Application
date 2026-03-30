# Payment Integration Plan (Blazor + Azure + API)

## Current capability in this workspace

This workspace now includes:

- Registration payment preference capture in `FetchSignup.razor`.
- Manual payment update flow for managers/admins.
- Hosted checkout session launch for `PayOnlineNow`.
- `PaymentReturn` status page with retry support.
- `Profile`-based `Pay Now` action for unpaid entries.

The backend now exposes dedicated payment fields and payment endpoints, so payment metadata is no longer limited to `CarShowEntryDto.Notes`.

## Requested payment behavior

- Optional payment during registration.
- Allow payment at registration time, later, or in person at event.
- Support preference for Cash App Pay, Venmo, Apple Pay, and Google Pay.
- Allow event manager/admin to manually mark proof-of-payment.

## Recommended provider strategy

A single provider does not always provide complete coverage for all payment methods in all regions/accounts.

Recommended pattern:

1. Use Stripe as primary for:
   - Apple Pay
   - Google Pay
   - Cash App Pay (account/region dependent)
2. Add Braintree for Venmo if Venmo coverage is required and enabled on your account.

## Azure role in this architecture

Azure hosts and secures your apps; payment processing remains with payment providers.

Use Azure for:

- `Azure App Service` (web + API hosting)
- `Azure Key Vault` (provider secrets/webhook signing secrets)
- `Azure Application Insights` (audit + diagnostics)
- `Azure Storage Queue` / `Azure Service Bus` (optional async webhook processing)
- `Azure SQL` (payment records)

## API contract (implemented backend)

### Payment fields on entry

- `PaymentStatus` (`Pending`, `Paid`, `Waived`, `Refunded`, ...)
- `PaymentTiming` (`PayOnlineNow`, `PayLater`, `PayAtEvent`)
- `PreferredPaymentMethod` (`CashAppPay`, `Venmo`, `ApplePay`, `GooglePay`, ...)
- `PaymentProofReference`
- `PaidAmount`
- `PaidAtUtc`
- `PaidByUserId`
- `PaymentProvider`
- `PaymentProviderSessionId`
- `PaymentProviderIntentId`

### Endpoints

1. `POST /api/CarShowEntries/{entryId}/payment/session`
   - Creates hosted checkout session.
2. `PUT /api/CarShowEntries/{entryId}/payment/manual`
   - Manual manager/admin payment update.
3. `GET /api/CarShowEntries/{entryId}/payment`
   - Payment status/details for entry.
4. `POST /api/payments/webhooks/{provider}`
   - Provider webhook ingestion and processing.

## Frontend flow (Blazor)

### Registration (`FetchSignup`)

1. User selects timing and preferred method.
2. App creates entry.
3. If `PayOnlineNow`, app creates payment session.
4. App redirects user to provider-hosted checkout.

### Return (`PaymentReturn`)

1. User returns to `/payment-return?entryId={id}&result=success|cancel`.
2. App loads payment status via `GET .../payment`.
3. If still pending, user can refresh or retry payment.

### Later payment (`Profile`)

1. User opens Event History.
2. Unpaid entries expose `Pay Now`.
3. App launches hosted checkout via payment session endpoint.

### Manual/in-person payment (`FetchCarShows`)

1. Manager opens manual payment dialog.
2. App submits `PUT .../payment/manual`.
3. Entry payment state is updated and reflected in UI.

## Security and compliance checklist

- Never store raw card data in app or API.
- Use hosted checkout from providers.
- Verify webhook signatures.
- Make webhook processing idempotent (dedupe by provider event ID).
- Enforce role-based authorization for manual payment updates.
- Audit log all payment status transitions.
- Validate `successUrl`/`cancelUrl` host whitelist on session creation.







## Expanded setup instructions (Provider, Secrets, Return URLs, Azure + Key Vault)

## 1) Provider selection/account

For this solution:

- `CashAppPay`, `ApplePay`, `GooglePay` -> Stripe
- `Venmo` -> Braintree

Recommended rollout:

1. Create Stripe account (test + live).
2. Enable Stripe methods:
   - Cards
   - Apple Pay
   - Google Pay
   - Cash App Pay (if supported)
3. Create Braintree account and enable Venmo.
4. Confirm method availability for your business type and geography.

## 2) Secrets you need

### Stripe

- Secret key (`sk_test_...`, later `sk_live_...`)
- Webhook signing secret (`whsec_...`)

### Braintree

- Merchant ID
- Public key
- Private key
- Webhook signature/verification values

Do not store secrets directly in source-controlled files.

## 3) Configured return URLs

Frontend expects:

- Success:
  - `https://<web-host>/payment-return?entryId={ENTRY_ID}&result=success`
- Cancel:
  - `https://<web-host>/payment-return?entryId={ENTRY_ID}&result=cancel`

Notes:

- Use deployed HTTPS hostnames only.
- Register production hostnames in provider dashboard before go-live.
- Session creation should only allow trusted return URL hosts.

## 4) Azure App Settings + Key Vault references

### Step A: Create Key Vault secrets

- `Stripe--SecretKey`
- `Stripe--WebhookSecret`
- `Braintree--MerchantId`
- `Braintree--PublicKey`
- `Braintree--PrivateKey`
- `Braintree--WebhookSecret`

### Step B: Configure API App Service settings

Use Key Vault references:

- `Stripe__SecretKey = @Microsoft.KeyVault(SecretUri=...)`
- `Stripe__WebhookSecret = @Microsoft.KeyVault(SecretUri=...)`
- `Braintree__MerchantId = @Microsoft.KeyVault(SecretUri=...)`
- `Braintree__PublicKey = @Microsoft.KeyVault(SecretUri=...)`
- `Braintree__PrivateKey = @Microsoft.KeyVault(SecretUri=...)`
- `Braintree__WebhookSecret = @Microsoft.KeyVault(SecretUri=...)`

### Step C: Identity and access

1. Enable system-assigned managed identity on API App Service.
2. Grant that identity `Get` access to Key Vault secrets.
3. Restart API App Service.
4. Verify effective configuration via API startup logs/health checks.

## 5) Minimal recommended API configuration schema

- `Payments__DefaultProvider` (example: `Stripe`)
- `Payments__AllowedReturnUrlHosts` (comma-separated)
- `Stripe__SecretKey`
- `Stripe__WebhookSecret`
- `Braintree__MerchantId`
- `Braintree__PublicKey`
- `Braintree__PrivateKey`
- `Braintree__WebhookSecret`

`Payments__AllowedReturnUrlHosts` should be used to validate session `SuccessUrl` and `CancelUrl`.

## 6) Webhook setup (critical)

### Stripe

- Endpoint: `POST /api/payments/webhooks/stripe`
- Subscribe to at least:
  - `checkout.session.completed`
  - `payment_intent.succeeded`
- Optional:
  - failure/refund/dispute events

### Braintree

- Endpoint: `POST /api/payments/webhooks/braintree`
- Subscribe to:
  - successful transaction events
  - dispute/refund events (as needed)

### Processing rules

- Verify webhook signature on every request.
- Deduplicate provider event IDs.
- Persist event IDs + timestamps for audit.
- Apply safe state transitions (`Pending` -> `Paid`, etc.).

## 7) Go-live sequence

1. Validate complete end-to-end test-mode flow.
2. Confirm `PaymentReturn` status updates correctly.
3. Confirm manager manual payment flow still works.
4. Swap test secrets to live secrets in Key Vault.
5. Update production webhook endpoints in provider dashboards.
6. Run one low-value live transaction and confirm reconciliation.

## Incremental rollout status

1. **Phase 1**: Completed.
2. **Phase 2**: Completed.
3. **Phase 3**: Completed (hosted checkout + return/retry flow).
4. **Phase 4**: In progress.
   - Completed in this workspace:
     - Manager payment operations dashboard (`FetchPayments`) with filters and reconciliation snapshot.
     - Payment navigation entry for manager roles.
   - Remaining (recommended):
     - Export/report artifacts (CSV/PDF as needed).
     - Scheduled reconciliation jobs and exception queues.
     - Provider settlement matching and accounting handoff automation.

## 8) Ops runbook (common issues + troubleshooting)

### A. Payment remains `Pending` after checkout success

**Symptoms**

- User completes provider checkout and returns to `PaymentReturn`.
- `PaymentStatus` remains `Pending`.

**Checks**

1. Confirm webhook endpoint was called by provider dashboard logs.
2. Confirm API returned `2xx` to webhook request.
3. Confirm webhook signature validation passed.
4. Confirm webhook event maps to correct `entryId`.
5. Confirm idempotency logic did not incorrectly suppress a first-time event.

**Actions**

- Retry status load in `PaymentReturn` (already implemented).
- Re-send webhook event from provider dashboard.
- If needed, manager/admin can apply manual payment update with documented proof.

---

### B. Session creation fails (`POST .../payment/session`)

**Symptoms**

- UI shows unable to start payment checkout.
- Checkout URL is missing in response.

**Checks**

1. Verify provider credentials resolved from Key Vault.
2. Verify `Payments__AllowedReturnUrlHosts` includes current host.
3. Verify `Amount` is > 0 and method/provider mapping is valid.
4. Verify provider account has requested method enabled.

**Actions**

- Correct config and restart API App Service.
- Retry from `Profile` (`Pay Now`) or `PaymentReturn` (`Retry Payment`).

---

### C. Webhook signature verification fails

**Symptoms**

- Webhook endpoint receives requests but rejects as unauthorized/invalid signature.

**Checks**

1. Confirm correct webhook signing secret in Key Vault.
2. Confirm App Service setting points to current secret version.
3. Confirm no payload mutation before signature verification.

**Actions**

- Rotate and reapply webhook secret.
- Re-run provider webhook test event.

---

### D. Venmo unavailable in checkout

**Symptoms**

- Venmo selected but unavailable at provider checkout.

**Checks**

1. Confirm Braintree Venmo feature enabled for account.
2. Confirm region/currency/device support.
3. Confirm environment mode alignment (test vs live).

**Actions**

- Fallback to other enabled methods.
- Keep provider/method eligibility matrix documented for support.

---

### E. Manual payment mismatch during reconciliation

**Symptoms**

- Amount/proof in manual update does not match expected entry fee.

**Checks**

1. Compare `PaidAmount`, `PaymentProofReference`, `PaidByUserId`, `PaidAtUtc`.
2. Verify manager/admin identity and audit trail.

**Actions**

- Require correction update by authorized manager/admin.
- Add internal note with discrepancy reason.

---

### F. Recommended operational dashboards/alerts

Track in Application Insights and operational dashboards:

- Count of payment session creation failures.
- Webhook failure rate by provider.
- `Pending` payments older than threshold (e.g., 15 minutes).
- Manual payment updates count and user distribution.
- Provider response latency and error distribution.

Set alerts for:

- Sudden webhook failure spikes.
- Repeated signature validation failures.
- High volume of stale `Pending` payments.

---

### G. Support response checklist (quick)

1. Capture `entryId`, user ID, timestamp, and reported method.
2. Check payment record (`GET .../payment`) and current status.
3. Check webhook/provider logs for matching transaction/event.
4. Decide: retry checkout, re-send webhook, or manual update.
5. Document resolution and retain audit references.