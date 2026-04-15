# Testing Guide for Payment System Refactor

## Critical Issues Fixed

### 1. Authentication Issue ✅
**What was wrong**: API client was using `AnonymousAuthenticationProvider()` - no auth headers were sent
**What was fixed**: Created custom `UserSessionAuthenticationProvider` that adds Basic Auth headers
**How to verify**: 
- Open browser dev tools → Network tab
- Log in to the application
- Perform any API call (create entry, load shows, etc.)
- Check the request headers - should see: `Authorization: Basic {base64-encoded-credentials}`

## Test Plan

### Phase 1: Authentication Testing

1. **Login and API Calls**
   - Navigate to `/fetch-login`
   - Log in with valid credentials
   - Open dev tools Network tab
   - Navigate to any page that loads data (e.g., `/fetch-car-shows`)
   - **✓ Verify**: All API requests include `Authorization` header
   - **✓ Verify**: API returns data successfully (not 401 Unauthorized)

2. **Session Persistence**
   - Log in
   - Refresh the page
   - **✓ Verify**: Still logged in
   - **✓ Verify**: API calls still have auth headers

### Phase 2: Registration Flow Testing

1. **Complete Registration Flow**
   - Log in
   - Navigate to `/fetch-signup`
   - Fill out form:
     - Select a car show
     - Enter personal info (first name, last name, email, phone)
     - Enter car info (year, make, model, VIN, color)
   - Click "Register Entry"
   - **✓ Verify**: Entry is created successfully
   - **✓ Verify**: Automatically redirected to `/payment/{EntryId}`
   - **✓ Verify**: Payment page shows correct entry details

2. **Form Validation**
   - Try submitting with empty required fields
   - **✓ Verify**: Validation messages appear
   - **✓ Verify**: Form does not submit

### Phase 3: Payment Page Testing

1. **Online Payment Flow (Stripe - Cash App Pay)**
   - Navigate to `/payment/{EntryId}` (or complete registration)
   - Select "Cash App Pay"
   - Enter amount (e.g., $50)
   - Click "Proceed to Checkout"
   - **✓ Verify**: Redirected to Stripe checkout page
   - **Note**: Actual payment requires Stripe configuration

2. **Online Payment Flow (Braintree - Venmo)**
   - Select "Venmo"
   - Enter amount
   - Click "Proceed to Checkout"
   - **✓ Verify**: Redirected to Braintree checkout page
   - **Note**: Actual payment requires Braintree configuration

3. **Manual Payment Flow (Cash)**
   - Select "Cash"
   - Enter amount (e.g., $50)
   - Enter payment reference (e.g., "Receipt #12345")
   - Enter notes (optional)
   - Click "Record Payment"
   - **✓ Verify**: Payment recorded successfully
   - **✓ Verify**: Redirected to profile page
   - **✓ Verify**: Entry shows as "Paid" in profile

4. **Already Paid Entry**
   - Navigate to payment page for an already-paid entry
   - **✓ Verify**: Shows "Already Paid" message
   - **✓ Verify**: Shows paid amount and reference
   - **✓ Verify**: Cannot submit payment again

5. **Form Validation**
   - Try submitting without selecting payment method
   - **✓ Verify**: Validation message appears
   - Try submitting without entering amount
   - **✓ Verify**: Validation message appears
   - Try entering negative or zero amount
   - **✓ Verify**: Validation prevents it

### Phase 4: Payment Return Testing

1. **Successful Payment Return**
   - Complete online payment flow
   - Return to site after payment
   - **✓ Verify**: Lands on `/payment-return?entryId={id}&result=success`
   - **✓ Verify**: Shows success message
   - **✓ Verify**: Shows payment status
   - **✓ Verify**: Can refresh status
   - **✓ Verify**: Can navigate to profile

2. **Cancelled Payment Return**
   - Start online payment flow
   - Cancel at checkout
   - **✓ Verify**: Lands on `/payment-return?entryId={id}&result=cancel`
   - **✓ Verify**: Shows cancellation message
   - **✓ Verify**: Can retry payment
   - **✓ Verify**: Retry button works

3. **Pending Payment**
   - View payment return for pending payment
   - **✓ Verify**: Shows "still pending" message
   - **✓ Verify**: Can retry payment

### Phase 5: Profile Integration Testing

