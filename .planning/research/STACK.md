# Stack Research

**Domain:** Festival Lineup to Spotify Playlist Web App
**Researched:** 2026-01-24
**Confidence:** HIGH

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET | 10.0 LTS | Backend API framework | Latest LTS release (Jan 2026) with 3 years support until Nov 2028. Built-in AI integration, enhanced performance, and C# 14 support. Microsoft's recommended version for new production applications. |
| React | 19.2.1 | Frontend UI library | Latest stable version with security patches (Dec 2025). Server Components stable, Actions API integrated, built-in React Compiler for automatic optimizations. Backed by Meta and fully production-ready. |
| TypeScript | 5.9.3 | Type-safe JavaScript | Latest stable version with excellent React integration. Essential for large applications to catch errors at compile time. @types/react 19.2.8 provides comprehensive React typings. |
| Vite | 7.3.1 | Build tool & dev server | Modern, fast alternative to Create React App (now deprecated). 3-10x faster builds, native ESM support, lightning-fast HMR. Vite 8 beta available but 7.x recommended for production stability. |
| Node.js | 22.12+ | JavaScript runtime | Required by Vite (minimum 20.19+). LTS version recommended for stability. |

### Frontend Framework & Routing

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| React Router | 7.12.0 | Client-side routing | Latest stable version (Jan 2025). Non-breaking upgrade from v6. Simplified package structure - import everything from 'react-router'. Use for navigation between upload, auth, and playlist views. |
| TanStack Query | 5.90.20 | Server state management | Latest version (Jan 2025). Industry standard for data fetching, caching, and synchronization. Essential for managing Spotify API responses and Claude API interactions. Auto-caching, background refetching, optimistic updates. |
| Zustand | Latest | Client state management | Lightweight alternative to Redux for UI state (theme, user preferences). Use alongside TanStack Query - Zustand for client state, TanStack Query for server state. Minimal boilerplate, excellent performance. |

### UI & Styling

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Tailwind CSS | 3.x (stable) or 4.x (cutting-edge) | Utility-first CSS framework | v3.x for production stability, v4.x for 3-10x faster builds (Rust engine). AI-friendly for code generation. Use design tokens in tailwind.config.js for consistent spacing/colors. |
| Headless UI | 2.0+ | Unstyled UI components | Fully accessible components (modals, dropdowns, menus) that work with Tailwind. v2.0 uses data-* attributes for better React Server Components compatibility. |

### Backend API Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ASP.NET Core Minimal APIs | .NET 10 | HTTP API endpoints | Microsoft's recommended approach for new APIs (2025). Faster performance, cleaner code, less boilerplate than controllers. Use for lightweight microservices and simple API structures. |
| SpotifyAPI-NET | 7.2.1 | Spotify Web API client | Latest stable community library (Oct 2024). 74+ endpoints, typed responses, OAuth PKCE support. Most actively maintained C# SDK for Spotify. Install: SpotifyAPI.Web |
| Anthropic.SDK | 5.9.0 | Claude API client | Latest unofficial SDK (Jan 2026). .NET 10 support, ModelContextProtocol integration, Microsoft.Extensions.AI compatibility. More mature than official beta SDK. Install: Anthropic.SDK |

### Azure Infrastructure

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| Azure Front Door | Latest | Global CDN & WAF | Recommended reverse proxy for production SPAs. Handles CORS, caching, SSL termination. 99.99% availability SLA. Route both SPA and API through single domain to simplify CORS. |
| Azure App Service | Premium v4 (Pv4) | Backend hosting | 25% performance uplift with NVMe storage. 99.99% SLA with just 2 instances (instead of 3). Autoinstrumentation for Application Insights. |
| Azure Key Vault | Latest | Secrets management | Production standard for API keys (Spotify, Anthropic). Never hardcode secrets. Use DefaultAzureCredential for passwordless auth. |
| Azure Application Insights | Latest | Monitoring & telemetry | Built-in Azure monitoring. Transition to connection strings (instrumentation keys deprecated Mar 2025). Package: Microsoft.ApplicationInsights.AspNetCore |

