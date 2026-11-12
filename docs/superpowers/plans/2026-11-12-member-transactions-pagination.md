# Member Transactions Pagination Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement 7 transactions/page pagination in the Household Member Details view with filters preserved and FEMS teal styling.

**Architecture:** Extend `MemberFinanceDetailViewModel` with pagination metadata (`CurrentPage`, `PageSize`, `TotalTransactions`, `TotalPages`). Update `HouseholdService` to calculate total filtered count and slice transactions using LINQ `.Skip().Take()`. Pass `page` from `HouseholdController.MemberDetails` and render pagination navigation with route parameters preserved in `MemberDetails.cshtml`.

**Tech Stack:** ASP.NET Core 8 MVC, Razor Views, LINQ, Bootstrap 5, Scoped CSS.

## Global Constraints
- Page size fixed at 7 transactions per page.
- URL query parameters (`id`, `walletId`, `transactionType`, `search`, `page`) and fragment (`#transactions-section`) must be preserved.
- Sequential index (`#`) across pages calculated as `(CurrentPage - 1) * PageSize + stt`.
- FEMS color palette: Primary Teal (`#0D9488`).
- Clean build: 0 errors, 0 warnings.

---

### Task 1: Update ViewModel with Pagination Properties
**Files:**
- Modify: `FEMS/ViewModels/Household/MemberFinanceDetailViewModel.cs`

- [ ] Add `CurrentPage`, `PageSize`, `TotalTransactions`, and `TotalPages` to `MemberFinanceDetailViewModel`.

### Task 2: Update Service Interface and Implementation
**Files:**
- Modify: `FEMS/Services/Interfaces/IHouseholdService.cs`
- Modify: `FEMS/Services/Implementations/HouseholdService.cs`

- [ ] Update `IHouseholdService.GetMemberFinanceDetailAsync` signature to include `int page = 1, int pageSize = 7`.
- [ ] In `HouseholdService.GetMemberFinanceDetailAsync`, compute `TotalTransactions` after filtering.
- [ ] Apply `.Skip((page - 1) * pageSize).Take(pageSize)` to `sortedTransactions`.
- [ ] Assign `CurrentPage`, `PageSize`, `TotalTransactions` on `detailVm`.

### Task 3: Update Controller Action
**Files:**
- Modify: `FEMS/Controllers/HouseholdController.cs`

- [ ] Update `MemberDetails` GET action to accept `int page = 1`.
- [ ] Pass `page` into `_householdService.GetMemberFinanceDetailAsync`.

### Task 4: Update Razor View and CSS
**Files:**
- Modify: `FEMS/Views/Household/MemberDetails.cshtml`
- Modify: `FEMS/Views/Household/MemberDetails.cshtml.css`

- [ ] Update row numbering `#` to `(Model.CurrentPage - 1) * Model.PageSize + stt`.
- [ ] Render pagination footer with summary text and page links.
- [ ] Style `.pagination`, `.page-link`, `.page-item.active .page-link` in `MemberDetails.cshtml.css`.

### Task 5: Compilation and Verification
**Files:**
- Verify: Full solution build

- [ ] Run `dotnet build` to ensure 0 errors, 0 warnings.
- [ ] Commit implementation to git branch `trung`.