1. **View Entries**
   - Navigate to `/profile`
   - Go to "Event History" tab
   - **✓ Verify**: Shows list of entries
   - **✓ Verify**: Shows payment status for each

2. **Pay Now Action**
   - Find an unpaid entry
   - Click "Pay Now" (if available)
   - **✓ Verify**: Redirected to payment page
   - **✓ Verify**: Can complete payment

### Phase 6: Accessibility Testing

1. **Keyboard Navigation**
   - Use Tab key to navigate through forms
   - **✓ Verify**: All interactive elements are reachable
   - **✓ Verify**: Focus indicators are visible (3px outline)
   - **✓ Verify**: Can submit forms with Enter key

2. **Touch Target Sizes**
   - Test on mobile device or responsive mode
   - **✓ Verify**: All buttons are at least 44x44px (desktop)
   - **✓ Verify**: All buttons are at least 48x48px (mobile)
   - **✓ Verify**: Buttons don't overlap

3. **Color Contrast**
   - Test in light mode
   - **✓ Verify**: All text is readable
   - **✓ Verify**: Buttons have good contrast
   - Test in dark mode
   - **✓ Verify**: All text is readable
   - **✓ Verify**: Buttons have good contrast
   - **✓ Verify**: Input fields are visible

4. **Screen Reader Testing** (Optional but recommended)
   - Use screen reader (NVDA, JAWS, or VoiceOver)
   - **✓ Verify**: Form labels are announced
   - **✓ Verify**: Error messages are announced
   - **✓ Verify**: Button purposes are clear

### Phase 7: Error Handling Testing

1. **Network Errors**
   - Disconnect from internet
   - Try to submit registration
   - **✓ Verify**: Shows appropriate error message
   - Reconnect
   - Try again
   - **✓ Verify**: Works after reconnection

2. **Invalid Entry ID**
   - Navigate to `/payment/99999` (non-existent entry)
   - **✓ Verify**: Shows "Entry not found" error

3. **Unauthorized Access**
   - Log out
   - Try to navigate to `/payment/{EntryId}`
   - **✓ Verify**: Redirected to login page
   - **✓ Verify**: After login, redirected back to payment page

### Phase 8: Responsive Design Testing

Test on various screen sizes:

1. **Mobile (320px - 767px)**
   - **✓ Verify**: Forms are usable
   - **✓ Verify**: Buttons are properly sized (48x48px)
   - **✓ Verify**: Text is readable
   - **✓ Verify**: No horizontal scrolling

2. **Tablet (768px - 1023px)**
   - **✓ Verify**: Layout adapts properly
   - **✓ Verify**: Two-column layouts work

3. **Desktop (1024px+)**
   - **✓ Verify**: Full layout displays correctly
   - **✓ Verify**: Maximum width constraint is respected

## Known Limitations

1. **Payment Provider Configuration Required**
   - Stripe and Braintree must be configured in the backend API
   - Without configuration, checkout redirects will fail
   - This is expected - backend team must configure provider credentials

2. **Webhook Processing**
   - Payment status updates via webhooks may take a few seconds
   - Users might need to refresh the payment return page
   - This is normal behavior documented in the UI

3. **Basic Authentication**
   - Currently uses Basic Auth (username:password in header)
   - Consider upgrading to token-based auth for production
   - Current implementation is secure over HTTPS but not ideal for production

## Success Criteria

✅ All API calls include authentication headers
✅ Registration flow redirects to payment page
✅ Payment page displays correctly
✅ Online payments redirect to provider checkout
✅ Manual payments are recorded successfully
✅ Payment return page handles all scenarios
✅ Profile integration works
✅ All forms validate properly
✅ Accessibility requirements met (WCAG 2.1 Level AAA touch targets)
✅ Responsive design works on all screen sizes
✅ Error handling is robust

## Regression Testing

Ensure these existing features still work:

- ✅ Login/Logout
- ✅ User registration
- ✅ Profile updates
- ✅ Car show management (for admins)
- ✅ Manual payment recording (for admins)
- ✅ Theme switching (light/dark mode)

## Performance Testing (Optional)

- Test with slow network connection
- Test with many entries in profile
- Verify page load times are acceptable

## Browser Compatibility

Test in:
- ✅ Chrome/Edge (Chromium)
- ✅ Firefox
- ✅ Safari (macOS/iOS)
- ✅ Mobile browsers (Chrome Mobile, Safari Mobile)
