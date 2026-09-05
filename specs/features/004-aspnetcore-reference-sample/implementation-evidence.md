# Feature 004 Implementation Evidence

## Implemented behavior

- The ASP.NET Core sample is an executable `net10.0` Minimal API with authenticated `POST /api/assistant`.
- The endpoint validates message input, forwards request cancellation to `AIAgent`, returns `{ "answer": "..." }`, and returns safe Problem Details for unexpected failures.
- Microsoft Identity Web protects the API, enables downstream token acquisition, and uses its in-memory token cache for the sample.
- The sample-local `MicrosoftIdentityWebRetrievalTokenProvider` obtains `https://graph.microsoft.com/.default` for the current `HttpContext` user. It does not use an application identity for Microsoft Graph retrieval.
- Azure OpenAI uses `DefaultAzureCredential` only for the model service. The chat client is decorated with automatic Microsoft 365 retrieval before `AsAIAgent` creates the agent.
- Tests use `WebApplicationFactory`, deterministic authentication, and a native agent backed by a stub `IChatClient`; no test requires a tenant, Graph, SharePoint, or Azure OpenAI.

## Static validation

The VS Code C# diagnostics check reported no errors for the sample and its test project after the final edits.

## Pending command validation

The following focused tests and Release build were attempted but did not start because the shared terminal remained at `cmdand heredoc>` before executing the command:

```sh
dotnet test --project tests/Microsoft365Retrieval.AspNetCore.Tests/Microsoft365Retrieval.AspNetCore.Tests.csproj --configuration Release
dotnet build samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj --configuration Release
```

Run the canonical solution restore, Release build, and test commands after the terminal session is reset. No live tenant validation was performed; it remains explicitly opt-in.