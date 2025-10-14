# AI Chat Web App Implementation Plan

## Context & Objectives

- Build a .NET Aspire-orchestrated AI Chat Web App sample that can ingest user documents, convert them to Markdown through the MarkItDown server, and surface the Markdown inside the chat UI.
- Reuse official .NET AI templates and align with GitHub Models or Azure AI Foundry guidance while switching to the `gpt-5-mini` chat model.
- Integrate the existing MarkItDownServer container as an Aspire-managed resource and expose it to the chat application through the MCP tooling model.
- Add a document upload capability, connect it to the MarkItDown conversion workflow, and display a live Markdown preview in the front-end.

## Assumptions & Prerequisites

- .NET 9 SDK, latest .NET Aspire workload, and the `Microsoft.Extensions.AI.Templates` package are available on the developer machine.
- Docker Desktop (or compatible runtime) is installed and running for Aspire container orchestration.
- Access to GitHub Models (personal access token with Models scope) or Azure AI Foundry credentials is available.
- The MarkItDownServer Docker image can be built locally from this repository or published to a registry that Aspire can pull from.
- MCP client/server contract is based on current GitHub Models MCP documentation (validate before implementation).

## Plan Overview (Progressively Refined)

### Stage 1 – High-Level Milestones

1. Prepare the local environment and template prerequisites.
2. Scaffold the AI Chat Web App with Aspire orchestration.
3. Register the MarkItDown server container inside Aspire.
4. Extend the chat service to ingest and convert uploaded documents to Markdown via MCP.
5. Update the AI chat flow to use `gpt-5-mini` through GitHub Models or Azure AI Foundry.
6. Enhance the front-end UI with upload, conversion status, and Markdown preview.
7. Establish testing, documentation, and hand-off artifacts.

### Stage 2 – Milestones Expanded Into Workstreams

- **Environment & Tooling**
  - Validate .NET SDK, Aspire workload, and template installation.
  - Confirm Docker availability and MarkItDownServer image build.
- **Solution Scaffolding**
  - Use `dotnet new install Microsoft.Extensions.AI.Templates` and `dotnet new ai-chat-webapp --include-aspire`.
  - Configure solution structure (AppHost, Service defaults, Web, Worker if present).
- **Service Orchestration**
  - Decide on MarkItDown container strategy (local build vs registry + Aspire container resource).
  - Add Aspire resource declarations and wiring for chat app dependencies.
- **MCP Integration**
  - Map MarkItDown API into MCP tool definition.
  - Register MCP client inside chat service and expose conversions via the AI pipeline.
- **AI Model Configuration**
  - Swap default models to `gpt-5-mini` and ensure credential injection via user secrets / environment variables.
- **Document Upload & Conversion Flow**
  - Implement server-side upload handling, storage, queueing conversion job, and retrieving Markdown.
  - Integrate Markdown response into chat messages, including citation metadata if available.
- **Frontend Enhancements**
  - Add upload controls, status indicators, and Markdown preview rendering with sanitation.
- **Quality & Operations**
  - Add unit/integration tests, Aspire dashboard validation, docs updates, and future deployment guidance.

### Stage 3 – Detailed Task Breakdown

#### 1. Environment & Template Preparation

- Verify `.NET 9.0` SDK installation (`dotnet --info`).
- Install (or update) `.NET Aspire` workloads (`dotnet workload install aspire`).
- Install the AI Chat Web App templates (`dotnet new install Microsoft.Extensions.AI.Templates`).
- Confirm Docker Desktop is running; test `docker ps`.
- Build MarkItDownServer image locally (`docker build -t markitdownserver:local .`).

#### 2. Scaffold the Aspire Solution

- Create a new solution folder (e.g., `samples/AiChatWebApp`).
- Run `dotnet new ai-chat-webapp --include-aspire --output AiChatWebApp`. Capture generated structure:
  - `AiChatWebApp.sln`
  - `AiChatWebApp.AppHost`
  - `AiChatWebApp.ServiceDefaults`
  - `AiChatWebApp`
  - optional worker components.
- Add solution to repository under an appropriate samples path.
- Open solution in IDE and run once to validate baseline functionality.

#### 3. Aspire Orchestration Adjustments

