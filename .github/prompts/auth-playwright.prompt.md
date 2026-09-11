---
agent: playwright
---

Open 'http://localhost:5005/auth/Account/Login?ReturnUrl=http%3A%2F%2Flocalhost%3A5005%2Fauth%2FAccount%2FManage' with playwright mcp and login with these credentials:

- username: 'john.doe@test.com'
- password: 'Secret12#

Use playwright mcp to take screenshots to see how it looks like if necessary.
After successfull login, the rest of the pages are available at 'http://localhost:5005/auth/Account/Manage'.
Start project with 'dotnet run --project src/services/Auth/', restart using the same command.
