EmailLoadTester

Simple .NET 9 console app to send bulk emails to a local SMTP server for Spamma testing.

Usage (Windows cmd.exe):

    cd tools\EmailLoadTester
    dotnet run -- --host localhost --port 20 --from tester@localhost --to test@spamma.io --batch 50 --batches 2

Options:
  --host     SMTP host (default: localhost)
  --port     SMTP port (default: 20)
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

