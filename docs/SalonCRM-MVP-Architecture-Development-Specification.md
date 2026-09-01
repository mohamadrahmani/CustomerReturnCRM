# SalonCRM MVP Architecture & Development Specification

## Purpose

This document is the source of truth for implementing the SalonCRM MVP
with Codex.

The product starts with women's beauty salons but the architecture must
remain generic for other service businesses.

Core value: Help businesses retain customers by tracking services,
visits, expected return behavior, and follow-up actions.

------------------------------------------------------------------------

# Technology

-   ASP.NET Core Web API
-   Clean Architecture
-   Entity Framework Core
-   SQL Server
-   ASP.NET Core Identity
-   JWT Authentication
-   Lightweight CQRS

Avoid: - Microservices - Over-engineered repositories - Complex event
systems - Unnecessary enterprise patterns

------------------------------------------------------------------------

# Solution Structure

src/ - SalonCRM.Domain - SalonCRM.Application -
SalonCRM.Infrastructure - SalonCRM.API

------------------------------------------------------------------------

# Tenant Model

Business is the Tenant.

All operational data belongs to a Business.

Supported database model: User can belong to multiple Businesses.

MVP UX: User works with one current Business.

------------------------------------------------------------------------

# Identity

Use ASP.NET Core Identity.

Main relation:

AspNetUsers \| BusinessMember \| Business

BusinessMember:

-   Id Guid
-   BusinessId Guid
-   UserId Guid
-   Role
-   CreatedAt
-   UpdatedAt

Roles: - Owner - Admin - Staff

------------------------------------------------------------------------

# Entities

## Business

Fields: - Id Guid - Name - BusinessType - Mobile - Address nullable -
City nullable - IsActive - CreatedAt - UpdatedAt

------------------------------------------------------------------------

## Staff

Staff is a service provider, not necessarily a system user.

Fields: - Id Guid - BusinessId - FirstName - LastName - Mobile
nullable - UserId nullable - IsActive - CreatedAt - UpdatedAt

No Staff-Service limitation in MVP.

------------------------------------------------------------------------

## Customer

Fields: - Id Guid - BusinessId - FirstName - LastName nullable -
Mobile - BirthDate nullable - Note nullable - IsActive - CreatedAt -
UpdatedAt

Unique: BusinessId + Mobile

Do not store: - LastVisitDate - TotalVisits - TotalPurchase

Calculate them.

------------------------------------------------------------------------

## Service

Fields: - Id Guid - BusinessId - Title - Description nullable -
DefaultPrice - DefaultDurationMinutes - SuggestedReturnDays nullable -
IsActive - CreatedAt - UpdatedAt

Unique: BusinessId + Title

------------------------------------------------------------------------

## ServiceTemplate

Used only during onboarding.

Fields: - Id Guid - BusinessType - Title - DefaultDurationMinutes -
SuggestedReturnDays - IsActive - CreatedAt

Template is copied into Business Service.

------------------------------------------------------------------------

# Appointment

Appointment = planned work.

Fields: - Id Guid - BusinessId - CustomerId - StartAt - EndAt - Status -
Note nullable - CreatedAt - UpdatedAt

Statuses: - Pending - Confirmed - Completed - Cancelled - NoShow

Appointment requires at least one AppointmentService.

------------------------------------------------------------------------

# AppointmentService

Fields: - Id Guid - AppointmentId - ServiceId - StaffId

Snapshots: - ServiceTitle - Price - DurationMinutes

------------------------------------------------------------------------

# Visit

Visit = actual performed work.

Can be created: - From Appointment completion - Directly without
Appointment

Fields: - Id Guid - BusinessId - CustomerId - AppointmentId nullable -
VisitAt - TotalAmount nullable - Note nullable - CreatedAt - UpdatedAt

------------------------------------------------------------------------

# VisitService

Source of return analysis.

Fields: - Id Guid - VisitId - ServiceId - StaffId

Snapshots: - ServiceTitle - Price - DurationMinutes -
SuggestedReturnDays

------------------------------------------------------------------------

# Complete Appointment Flow

When appointment is completed:

1.  Set Appointment status to Completed
2.  Create Visit
3.  Copy AppointmentServices into VisitServices
4.  Preserve snapshots

------------------------------------------------------------------------

# Return Engine

ExpectedReturn is NOT stored.

Calculation:

Last VisitService for a Service + SuggestedReturnDays =
ExpectedReturnDate

Return status belongs to:

Customer + Service

------------------------------------------------------------------------

# Dashboard and Smart Lists

Dashboard is action oriented.

Smart Lists:

## Overdue

ExpectedReturnDate passed and no future appointment.

## DueSoon

Return date is close.

## AtRisk

Customer passed the expected cycle significantly.

## No Recent Visit

Customer has been inactive.

Smart Lists are queries, not tables.

------------------------------------------------------------------------

# Reminder

Reminder is a human task, not automatic prediction.

Fields: - Id Guid - BusinessId - CustomerId - ServiceId nullable -
Title - DueAt - Status - Note nullable - CreatedByUserId - CompletedAt
nullable - CreatedAt - UpdatedAt

Statuses: - Pending - Completed - Cancelled

Flow:

Smart List -\> User Decision -\> Reminder

No automatic reminders in MVP.

------------------------------------------------------------------------

# Database Rules

Primary keys: All domain entities use Guid.

Audit: CreatedAt UpdatedAt nullable

Soft delete: Only: - Business - Staff - Customer - Service

Use IsActive.

Avoid dangerous cascade deletes.

Allowed: Appointment -\> AppointmentService Visit -\> VisitService

------------------------------------------------------------------------

# Development Order

Phase 1: - Create solution - Clean Architecture - Identity - DbContext -
Initial migrations

Phase 2: - Business setup - Owner member - Owner staff

Phase 3: - Customer CRUD - Service CRUD

Phase 4: - Appointment - Calendar - Complete Appointment

Phase 5: - Visit

Phase 6: - Return Engine - Customer Profile - Smart Lists

Phase 7: - Reminder

------------------------------------------------------------------------

# Codex Instructions

This document is the source of truth.

Start from Phase 1.

Do not redesign architecture.

Prefer simple maintainable code.

Do not add features outside MVP without approval.
