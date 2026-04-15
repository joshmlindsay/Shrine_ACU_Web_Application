# Payment Amount Constraints - Implementation Summary

## Overview

The payment amount field now enforces constraints to ensure data quality and prevent user errors:

1. ✅ **Minimum**: $0.00 (cannot be negative)
2. ✅ **Maximum**: Default payment amount from backend (if available)
3. ✅ **Whole Dollars Only**: Cents are automatically rounded down to .00
4. ✅ **Display Format**: Always shows .00 (e.g., $25.00, not $25)

## Features Implemented

### 1. Automatic Rounding to Whole Dollars

**Behavior**:
- If user enters `25.99` → automatically becomes `25.00`
- If user enters `30.50` → automatically becomes `30.00`
- If user enters `100.01` → automatically becomes `100.00`

**Implementation**:
```csharp
private void OnAmountChanged(double? value)
{
    if (paymentForm.Amount.HasValue)
    {
        // Round down to whole dollars (remove cents)
        paymentForm.Amount = Math.Floor(paymentForm.Amount.Value);
        
        // ... additional constraints
    }
}
```

### 2. Minimum Constraint ($0.00)

**Behavior**:
- Field has `Min="0"` attribute
- If user tries to enter negative value → automatically becomes `0.00`

**Implementation**:
```razor
<RadzenNumeric Name="Amount" 
               Min="0" 
               ... />
```

### 3. Maximum Constraint (Default Payment Amount)

**Behavior**:
- When backend provides a default amount (e.g., $25.00), this becomes the maximum
- Field shows placeholder: `Max: $25.00`
- Below field shows: "Maximum payment amount: $25.00"
- If user tries to exceed max → automatically capped at maximum

**Implementation**:
```razor
<RadzenNumeric Name="Amount" 
               Max="@((decimal?)maxPaymentAmount)"
               Placeholder="@(maxPaymentAmount.HasValue ? $"Max: {maxPaymentAmount.Value:C}" : "Enter payment amount")" />

@if (maxPaymentAmount.HasValue)
{
    <RadzenText>Maximum payment amount: @maxPaymentAmount.Value.ToString("C")</RadzenText>
}
```

### 4. Currency Formatting

**Behavior**:
- Display format: `c2` (currency with 2 decimal places)
- Shows: `$25.00` instead of `$25`
- Shows: `$100.00` instead of `$100`

**Implementation**:
```razor
<RadzenNumeric Format="c2" ... />
```

## User Experience Examples

### Scenario 1: Normal Entry
```
User Action:                  Result:
1. Field loads                → Shows: $0.00 (if no default)
                              → OR: $25.00 (if default exists)
2. User types "25"            → Shows: $25.00
3. User submits               → ✅ Accepts $25.00
```

### Scenario 2: User Enters Cents
```
User Action:                  Result:
1. User types "25.99"         → Auto-changes to: $25.00
2. User types "30.50"         → Auto-changes to: $30.00
3. User types "100.25"        → Auto-changes to: $100.00
```

### Scenario 3: User Exceeds Maximum
```
User Action:                  Result:
1. Default amount is $25.00   → Field shows "Max: $25.00"
2. User tries "30"            → Blocked/capped at: $25.00
3. User tries "100"           → Blocked/capped at: $25.00
4. User tries "25"            → ✅ Accepts $25.00
```

### Scenario 4: User Enters Negative
```
User Action:                  Result:
1. User tries "-10"           → Auto-corrects to: $0.00
2. User tries "-0.50"         → Auto-corrects to: $0.00
```

## Validation

### Client-Side Validation (Real-Time)

**OnAmountChanged Event**:
```csharp
private void OnAmountChanged(double? value)
{
    if (paymentForm.Amount.HasValue)
    {
        // 1. Round down cents
        paymentForm.Amount = Math.Floor(paymentForm.Amount.Value);
        
        // 2. Enforce minimum
        if (paymentForm.Amount < 0)
            paymentForm.Amount = 0;
        
        // 3. Enforce maximum
        if (maxPaymentAmount.HasValue && paymentForm.Amount > (double)maxPaymentAmount)
            paymentForm.Amount = (double)maxPaymentAmount;
    }
}
```

### Server-Side Validation (On Submit)

**ProcessPaymentAsync Method**:
```csharp
private async Task ProcessPaymentAsync(PaymentFormModel _)
{
    // 1. Check if amount exists and is positive
    if (paymentForm.Amount is null or <= 0)
    {
        errorMessage = "Please enter a valid payment amount.";
        return;
    }

    // 2. Check if amount exceeds maximum
    if (maxPaymentAmount.HasValue && paymentForm.Amount > (double)maxPaymentAmount)
    {
        errorMessage = $"Payment amount cannot exceed {maxPaymentAmount.Value:C}.";
        return;
    }
    
    // ... continue processing
}
```

## How Maximum is Determined

### Source 1: Backend Default (Preferred)
```csharp
var paymentInfo = await Client.Api.CarShowEntries[EntryId].Payment.GetAsync();

if (paymentInfo?.PaidAmount.HasValue == true && paymentInfo.PaidAmount.Value > 0)
{
    var defaultAmount = Math.Floor(paymentInfo.PaidAmount.Value);
    maxPaymentAmount = (decimal)defaultAmount;
}
```

