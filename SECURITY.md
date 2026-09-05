# Security Policy

Dowe LanCaster is a Windows-to-Roku application that discovers devices and
streams media over a local network. Security reports are welcome, especially
when they involve network exposure, untrusted media or URLs, command execution,
installer integrity, or unintended access to local files.

## Supported versions

Security fixes are provided for the latest released version. Older releases may
not receive patches.

<!-- supported-versions:start -->
| Version | Supported |
| --- | --- |
| 0.9.5.34 | Yes |
| 0.9.5.33 and earlier | No |
<!-- supported-versions:end -->

<br />

Users should upgrade to the newest release before reporting a problem that may
already have been corrected.

## Reporting a vulnerability

Please do **not** disclose suspected vulnerabilities in a public issue,
discussion, pull request, social-media post, or other public channel.

Use GitHub's private vulnerability reporting feature:

1. Open this repository on GitHub.
2. Select **Security**.
3. Select **Advisories**.
4. Select **Report a vulnerability**.

If private vulnerability reporting is not available, open a public issue that
contains only the statement that you need a private security contact. Do not
include exploit details, logs, URLs, IP addresses, credentials, personal data,
or other sensitive information. A maintainer can then arrange a private channel.

## What to include

A useful report contains as much of the following information as possible:

* The affected Dowe LanCaster version and how it was installed.
* The Windows and Roku models/versions involved.
* The affected feature, such as Link Cast, Live Cast, File Cast, Folder Cast,
  Roku discovery, the remote control, the local streaming server, or the
  installer.
* A clear description of the security impact and who could exploit it.
* Reproduction steps or a minimal proof of concept.
* Whether exploitation requires access to the same local network.
* Relevant ports, requests, media types, filenames, or sanitized logs.
* Any mitigations or suggested fixes you have identified.

Remove secrets and personal information from screenshots, recordings, logs, and
sample files. Do not submit copyrighted media that you are not authorized to
share.

## Response process

Maintainers will make a best-effort attempt to:

1. Acknowledge a complete report within 7 calendar days.
2. Confirm whether the issue can be reproduced and is in scope.
3. Assess severity, affected versions, and available mitigations.
4. Coordinate a fix and disclosure date with the reporter when appropriate.
5. Credit the reporter in the advisory or release notes if requested.

Response and remediation times depend on severity, reproducibility, project
resources, and the involvement of upstream dependencies. Please allow a
reasonable remediation period before publishing technical details.

## In-scope examples

Examples of issues that are generally in scope include:

* Remote code execution or command/argument injection.
* Unsafe handling of untrusted URLs, playlists, filenames, or media metadata.
* Path traversal or unintended reading, writing, or disclosure of local files.
* Local streaming servers being reachable beyond the intended private network.
* Missing authorization that allows another LAN device to control casting or
  access streamed content unexpectedly.
* Server-side request forgery or access to unintended local/network resources.
* Installer, update, release-archive, or dependency-integrity weaknesses.
* Exposure of credentials, tokens, private URLs, IP addresses, or sensitive
  diagnostic information.
* Vulnerabilities in the bundled Roku receiver or its communication with the
  Windows application.
* Denial-of-service conditions that are reliable and have meaningful security
  impact.

The application may use or interact with FFmpeg, yt-dlp, .NET, Windows media
APIs, Roku ECP, and third-party media services. Reports should explain how the
issue affects Dowe LanCaster rather than only identifying an upstream version.
Upstream vulnerabilities should also be reported to the relevant upstream
project when appropriate.

## Generally out of scope

The following are normally not treated as Dowe LanCaster security
vulnerabilities unless they demonstrate additional concrete security impact:

* A media site being unsupported, changing behavior, or blocking extraction.
* DRM, paywall, subscription, authentication, or protected-playback bypass
  requests.
* Problems that require an already fully compromised Windows account.
* Self-XSS or actions that require a user to paste and execute arbitrary code.
* Missing hardening recommendations without a demonstrated attack path.
* Automated dependency or scanner output without a reproducible impact on the
  application.
* Denial of service that only affects the reporter's own session and is resolved
  by restarting the application.
* Social engineering, phishing, or physical attacks.

## Testing guidelines

Security research must use devices, accounts, networks, and media that you own
or are explicitly authorized to test. Do not:

* Access, modify, retain, or destroy another person's data.
* Interrupt another person's Roku device, network, or streaming session.
* Perform broad network scanning or high-volume traffic against systems you do
  not control.
* Attempt to bypass DRM, paywalls, authentication, or service access controls.
* Use a vulnerability for persistence, lateral movement, or data exfiltration
  beyond the minimum evidence required to demonstrate the issue.

Stop testing and report the issue if you encounter data belonging to another
person.

## Coordinated disclosure and safe harbor

The project asks reporters to keep vulnerability details confidential until a
fix or agreed disclosure date is available. Maintainers will not pursue legal
action against good-faith research that follows this policy, avoids privacy and
service disruption, and complies with applicable law. This statement does not
authorize testing of third-party systems and cannot bind third parties.

Thank you for helping keep Dowe LanCaster users and their local networks safe.
