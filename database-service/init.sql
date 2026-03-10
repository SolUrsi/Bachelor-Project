-- Tabell for alle innkommende training-events
CREATE TABLE IF NOT EXISTS training_events (
    id          SERIAL       PRIMARY KEY,
    event_type  VARCHAR(50),
    session_id  VARCHAR(100),
    scenario_id VARCHAR(100),
    timestamp   TIMESTAMPTZ,
    topic       VARCHAR(100),
    payload     JSONB,
    received_at TIMESTAMPTZ  DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_events_session_id  ON training_events (session_id);
CREATE INDEX IF NOT EXISTS idx_events_event_type  ON training_events (event_type);
CREATE INDEX IF NOT EXISTS idx_events_received_at ON training_events (received_at);

-- Enkel view for session-sammendrag
-- payload (column) stores the full JSON blob; payload->'payload' reaches the nested payload object.
CREATE OR REPLACE VIEW session_summary AS
SELECT
    session_id,
    scenario_id,
    MIN(received_at) AS started_at,
    MAX(received_at) AS last_event_at,
    COUNT(*)         AS event_count,
    MAX(CASE WHEN event_type = 'SESSION_COMPLETED'
        THEN (payload->'payload'->>'finalScore')::int   END) AS final_score,
    MAX(CASE WHEN event_type = 'SESSION_COMPLETED'
        THEN (payload->'payload'->>'duration')::float   END) AS duration_seconds
FROM training_events
GROUP BY session_id, scenario_id;
