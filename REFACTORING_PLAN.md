# Viagem Architecture Refactoring Plan

This document details the plan to refactor the Viagem application based on a comprehensive codebase review. The goal is to address architectural issues, resolve redundant data loads, consolidate models, extract bloated component logic into services, and ensure a clean, visually responsive, and highly performant (fast) UI.

## Goal Description

The current architecture suffers from the following major issues:

1. **Redundant Data Loads & Inefficiencies:**
   - **N+1 API Calls in UI:** `SaveAll()` loops in `TripHeroSection.razor` and `BasicInfoSection.razor` execute individual `AddDestinationAsync` and `RemoveTravellerAsync` calls per item sequentially instead of batching them.
   - **Duplicate Fetching:** Components like `ExpensesSection.razor` independently re-fetch their data on initialization instead of correctly accepting `InitialItems` passed down from the parent.
   - **Cartesian Explosion Risk:** Repositories (like `TransportationRepository` and `ActivityRepository`) eagerly load multiple collections via `.Include().ThenInclude()` without using `.AsSplitQuery()`.

2. **Model Bleeding (UI Concerns in EF Entities):**
   - Entities like `TripDestination` and `Transportation` contain computed properties (e.g., `DisplayName` and `CostAmount`) that assume eager loading of navigation properties and mix UI logic directly into the database models.
   - The application relies heavily on manual DTO mapping inside Blazor components rather than utilizing full Request/Command DTOs in the service layer.

3. **Bloated Methods & God Components:**
   - Massive components (`TripHeroSection.razor`, `BasicInfoSection.razor`, `TransportationSection.razor`) mix presentation, file stream handling, timezone calculations, and complex relational diffing. 
   - Major duplication exists between the edit modals in `BasicInfoSection` and `TripHeroSection`.

4. **UI Responsiveness (Layout & Performance):**
   - **Layout Squeezing:** Hardcoded grid columns (e.g., `grid-cols-2`) cause UI squeezing on mobile devices.
   - **Visual Sluggishness:** Absence of skeleton loaders results in jarring layout shifts during data fetching.
   - **Performance Bottlenecks:** The UI feels "slow" due to heavy synchronous operations, redundant state re-rendering (`StateHasChanged` over-triggering), and failing to use Blazor `StreamingRendering` or virtualization for long lists.

---

## Proposed Changes

### 1. Data & Model Layer (Viagem.Data)

#### [MODIFY] EF Core Entities
- Strip UI-computed properties (`DisplayName` from `TripDestination.cs`, `CostAmount` from `Transportation.cs`) and move them into the corresponding ViewModels in the Service layer.

#### [MODIFY] Repositories (`TransportationRepository.cs`, `ActivityRepository.cs`, `TripRepository.cs`)
- Append `.AsSplitQuery()` to all EF Core queries that `.Include()` multiple collection navigation properties to prevent Cartesian explosions and drastically speed up heavy queries.

---

### 2. Service Layer (Viagem/Services)

#### [NEW] `Viagem/Services/ViewModels/UpdateTripRequest.cs` (and others)
- Create comprehensive structured update DTOs. This allows the UI to pass the final intended state (e.g., list of destinations/travellers) to the Service layer, which will calculate the diffs and apply them transactionally.

#### [MODIFY] `Viagem/Services/TripService.cs`
- Refactor the multiple granular methods (Add/Remove Destination/Traveller) into coarse-grained methods (e.g., `UpdateTripDetailsAsync`) that execute all relational changes in a single batch, eliminating N+1 API calls from the UI.

---

### 3. UI Component Layer (Viagem/Components)

#### [MODIFY] Performance & Speed Optimizations (Addressing UI Slowness)
- **Eliminate Over-Rendering:** Review component lifecycles (`OnInitializedAsync`, `OnParametersSetAsync`) to prevent unnecessary data fetching when parameters haven't changed. Limit `StateHasChanged()` calls strictly to when state actually mutates.
- **Enable Streaming Rendering:** Add `@attribute [StreamRendering]` to parent pages so the user sees the shell of the page instantly rather than waiting for slow database queries to resolve.
- **Data Loading:** Update `ExpensesSection.razor` to correctly accept `InitialItems` rather than ignoring them and re-fetching data independently.
- **Skeleton Loaders:** Introduce skeleton loader components (e.g., `<TransportationSkeleton />`) to provide immediate visual feedback while the optimized queries run in the background.

#### [MODIFY] Decompose "God Components"
- Extract the complex "Edit Trip" and "Manage Travellers" modal forms from `TripHeroSection.razor` and `BasicInfoSection.razor` into a shared `EditTripModal.razor` component to eliminate duplication and reduce bloat.
- Extract individual transportation forms (Flight, Train, Bus) from `TransportationSection.razor` into sub-components.

#### [MODIFY] Visual Responsive Layout Fixes
- Replace hardcoded fixed grids (e.g., `grid-cols-2`) with responsive Tailwind prefixes (`grid-cols-1 sm:grid-cols-2`) across all components (especially `BasicInfoSection`, `TransportationSection`, and `LodgingSection`).

---

## Verification Plan

### Automated Tests
- Run `dotnet build` constantly during the extraction phase to ensure no compiler errors.

### Manual Verification
1. **Performance & Speed:** Measure the loading time of `ViewTrip.razor`. Ensure that Streaming Rendering displays the layout immediately, and the optimized Split Queries populate the data much faster than the old N+1 approach. 
2. **Trip Editing:** Open the newly extracted "Edit Trip" modal, change destinations and travellers, save, and verify changes persist correctly without breaking the UI. Inspect server logs to ensure only a single transactional DB save occurs.
3. **Responsiveness & UX:** Shrink the browser window to mobile dimensions and verify that forms wrap logically into single columns.
