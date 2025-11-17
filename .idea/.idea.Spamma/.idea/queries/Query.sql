-- Delete malformed passkey lookup records
DELETE FROM mt_doc_passkeylookup;

-- Delete related events (if they're also corrupted)
DELETE FROM mt_events WHERE stream_id IN (
    SELECT id FROM mt_streams WHERE type = 'Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey'
);
DELETE FROM mt_streams WHERE type = 'Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey';