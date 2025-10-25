# Spamma

**Spamma** is an open-source, self-hostable alternative to Mailtrap. It functions as an SMTP server that captures incoming emails and displays them through a web interface. Built with .NET 9 and Blazor WebAssembly, Spamma can receive emails directly via SMTP or through MX records, and provides a comprehensive interface for monitoring and managing captured emails. Developed following Clean Architecture with CQRS and event sourcing patterns.

## ✨ Key Features

- **📨 SMTP Email Capture** - Receive emails via SMTP protocol with full message capture including headers, body, and attachments
- **🌐 Direct & MX Record Support** - Connect applications directly via SMTP or configure MX records to capture domain emails
- **🔍 Web Interface** - Beautiful Blazor WebAssembly UI for browsing, searching, and inspecting captured emails
- **👥 Multi-User Support** - Built-in user authentication and authorization with role-based access control
- **🏢 Multi-Domain Management** - Manage and organize multiple email domains within a single Spamma instance
- **📧 Full Email Inspection** - View complete email details including headers, body (HTML/plain text), attachments, and metadata
- **🗂️ Email Organization** *(WIP)* - Organize emails by domain, search by sender/recipient, subject, and content
- **📊 Event Sourcing** - Complete audit trail of all email operations with full event history
- **� Self-Hosted & Private** - Run on your own infrastructure with complete data control and privacy
- **�📡 Integration Ready** *(WIP)* - RESTful API for programmatic access to captured emails
- **🐳 Docker Ready** - Pre-configured Docker Compose setup for rapid deployment
- **⚡ Scalable Architecture** - Modular design using CQRS, event sourcing, and CAP for distributed systems

## Use Cases

### Development & Testing

- Replace Mailtrap/MailHog for local development environments
- Test email functionality without sending to real email providers
- Debug email formatting and content issues during development
- Run integration tests that verify email sending logic

### QA & Quality Assurance

- Validate email workflows in staging environments
- Test multi-recipient scenarios and bulk email operations
- Verify email templates render correctly across different email clients
- Capture and inspect emails generated during test automation

### Debugging & Troubleshooting

- Inspect email headers for delivery issues
- View raw email content to diagnose formatting problems
- Monitor email flow in production-like environments before deployment

### Self-Hosted Email Inspection

- Keep all email data private on your own infrastructure
- Avoid third-party service dependencies and costs
- Comply with data residency and privacy requirements
- Full control over data retention and compliance policies

### Integration Testing

- Mock email delivery in CI/CD pipelines without external services
- Verify application email functionality automatically
- Test email-triggered workflows and automation
- Eliminate flaky tests caused by external email service dependencies

## Architecture

### Modular Monolith Structure

The application is organized as a **modular monolith** with self-contained domain modules under `src/modules/`:

- **`Spamma.Modules.UserManagement`** - Authentication, user identity, and session management
- **`Spamma.Modules.DomainManagement`** - Domain/hostname management and verification
- **`Spamma.Modules.EmailInbox`** - Email processing, inbox features, and message handling
- **`Spamma.Modules.Common`** - Shared contracts, integration events, and cross-module utilities

Each module follows the pattern: `Module/Module.Client` where `.Client` contains contracts and DTOs for cross-module communication.

### Technology Stack

**Backend:**

- .NET 9
- Marten (PostgreSQL event store for event sourcing)
- MediatR (CQRS pattern implementation)
- FluentValidation (command/query validation)
- CAP (distributed transaction management with Redis)

**Frontend:**

- Blazor WebAssembly
- Tailwind CSS v4 (via webpack)
- TypeScript

**Infrastructure:**

- Docker & Docker Compose
- PostgreSQL (event store)
- Redis (CAP broker)
- MailHog (email testing)

## 🚀 Getting Started

### Prerequisites

- .NET 9 SDK
- Node.js 20+
- Docker & Docker Compose
- PostgreSQL 15+ (or use Docker Compose)
- Redis (or use Docker Compose)

### Local Development

**1. Start infrastructure:**

```bash
docker-compose up -d
```

**2. Build and run:**

```bash
dotnet run --project src/Spamma.App/Spamma.App/Spamma.App.csproj
```