### Authentication & Security

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Azure.Identity | Latest | Azure authentication | DefaultAzureCredential for Key Vault access. Works locally (Azure CLI) and in production (Managed Identity). Install: Azure.Identity |
| Azure.Security.KeyVault.Secrets | Latest | Key Vault SDK | Retrieve Spotify/Anthropic API keys at runtime. Use with dependency injection via Microsoft.Extensions.Azure. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| ESLint (flat config) | JavaScript/TypeScript linting | Use eslint.config.mjs with typescript-eslint recommended config and React plugin. Flat config is 2025 standard. |
| Prettier | Code formatting | Install eslint-plugin-prettier for integration. Config: singleQuote, trailingComma: "es5", tabWidth: 2, printWidth: 100. |
| Vitest | Unit testing (frontend) | Modern replacement for Jest. 3x faster, native ESM, built for Vite. Use with @testing-library/react 16.3.0 for component testing. |
| xUnit | Unit testing (backend) | Recommended over NUnit for new .NET projects. Faster, modern design, built-in parallelization, best for .NET 6+ and cloud-native apps. |

### CI/CD & Infrastructure as Code

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| GitHub Actions | Latest | CI/CD pipeline | Native GitHub integration. Use Azure/webapps-deploy action for App Service. Automatic workflow generation from Azure Static Web Apps. |
| Azure Bicep | Latest | Infrastructure as Code | Azure-native IaC with no state management (ARM handles it). Simpler than Terraform for Azure-only projects. MIT licensed. Use for App Service, Key Vault, Front Door provisioning. |

## Installation

### Frontend
```bash
# Core
npm create vite@latest conflicted-lineup -- --template react-ts
cd conflicted-lineup
npm install react-router@latest @tanstack/react-query@latest zustand

# UI
npm install tailwindcss@latest postcss autoprefixer
npm install @headlessui/react

# Dev dependencies
npm install -D eslint@latest typescript-eslint @eslint/js eslint-plugin-react
npm install -D prettier eslint-plugin-prettier eslint-config-prettier
npm install -D vitest @vitejs/plugin-react jsdom @testing-library/react @testing-library/dom
```

