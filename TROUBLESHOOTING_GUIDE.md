# Troubleshooting Guide - Payment System

## Common Issues and Solutions

### Issue 1: API Returns 401 Unauthorized

**Symptoms**:
- API calls fail with 401 status code
- Data doesn't load on pages
- Console shows authentication errors

**Causes**:
- User not logged in
- Authentication headers not being sent
- Invalid credentials

**Solutions**:
1. Verify user is logged in:
   ```
   - Check if CurrentUser is populated in UserSessionService
   - Check browser localStorage for "shrine_current_user"
   ```

2. Verify auth headers are being sent:
   ```
   - Open Dev Tools → Network tab
   - Make an API call
   - Check request headers for "Authorization: Basic ..."
   - If missing, UserSessionAuthenticationProvider might not be registered
   ```

3. Check Program.cs registration:
   ```csharp
   // Should have this:
   var authProvider = new UserSessionAuthenticationProvider(userSession);
   var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
   
   // NOT this:
   var requestAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
   ```

4. Verify user password is stored:
   ```
   - Check if CurrentUser.Password is populated
   - Password should be stored when user logs in
   ```

### Issue 2: Payment Page Shows "Entry not found"

**Symptoms**:
- Navigating to /payment/{EntryId} shows error
- Entry details don't load

**Causes**:
- Entry doesn't exist
- Wrong entry ID
- User doesn't have permission to view entry
- API authentication issue

**Solutions**:
1. Verify entry exists:
   ```
   - Check database for entry with that ID
   - Check if entry was successfully created during registration
   ```

2. Check URL parameter:
   ```
   - Ensure URL has valid integer ID: /payment/123
   - Not: /payment/ or /payment/abc
   ```

3. Check console for errors:
   ```
   - Look for API call failures
   - Check for authentication errors
   ```

### Issue 3: Cannot Complete Online Payment

**Symptoms**:
- "Proceed to Checkout" button doesn't redirect
- Error message about payment session
- Checkout URL not returned

**Causes**:
- Payment provider not configured in backend
- API call failing
- Missing environment variables in backend
- Provider credentials invalid

**Solutions**:
1. Check backend API configuration:
   ```
   - Verify Stripe API key is set
   - Verify Braintree credentials are set
   - Check Azure Key Vault configuration
   ```

2. Check browser console:
   ```
   - Look for error messages
   - Check network tab for API call details
   ```

3. Verify API endpoint is working:
   ```
   POST /api/CarShowEntries/{entryId}/payment/session
   - Should return { CheckoutUrl: "https://..." }
   ```

4. Backend team should check:
   ```
   - Provider SDK configuration
   - Provider account status
   - Provider test mode vs live mode
   ```

### Issue 4: Manual Payment Not Recording

**Symptoms**:
- "Record Payment" button submits but nothing happens
- Payment status stays "Pending"
- No redirect to profile

**Causes**:
- API call failing
- Validation errors
- Entry update failed
- Permission issues

**Solutions**:
1. Check browser console for errors

2. Verify PUT call is successful:
   ```
   - Check network tab
   - Look for PUT /api/CarShowEntries/{entryId}
   - Should return 200 OK
   ```

3. Check form data:
   ```
   - Amount should be > 0
   - Payment method should be selected
   ```

4. Verify user permissions:
   ```
   - User should have write access
   - Check UserSession.CanWrite
   ```

### Issue 5: Payment Return Page Shows Wrong Status

**Symptoms**:
- Payment completed but shows "Pending"
- Payment status not updating

**Causes**:
- Webhook not processed yet
- Webhook failed
- Provider callback not configured
- Time delay in webhook processing

**Solutions**:
1. Wait a few seconds and click "Refresh Status"
   ```
   - Webhook processing can take 2-10 seconds
   - This is normal behavior
   ```

2. Check backend webhook logs:
   ```
   - Verify webhook was received
   - Check for processing errors
   ```