**3. Frontend assets (if modified):**

```bash
cd src/Spamma.App/Spamma.App
npm ci
npm run build
```

The application will be available at `https://localhost:7181` (adjust port as needed).

### Configuration

Connection strings and settings are configured in `appsettings.json` and `appsettings.Development.json`. Docker services are accessible at:

- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`
- MailHog: `http://localhost:1025` (SMTP), `http://localhost:8025` (UI)

### SMTP Configuration

Connect your application or mail client to Spamma using the following SMTP settings:

#### Direct SMTP Connection

```text
SMTP Host: localhost (or your Spamma server address)
SMTP Port: 1025 (default SMTP port - adjust based on your deployment)
Authentication: Not required (Spamma accepts all emails)
Encryption: None (for local development)
```

#### Common Framework Examples

**.NET Applications:**

```csharp
var smtpClient = new SmtpClient("localhost")
{
    Port = 1025,
    DeliveryMethod = SmtpDeliveryMethod.Network,
    UseDefaultCredentials = false
};

using var message = new MailMessage("from@example.com", "to@example.com")
{
    Subject = "Test Email",
    Body = "This email will be captured by Spamma"
};

smtpClient.Send(message);
```

**Node.js (Nodemailer):**

```javascript
const nodemailer = require('nodemailer');

const transporter = nodemailer.createTransport({
  host: 'localhost',
  port: 1025,
  secure: false,
  ignoreTLS: true
});

await transporter.sendMail({
  from: 'sender@example.com',
  to: 'recipient@example.com',
  subject: 'Test Email',
  text: 'This email will be captured by Spamma'
});
```

**Python (smtplib):**

```python
import smtplib
from email.mime.text import MIMEText

sender = 'sender@example.com'
recipient = 'recipient@example.com'
subject = 'Test Email'
body = 'This email will be captured by Spamma'

msg = MIMEText(body)
msg['Subject'] = subject
msg['From'] = sender
msg['To'] = recipient

with smtplib.SMTP('localhost', 1025) as server:
    server.sendmail(sender, recipient, msg.as_string())
```

#### MX Record Configuration

To capture emails sent to your domain:

1. **Create a domain in Spamma** via the web interface
2. **Update your DNS MX records** to point to your Spamma server:

   ```text
   your-domain.com  MX  10  mail.your-server.com
   ```

3. **Emails sent to @your-domain.com** will be captured and displayed in Spamma

#### View Captured Emails

- **Web UI:** Navigate to `https://localhost:7181` (or your Spamma instance URL)
- **Browse by domain** in the sidebar
- **Search emails** by sender, recipient, subject, or content
- **View full email details** including headers, body, and attachments

## 📐 Key Architectural Patterns

### CQRS (Command Query Responsibility Segregation)

- **Commands** - State-changing operations (inherit from `BluQube.Commands.CommandHandler<T>`)
- **Queries** - Read-only operations (inherit from `BluQube.Queries.QueryProcessor<TQuery, TResult>`)
- All handlers include automatic validation via `IValidator<T>` injection
- Results wrapped in `CommandResult` / `QueryResult` for consistent error handling

### Event Sourcing

- All state changes are captured as events in Marten
- Events flow through CAP for distributed pub/sub across modules
- Projections generate read models from event streams
- Full audit trail and temporal queries supported

### Integration Events

Cross-module communication via CAP:

```csharp
// Publish
await IIntegrationEventPublisher.PublishAsync(new UserStatusUpdatedEvent(...));

// Subscribe
[CapSubscribe("UserStatusUpdated")]
public async Task OnUserStatusUpdated(UserStatusUpdatedIntegrationEvent @event) { }
```

### Module Structure

Each module contains:

```text
Module/
├── Application/
│   ├── CommandHandlers/      # CQRS command execution
│   ├── QueryProcessors/      # Query handling
│   ├── Authorizers/          # Custom authorization logic
│   └── Validators/           # FluentValidation rules
├── Domain/
│   └── [Entity]Aggregate/    # Domain aggregates with business logic
└── Infrastructure/
    ├── Projections/          # Marten event projections
    └── Repositories/         # Data access implementations
```

