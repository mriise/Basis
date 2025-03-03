# DID Authentication

Serves as the home for authentication based on Decentralized Identifiers
(DIDs).

## How to use

```csharp
using Basis.Contrib.Auth.DecentralizedIds;

// First, we instantiate the authentication
var cfg = new Config();
var didAuth = new DidAuthentication(cfg);
```
