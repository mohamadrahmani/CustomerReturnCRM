# SalonCRM Product Context & Decision Log

## Purpose of This Document

This document contains the product thinking, architectural decisions,
and business rules behind SalonCRM.

It is intended to be read by AI agents, developers, and future team
members before making changes to the system.

This document explains not only WHAT was designed, but also WHY
decisions were made.

The other documents define: - Project understanding - Technical
architecture

This document is the bridge between product vision and implementation.

------------------------------------------------------------------------

# 1. Product Core

SalonCRM is NOT primarily a scheduling application.

Appointment management is only one capability.

The main product value is:

> Helping service businesses bring customers back by understanding
> customer behavior and enabling timely follow-up.

The core business loop:

    Customer
       ↓
    Appointment
       ↓
    Visit
       ↓
    Service History
       ↓
    Return Analysis
       ↓
    Smart List
       ↓
    Action
       ↓
    Customer Returns

Every technical decision should support this loop.

------------------------------------------------------------------------

# 2. Initial Market and General Architecture

The first target market is women's beauty salons.

However, the architecture must not become salon-specific.

The system should support other service businesses in the future.

Examples:

-   Beauty salon
-   Repair services
-   Clinics
-   Periodic maintenance services

The concept of:

Customer + Service + Visit + Return Cycle

is the generic core.

------------------------------------------------------------------------

# 3. MVP Philosophy

The MVP must be useful for a one-person business.

A single-person salon is not an edge case.

The product must work naturally when:

-   One person owns the business.
-   The same person provides services.
-   There are no employees.
-   There are no complex permissions.

Avoid forcing enterprise concepts into the first experience.

------------------------------------------------------------------------

# 4. Important Product Decisions

## Decision: Business is the Tenant

Reason:

Every operational record belongs to a business.

Business owns:

-   Customers
-   Staff
-   Services
-   Appointments
-   Visits
-   Reminders

The system is multi-tenant by design.

------------------------------------------------------------------------

# Decision: User is Different from Staff

Reason:

A person who logs into the system is not necessarily a service provider.

Example:

Owner:

-   Has User account
-   Has Staff record

Employee:

-   Has Staff record
-   May not have User account

Therefore:

User != Staff

------------------------------------------------------------------------

# Decision: Appointment is Different from Visit

This is one of the most important domain decisions.

Appointment:

Represents planned work.

Example:

"Customer will come tomorrow at 2 PM."

Visit:

Represents reality.

Example:

"Customer actually came and received these services."

They cannot be the same entity.

Reasons:

-   Customer may not show up.
-   Services may change.
-   Additional services may be added.
-   Final price may differ.

Therefore:

    Appointment = Plan

    Visit = Reality

------------------------------------------------------------------------

# Decision: Visit Can Exist Without Appointment

Reason:

Real businesses do not always work by appointment.

Examples:

-   Walk-in customer
-   Phone booking
-   Emergency visit

Therefore:

Visit.AppointmentId is optional.

------------------------------------------------------------------------

# Decision: ExpectedReturn Is Not Stored

ExpectedReturn is an analysis result.

It is not a business transaction.

The calculation:

    Last VisitService
    +
    SuggestedReturnDays
    =
    ExpectedReturnDate

is performed dynamically.

Do not create:

    ExpectedReturns Table

Reason:

The result can change as new information arrives.

------------------------------------------------------------------------

# Decision: Smart Lists Are Queries, Not Entities

Smart Lists are generated views of business opportunities.

Examples:

-   Customers overdue for return
-   Customers approaching return time
-   Customers at risk

Do not create:

    SmartList Table

for MVP.

Reason:

The list is logic, not stored business data.

------------------------------------------------------------------------

# Decision: Reminder Is Manual in MVP

ExpectedReturn does not automatically create reminders.

Flow:

    Analysis
       ↓
    Smart List
       ↓
    Human Decision
       ↓
    Reminder

Reason:

Automatic reminders can create noise.

The system should suggest actions, not overwhelm users.

------------------------------------------------------------------------

# Decision: SMS Is Future Capability

SMS is planned but not part of MVP implementation.

Potential future locations:

-   Appointment reminders
-   Return follow-up
-   Campaigns
-   Post-visit messages

The architecture should allow adding communication actions later.

------------------------------------------------------------------------

# Decision: Snapshot Historical Data

Historical records must not change when master data changes.

Example:

Today:

Service: Coloring Price: 2,000,000

Next month:

Service: Coloring Price: 3,000,000

Old visits must remain:

Price: 2,000,000

Therefore snapshot fields exist in:

-   AppointmentService
-   VisitService

------------------------------------------------------------------------

# Decision: Staff-Service Restriction Does Not Exist in MVP

Any staff can perform any service.

Do not create:

    StaffService

yet.

Reason:

Most small businesses do not need this complexity initially.

Can be added later.

------------------------------------------------------------------------

# Decision: Service Template Exists

Templates help onboarding.

Example:

System suggests:

-   Hair coloring
-   Nail service
-   Facial

The user selects one.

The system copies it into Business Service.

Important:

Template is not connected after creation.

Flow:

    ServiceTemplate
            |
            Copy
            ↓
    Business Service

------------------------------------------------------------------------

# 5. Main User Experience Flow

First setup:

    Register
       ↓
    Create Business
       ↓
    Create Owner Member
       ↓
    Create Owner Staff
       ↓
    Create First Service
       ↓
    Dashboard

First operational flow:

    Create Customer
       ↓
    Create Appointment
       ↓
    Complete Appointment
       ↓
    Create Visit
       ↓
    Calculate Return
       ↓
    Show Smart List
       ↓
    Create Reminder if needed

------------------------------------------------------------------------

# 6. Things We Are Intentionally NOT Building

Do not add these without approval:

-   Branch management
-   Online booking
-   Payment system
-   Accounting
-   Inventory
-   Marketing campaigns
-   SMS provider
-   Mobile app
-   Advanced permissions
-   Automatic reminder engine

------------------------------------------------------------------------

# 7. Development Principles

When implementing:

1.  Prefer simplicity.
2.  Preserve MVP boundaries.
3.  Do not add enterprise complexity.
4.  Respect domain decisions.
5.  Ask before changing core assumptions.

A technically elegant solution that damages product simplicity is not
acceptable.

------------------------------------------------------------------------

# 8. Guidance for Codex

Before coding:

Read these documents together:

1.  SalonCRM-Project-Understanding.md
    -   Product explanation
2.  SalonCRM-Product-Context-And-Decision-Log.md
    -   Product decisions and reasons
3.  SalonCRM-MVP-Architecture-Development-Specification.md
    -   Technical implementation details

These documents together represent the source of truth for the project.

Do not redesign the product based on assumptions.

Implement according to these decisions.