### Module Registration

Modules expose static `Module` class with extension methods:

```csharp
// In Program.cs
builder.Services
    .AddUserManagement()
    .AddDomainManagement()
    .AddEmailInbox();
```

## 🧪 Testing

**Test Coverage:** 136 tests (100% pass rate) across 3 modules

- UserManagement: 37 tests
- DomainManagement: 83 tests
- EmailInbox: 16 tests

### Test Structure

Tests use **verification-based patterns** with the Result monad:

```csharp
// Domain test example
var user = new UserBuilder()
    .WithName("John Doe")
    .WithEmail("john@example.com")
    .Build();

var result = user.StartAuthentication(DateTime.UtcNow);
result.ShouldBeOk(authEvent =>
{
    authEvent.AuthenticationAttemptId.Should().NotBe(Guid.Empty);
});
```

**Running Tests:**

```bash
# All tests
dotnet test Spamma.sln

# Specific project
dotnet test tests/Spamma.Modules.UserManagement.Tests/

# With coverage
dotnet test Spamma.sln --collect:"XPlat Code Coverage"
```

## 🔄 CI/CD Pipelines

### Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **ci.yml** | Push to main/develop, PRs | Build, test, publish results |
| **build-docker.yml** | Nightly + version tags (v*) | Build & push Docker image with tests |
| **build-docker-manual.yml** | Manual workflow dispatch | Build & push Docker with custom tag |

### Docker Build Process

Docker builds include integrated testing:

1. Build stage compiles application (Release config)
2. **Test stage** runs all 136 tests inside container
3. Test results extracted and published to GitHub Actions
4. If tests fail, image push is blocked
5. Final stage publishes passing image to GHCR

```bash
# Manual Docker build with test validation
docker build --target test -f src/Spamma.App/Spamma.App/Dockerfile .
```

## 📦 Building Docker Images

### Automated (via GitHub Actions)

**Nightly build:**

- Triggers automatically at 2 AM UTC daily
- Tags: `nightly-YYYYMMDD-HHMMSS`

**Release build:**

- Triggers on version tags (v1.0.0, v2.0.1, etc.)
- Tags: semver version (1.0.0), latest

**Manual build:**

- Workflow dispatch with custom tag
- Tags: custom-tag, custom-tag-YYYYMMDD-HHMMSS, commit-SHA

### Local Build

```bash
docker build -f src/Spamma.App/Spamma.App/Dockerfile \
  --build-arg BUILD_VERSION=1.0.0 \
  --build-arg BUILD_DATE=$(date -u +'%Y-%m-%dT%H:%M:%SZ') \
  --build-arg VCS_REF=$(git rev-parse HEAD) \
  -t spamma:latest .
```

## 🔐 Security & Authorization

- **Magic Link Authentication** - Users log in via magic links sent to their email (no passwords). An email service (like Spamma itself for testing!) is required to send authentication emails.
  - *Future: Passkey support* - WebAuthn/FIDO2 passkeys will be added as an alternative authentication method
- Custom `MustBeAuthenticatedRequirement` for secured endpoints
- Role-based authorization (via module-specific policies)
- Strongly-typed IDs for domain entities (UserId, DomainId, etc.)
- Injected `TimeProvider` for testable time operations

## 📊 Project Statistics

- **Lines of Code:** ~2,500+ (business logic)
- **Test Coverage:** 60-70% estimated
- **Modules:** 3 core + 1 common
- **Test Projects:** 3
- **GitHub Actions Workflows:** 3

## 🛠️ Development Workflow

1. **Create feature branch:** `git checkout -b feature/my-feature`
2. **Implement changes** following module structure
3. **Add/update tests** to maintain coverage
4. **Run locally:** `dotnet run`, `dotnet test`
5. **Push to GitHub** → CI runs automatically
6. **Create PR** → Workflows validate
7. **Merge to main** → Nightly Docker build runs

## 📝 Conventions

