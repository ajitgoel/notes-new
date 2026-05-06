# rxwa-caas-ui-admin

This application is secured using Azure AD (MSAL) for authentication and authorization.

## Security Architecture

### Authentication (Azure AD / MSAL)

The application uses `@albertsons-authn/abs-node-authn` (a wrapper around MSAL for Node.js) to manage the authentication flow.

  

- **Client ID & Authority**: Configured via environment variables (`MSAL_CLIENT_ID`, `MSAL_AUTHORITY`).

- **Redirect URI**: `/caas/admin/azureAdRedirect` handles the callback from Azure AD.

- **Login Flow**: When an unauthenticated user accesses the app, they are redirected to Azure AD for login. Upon successful login, the `msalExchangeCodeForToken` method handles the token exchange and sets the authentication cookie.

  

### Cookie Handling

The application uses a secure cookie to maintain the session state.

  

- **Cookie Name**: `ent-abs-auth` (defined as `MSAL_COOKIE_NAME`).

- **Structure**: The cookie stores a JSON string containing the MSAL authentication results, including the `accessToken`.

- **Decoding**: The server-side middleware extracts this cookie and decodes the JWT `accessToken` using the `jsonwebtoken` library.

- **Identity Extraction**: The `email` or `upn` (User Principal Name) is extracted from the decoded JWT payload to identify the user.

  

### Authorization

Authorization is enforced at the middleware level in `src/server/middleware/nextAppWithAuth.ts`.

  

- **Allowed Emails**: A list of authorized users is maintained via the `ALLOWED_EMAILS` environment variable (comma-separated).

- **Validation**: For every request, the middleware:

1. Checks if the `ent-abs-auth` cookie is present and valid.

2. Extracts the user's email from the token.

3. Validates the email against the `ALLOWED_EMAILS` list.

- **Access Control**: If the user is not in the allowed list, they receive a `403 Forbidden` response. If they are not authenticated, they are redirected to the login flow.

  

### Authentication & Authorization Flow

  

```mermaid

sequenceDiagram

participant User

participant UI as UI Server (Fastify)

participant AAD as Azure AD

participant GQL as GraphQL Service (Java)

  

User->>UI: Access /caas/admin

UI->>UI: Check ent-abs-auth cookie

alt No Cookie

UI->>User: Redirect to Azure AD Login

User->>AAD: Login

AAD->>User: Authorization Code

User->>UI: Callback with Code (/azureAdRedirect)

UI->>AAD: Exchange Code for Token

AAD->>UI: Access Token

UI->>User: Set ent-abs-auth Cookie (JSON with Access Token)

UI->>User: Redirect to /caas/admin

else Cookie Present

UI->>UI: Decode JWT from Cookie

UI->>UI: Validate Email against ALLOWED_EMAILS

alt Email Not Allowed

UI->>User: 403 Forbidden

else Email Allowed

UI->>User: Serve Next.js App

end

end

  

User->>GQL: GraphQL Request (via UI Proxy/Browser)

Note over GQL: Validates Bearer Token in Authorization Header

GQL->>User: Protected Data

```

  

## Deployment environments Release Managers

> - health-release-managers