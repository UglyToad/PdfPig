# Security Policy

[![Report a Vulnerability](https://img.shields.io/badge/Report%20a-Vulnerability-red)](https://github.com/UglyToad/PdfPig/security/advisories/new)

_Last updated: 27 August 2025_

Thanks for helping keep **PdfPig** and its users safe. This document explains how to report vulnerabilities, what’s in scope, how we triage, and guidance for safely using PdfPig with untrusted PDFs.

## Supported Versions

We provide security fixes for the latest release and, when feasible, the previous minor line.

| Version | Supported          |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |

If you depend on older versions, please upgrade to receive security patches.

## Reporting a Vulnerability

Please report security issues **privately** so we can triage and prepare a fix before public disclosure.

- **Preferred:** [GitHub Security Advisory](https://github.com/UglyToad/PdfPig/security/advisories/new)  
- **Alternative:** If you cannot use advisories, open a blank issue and immediately email a maintainer with a link asking to convert it to a private advisory (do **not** include details in the public issue).

When reporting, include:

- A clear description of the issue and impact
- A minimal PoC (e.g., a crafted PDF) if possible
- Affected version(s) and your environment
- Suggested fix or mitigation (if known)

We do **not** operate a paid bug bounty. Responsible disclosure credit is gladly given unless you prefer to remain anonymous.

## Coordinated Disclosure & Timelines

We aim to:

- Acknowledge your report within **7 business days**
- Provide an initial triage outcome within **14–30 days**
- Release a fix and advisory as soon as reasonably possible, based on severity and complexity

We will request reasonable time for users to update before public disclosure. Please avoid public discussion until a fix and advisory are published.

## Scope

### In scope

- Vulnerabilities in PdfPig code and release artifacts that could lead to:
  - Memory corruption, crashes, or undefined behavior from crafted PDFs
  - Denial of service (CPU, memory, decompression bombs) beyond reasonable resource expectations
  - Path traversal / unsafe file writes if originating from PdfPig APIs
  - Logic flaws that expose data or break documented expectations

### Out of scope

- Issues in downstream projects that consume PdfPig
- Security problems caused by misuse of the API (e.g., writing unvalidated data to disk)
- Vulnerabilities exclusively in third-party dependencies (please report upstream; we will still track and update)
- Performance limitations that are not security issues
- Feature requests

## Triage & Severity

We use CVSS v3.x as a guide to classify severity (Critical/High/Medium/Low).  
Where appropriate, we will publish a GitHub Security Advisory and request a **CVE** via GitHub’s CNA flow.

Priorities (roughly):

1. Memory safety / RCE vectors  
2. Sandbox escapes enabled by PdfPig behavior  
3. DoS vectors (e.g., decompression bombs) that are not reasonably mitigable by consumers  
4. Path traversal / unsafe file I/O originating from library defaults  
5. Information disclosure through parsing quirks or metadata handling

## Responsible Testing Guidelines

- Do **not** test with live production systems.  
- Use private repositories and data; avoid publicly posting PoCs before disclosure.  
- If sharing a malicious/crafted PDF, consider a password-protected ZIP (password `infected`) via the private advisory flow.  
- Don’t run automated scanners that create spam issues or PRs.