- **Error Codes:** Use `CommonErrorCodes` from shared module
- **Entity IDs:** Strongly-typed (e.g., `UserId`, `DomainId`)
- **Validation:** FluentValidation per module, validated in handlers
- **Event Naming:** Past tense, domain-specific (UserStatusUpdatedIntegrationEvent)
- **Configuration:** Environment-specific appsettings files

## � API Documentation

Spamma exposes RESTful APIs for programmatic access to captured emails and domains.

### Authentication

Include JWT token in Authorization header:

```text
Authorization: Bearer <your-jwt-token>
```

### Common Endpoints

**Get all domains:**

```http
GET /api/domains
Authorization: Bearer <token>
```

**Get emails for a domain:**

```http
GET /api/domains/{domainId}/emails?page=1&pageSize=50
Authorization: Bearer <token>
```

**Get email details:**

```http
GET /api/emails/{emailId}
Authorization: Bearer <token>
```

**Search emails:**

```http
GET /api/emails/search?sender=user@example.com&subject=test
Authorization: Bearer <token>
```

**Delete email:**

```http
DELETE /api/emails/{emailId}
Authorization: Bearer <token>
```

For complete OpenAPI/Swagger documentation, visit `/swagger` on your Spamma instance after starting it.

## 🐳 Deployment

### Docker Compose (Recommended for Development)

```bash
docker-compose up -d
```

This starts PostgreSQL, Redis, and Spamma in containers. Access at `https://localhost:7181`.

### Production Deployment

For production deployments:

1. **Use environment-specific configs:**
   - `appsettings.Production.json` for production settings
   - Set connection strings via environment variables

2. **Configure database:**
   - Use managed PostgreSQL (AWS RDS, Azure Database, etc.)
   - Ensure proper backups and failover

3. **Set up Redis:**
   - Use managed Redis service or Redis cluster
   - Configure appropriate eviction policies

4. **Enable HTTPS:**
   - Use valid SSL certificates (LetsEncrypt, commercial CA)
   - Set `ForwardedProtoHeader` for reverse proxies

5. **Secure SMTP:**
   - Consider running SMTP on restricted network or VPN
   - Monitor for spam/abuse
   - Set rate limits on email ingestion

### Docker Build for Production

```bash
docker build -f src/Spamma.App/Spamma.App/Dockerfile \
  -t ghcr.io/yourusername/spamma:latest \
  --build-arg BUILD_VERSION=1.0.0 .

docker push ghcr.io/yourusername/spamma:latest
```

## 🐛 Troubleshooting

### SMTP Connection Issues

**Problem:** Cannot connect to SMTP server

- **Check firewall:** Ensure port 1025 is open on the Spamma server
- **Verify host:** Use correct server address/IP (not `localhost` if connecting remotely)
- **Check logs:** View Spamma logs for connection errors: `docker-compose logs spamma-app`

**Problem:** Emails not appearing in UI after sending

- **Check email module:** Ensure EmailInbox module is running
- **View logs:** `docker-compose logs | grep -i email`
- **Verify database:** Check PostgreSQL is accessible: `docker-compose logs postgres`

### Database Connection Errors

**Problem:** "Cannot connect to PostgreSQL"

- **Check PostgreSQL:** Ensure container is running: `docker-compose ps`
- **Verify connection string:** Check `appsettings.json` matches Docker Compose config
- **Wait for startup:** PostgreSQL needs ~5-10 seconds to initialize on first start

**Solution:**

```bash
# Restart containers
docker-compose down
docker-compose up -d

# View logs
docker-compose logs -f postgres
```

### Frontend Build Issues

**Problem:** Webpack fails to build assets

- **Clear cache:** `rm -rf node_modules dist && npm ci && npm run build`
- **Check Node version:** Requires Node.js 20+: `node --version`
- **Verify npm:** `npm --version` should be 10+

### Port Already in Use

**Problem:** Port 1025, 5432, 6379, or 7181 is already in use

```bash
# Check what's using the port (Windows)
netstat -ano | findstr :1025

# Kill process
taskkill /PID <process-id> /F

# Or use different ports in docker-compose.yml
```

### High Memory Usage

**Problem:** Application consuming excessive memory

- **Check database:** Large event store can consume memory
- **Review projections:** Ensure projections aren't loading entire event history
- **Increase container limits:** Update docker-compose.yml memory constraints