### Source 2: Entry's Paid Amount (If Already Paid)
```csharp
if (entry.PaidAmount.HasValue && entry.PaidAmount.Value > 0)
{
    paymentForm.Amount = entry.PaidAmount;
    maxPaymentAmount = (decimal)Math.Floor(entry.PaidAmount.Value);
}
```

### Fallback: No Maximum
If neither source provides an amount:
- `maxPaymentAmount = null`
- User can enter any positive amount
- Field shows: "Enter payment amount" placeholder

## Code Changes

### File: `Payment.razor`

**Variables Added**:
```csharp
private decimal? maxPaymentAmount; // Store the maximum allowed payment amount
```

**UI Changes**:
```razor
<RadzenNumeric Name="Amount" 
               TValue="double?" 
               @bind-Value="paymentForm.Amount" 
               Change="@OnAmountChanged"       <!-- NEW: Handle value changes -->
               Min="0"                         <!-- NEW: Enforce minimum -->
               Max="@((decimal?)maxPaymentAmount)" <!-- NEW: Enforce maximum -->
               Step="1"                        <!-- NEW: Whole number increments -->
               Format="c2"                     <!-- NEW: Currency format with .00 -->
               Placeholder="@(maxPaymentAmount.HasValue ? $"Max: {maxPaymentAmount.Value:C}" : "Enter payment amount")" />

<!-- NEW: Display max amount hint -->
@if (maxPaymentAmount.HasValue)
{
    <RadzenText TextStyle="TextStyle.Caption">
        Maximum payment amount: @maxPaymentAmount.Value.ToString("C")
    </RadzenText>
}
```

**Methods Added**:
```csharp
private void OnAmountChanged(double? value)
{
    // Enforces whole dollars, min, and max constraints
}
```

**Methods Updated**:
```csharp
private async Task TryLoadDefaultPaymentAmountAsync()
{
    // Now sets maxPaymentAmount in addition to prefilling amount
}

private async Task ProcessPaymentAsync(PaymentFormModel _)
{
    // Now validates against maxPaymentAmount
}
```

## Testing

### Manual Testing Steps

1. **Test Whole Dollar Rounding**:
   ```
   - Navigate to /payment/{EntryId}
   - Enter "25.99" in amount field
   - Verify it changes to "25.00"
   - Enter "30.50"
   - Verify it changes to "30.00"
   ```

2. **Test Minimum Constraint**:
   ```
   - Try entering "-10"
   - Verify it changes to "0.00"
   ```

3. **Test Maximum Constraint**:
   ```
   - If default is $25.00
   - Try entering "30"
   - Verify it caps at "25.00"
   - Verify message shows "Maximum payment amount: $25.00"
   ```

4. **Test Currency Display**:
   ```
   - Enter "25"
   - Verify display shows "$25.00" not "$25"
   ```

5. **Test Submission Validation**:
   ```
   - Try submitting with amount > max
   - Verify error: "Payment amount cannot exceed $XX.XX"
   ```

### Browser DevTools Check

1. Open Developer Tools → Console
2. Watch for any JavaScript errors when typing amounts
3. Check Network tab for API calls when loading default amount

## Troubleshooting

### Amount Doesn't Round Down

**Issue**: User enters "25.50" but it stays as "25.50"

**Cause**: JavaScript error preventing OnAmountChanged from firing

**Solution**: 
- Check browser console for errors
- Hard refresh (Ctrl+Shift+R)
- Verify RadzenNumeric component version

### No Maximum Displayed

**Issue**: "Maximum payment amount" message doesn't appear

**Cause**: Backend not returning default amount

**Solution**:
- Check Network tab for `/api/CarShowEntries/{id}/Payment` response
- Verify backend has configured default amount
- Check authentication (401 errors)

### Maximum is Wrong

**Issue**: Maximum shows incorrect amount

**Cause**: Entry already has different paid amount

**Solution**:
- Check entry status in database
- Verify PaidAmount field
- Backend team may need to update default configuration

## Future Enhancements

### Option 1: Allow Cents for Certain Payment Methods
```csharp
private void OnAmountChanged(double? value)
{
    if (paymentForm.Amount.HasValue)
    {
        // Only round down for cash payments
        if (paymentForm.PaymentMethod == PaymentMethodCash)
        {
            paymentForm.Amount = Math.Floor(paymentForm.Amount.Value);
        }
        // Allow cents for online payments
    }
}
```

### Option 2: Configurable Maximum per Car Show
```csharp
// Backend adds RegistrationFee property to CarShowDto
if (selectedShow?.RegistrationFee.HasValue == true)
{
    maxPaymentAmount = selectedShow.RegistrationFee;
}
```

### Option 3: Warning Instead of Hard Block
```csharp
@if (paymentForm.Amount > maxPaymentAmount)
{
    <RadzenAlert AlertStyle="AlertStyle.Warning">
        Recommended amount is {maxPaymentAmount:C}. 
        Are you sure you want to pay more?
    </RadzenAlert>
}
```

## Summary

✅ **Minimum**: $0.00 enforced  
✅ **Maximum**: Default amount from backend (if available)  
✅ **Whole Dollars**: Cents automatically rounded down  
✅ **Display**: Always shows .00 format  
✅ **Validation**: Client-side (real-time) and server-side (on submit)  
✅ **User-Friendly**: Clear messaging about constraints  
✅ **Build Status**: Successful ✓

The payment amount field now provides a clean, constrained experience that prevents data entry errors while maintaining flexibility when no default is configured.
