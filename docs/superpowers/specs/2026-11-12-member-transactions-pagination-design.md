# Design Spec: Member Transactions Pagination (7 Items / Page)

## 1. Overview
In the Household module, when a Manager or Creator (Chủ hộ) views a member's financial details (`/Household/MemberDetails?id={memberId}`), the transaction history table displays all transactions without pagination. When a member accumulates many transactions, the card becomes excessively long. This specification defines pagination for the member transactions table with a fixed size of 7 items per page, matching FEMS conventions.

## 2. Requirements
- **Page Size**: Fixed at 7 transactions per page.
- **Filters Preservation**: Pagination links must preserve active filter parameters:
  - `id` (Member ID)
  - `walletId` (Selected wallet filter)
  - `transactionType` (Income / Expense / All)
  - `search` (Search keyword)
  - `#transactions-section` (URL hash anchor to maintain scroll position at the transactions table)
- **Pagination Controls**:
  - Summary text: `Hiển thị X trên tổng số Y giao dịch (Trang CurrentPage / TotalPages)`.
  - Previous button (`<`), page numbers (`1`, `2`, ...), Next button (`>`).
  - Active page highlighted in FEMS Primary Teal (`#0D9488`).
  - Disabled state for previous on first page and next on last page.
- **Non-breaking**: All existing wallet and alert threshold functionality remains unaffected.

## 3. Architecture & Data Flow

```
[Browser: MemberDetails View]
        │
        │ GET /Household/MemberDetails?id=1&walletId=2&transactionType=Expense&search=cafe&page=2
        ▼
[HouseholdController.MemberDetails]
        │
        │ Calls GetMemberFinanceDetailAsync(managerUserId, targetMemberId, walletId, transactionType, search, page, pageSize=7)
        ▼
[HouseholdService.GetMemberFinanceDetailAsync]
        │
        ├─ 1. Verifies manager/creator permissions
        ├─ 2. Retrieves wallets, incomes, expenses from IHouseholdRepository
        ├─ 3. Applies filters (walletId, transactionType, search)
        ├─ 4. Counts TotalTransactions
        ├─ 5. Sorts descending by Date, then ID
        ├─ 6. Applies .Skip((page - 1) * 7).Take(7)
        └─ 7. Constructs MemberFinanceDetailViewModel with pagination metadata
        ▼
[Browser: MemberDetails.cshtml]
        │
        └─ Renders 7 rows with footer summary & pagination navigation
```

## 4. Components & File Modifications

### 4.1. `FEMS/ViewModels/Household/MemberFinanceDetailViewModel.cs`
Add pagination properties:
```csharp
public int CurrentPage { get; set; } = 1;
public int PageSize { get; set; } = 7;
public int TotalTransactions { get; set; }
public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalTransactions / PageSize) : 0;
```

### 4.2. `FEMS/Services/Interfaces/IHouseholdService.cs`
Update method signature:
```csharp
Task<(bool Success, string Message, MemberFinanceDetailViewModel? Data)> GetMemberFinanceDetailAsync(
    int managerUserId,
    int targetHouseholdMemberId,
    int? walletId = null,
    string? transactionType = null,
    string? search = null,
    int page = 1,
    int pageSize = 7);
```

### 4.3. `FEMS/Services/Implementations/HouseholdService.cs`
- Update `GetMemberFinanceDetailAsync` to accept `int page = 1, int pageSize = 7`.
- Validate `page < 1 => page = 1`.
- Compute `int totalTransactions = sortedTransactions.Count;`.
- Calculate `var pagedTransactions = sortedTransactions.Skip((page - 1) * pageSize).Take(pageSize).ToList();`.
- Set `detailVm.Transactions = pagedTransactions;`, `detailVm.CurrentPage = page;`, `detailVm.PageSize = pageSize;`, `detailVm.TotalTransactions = totalTransactions;`.

### 4.4. `FEMS/Controllers/HouseholdController.cs`
Update `MemberDetails` action to accept `int page = 1`:
```csharp
[HttpGet]
public async Task<IActionResult> MemberDetails(
    int id,
    int? walletId = null,
    string? transactionType = null,
    string? search = null,
    int page = 1)
```
Pass `page` and `pageSize = 7` to service call.

### 4.5. `FEMS/Views/Household/MemberDetails.cshtml`
- Above table or in footer:
  `Hiển thị <strong>@Model.Transactions.Count</strong> trên tổng số <strong>@Model.TotalTransactions</strong> giao dịch (Trang @Model.CurrentPage / @Math.Max(Model.TotalPages, 1))`
- Add pagination nav bar:
  - Previous (`<`)
  - Page links with route data (`id`, `walletId`, `transactionType`, `search`, `page`, fragment `transactions-section`)
  - Next (`>`)
- Maintain sequential index `#` based on `(Model.CurrentPage - 1) * Model.PageSize + stt`.

### 4.6. `FEMS/Views/Household/MemberDetails.cshtml.css`
Style pagination controls:
- Active page pill with `#0D9488` background, white text.
- Page link hover effects.
- Alignment flexbox row between summary text and pagination buttons.

## 5. Testing & Verification
- Compile project with `dotnet build` confirming 0 errors, 0 warnings.
- Verify filtering + pagination preserves search/wallet query params.
- Verify correct calculation of STT index across pages (e.g. Page 1: 1-7, Page 2: 8-14).