### Backend
```bash
# Create .NET 10 Web API
dotnet new web -n ConflictedLineup.Api

# Add packages
dotnet add package SpotifyAPI.Web
dotnet add package Anthropic.SDK
dotnet add package Azure.Identity
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Microsoft.ApplicationInsights.AspNetCore
dotnet add package Microsoft.Extensions.Azure

# Testing
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| ASP.NET Core Minimal APIs | Controller-based APIs | Use controllers for large applications requiring extensive model binding extensibility (IModelBinderProvider, IModelBinder) or complex routing middleware. Minimal APIs can become cluttered in Program.cs as apps scale. |
| Zustand | Redux Toolkit | Use Redux for large enterprise apps with complex state orchestration across many features. Redux has more boilerplate but better DevTools and middleware ecosystem. |
| Zustand | React Context API | Use Context API for simple, low-frequency state (theme, auth status) in small apps with zero dependencies. Context causes unnecessary re-renders in large component trees. |
| SpotifyAPI-NET | Direct HTTP calls | Build custom HTTP client only if you need features not in SpotifyAPI-NET. Library handles OAuth, rate limiting, typed responses automatically. |
| Anthropic.SDK (unofficial) | Official anthropic-sdk-csharp | Official SDK is in beta and less battle-tested. Use official SDK if you need guaranteed support or Microsoft.Extensions.AI.IChatClient interface. |
| Azure Bicep | Terraform | Use Terraform for multi-cloud deployments (AWS, GCP, Azure). Terraform requires state management but supports broader ecosystem. Bicep simpler for Azure-only. |
| Vite | Next.js | Use Next.js if you need SSR, SSG, or ISR. This is a client-side SPA with API backend - no server-side rendering required. Next.js adds unnecessary complexity. |
| TanStack Query | SWR | SWR is lighter but TanStack Query has richer feature set (mutations, optimistic updates, pagination, infinite scroll). Use SWR for simpler data fetching needs. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Create React App | Officially deprecated. Slow builds, outdated tooling. No longer maintained by React team. | Vite - faster, modern, actively maintained |
| Implicit Grant Flow (Spotify OAuth) | Deprecated by Spotify. Removed Nov 27, 2025. Security vulnerabilities. | Authorization Code Flow with PKCE |
| HTTP redirect URIs (Spotify OAuth) | Disabled Nov 27, 2025. Security requirement. | HTTPS redirect URIs (allow http://127.0.0.1 for local dev) |
| Instrumentation Keys (App Insights) | Support ends Mar 31, 2025. No new features or updates. | Connection Strings |
| .NET 7 | Out of support. No security patches. | .NET 8 LTS (support until Nov 2026) or .NET 10 LTS (support until Nov 2028) |
| NuGet package restore in CI without locking | Non-deterministic builds. Version drift across environments. | Use package lock files (packages.lock.json) |
| Access keys hardcoded in config | Security risk. Exposes secrets in source control. | Azure Key Vault + Managed Identity |

## Stack Patterns by Variant

**If deploying to Azure Static Web Apps:**
- Use Static Web Apps for frontend hosting (includes CDN, automatic SSL)
- Use Azure Functions for lightweight API endpoints instead of App Service
- Because Static Web Apps provides integrated hosting for SPAs with serverless APIs

**If requiring server-side rendering:**
- Replace Vite SPA with Next.js 15+
- Deploy to Azure Static Web Apps or App Service
- Because Next.js provides SSR/SSG capabilities for SEO and performance

**If handling high-volume AI requests:**
- Implement request queuing with Azure Service Bus
- Use Azure Functions with Durable Functions for orchestration
- Cache Claude responses in Azure Cosmos DB or Redis
- Because AI API calls are expensive and rate-limited

**If supporting multiple music platforms:**
- Abstract music service API behind interface (IMusicService)
- Create SpotifyService, AppleMusicService implementations
- Use strategy pattern for platform-specific OAuth flows
- Because different platforms have different auth and API patterns

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| .NET 10.0 | SpotifyAPI.Web 7.2.1 | Targets .NET 8.0 but compatible with .NET 10 via multi-targeting |
| .NET 10.0 | Anthropic.SDK 5.9.0 | Explicitly targets .NET 10 |
| React 19.2.1 | React Router 7.12.0 | Fully compatible - React Router 7 designed for React 18+ |
| React 19.2.1 | TanStack Query 5.90.20 | Fully compatible - TanStack Query v5 supports React 18+ |
| Vite 7.3.1 | Node.js 22.12+ | Requires Node 20.19+ or 22.12+ minimum |
| TypeScript 5.9.3 | React 19.2.1 | Use @types/react 19.2.8 for type definitions |
| ESLint flat config | typescript-eslint latest | Flat config is standard for 2025 - use eslint.config.mjs |
| Tailwind CSS 4.x | PostCSS | Tailwind v4 includes Rust engine - PostCSS optional for most use cases |

## Confidence Assessment

| Technology Category | Confidence | Source |
|---------------------|------------|--------|
| .NET 10 LTS | HIGH | Verified via official Microsoft downloads page - 10.0.2 released Jan 13, 2026 |
| React 19.2.1 | HIGH | Verified via official React blog - security patch Dec 3, 2025 |
| SpotifyAPI-NET 7.2.1 | HIGH | Verified via GitHub releases - Oct 12, 2024 release |
| Anthropic.SDK 5.9.0 | HIGH | Verified via NuGet - published Jan 22, 2026 |
| ASP.NET Minimal APIs | HIGH | Official Microsoft recommendation per Learn documentation 2025 |
| Azure App Service Pv4 | HIGH | Public preview announced at Microsoft Build 2025 |
| Spotify PKCE requirement | HIGH | Official Spotify developer blog - migration deadline Nov 27, 2025 |
| Vite as React standard | HIGH | React team official recommendation, CRA deprecated |
| xUnit for .NET testing | MEDIUM | Community consensus from multiple 2025 comparisons, not official Microsoft stance |
| Zustand vs Redux | MEDIUM | Community preference for modern apps, situation-dependent |
| Tailwind v4 production readiness | MEDIUM | Recently released (late 2024), v3.x more battle-tested for production |

## Sources

### Official Documentation (HIGH confidence)
- [.NET Downloads - Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet) - .NET 10 LTS verification
- [React v19.2 - React Blog](https://react.dev/blog) - React version verification
- [SpotifyAPI-NET Releases](https://github.com/JohnnyCrazy/SpotifyAPI-NET/releases) - Library version
- [Anthropic.SDK NuGet](https://www.nuget.org/packages/Anthropic.SDK) - Package version
- [Spotify Authorization PKCE Flow](https://developer.spotify.com/documentation/web-api/tutorials/code-pkce-flow) - OAuth requirements
- [ASP.NET Core APIs Overview - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-10.0) - Minimal APIs recommendation
- [Azure App Service Best Practices - Microsoft Learn](https://learn.microsoft.com/en-us/azure/app-service/app-service-best-practices) - Infrastructure guidance
- [Azure Key Vault Developer's Guide - Microsoft Learn](https://learn.microsoft.com/en-us/azure/key-vault/general/developers-guide) - Security patterns

### Community Resources (MEDIUM-HIGH confidence)
- [React Router Documentation](https://reactrouter.com/) - Latest version 7.12.0
- [TanStack Query Documentation](https://tanstack.com/query/latest) - Version 5.90.20
- [Vite Releases](https://vite.dev/releases) - Version 7.3.1
- [TypeScript Releases](https://github.com/microsoft/typescript/releases) - Version 5.9.3
- [React State Management 2025 Comparison](https://dev.to/cristiansifuentes/react-state-management-in-2025-context-api-vs-zustand-385m) - Zustand vs alternatives
- [xUnit vs NUnit 2025 Comparison](https://www.tatvasoft.com/outsourcing/2025/06/xunit-vs-nunit-vs-mstest.html) - Testing framework recommendation
- [Tailwind CSS v4 Migration Guide](https://medium.com/better-dev-nextjs-react/tailwind-v4-migration-from-javascript-config-to-css-first-in-2025-ff3f59b215ca) - Latest version guidance
- [Azure Bicep vs Terraform 2025](https://xebia.com/blog/infrastructure-as-code-on-azure-bicep-vs-terraform-vs-pulumi/) - IaC comparison

### Key Blog Posts & Announcements (MEDIUM confidence)
- [.NET 10 What's New - Medium](https://medium.com/@sparklewebhelp/net-10-whats-new-in-2025-the-complete-guide-c7b030b490df) - Feature overview
- [React 19 Features 2025 - GrapesTech](https://www.grapestechsolutions.com/blog/reactjs-latest-version-19-updates/) - Production readiness
- [Spotify OAuth Migration Reminder](https://developer.spotify.com/blog/2025-10-14-reminder-oauth-migration-27-nov-2025) - Security requirements
- [Azure App Service at MS Build 2025](https://techcommunity.microsoft.com/blog/appsonazureblog/whats-new-in-azure-app-service-at-msbuild-2025/4412465) - Pv4 announcement
- [Vitest with React Testing Library Guide](https://blog.incubyte.co/blog/vitest-react-testing-library-guide/) - Testing setup
- [Modern ESLint Flat Config 2025](https://advancedfrontends.com/eslint-flat-config-typescript-javascript/) - Linting configuration

---
*Stack research for: Festival Lineup to Spotify Playlist Web App*
*Researched: 2026-01-24*
*Confidence: HIGH - Versions verified with official sources*
