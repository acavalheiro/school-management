# Spec: Student Files (photos, invoices) — security requirements

Status: not implemented. These are the non-negotiable security requirements any implementation must meet;
objectives, user stories and API shape are still to be written. Moved out of `CLAUDE.md` on 2026-11-23.

## Why

ATL stores personal data about **minors**. Photos and invoices are the most sensitive data the app will hold,
and file serving is the easiest place to break tenant isolation, because blob stores have no EF Core query filter.

## Requirements

### Access

- No public or guessable URLs, and no static-file serving of user content. Every download goes through an
  authorized endpoint that loads the owning record and re-checks its `TenantId` against the caller's.
- Store blobs outside the database with private ACLs. Issue short-lived pre-signed URLs per request, never
  persistent ones.
- Include the tenant in the object key (`{tenantId}/{studentId}/{fileId}`) and validate the key's tenant prefix
  before streaming, so an app-layer bug cannot cross tenants silently.

### Upload

- Verify the type by magic bytes, not `Content-Type` or extension. Whitelist formats and cap the size.
- Generate the filename on the server.
- **Strip EXIF from images**: geolocation in photos of children is a real risk.

### Download

- Send `Content-Disposition: attachment` and `X-Content-Type-Options: nosniff`.

### Storage, audit and erasure

- Encrypt at rest.
- Audit-log every access: who, when, which student.
- Deletion removes the blob, not just the row.
- Photos of minors need a recorded consent basis and a working right-to-erasure path.

## Testing

Per `CLAUDE.md`, file serving is security-critical: add integration tests for cross-tenant download attempts
(different tenant's file id, tampered object key) and confirm they fail if the tenant check is removed.
