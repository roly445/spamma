EmailLoadTester

Simple .NET 10 console app to send bulk emails to a local SMTP server for Spamma testing.

Usage (Windows cmd.exe):

    cd tools\EmailLoadTester
    dotnet run -- --host localhost --port 25 --from tester@localhost --to test@spamma.io --batch 50 --batches 2

Options:
  --host     SMTP host (default: localhost)
  --port     SMTP port (default: 25)
  --tls      TLS mode: none, starttls, starttls-when-available, auto, ssl (default: none)
  --allow-invalid-cert Ignore TLS certificate validation errors for testing only (true/false)
  --timeout  SMTP operation timeout in milliseconds (default: 10000)
  --from     From address
  --to       To address
  --batch    Number of emails per batch (1-100)
  --batches  Number of batches to send
  --subject  Email subject prefix
  --html     HTML body content
  --text     Text body content
  --campaign Campaign value to send as the x-spamma-camp header

When `--campaign` is provided, every message includes the `x-spamma-camp` header with that value.

Catch-all campaign example:

    dotnet run --project .\tools\Spamma.Tools.EmailLoadTester\Spamma.Tools.EmailLoadTester.csproj -- --host 127.0.0.1 --port 2025 --from noreply@github.com --to anything@unregistered-catchall.test --batch 1 --batches 1 --subject "Campaign Catch-all GitHub sender test" --campaign my-campaign

STARTTLS example for port 587:

    dotnet run --project .\tools\Spamma.Tools.EmailLoadTester\Spamma.Tools.EmailLoadTester.csproj -- --host mail.example.com --port 587 --tls starttls --from noreply@github.com --to anything@unregistered-catchall.test --batch 1 --batches 1 --subject "Catch-all over STARTTLS"

Implicit TLS example with certificate validation disabled for testing:

    dotnet run --project .\tools\Spamma.Tools.EmailLoadTester\Spamma.Tools.EmailLoadTester.csproj -- --host 192.168.1.5 --port 587 --tls ssl --allow-invalid-cert true --from noreply@github.com --to anything@unregistered-catchall.test --batch 1 --batches 1 --subject "Catch-all over TLS"
