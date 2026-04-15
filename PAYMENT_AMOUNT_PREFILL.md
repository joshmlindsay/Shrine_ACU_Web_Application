# Payment Amount Pre-Fill Feature

## Overview

The payment page now attempts to pre-fill the payment amount from the Azure service's default configuration.

## How It Works

When the Payment page loads (`/payment/{EntryId}`), it follows this logic:

### 1. **Load Entry Information**
```csharp
entry = await Client.Api.CarShowEntries[EntryId].GetAsync();
```

### 2. **Check for Existing Payment Amount**
If the entry has already been paid or has an amount set:
```csharp
if (entry.PaidAmount.HasValue && entry.PaidAmount.Value > 0)
{
    paymentForm.Amount = entry.PaidAmount;
}
```

### 3. **Fetch Default Amount from Backend**
If no amount is set on the entry, the page queries the payment API endpoint:
```csharp
var paymentInfo = await Client.Api.CarShowEntries[EntryId].Payment.GetAsync();

if (paymentInfo?.PaidAmount.HasValue == true && paymentInfo.PaidAmount.Value > 0)
{
    paymentForm.Amount = paymentInfo.PaidAmount.Value;
}
```

### 4. **Fallback to Manual Entry**
If the backend doesn't return a default amount, the field remains empty and the user must enter it manually.

## Backend Requirements

For this feature to work optimally, the **Azure backend service** should:

1. **Configure a default payment amount** for car show entries
2. **Return this amount** via the `/api/CarShowEntries/{id}/Payment` GET endpoint
3. Alternatively, **set the amount when creating the entry** so `PaidAmount` is populated

## API Endpoints Used

### Primary Endpoint
```http
GET /api/CarShowEntries/{EntryId}/Payment
```

**Expected Response**:
```json
{
  "entryId": 123,
  "paidAmount": 25.00,
  "paymentStatus": "Pending",
  ...
}
```

### Fallback
If the payment endpoint doesn't return a default:
- The entry's `PaidAmount` property is checked
- If still null, the field remains empty for manual entry

## User Experience

### Scenario 1: Backend Has Default Amount ✅
1. User navigates to `/payment/123`
2. Page loads entry details
3. **Amount field is pre-filled with $25.00** (or configured amount)
4. User selects payment method and proceeds

### Scenario 2: No Default Configured ⚠️
1. User navigates to `/payment/123`
2. Page loads entry details
3. **Amount field is empty**
4. User must manually enter the amount
5. User selects payment method and proceeds

### Scenario 3: Already Paid ✅
1. User navigates to `/payment/123`
2. Page loads entry details
3. **"Already paid" message is shown**
4. Amount displays as read-only
5. User can return to profile

## Code Changes

### File: `Payment.razor`

**Method Added**: `TryLoadDefaultPaymentAmountAsync()`
```csharp
private async Task TryLoadDefaultPaymentAmountAsync()
{
    try
    {
        // Call the payment endpoint to get default amount
        var paymentInfo = await Client.Api.CarShowEntries[EntryId].Payment.GetAsync();
        
        if (paymentInfo?.PaidAmount.HasValue == true && paymentInfo.PaidAmount.Value > 0)
        {
            paymentForm.Amount = paymentInfo.PaidAmount.Value;
        }
    }
    catch
    {
        // If we can't get the default amount, just leave it blank
    }
}
```

**Method Updated**: `LoadEntryAsync()`
- Now calls `TryLoadDefaultPaymentAmountAsync()` if entry has no amount
- Pre-fills from entry's `PaidAmount` if already set
- Handles all error cases gracefully

## Testing

### Manual Testing Steps

1. **Test with Default Amount**:
   ```
   - Create a new entry (don't pay yet)
   - Navigate to /payment/{EntryId}
   - Verify amount field shows default value
   ```

2. **Test After Payment**:
   ```
   - Complete payment for an entry
   - Return to /payment/{EntryId}
   - Verify "Already paid" message appears
   - Verify amount shows actual paid amount
   ```

3. **Test Without Default**:
   ```
   - If backend has no default configured
   - Navigate to /payment/{EntryId}
   - Verify amount field is empty but accepts manual input
   ```

### Browser DevTools Check

Open Developer Tools → Network tab:

1. Look for request: `GET /api/CarShowEntries/{id}/Payment`
2. Check response for `paidAmount` property
3. Verify value matches what appears in UI

## Troubleshooting

### Amount Field is Empty

**Possible Causes**:
1. Backend hasn't configured a default amount
2. Payment API endpoint returns null for `paidAmount`
3. API authentication issue (check for 401 errors)

**Solution**:
- Check browser console for errors
- Verify API endpoint in Network tab
- Contact backend team to configure default amount

### Wrong Amount Appears

**Possible Causes**:
1. Entry already has a `paidAmount` from previous attempt
2. Backend returning incorrect default
3. Caching issue

**Solution**:
- Verify entry status in database
- Hard refresh browser (Ctrl+Shift+R)
- Check if entry is in "Paid" status

### API Call Fails

**Possible Causes**:
1. Entry doesn't exist
2. User not authenticated
3. Payment endpoint not available

**Solution**:
- Code wraps call in try-catch (graceful failure)
- User can still manually enter amount
- Check TROUBLESHOOTING_GUIDE.md

## Future Enhancements

### Option 1: Show-Level Default
```csharp
// Get default from CarShowDto instead of entry
if (selectedShow?.RegistrationFee.HasValue == true)
{
    paymentForm.Amount = selectedShow.RegistrationFee.Value;
}
```

### Option 2: Display Default as Placeholder
```razor
<RadzenNumeric Name="Amount" 
               TValue="double?" 
               @bind-Value="paymentForm.Amount" 
               Placeholder="@($"Default: {defaultAmount:C}")"
               ... />
```

### Option 3: API Endpoint Enhancement
**Backend team adds**:
```http
GET /api/CarShows/{id}/DefaultPaymentAmount
```

Returns:
```json
{
  "defaultAmount": 25.00,
  "currency": "USD"
}
```

## Summary

✅ **Implemented**: Automatic pre-fill from backend default amount  
✅ **Works**: If Azure service provides default via Payment API  
✅ **Graceful**: Falls back to manual entry if no default available  
✅ **User-Friendly**: Shows correct amount for paid entries  
✅ **Build Status**: Successful ✓

The payment amount will now automatically populate if the Azure service has configured a default amount and returns it through the Payment API endpoint.
