select *
From mt_streams
         id not in (select stream_id from mt_streams)