-- Create idempotency_keys table to track API request idempotency
CREATE TABLE idempotency_keys (
    idempotency_key VARCHAR(255) NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    operation_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    last_accessed_at TIMESTAMPTZ NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'utc'),
    expires_at TIMESTAMPTZ NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'utc' + INTERVAL '24 hours'),
    status VARCHAR(50) NOT NULL DEFAULT 'in-progress',
    response_status_code INT,
    response_headers JSONB,
    response_body BYTEA,
    PRIMARY KEY (idempotency_key, user_id, operation_name)
);

CREATE INDEX idx_idempotency_keys_expires_at ON idempotency_keys(expires_at);