3. Verify webhook URLs are configured in provider dashboard:
   ```
   Stripe: https://your-api.azurewebsites.net/api/payments/webhooks/stripe
   Braintree: https://your-api.azurewebsites.net/api/payments/webhooks/braintree
   ```

4. Check webhook signature validation:
   ```
   - Ensure webhook signing secrets are correct
   - Check backend logs for signature validation errors
   ```

### Issue 6: Theme/Styling Issues

**Symptoms**:
- Buttons hard to see
- Colors don't look right
- Dark mode issues

**Causes**:
- CSS not loaded
- Theme not applied
- Cache issues

**Solutions**:
1. Hard refresh browser:
   ```
   - Ctrl+Shift+R (Windows/Linux)
   - Cmd+Shift+R (Mac)
   ```

2. Clear browser cache

3. Check app.css is loaded:
   ```
   - Look in Dev Tools → Network tab
   - Should see app.css loaded
   ```

4. Verify theme.js is working:
   ```
   - Check browser console for errors
   - Look for data-theme attribute on <html> element
   ```

### Issue 7: Responsive Design Issues on Mobile

**Symptoms**:
- Buttons too small
- Text overlapping
- Horizontal scrolling

**Causes**:
- CSS media queries not applied
- Viewport meta tag missing
- Browser zoom

**Solutions**:
1. Check viewport meta tag in _Host.cshtml or App.razor:
   ```html
   <meta name="viewport" content="width=device-width, initial-scale=1.0" />
   ```

2. Reset browser zoom to 100%

3. Clear cache and reload

4. Check CSS media queries in app.css are loading

### Issue 8: Buttons Not Clickable

**Symptoms**:
- Clicking buttons does nothing
- No visual feedback
- Forms don't submit

**Causes**:
- JavaScript errors
- Blazor circuit broken
- Event handlers not attached

**Solutions**:
1. Check browser console for errors:
   ```
   - Look for Blazor SignalR errors
   - Look for JavaScript errors
   ```

2. Refresh the page:
   ```
   - Blazor circuit might have disconnected
   ```

3. Check if @onclick or Click attributes are properly set

4. Verify component is in interactive render mode:
   ```razor
   @rendermode InteractiveServer
   ```

## Debugging Tips

### Enable Verbose Logging

In appsettings.Development.json:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Shrine_ACU_Web_Application": "Debug"
    }
  }
}
```

### Check Browser Developer Tools

1. **Console Tab**:
   - JavaScript errors
   - Blazor errors
   - Log messages

2. **Network Tab**:
   - API call details
   - Request/response headers
   - Status codes
   - Response bodies

3. **Application Tab**:
   - localStorage contents
   - Session data
   - Cookies

### Test API Directly

Use tools like:
- Postman
- curl
- Thunder Client (VS Code extension)

Example curl command:
```bash
curl -X GET "https://your-api.azurewebsites.net/api/CarShowEntries/123" \
  -H "Authorization: Basic base64(username:password)"
```

### Verify Database State

Check SQL Server for:
- Entry records
- Payment status
- User records

### Check Azure Configuration

If deployed to Azure:
1. Check App Service logs
2. Check Application Insights
3. Verify Key Vault access
4. Check connection strings
5. Verify environment variables

## Getting Help

If issues persist:

1. **Collect Information**:
   - Browser console errors
   - Network tab screenshots
   - Steps to reproduce
   - Expected vs actual behavior

2. **Check Documentation**:
   - PAYMENT_REFACTOR_SUMMARY.md
   - PAYMENT_FLOW_DIAGRAM.md
   - PaymentIntegrationPlan.md

3. **Backend Team**:
   - Payment provider configuration
   - Webhook processing
   - API endpoint issues
   - Database problems

4. **Frontend Team**:
   - UI/UX issues
   - Styling problems
   - Client-side errors
   - Accessibility concerns
