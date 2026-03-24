# GitHub SDKs: A Comprehensive Research Report

**Date:** March 24, 2026
**Research Query:** `github/github-sdk`

---

## Executive Summary

There is no single repository at `github/github-sdk`. Instead, GitHub maintains **two distinct SDK ecosystems** for different purposes:

1. **Octokit** — The official GitHub REST/GraphQL API client libraries, maintained under the [`octokit`](https://github.com/octokit) organization. These are the traditional SDKs for interacting with GitHub's platform APIs (repos, issues, PRs, users, etc.). Available in JavaScript/TypeScript, Ruby, .NET, Go, and Objective-C. The newer Go and .NET SDKs are **auto-generated** from GitHub's OpenAPI spec using Microsoft's Kiota tool[^1].

2. **Copilot SDK** — A newer, fast-growing SDK at [`github/copilot-sdk`](https://github.com/github/copilot-sdk) (8k stars) for embedding GitHub Copilot's agentic AI workflows into applications. Available in TypeScript, Python, Go, .NET, and Java. Currently in **Technical Preview**[^2].

Additionally, GitHub maintains **GitHub Models AI SDK** ([`github/models-ai-sdk`](https://github.com/github/models-ai-sdk)) for integrating with GitHub's model inference catalog, and **Copilot Engine SDK** ([`github/copilot-engine-sdk`](https://github.com/github/copilot-engine-sdk)) for building engines on the Copilot agent platform[^3][^4].

---

## Table of Contents

- [GitHub SDKs: A Comprehensive Research Report](#github-sdks-a-comprehensive-research-report)
  - [Executive Summary](#executive-summary)
  - [Table of Contents](#table-of-contents)
  - [Part 1: Octokit — GitHub API SDKs](#part-1-octokit--github-api-sdks)
    - [Architecture Overview](#architecture-overview)
    - [Hand-Maintained SDKs](#hand-maintained-sdks)
      - [octokit.js (JavaScript/TypeScript) — ⭐ 7.7k](#octokitjs-javascripttypescript---77k)
      - [octokit.rb (Ruby) — ⭐ 3.9k](#octokitrb-ruby---39k)
      - [octokit.net (C#/.NET) — ⭐ 2.8k](#octokitnet-cnet---28k)
      - [octokit.objc (Objective-C) — ⭐ 1.8k](#octokitobjc-objective-c---18k)
    - [Generated SDKs (Kiota)](#generated-sdks-kiota)
      - [dotnet-sdk (C#/.NET — Generated) — ⭐ 64](#dotnet-sdk-cnet--generated---64)
      - [go-sdk (Go — Generated) — ⭐ 86](#go-sdk-go--generated---86)
    - [OpenAPI Specification](#openapi-specification)
    - [Source Generator Pipeline](#source-generator-pipeline)
  - [Part 2: Copilot SDK — Agentic AI Integration](#part-2-copilot-sdk--agentic-ai-integration)
    - [Copilot SDK Architecture](#copilot-sdk-architecture)
    - [Language SDKs](#language-sdks)
    - [Key Features](#key-features)
      - [Core Concepts](#core-concepts)
      - [Event System](#event-system)
      - [Custom Tool Definition (Node.js Example)](#custom-tool-definition-nodejs-example)
      - [Permission Handling](#permission-handling)
    - [Copilot SDK Authentication](#copilot-sdk-authentication)
  - [Part 3: Supporting SDKs](#part-3-supporting-sdks)
    - [GitHub Models AI SDK](#github-models-ai-sdk)
    - [Copilot Engine SDK](#copilot-engine-sdk)
  - [Key Repositories Summary](#key-repositories-summary)
    - [Octokit (GitHub API SDKs)](#octokit-github-api-sdks)
    - [Copilot \& AI SDKs](#copilot--ai-sdks)
  - [Choosing the Right SDK](#choosing-the-right-sdk)
  - [Confidence Assessment](#confidence-assessment)
  - [Footnotes](#footnotes)

---

## Part 1: Octokit — GitHub API SDKs

### Architecture Overview

Octokit is GitHub's official collection of client libraries for the GitHub REST and GraphQL APIs. The ecosystem is organized under the [`octokit`](https://github.com/octokit) GitHub organization and consists of two generations:

```text
┌──────────────────────────────────────────────────────────────────┐
│                    GitHub Platform APIs                          │
│              (REST API v3 + GraphQL API v4)                      │
└───────────────┬──────────────────────────┬───────────────────────┘
                │                          │
    ┌───────────▼───────────┐  ┌───────────▼───────────────────┐
    │  Hand-Maintained SDKs │  │  Generated SDKs (Kiota)       │
    │  ─────────────────    │  │  ──────────────────────        │
    │  • octokit.js (TS)    │  │  • dotnet-sdk (C#)            │
    │  • octokit.rb (Ruby)  │  │  • go-sdk (Go)                │
    │  • octokit.net (C#)   │  │                               │
    │  • octokit.objc (ObjC)│  │  Auto-generated from:         │
    └───────────────────────┘  │  github/rest-api-description   │
                               │  via octokit/source-generator  │
                               └───────────────────────────────┘
```

**Generation 1 (Hand-Maintained):** Traditional, manually-written API clients with idiomatic language designs. These remain actively maintained and are the most mature[^5].

**Generation 2 (Auto-Generated):** Newer SDKs automatically generated from GitHub's OpenAPI specification using Microsoft's [Kiota](https://github.com/microsoft/kiota) tool. Announced January 2024, these provide near-100% REST API coverage automatically[^1].

### Hand-Maintained SDKs

#### octokit.js (JavaScript/TypeScript) — ⭐ 7.7k

The flagship SDK. An "all-batteries-included" package integrating three sub-libraries[^6]:

1. **API client** — REST API requests, GraphQL queries, authentication
2. **App client** — GitHub App & installations, Webhooks, OAuth
3. **Action client** — Pre-authenticated client for GitHub Actions

**Key features:**

- Universal (browsers, Node.js, Deno)
- 100% test coverage, full TypeScript declarations
- Plugin architecture: throttling (`@octokit/plugin-throttling`), retry (`@octokit/plugin-retry`), pagination
- REST endpoint methods auto-generated from OpenAPI spec
- Multiple auth strategies (token, GitHub App, OAuth)

**Package ecosystem:**

| Package                                                                                                 | Purpose                    | Stars |
| ------------------------------------------------------------------------------------------------------- | -------------------------- | ----- |
| [`octokit/core.js`](https://github.com/octokit/core.js)                                                 | Minimal, extendable client | 1.3k  |
| [`octokit/rest.js`](https://github.com/octokit/rest.js)                                                 | REST API client            | 651   |
| [`octokit/graphql.js`](https://github.com/octokit/graphql.js)                                           | GraphQL API client         | 493   |
| [`octokit/request.js`](https://github.com/octokit/request.js)                                           | HTTP request layer         | 258   |
| [`octokit/webhooks.js`](https://github.com/octokit/webhooks.js)                                         | Webhook event handling     | 338   |
| [`octokit/app.js`](https://github.com/octokit/app.js)                                                   | GitHub App toolset         | 186   |
| [`octokit/auth-app.js`](https://github.com/octokit/auth-app.js)                                         | App authentication         | 170   |
| [`octokit/action.js`](https://github.com/octokit/action.js)                                             | GitHub Actions client      | 210   |
| [`octokit/plugin-throttling.js`](https://github.com/octokit/plugin-throttling.js)                       | Rate limit handling        | 126   |
| [`octokit/plugin-rest-endpoint-methods.js`](https://github.com/octokit/plugin-rest-endpoint-methods.js) | REST endpoint methods      | 133   |

**Installation & Usage:**

```typescript
import { Octokit, App } from "octokit";

const octokit = new Octokit({ auth: `personal-access-token123` });

// REST API - typed endpoint methods
const { data: { login } } = await octokit.rest.users.getAuthenticated();

// REST API - raw request
await octokit.request("POST /repos/{owner}/{repo}/issues", {
  owner: "octocat", repo: "hello-world",
  title: "Hello, world!",
});

// GraphQL
const { repository } = await octokit.graphql(`{
  repository(owner: "octocat", name: "hello-world") {
    issues(last: 3) { nodes { title } }
  }
}`);

// Pagination
for await (const { data: issues } of octokit.paginate.iterator(
  octokit.rest.issues.listForRepo, { owner: "octocat", repo: "hello-world" }
)) {
  for (const issue of issues) console.log(issue.title);
}
```

#### octokit.rb (Ruby) — ⭐ 3.9k

Ruby toolkit following Ruby idioms with a flat API client[^7]:

```ruby
client = Octokit::Client.new(access_token: 'token')
user = client.user('octocat')
puts user.name

# Auto-pagination
client.auto_paginate = true
issues = client.issues('rails/rails')

# GitHub Enterprise support
Octokit.configure { |c| c.api_endpoint = "https://ghe.example.com/api/v3/" }
```

- Supports `.netrc` file authentication
- Includes `EnterpriseAdminClient` and `ManageGHESClient` for GHES
- Targets Ruby with idiomatic Faraday-based HTTP

#### octokit.net (C#/.NET) — ⭐ 2.8k

Hand-maintained .NET client library[^8]:

```csharp
var github = new GitHubClient(new ProductHeaderValue("MyApp"));
var user = await github.User.Get("octocat");
Console.WriteLine($"{user.Followers} followers");
```

- Targets .NET Framework 4.6.1+ and .NET Standard 2.0+
- Also offers `Octokit.Reactive` (Rx.NET-based) on NuGet
- NuGet package: `Octokit`

> **Note:** This is the **classic**, hand-maintained .NET client. The newer **generated** .NET SDK is at [`octokit/dotnet-sdk`](https://github.com/octokit/dotnet-sdk) (see below).

#### octokit.objc (Objective-C) — ⭐ 1.8k

Legacy Objective-C client. No updates since December 2018[^9].

### Generated SDKs (Kiota)

In January 2024, GitHub announced a shift toward **auto-generated SDKs** built with Microsoft's [Kiota](https://github.com/microsoft/kiota) tool from GitHub's [OpenAPI specification](https://github.com/github/rest-api-description)[^1]. These SDKs provide near-100% REST API coverage and are automatically updated when the API changes.

> "By generating what is known, we will focus on the more interesting question of what's needed next." — Nick Floyd, GitHub Blog[^1]

#### dotnet-sdk (C#/.NET — Generated) — ⭐ 64

**Status:** Alpha. Not yet stable; breaking changes may occur[^10].

**NuGet package:** `GitHub.Octokit.SDK`

Targets all three GitHub products:

| Variant           | Repository                                                                                        | NuGet Package             |
| ----------------- | ------------------------------------------------------------------------------------------------- | ------------------------- |
| GitHub.com        | [`octokit/dotnet-sdk`](https://github.com/octokit/dotnet-sdk)                                     | `GitHub.Octokit.SDK`      |
| Enterprise Cloud  | [`octokit/dotnet-sdk-enterprise-cloud`](https://github.com/octokit/dotnet-sdk-enterprise-cloud)   | `GitHub.Octokit.GHEC.SDK` |
| Enterprise Server | [`octokit/dotnet-sdk-enterprise-server`](https://github.com/octokit/dotnet-sdk-enterprise-server) | `GitHub.Octokit.GHES.SDK` |

**Usage:**

```csharp
using GitHub;
using GitHub.Octokit.Client;
using GitHub.Octokit.Client.Authentication;

var tokenProvider = new TokenProvider(Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? "");
var adapter = RequestAdapter.Create(new TokenAuthProvider(tokenProvider));
var gitHubClient = new GitHubClient(adapter);

var repos = await gitHubClient.Repositories.GetAsync();
repos?.ForEach(repo => Console.WriteLine(repo.FullName));
```

**Source organization:**

- `Authentication` — credential handling
- `Client` — API plumbing construction
- `Middleware` — request/response mutators
- `Octokit` — request/response type models (generated)

#### go-sdk (Go — Generated) — ⭐ 86

**Status:** Alpha[^11].

Also has Enterprise Cloud and Enterprise Server variants:

| Variant           | Repository                                                                                |
| ----------------- | ----------------------------------------------------------------------------------------- |
| GitHub.com        | [`octokit/go-sdk`](https://github.com/octokit/go-sdk)                                     |
| Enterprise Cloud  | [`octokit/go-sdk-enterprise-cloud`](https://github.com/octokit/go-sdk-enterprise-cloud)   |
| Enterprise Server | [`octokit/go-sdk-enterprise-server`](https://github.com/octokit/go-sdk-enterprise-server) |

**Usage:**

```go
client, err := pkg.NewApiClient(
    pkg.WithTokenAuthentication(os.Getenv("GITHUB_TOKEN")),
)
// or GitHub App authentication:
client, err := pkg.NewApiClient(
    pkg.WithGitHubAppAuthentication("/path/to/pem", "client-ID", installationID),
)
```

### OpenAPI Specification

The foundation for generated SDKs is [`github/rest-api-description`](https://github.com/github/rest-api-description)[^12], which contains machine-readable OpenAPI descriptions of GitHub's REST API.

- **Version:** Stable since release 1.1.4
- **Formats:** OpenAPI 3.0 (`descriptions/`) and 3.1 (`descriptions-next/`)
- **Each document is available in:** bundled (preferred) and dereferenced formats
- **Automatically kept in sync** with the actual GitHub API validation layer
- **Not accepting direct PRs** — changes flow from GitHub's internal API description
- Uses vendor extensions (`x-multi-segment`, etc.) for GitHub-specific concepts

### Source Generator Pipeline

The [`octokit/source-generator`](https://github.com/octokit/source-generator) repository contains the code generation pipeline[^13]:

```
github/rest-api-description (OpenAPI spec)
         │
         ▼
octokit/source-generator (Kiota generation scripts)
         │
    ┌────┴────┐
    ▼         ▼
go-sdk    dotnet-sdk
```

- Runs `./scripts/generate-go.sh` or `./scripts/generate-csharp.sh`
- Periodically ingests latest OpenAPI spec via GitHub Actions workflows
- Generates PRs to the SDK repos when diffs exist (e.g., `build-go.yml`)
- Requires .NET 7, latest Go, and Kiota CLI (`dotnet tool install --global Microsoft.OpenApi.Kiota`)
- Design rationale documented in `docs/DESIGN.md`

---

## Part 2: Copilot SDK — Agentic AI Integration

### Copilot SDK Architecture

The [**GitHub Copilot SDK**](https://github.com/github/copilot-sdk) (8k ⭐, 1k forks) is a multi-platform SDK for embedding GitHub Copilot's agentic workflows programmatically. It exposes the same engine behind the Copilot CLI as a programmable runtime[^2].

**Status:** Technical Preview (not yet production-ready)

```
┌─────────────────────────┐
│   Your Application      │
│   (Python/TS/Go/.NET)   │
└───────────┬─────────────┘
            │  SDK Client API
            ▼
┌─────────────────────────┐
│   Copilot SDK           │
│   (Language-specific)   │
└───────────┬─────────────┘
            │  JSON-RPC (stdio or TCP)
            ▼
┌─────────────────────────┐
│   Copilot CLI            │
│   (Server Mode)          │
│   ─────────────────      │
│   • Agent Runtime        │
│   • Tool Invocation      │
│   • File Edits           │
│   • Planning             │
│   • Model Management     │
└─────────────────────────┘
```

All SDKs communicate with the Copilot CLI server via **JSON-RPC**. The SDK manages CLI process lifecycle automatically, but can also connect to an external CLI server[^2].

### Language SDKs

| SDK                    | Location                                                                | Package                                   | Status            |
| ---------------------- | ----------------------------------------------------------------------- | ----------------------------------------- | ----------------- |
| **Node.js/TypeScript** | [`nodejs/`](https://github.com/github/copilot-sdk/tree/main/nodejs)     | `npm install @github/copilot-sdk`         | Technical Preview |
| **Python**             | [`python/`](https://github.com/github/copilot-sdk/tree/main/python)     | `pip install github-copilot-sdk`          | Technical Preview |
| **Go**                 | [`go/`](https://github.com/github/copilot-sdk/tree/main/go)             | `go get github.com/github/copilot-sdk/go` | Technical Preview |
| **.NET**               | [`dotnet/`](https://github.com/github/copilot-sdk/tree/main/dotnet)     | `dotnet add package GitHub.Copilot.SDK`   | Technical Preview |
| **Java**               | [`github/copilot-sdk-java`](https://github.com/github/copilot-sdk-java) | Maven: `com.github:copilot-sdk-java`      | Technical Preview |

The Java SDK is maintained in a separate repository and tracks the .NET reference implementation via an **agentic upstream merge** process — a weekly GitHub Actions workflow that detects upstream changes, creates an issue, and assigns it to the Copilot coding agent for automatic porting[^14].

**Community (unofficial) SDKs:** Rust, Clojure, C++[^2].

### Key Features

#### Core Concepts

1. **`CopilotClient`** — Manages the CLI server lifecycle and connections
2. **`CopilotSession`** — Represents a single conversation session with event-driven message handling
3. **Custom Tools** — Define tools that Copilot can invoke (weather lookup, database queries, etc.)
4. **Streaming** — Receive assistant responses as chunks via `assistant.message_delta` events
5. **BYOK (Bring Your Own Key)** — Use your own API keys from OpenAI, Azure, Anthropic without GitHub auth[^2]
6. **Infinite Sessions** — Automatic context compaction for long-running sessions
7. **Session Persistence** — Save and resume sessions across application restarts
8. **MCP Integration** — Model Context Protocol server support

#### Event System

Sessions emit typed events during processing[^15]:

| Event                       | Description                 |
| --------------------------- | --------------------------- |
| `user.message`              | User message added          |
| `assistant.message`         | Complete assistant response |
| `assistant.message_delta`   | Streaming response chunk    |
| `assistant.reasoning`       | Chain-of-thought content    |
| `assistant.reasoning_delta` | Streaming reasoning chunk   |
| `tool.execution_start`      | Tool execution started      |
| `tool.execution_complete`   | Tool execution completed    |
| `session.idle`              | Session finished processing |
| `session.usage_info`        | Token usage metrics         |

#### Custom Tool Definition (Node.js Example)

```typescript
import { CopilotClient, defineTool } from "@github/copilot-sdk";

const getWeather = defineTool("get_weather", {
  description: "Get the current weather for a city",
  parameters: {
    type: "object",
    properties: {
      city: { type: "string", description: "The city name" },
    },
    required: ["city"],
  },
  handler: async (args: { city: string }) => {
    return { city: args.city, temperature: "62°F", condition: "cloudy" };
  },
});

const client = new CopilotClient();
const session = await client.createSession({
  model: "gpt-4.1",
  tools: [getWeather],
  onPermissionRequest: approveAll,
});
```

#### Permission Handling

Every tool execution requires explicit approval. Use `approveAll` for development or implement a custom handler for production[^15]:

```typescript
const session = await client.createSession({
  onPermissionRequest: async (request) => {
    if (request.toolName === "dangerous_tool") {
      return { approved: false, reason: "Not allowed" };
    }
    return { approved: true };
  },
});
```

### Copilot SDK Authentication

Four authentication methods[^2]:

1. **GitHub signed-in user** — Uses stored OAuth credentials from `copilot` CLI login
2. **OAuth GitHub App** — Pass user tokens from your GitHub OAuth app
3. **Environment variables** — `COPILOT_GITHUB_TOKEN`, `GH_TOKEN`, `GITHUB_TOKEN`
4. **BYOK** — Use your own API keys (no GitHub auth required)

**Billing:** SDK usage counts toward your Copilot premium request quota. A free tier with limited usage is available[^2].

---

## Part 3: Supporting SDKs

### GitHub Models AI SDK

[`github/models-ai-sdk`](https://github.com/github/models-ai-sdk) (20 ⭐) — A provider for the [Vercel AI SDK](https://ai-sdk.dev/docs) that connects to [GitHub Models](https://github.com/features/models), GitHub's catalog of LLMs from providers like xAI, OpenAI, and Meta[^3].

**Installation:** `npm i @github/models`

```typescript
import { githubModels } from '@github/models';
import { generateText } from 'ai';

const result = await generateText({
  model: githubModels('meta/meta-llama-3.1-8b-instruct'),
  prompt: 'Write a haiku about programming.',
});
```

**Configuration options:** `apiKey`, `org` (organization billing attribution), `baseURL` (defaults to `https://models.github.ai/inference`), `headers`, custom `fetch`.

### Copilot Engine SDK

[`github/copilot-engine-sdk`](https://github.com/github/copilot-engine-sdk) (4 ⭐) — SDK for building **engines** that run on the GitHub Copilot agent platform[^4]. This is a lower-level SDK for building autonomous coding agents that can:

- Clone repositories and push changes
- Send structured events to the platform API
- Manage PR descriptions and comment replies
- Discover user-configured MCP servers

**Key components:**

| Component                     | Purpose                                                        |
| ----------------------------- | -------------------------------------------------------------- |
| `PlatformClient`              | Event reporting to the agent platform API                      |
| `cloneRepo` / `commitAndPush` | Secure git operations (tokens via `http.extraHeader`)          |
| `createEngineMcpServer`       | MCP server with `report_progress` and `reply_to_comment` tools |
| `discoverMCPServers`          | MCP proxy discovery for user-configured servers                |
| `engine-cli`                  | CLI testing harness (Go) that simulates the platform locally   |

**Environment variables received from the platform:**

| Variable                    | Description                   |
| --------------------------- | ----------------------------- |
| `GITHUB_JOB_ID`             | Unique job identifier         |
| `GITHUB_PLATFORM_API_TOKEN` | Platform API authentication   |
| `GITHUB_INFERENCE_TOKEN`    | Token for LLM inference calls |
| `GITHUB_GIT_TOKEN`          | Token for git operations      |

---

## Key Repositories Summary

### Octokit (GitHub API SDKs)

| Repository                                                                                          | Type              | Language    | Stars | Package                      |
| --------------------------------------------------------------------------------------------------- | ----------------- | ----------- | ----- | ---------------------------- |
| [octokit/octokit.js](https://github.com/octokit/octokit.js)                                         | Hand-maintained   | TypeScript  | 7.7k  | `octokit` (npm)              |
| [octokit/octokit.rb](https://github.com/octokit/octokit.rb)                                         | Hand-maintained   | Ruby        | 3.9k  | `octokit` (gem)              |
| [octokit/octokit.net](https://github.com/octokit/octokit.net)                                       | Hand-maintained   | C#          | 2.8k  | `Octokit` (NuGet)            |
| [octokit/dotnet-sdk](https://github.com/octokit/dotnet-sdk)                                         | Generated (Kiota) | C#          | 64    | `GitHub.Octokit.SDK` (NuGet) |
| [octokit/go-sdk](https://github.com/octokit/go-sdk)                                                 | Generated (Kiota) | Go          | 86    | `github.com/octokit/go-sdk`  |
| [octokit/source-generator](https://github.com/octokit/source-generator)                             | Tooling           | Shell/Kiota | —     | —                            |
| [github/rest-api-description](https://github.com/github/rest-api-description)                       | Specification     | OpenAPI     | —     | —                            |
| [integrations/terraform-provider-github](https://github.com/integrations/terraform-provider-github) | Terraform         | Go          | —     | Terraform Registry           |

### Copilot & AI SDKs

| Repository                                                                | Purpose                        | Stars | Package                                                             |
| ------------------------------------------------------------------------- | ------------------------------ | ----- | ------------------------------------------------------------------- |
| [github/copilot-sdk](https://github.com/github/copilot-sdk)               | Copilot Agent SDK (multi-lang) | 8k    | `@github/copilot-sdk` / `github-copilot-sdk` / `GitHub.Copilot.SDK` |
| [github/copilot-sdk-java](https://github.com/github/copilot-sdk-java)     | Copilot Agent SDK (Java)       | 27    | `com.github:copilot-sdk-java`                                       |
| [github/copilot-engine-sdk](https://github.com/github/copilot-engine-sdk) | Copilot Engine platform SDK    | 4     | `@github/copilot-engine-sdk`                                        |
| [github/models-ai-sdk](https://github.com/github/models-ai-sdk)           | GitHub Models AI SDK           | 20    | `@github/models`                                                    |

---

## Choosing the Right SDK

```
What do you need?
       │
       ├─► Interact with GitHub API (repos, issues, PRs, users)?
       │       │
       │       ├─► JavaScript/TypeScript → octokit.js
       │       ├─► Ruby → octokit.rb
       │       ├─► C# (.NET, mature) → octokit.net
       │       ├─► C# (.NET, generated, bleeding-edge) → dotnet-sdk
       │       ├─► Go → go-sdk
       │       └─► Terraform → terraform-provider-github
       │
       ├─► Embed Copilot agentic AI in your application?
       │       └─► github/copilot-sdk (TS, Python, Go, .NET, Java)
       │
       ├─► Build an engine on the Copilot agent platform?
       │       └─► github/copilot-engine-sdk
       │
       └─► Use GitHub Models for LLM inference?
               └─► github/models-ai-sdk
```

---

## Confidence Assessment

| Area                               | Confidence      | Notes                                                    |
| ---------------------------------- | --------------- | -------------------------------------------------------- |
| `github/github-sdk` does not exist | **High**        | Confirmed via 404 response                               |
| Octokit ecosystem structure        | **High**        | Verified via org page, individual repos, and GitHub blog |
| Generated SDK approach (Kiota)     | **High**        | Confirmed via GitHub blog post and source-generator repo |
| Copilot SDK architecture & API     | **High**        | Verified via README and docs in the repo                 |
| Copilot SDK production readiness   | **High**        | Explicitly marked "Technical Preview" in README          |
| Copilot Engine SDK details         | **Medium-High** | Based on README; repo is newer and less documented       |
| Generated SDK stability            | **High**        | Both explicitly marked "alpha" / "not yet stable"        |
| Star counts and activity           | **Medium**      | Approximate, based on GitHub pages as of March 2026      |

**Assumptions made:**

- The user's query `github/github-sdk` refers to GitHub's SDK ecosystem broadly, not a specific repo (since that repo does not exist).
- All information is sourced from public GitHub repositories and the GitHub blog.

---

## Footnotes

[^1]: [GitHub Blog: Our move to generated SDKs](https://github.blog/news-insights/product-news/our-move-to-generated-sdks/) — Published January 3, 2024 by Nick Floyd

[^2]: [`github/copilot-sdk` README](https://github.com/github/copilot-sdk) — Main repository README with architecture, FAQ, and quick links

[^3]: [`github/models-ai-sdk` README](https://github.com/github/models-ai-sdk) — GitHub Models provider for the Vercel AI SDK

[^4]: [`github/copilot-engine-sdk` README](https://github.com/github/copilot-engine-sdk) — SDK for building Copilot agent platform engines

[^5]: [Octokit organization page](https://github.com/octokit) — Lists official SDKs and their community guidelines

[^6]: [`octokit/octokit.js` README](https://github.com/octokit/octokit.js) — Full documentation of the JavaScript/TypeScript SDK

[^7]: [`octokit/octokit.rb` README](https://github.com/octokit/octokit.rb) — Ruby toolkit documentation with usage examples

[^8]: [`octokit/octokit.net` README](https://github.com/octokit/octokit.net) — Hand-maintained .NET client library

[^9]: [`octokit/octokit.objc`](https://github.com/octokit/octokit.objc) — Last updated December 2018

[^10]: [`octokit/dotnet-sdk` README](https://github.com/octokit/dotnet-sdk) — Generated .NET SDK with Kiota, NuGet package `GitHub.Octokit.SDK`

[^11]: [`octokit/go-sdk` README](https://github.com/octokit/go-sdk) — Generated Go SDK with authentication examples

[^12]: [`github/rest-api-description` README](https://github.com/github/rest-api-description) — OpenAPI specification repository, stable since v1.1.4

[^13]: [`octokit/source-generator` README](https://github.com/octokit/source-generator) — Generation pipeline using Kiota with design docs

[^14]: [`github/copilot-sdk-java` README](https://github.com/github/copilot-sdk-java) — Java SDK with agentic upstream merge workflow

[^15]: [`github/copilot-sdk` Node.js README](https://github.com/github/copilot-sdk/tree/main/nodejs) — Full TypeScript API reference with event types and session management
