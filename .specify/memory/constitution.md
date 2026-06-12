<!--
SYNC IMPACT REPORT
==================
Version change: [unversioned template] → 1.0.0
Modified principles: N/A (initial ratification)
Added sections: Core Principles (5), Security Requirements, Development Workflow, Governance
Removed sections: N/A
Templates requiring updates:
  - .specify/templates/plan-template.md ✅ (Constitution Check section references principles)
  - .specify/templates/spec-template.md ✅ (no changes required)
  - .specify/templates/tasks-template.md ✅ (no changes required)
Follow-up TODOs: none
-->

# ContosoDashboard Constitution

## Core Principles

### I. Spec-Driven Development (NON-NEGOTIABLE)

Every feature or change MUST begin with a written specification before any implementation work starts.
Specifications are stored in `specs/[###-feature-name]/` and MUST include: user stories with acceptance
scenarios, a plan, and a task list. Skipping specification to save time is not permitted. The spec is
the contract between stakeholders and developers.

### II. Training-Context Simplicity

ContosoDashboard is a training project. All implementation decisions MUST favor clarity and
learnability over production-grade complexity. Code MUST demonstrate good practices in a simplified
form. Dependencies on external cloud services, paid APIs, or infrastructure not available offline are
PROHIBITED. The system MUST remain runnable in an offline environment.

### III. Security-Aware Design (Defense in Depth)

Even in a training context, security practices MUST be demonstrated correctly.
Every protected page MUST carry `[Authorize]` attributes. Service-level authorization checks MUST
be present to prevent IDOR vulnerabilities. Security headers (CSP, X-Frame-Options, etc.) MUST be
applied. The mock authentication system is permitted ONLY because this is a training project; any
real deployment MUST replace it with a proper identity provider (Azure AD, Auth0, or equivalent).

### IV. Service-Layer Isolation

Business logic MUST reside in the `Services/` layer. Razor components and pages MUST NOT contain
business logic or direct data-access code. Each service class MUST have a single, clearly named
responsibility (e.g., `TaskService`, `ProjectService`). This separation ensures testability and
mirrors production-grade architectural patterns appropriate for training audiences.

### V. Incremental, Story-Driven Delivery

Features MUST be broken into independently testable user stories (P1, P2, P3…). Each story MUST
be implementable, testable, and demonstrable in isolation without requiring other stories to be
complete. Tasks MUST be organized by user story so any story can serve as an MVP increment.
Gold-plating and scope expansion beyond specified stories are prohibited.

## Security Requirements

- Authentication: Cookie-based with 8-hour sliding expiration (training only; production requires OAuth 2.0 / OIDC).
- Authorization: `[Authorize]` on all protected Razor pages; role-based access control (RBAC) enforced at both
  page and service level.
- IDOR protection: Service methods MUST validate that the requesting user has rights to the requested resource.
- Security headers: CSP, `X-Frame-Options`, `X-XSS-Protection` MUST be configured in middleware.
- No secrets or credentials committed to source control.
- Known limitation: No password hashing, MFA, or audit logging — documented in README as training-only gaps.

## Development Workflow

- All feature work starts with `/speckit.specify` → `/speckit.plan` → `/speckit.tasks` before any code is written.
- Pull requests MUST reference the feature spec (`specs/[###-feature-name]/spec.md`).
- Constitution Check in `plan.md` MUST be completed before Phase 0 research begins.
- Agent context file (`.github/copilot-instructions.md` or equivalent) MUST be refreshed after constitution
  amendments using the `speckit.agent-context.update` extension hook.
- Commit messages MUST follow Conventional Commits: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.

## Governance

This constitution supersedes all other documented practices. Amendments require:
1. A clear rationale explaining why the change improves training value or correctness.
2. Version bump following semantic versioning (MAJOR for principle removals/redefinitions,
   MINOR for new principles or sections, PATCH for clarifications and wording).
3. An update to `LAST_AMENDED_DATE` and increment of `CONSTITUTION_VERSION`.
4. Propagation review of all `.specify/templates/` files and agent context files.

All feature plans MUST include a Constitution Check gate referencing the five Core Principles.
Complexity MUST be justified against Principle II (Training-Context Simplicity) before introduction.

**Version**: 1.0.0 | **Ratified**: 2026-06-11 | **Last Amended**: 2026-06-11
