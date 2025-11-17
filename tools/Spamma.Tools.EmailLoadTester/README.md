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

Each batch sets the header X-Spamma-Comp: {guid} on every message in that batch.

