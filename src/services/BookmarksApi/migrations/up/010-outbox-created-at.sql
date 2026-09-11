alter table outbox add column if not exists created_at timestamp with time zone not null default now();
