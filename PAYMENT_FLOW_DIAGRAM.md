# Payment Flow Diagram

## New User Journey

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Registration                        │
│                     (/fetch-signup)                              │
│                                                                   │
│  1. Select Car Show                                              │
│  2. Enter Personal Info (Name, Email, Phone)                     │
│  3. Enter Car Info (Year, Make, Model, VIN, Color)               │
│  4. Click "Register Entry"                                       │
│                                                                   │
│  ✓ Entry Created (Status: Pending, Payment: Pending)             │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ Auto Redirect
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Payment Page                                │
│                  (/payment/{EntryId})                            │
│                                                                   │
│  • Shows Entry Summary (Vehicle, VIN, Show)                      │
│  • Select Payment Method:                                        │
│    - Cash App Pay → Stripe                                       │
│    - Venmo → Braintree                                           │
│    - Apple Pay → Stripe                                          │
│    - Google Pay → Stripe                                         │
│    - Cash → Manual Recording                                     │
│  • Enter Amount                                                  │
│  • Optional: Reference # (for manual payments)                   │
│  • Optional: Notes (for manual payments)                         │
│                                                                   │
└────────────────┬───────────────────────┬────────────────────────┘
                 │                       │
    Online Payment│          Manual Payment│
                 ▼                       ▼
┌──────────────────────────┐  ┌─────────────────────────────┐
│  Secure Checkout         │  │  Record Payment             │
│  (Stripe/Braintree)      │  │  • Save amount              │
│  • Provider-hosted page  │  │  • Save reference           │
│  • Secure card entry     │  │  • Save notes               │
│  • Completes transaction │  │  • Update status to "Paid"  │
│                          │  │  • Redirect to profile      │
└─────────┬────────────────┘  └─────────────────────────────┘
          │
          │ Provider Callback
          ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Payment Return                               │
│                  (/payment-return)                               │
│                                                                   │
│  • Shows payment status                                          │
│  • Options:                                                      │
│    - Refresh Status (check webhook updates)                      │
│    - Retry Payment (if still pending)                            │
│    - Go to Profile                                               │
│    - Back to Signup                                              │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Alternative Entry Points

### From Profile Page
```
Profile (/profile)
  └─> "Pay Now" button on unpaid entry
       └─> Payment Page (/payment/{EntryId})
```

### Manual Payment by Admin
```
Car Shows Management (/fetch-car-shows)
  └─> Manual payment dialog
       └─> Direct API call to record payment
            └─> Status updated in place
```

## Authentication Flow

```
┌──────────────┐
│  User Login  │
│              │
│  Credentials │
│  Saved in    │
│  UserSession │
└──────┬───────┘
       │
       ▼
┌─────────────────────────────────────┐
│  UserSessionAuthenticationProvider  │
│                                     │
│  • Reads CurrentUser credentials    │
│  • Encodes as Basic Auth            │
│  • Attaches Authorization header    │
│                                     │
└──────┬──────────────────────────────┘
       │
       ▼
┌──────────────────┐
│  API Requests    │
│                  │
│  All HTTP calls  │
│  now include:    │
│  Authorization:  │
│  Basic {creds}   │
└──────────────────┘
```

## Key Improvements

### Before
- ❌ Payment mixed with registration
- ❌ No authentication headers sent to API
- ❌ Cluttered signup form
- ❌ Hard to update payment separately
- ❌ Not industry-standard flow

### After
- ✅ Clean separation: Registration → Payment
- ✅ Proper authentication on all API calls
- ✅ Focused, streamlined interfaces
- ✅ Can initiate payment from multiple entry points
- ✅ Industry-standard checkout experience
- ✅ Better error handling and user feedback
- ✅ ADA compliant (WCAG 2.1 Level AAA touch targets)