## ❓ FAQ

**Q: Can I use Spamma in production?**

A: Yes! Spamma is production-ready. It uses event sourcing, CQRS, and proper database configuration. Start with staging/UAT environments first and monitor performance.

**Q: What happens to emails if I restart Spamma?**

A: Emails are persisted in PostgreSQL event store. They will still be visible after restart. Only events in-memory (not yet persisted) may be lost.

**Q: How many emails can Spamma handle?**

A: Performance depends on your infrastructure. PostgreSQL can handle millions of events. For high-volume scenarios, consider database tuning, proper indexing, and potentially event archival for old emails.

**Q: Can multiple users access the same Spamma instance?**

A: Yes! Spamma supports multi-user authentication and role-based access control. Each user can be granted different permissions per domain.

**Q: How do I back up captured emails?**

A: Emails are stored in PostgreSQL. Use standard PostgreSQL backup tools:

```bash
docker-compose exec postgres pg_dump -U postgres spamma > backup.sql
```

**Q: Can I delete old emails to free space?**

A: Yes, use the API or UI to delete individual emails. For batch deletion:

```bash
DELETE FROM [Events table] WHERE EmailCreatedAt < '2024-01-01';
```

**Q: Is there an email size limit?**

A: Default SMTP allows messages up to 25 MB. Adjust in configuration if needed.

**Q: Can I integrate Spamma with my CI/CD pipeline?**

A: Yes! The REST API allows you to programmatically check for test emails, delete them, and verify email functionality in automated tests.

**Q: Does Spamma support TLS/SSL on SMTP?**

A: Currently, Spamma accepts unencrypted SMTP. For secure scenarios, run it behind a reverse proxy or use a firewall to restrict access.

**Q: How do I contribute to Spamma?**

A: See the Contributing Guidelines section above. Fork the repo, create a feature branch, add tests, and submit a PR.

## 🤝 Contributing

Spamma welcomes contributions! Here's how to get involved:

### Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork:** `git clone https://github.com/yourusername/spamma.git`
3. **Create a feature branch:** `git checkout -b feature/my-feature`
4. **Set up development environment:** Follow the Local Development section

### Development Guidelines

- **Follow existing patterns:** Review module structure before implementing
- **Write tests:** Maintain >60% code coverage
- **Use verification-based testing:** Follow patterns in `Spamma.Tests.Common`
- **Follow conventions:** See Conventions section for naming and patterns
- **Document changes:** Update README if adding features

### Test Requirements

All changes must include tests. Run tests locally before submitting:

```bash
# Run all tests
dotnet test Spamma.sln

# Run specific module tests
dotnet test tests/Spamma.Modules.UserManagement.Tests/

# Generate coverage report
dotnet test Spamma.sln --collect:"XPlat Code Coverage"
```

### Submitting Changes

1. **Commit with clear messages:** `git commit -m "feat: add email filtering by sender"`
2. **Push to your fork:** `git push origin feature/my-feature`
3. **Create a Pull Request** on GitHub with:
   - Clear description of changes
   - Reference to related issues
   - Screenshots if UI changes
   - Test results showing all tests passing

### Code Review Process

- Maintainers will review your PR
- Address feedback by pushing additional commits
- Once approved, PR will be merged to main
- Your changes will be included in the next release

### Questions?

- Open a GitHub Discussion for questions
- Open an Issue for bug reports
- Check existing Issues before creating duplicates

## �🚧 Planned Features

- [ ] Integration event tests (8+ tests)
- [ ] Projection tests (10+ tests)
- [ ] Deployment secrets configuration
- [ ] Docker deployment structure (staging/production)

## 📚 Resources

- **Architecture:** See `src/modules/` structure and individual module README files
- **Testing:** See `tests/` directory and `Spamma.Tests.Common` helpers
- **CI/CD:** See `.github/workflows/` for workflow definitions

## 📞 Support

For issues or questions about the architecture, testing patterns, or CI/CD setup, refer to the inline code comments and copilot-instructions.md in the repo.

## 📄 License

See LICENSE file for details.