- In `AppHost` project:
  - Reference MarkItDownServer container as an Aspire resource using either:
    - **Option A:** Build local Dockerfile via Aspire `builder.AddDockerfile` + `builder.AddProject<AiChatWebAppAppHost>()` (evaluate viability).
    - **Option B:** Push/local tag `markitdownserver:local` and register via `builder.AddContainer("markitdownserver", "markitdownserver:local")`.
    - Document pros/cons; select based on developer experience & CI needs.
  - Configure environment variables (`PORT`, rate limiting toggles) for MarkItDown container.
  - Ensure network wiring: expose HTTP endpoint to chat web project (e.g., `withReference`).
- Confirm Aspire dashboard shows both the chat app and MarkItDown service running.

#### 4. MCP Tooling Integration

- Review current MCP spec used by GitHub Models.
- Define MCP tool manifest describing MarkItDown conversion capability (input: file reference; output: Markdown string or document ID).
- Within chat backend (likely `Program.cs` or service class):
  - Register MCP client using `IServiceCollection` extension.
  - Implement conversion adapter that wraps MarkItDown REST endpoint with MCP tool semantics.
  - Add error handling for unsupported formats and large files (>50 MB).
- Update chat pipeline to invoke MCP tool when user requests document conversion or when upload triggers processing.

#### 5. Document Upload Workflow

- Extend web project (Blazor components) with file upload UI (drag-and-drop + browse).
- On upload:
  - Store file temporarily (e.g., in `wwwroot/uploads` or ephemeral storage).
  - Call MCP tool to initiate conversion; pass file stream or storage reference.
  - Await conversion result; persist Markdown in in-memory cache or vector store ingest pipeline.
- Update ingestion pipeline to push Markdown into existing `JsonVectorStore` (or selected vector store) for retrieval-augmented generation.
- Ensure cleanup of temporary files.

#### 6. AI Chat Flow Update

- Swap default chat client registration to use `gpt-5-mini` with GitHub Models (default) or Azure AI Foundry fallback.
  - Configure secrets: `ConnectionStrings:openai` (Aspire) or provider-specific settings.
  - Add configuration toggles to support both providers via `IOptions`.
- Update embedding generator if required (choose a compatible embeddings model, e.g., `text-embedding-3-large`).
- Validate conversation flow end-to-end with Markdown data available in vector store.

#### 7. Front-End Markdown Experience

- After successful conversion, present Markdown preview beside chat timeline.
  - Use a Blazor Markdown renderer (e.g., `Markdig` via `Microsoft.AspNetCore.Components.Markdown`) or custom sanitiser.
  - Provide toggle between raw Markdown and rendered view.
  - Link preview entries to chat message citations where appropriate.
- Display conversion status (uploading, converting, ready) with user feedback and retry options on failure.

#### 8. Testing & Validation

- Unit tests: MCP adapter, document processing pipeline, configuration loading.
- Integration tests: run Aspire environment and simulate uploads using Playwright or WebDriver.
- Manual QA checklist:
  - Upload supported file types, validate Markdown output.
  - Test large file rejection, unsupported format messaging, network failure fallback.
  - Verify chat responses reference converted Markdown segments.
- Aspire dashboard and logs: confirm health of both services.

#### 9. Documentation & Operational Readiness

- Update repository README or create `samples/AiChatWebApp/README.md` with setup instructions.
- Document configuration for GitHub Models vs Azure AI Foundry (tokens, endpoints).
- Provide steps for publishing MarkItDownServer image to registry if required.
- Capture known limitations (file size, concurrency, MCP support matrix).
- Outline future enhancements (cloud storage integration, background processing, persistent vector store).

## Deliverables

- New sample solution under repository `samples` (or designated path) with Aspire-enabled AI Chat Web App.
- Aspire configuration referencing the MarkItDown container, running locally via `dotnet run` from AppHost.
- MCP integration code enabling Markdown conversions through MarkItDownServer.
- Updated UI supporting document upload, conversion progress, and Markdown preview.
- Supporting documentation (README updates, architecture diagram if helpful, configuration notes).

## Open Questions & Research Items

- Confirm MCP tooling support for REST-based conversion workflows (may require custom adapter layer).
- Determine best practice for large file streaming within Aspire environment (consider Azure Storage integration for future scalability).
- Decide whether converted Markdown is stored persistently or transiently for each chat session.
- Clarify licensing and rate limits for `gpt-5-mini` under GitHub Models vs Azure AI Foundry to guide configuration defaults.

## Next Steps

1. Validate prerequisites and build MarkItDown Docker image locally.
2. Scaffold the AI Chat Web App sample using the AI template with Aspire enabled.
3. Iterate through stages 3–9, verifying each milestone in Aspire before moving forward.
4. Document findings, trade-offs, and any deviations from this plan for future contributors.
